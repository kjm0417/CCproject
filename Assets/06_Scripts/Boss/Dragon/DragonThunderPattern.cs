using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2번 천둥번개
/// 화면 어두워짐 + 랜덤 grid 빨간 테두리 점등
/// -> 좌 / 우 한쪽부터 맑아지며 맑아진 칸부터 번개 ( 해당 칸 zoneDuration 동안 피해 )
/// </summary>
public class DragonThunderPattern : BossPattern
{
    [Header("대상")]
    [SerializeField] private BossArenaGrid grid;
    [SerializeField, Min(1)] private int strikeCount = 7;

    [Header("어두워짐 연출 ( 선택 )")]
    [SerializeField, Tooltip("전장을 덮는 검은 스프라이트 - 맑아질 때 좌 / 우로 밀려남")]
    private SpriteRenderer darkOverlay;
    [SerializeField, Range(0f, 1f)] private float darkAlpha = 0.45f;
    [SerializeField] private float darkenDuration = 0.5f;

    [Header("경고")]
    [SerializeField] private TelegraphStyle telegraphStyle = new TelegraphStyle();
    [SerializeField] private float warningDuration = 1.2f;

    [Header("번개")]
    [SerializeField, Tooltip("한쪽 끝에서 반대쪽 끝까지 맑아지는 시간")]
    private float sweepDuration = 2f;
    [SerializeField] private float zoneDuration = 3f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float tickInterval = 1f;
    [SerializeField] private GameObject lightningVfx;

    private Vector3 overlayOriginPos;

    private void Awake()
    {
        if (darkOverlay != null)
        {
            overlayOriginPos = darkOverlay.transform.position;
            SetOverlayAlpha(0f);
            darkOverlay.gameObject.SetActive(false);
        }
    }

    protected override IEnumerator Execute()
    {
        if (grid == null) yield break;

        // 1. 어두워짐 + 경고
        yield return Darken();

        List<Vector2Int> cells = grid.GetRandomCells(strikeCount);
        List<BossTelegraph> telegraphs = new List<BossTelegraph>();
        foreach (Vector2Int cell in cells)
        {
            BossTelegraph telegraph = CreateTelegraph(grid.GetCellShape(cell), telegraphStyle);
            telegraph.SetVisible(true);
            telegraphs.Add(telegraph);
        }

        yield return new WaitForSeconds(warningDuration);

        // 2. 한쪽부터 맑아지며 지나간 칸에 번개
        bool fromLeft = Random.value < 0.5f;
        float startX = fromLeft ? grid.MinWorld.x : grid.MaxWorld.x;
        float endX = fromLeft ? grid.MaxWorld.x : grid.MinWorld.x;
        float overlayWidth = darkOverlay != null ? darkOverlay.bounds.size.x : 0f;

        bool[] struck = new bool[cells.Count];
        int remain = cells.Count;
        float elapsed = 0f;

        while (remain > 0)
        {
            elapsed += Time.deltaTime;
            float t = sweepDuration > 0f ? Mathf.Clamp01(elapsed / sweepDuration) : 1f;
            float edgeX = Mathf.Lerp(startX, endX, t);

            if (darkOverlay != null)
            {
                darkOverlay.transform.position = overlayOriginPos + Vector3.right * (fromLeft ? overlayWidth : -overlayWidth) * t;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                if (struck[i]) continue;

                float cellX = grid.CellToWorld(cells[i]).x;
                bool passed = fromLeft ? cellX <= edgeX : cellX >= edgeX;
                if (!passed) continue;

                struck[i] = true;
                remain--;
                telegraphs[i].Release();
                SpawnDamageZone(grid.GetCellShape(cells[i]), null, damage, zoneDuration, tickInterval, lightningVfx);
            }

            yield return null;
        }

        HideOverlay();

        // 3. 마지막 번개 지속 시간 대기
        yield return new WaitForSeconds(zoneDuration);
    }

    private IEnumerator Darken()
    {
        if (darkOverlay == null) yield break;

        darkOverlay.transform.position = overlayOriginPos;
        darkOverlay.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < darkenDuration)
        {
            elapsed += Time.deltaTime;
            SetOverlayAlpha(Mathf.Lerp(0f, darkAlpha, elapsed / darkenDuration));
            yield return null;
        }
        SetOverlayAlpha(darkAlpha);
    }

    private void HideOverlay()
    {
        if (darkOverlay == null) return;

        darkOverlay.gameObject.SetActive(false);
        darkOverlay.transform.position = overlayOriginPos;
        SetOverlayAlpha(0f);
    }

    private void SetOverlayAlpha(float alpha)
    {
        Color color = darkOverlay.color;
        color.a = alpha;
        darkOverlay.color = color;
    }

    public override void Cleanup()
    {
        base.Cleanup();
        HideOverlay();
    }
}
