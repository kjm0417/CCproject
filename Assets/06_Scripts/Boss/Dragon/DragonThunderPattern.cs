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

    [Header("보스 어두워짐")]
    [SerializeField, Tooltip("같이 어두워질 보스 스프라이트 ( 비우면 보스 하위 SpriteRenderer 전부 )")]
    private List<SpriteRenderer> bossRenderers = new List<SpriteRenderer>();
    [SerializeField, Tooltip("어두울 때 보스 색 ( 원래 색에 곱함 )")]
    private Color bossDarkColor = new Color(0.2f, 0.2f, 0.28f, 1f);

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

    [Header("번개 기본 연출 ( lightningVfx 없을 때 )")]
    [SerializeField, Tooltip("번개 순간 번쩍임 색")]
    private Color strikeFlashColor = new Color(1f, 1f, 0.75f, 0.95f);
    [SerializeField, Tooltip("피해 유지 동안 칸 채움 색")]
    private Color strikeZoneColor = new Color(1f, 0.9f, 0.3f, 0.35f);
    [SerializeField] private float strikeFlashTime = 0.2f;

    private Vector3 overlayOriginPos;
    private readonly List<Color> bossOriginColors = new List<Color>();
    private bool bossDarkened;

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
        float t = 0f;

        // 번개가 다 떨어지고 어둠도 끝까지 걷힐 때까지
        while (remain > 0 || t < 1f)
        {
            elapsed += Time.deltaTime;
            t = sweepDuration > 0f ? Mathf.Clamp01(elapsed / sweepDuration) : 1f;
            float edgeX = Mathf.Lerp(startX, endX, t);
            float revealX = edgeX;

            if (darkOverlay != null)
            {
                darkOverlay.transform.position = overlayOriginPos + Vector3.right * (fromLeft ? overlayWidth : -overlayWidth) * t;
                // 어둠이 걷힌 경계 = 오버레이 뒤쪽 끝
                revealX = fromLeft ? darkOverlay.bounds.min.x : darkOverlay.bounds.max.x;
            }

            RevealBoss(revealX, fromLeft);

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
                if (lightningVfx == null) StartSubRoutine(StrikeRoutine(grid.GetCellShape(cells[i])));
            }

            yield return null;
        }

        HideOverlay();
        RestoreBoss();

        // 3. 마지막 번개 지속 시간 대기
        yield return new WaitForSeconds(zoneDuration);
    }

    private IEnumerator Darken()
    {
        CacheBossColors();

        if (darkOverlay != null)
        {
            darkOverlay.transform.position = overlayOriginPos;
            darkOverlay.gameObject.SetActive(true);
        }

        float elapsed = 0f;
        while (elapsed < darkenDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / darkenDuration);
            if (darkOverlay != null) SetOverlayAlpha(Mathf.Lerp(0f, darkAlpha, t));
            SetBossDarkness(t);
            yield return null;
        }

        if (darkOverlay != null) SetOverlayAlpha(darkAlpha);
        SetBossDarkness(1f);
    }

    #region 번개 기본 연출

    private static Sprite squareSprite;
    private readonly List<GameObject> strikeVisuals = new List<GameObject>();

    /// <summary> 번쩍임 -> 피해 유지 동안 옅은 채움 -> 사라짐 </summary>
    private IEnumerator StrikeRoutine(ZoneShape cell)
    {
        if (squareSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            squareSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        }

        GameObject go = new GameObject("ThunderStrike");
        go.transform.SetPositionAndRotation(cell.Center, Quaternion.Euler(0f, 0f, cell.Angle));
        go.transform.localScale = new Vector3(cell.size.x, cell.size.y, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.sortingLayerName = telegraphStyle.sortingLayerName;
        sr.sortingOrder = telegraphStyle.sortingOrder + 1;
        strikeVisuals.Add(go);

        float elapsed = 0f;
        while (elapsed < zoneDuration)
        {
            elapsed += Time.deltaTime;
            Color color = elapsed < strikeFlashTime
                ? Color.Lerp(strikeFlashColor, strikeZoneColor, elapsed / strikeFlashTime)
                : strikeZoneColor;

            // 마지막 0.3초 페이드 아웃
            float remain = zoneDuration - elapsed;
            if (remain < 0.3f) color.a *= Mathf.Clamp01(remain / 0.3f);

            sr.color = color;
            yield return null;
        }

        strikeVisuals.Remove(go);
        Destroy(go);
    }

    private void ClearStrikeVisuals()
    {
        foreach (GameObject go in strikeVisuals)
        {
            if (go != null) Destroy(go);
        }
        strikeVisuals.Clear();
    }

    #endregion

    #region 보스 어두워짐

    private void CacheBossColors()
    {
        if (bossRenderers.Count == 0 && Boss != null)
        {
            foreach (SpriteRenderer sr in Boss.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr != darkOverlay) bossRenderers.Add(sr);
            }
        }

        // 이미 어두운 상태 ( 이전 패턴 중단 등 ) 면 원래 색 유지
        if (bossDarkened) return;

        bossOriginColors.Clear();
        foreach (SpriteRenderer sr in bossRenderers)
        {
            bossOriginColors.Add(sr != null ? sr.color : Color.white);
        }
        bossDarkened = true;
    }

    /// <param name="darkness">0 : 원래 색 ~ 1 : bossDarkColor</param>
    private void SetBossDarkness(float darkness)
    {
        if (!bossDarkened) return;

        for (int i = 0; i < bossRenderers.Count; i++)
        {
            SetBossDarkness(i, darkness);
        }
    }

    private void SetBossDarkness(int index, float darkness)
    {
        SpriteRenderer sr = bossRenderers[index];
        if (sr == null) return;

        Color origin = bossOriginColors[index];
        sr.color = Color.Lerp(origin, origin * bossDarkColor, darkness);
    }

    /// <summary> 어둠이 걷힌 경계가 보스 스프라이트를 지나가는 만큼 서서히 밝아짐 </summary>
    private void RevealBoss(float revealX, bool fromLeft)
    {
        if (!bossDarkened) return;

        for (int i = 0; i < bossRenderers.Count; i++)
        {
            SpriteRenderer sr = bossRenderers[i];
            if (sr == null) continue;

            Bounds b = sr.bounds;
            float revealed = fromLeft
                ? Mathf.InverseLerp(b.min.x, b.max.x, revealX)
                : Mathf.InverseLerp(b.max.x, b.min.x, revealX);
            SetBossDarkness(i, 1f - revealed);
        }
    }

    private void RestoreBoss()
    {
        if (!bossDarkened) return;

        SetBossDarkness(0f);
        bossDarkened = false;
    }

    #endregion

    #region [이전] 오버레이만 어두워짐 / 번개 다 떨어지면 바로 종료
    // private IEnumerator Darken()
    // {
    //     if (darkOverlay == null) yield break;
    //
    //     darkOverlay.transform.position = overlayOriginPos;
    //     darkOverlay.gameObject.SetActive(true);
    //
    //     float elapsed = 0f;
    //     while (elapsed < darkenDuration)
    //     {
    //         elapsed += Time.deltaTime;
    //         SetOverlayAlpha(Mathf.Lerp(0f, darkAlpha, elapsed / darkenDuration));
    //         yield return null;
    //     }
    //     SetOverlayAlpha(darkAlpha);
    // }
    //
    // while (remain > 0) { ... t / edgeX / overlay 이동 / 번개 ... }
    #endregion

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
        RestoreBoss();
        ClearStrikeVisuals();
    }
}
