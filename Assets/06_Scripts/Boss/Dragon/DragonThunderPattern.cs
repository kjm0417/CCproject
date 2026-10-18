using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2번 천둥번개
/// 화면 어두워짐 + 랜덤 grid 묶음 ( strikeSize 칸 ) 빨간 테두리 점등
/// -> 좌 / 우 한쪽부터 맑아지며 맑아진 칸부터 번개 ( 해당 칸 zoneDuration 동안 피해 )
/// </summary>
public class DragonThunderPattern : BossPattern
{
    [Header("대상")]
    [SerializeField] private BossArenaGrid grid;
    [SerializeField, Min(1)] private int strikeCount = 7;
    [SerializeField, Tooltip("번개 1개가 덮는 칸 수 ( 2 x 2 = 4칸 )")]
    private Vector2Int strikeSize = new Vector2Int(2, 2);

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
    [SerializeField, Tooltip("번개 지점 경고 박스 표시 ( 끄면 대기 시간만 유지 )")]
    private bool showTelegraph;
    [SerializeField] private TelegraphStyle telegraphStyle = new TelegraphStyle();
    [SerializeField] private float warningDuration = 1.2f;

    [Header("번개")]
    [SerializeField, Tooltip("한쪽 끝에서 반대쪽 끝까지 맑아지는 시간")]
    private float sweepDuration = 2f;
    [SerializeField] private float zoneDuration = 3f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float tickInterval = 1f;
    [SerializeField, Tooltip("뇌격 지점에 생성할 이펙트 ( LightStrike_SFX )")]
    private GameObject lightningVfx;
    [SerializeField, Tooltip("이펙트 크기 배율")]
    private float lightningVfxScale = 1f;
    [SerializeField, Tooltip("전조 ( 번개 떨어지기 전 ) 프레임 크기 배율")]
    private float lightningWarnScale = 2f;
    [SerializeField, Tooltip("앞에서부터 전조 프레임 수 ( 이후 프레임은 lightningVfxScale )")]
    private int lightningWarnFrames = 6;
    [SerializeField, Tooltip("영역 중심 기준 이펙트 위치 보정")]
    private Vector2 lightningVfxOffset;

    [Header("보스 입 기 모으기 ( 패턴 시작 )")]
    [SerializeField, Tooltip("보스 입 위치")]
    private Transform mouthPoint;
    [SerializeField, Tooltip("패턴 시작하자마자 입에서 나오는 이펙트 ( LightStrike_SFX_4 ) - 중심이 입 번개 시작점")]
    private GameObject chargeVfx;
    [SerializeField, Tooltip("chargeVfx 스프라이트 안 구슬 중심 ( 프리팹 로컬 좌표 )")]
    private Vector2 chargeVfxCenter = new Vector2(0f, 1.12f);
    [SerializeField] private float chargeVfxScale = 1f;
    [SerializeField, Tooltip("chargeVfx 이후 나오는 이펙트 ( LightStrike_SFX_3 )")]
    private GameObject chargeBurstVfx;
    [SerializeField, Tooltip("chargeVfx 시작 후 chargeBurstVfx 까지 대기")]
    private float chargeBurstDelay = 0.5f;
    [SerializeField, Tooltip("chargeBurstVfx 스프라이트 안 중심 ( 프리팹 로컬 좌표 )")]
    private Vector2 chargeBurstVfxCenter = new Vector2(0.03f, 0.18f);
    [SerializeField, Tooltip("chargeVfx 보다 조금 크게")]
    private float chargeBurstVfxScale = 1.2f;

    [Header("보스 입 번개 ( 기 모은 지점 -> 뇌격 지점 )")]
    [SerializeField, Tooltip("입에서 뇌격 지점까지 뻗는 이펙트 ( LightStrike_SFX_2 )")]
    private GameObject mouthBoltVfx;
    [SerializeField, Tooltip("이펙트 스프라이트 안 번개 윗끝 ( 프리팹 로컬 좌표 )")]
    private Vector2 mouthBoltTop = new Vector2(-2.82f, 5.27f);
    [SerializeField, Tooltip("이펙트 스프라이트 안 번개 아래 끝 ( 프리팹 로컬 좌표 )")]
    private Vector2 mouthBoltBottom = new Vector2(0.48f, -5.43f);
    [SerializeField, Tooltip("번개 굵기 배율 ( 1 = 원본 굵기, 길이는 거리에 맞게 자동 )")]
    private float mouthBoltWidth = 1f;

    [Header("이펙트 종료")]
    [SerializeField, Tooltip("이펙트 애니 마지막 프레임 재생 후 사라지기까지 ( 0 = 바로 )")]
    private float vfxHideDelay;

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

        // 0. 입에서 기 모으기 ( 어두워짐과 동시 진행 )
        chargeDone = false;
        if (mouthPoint != null) StartSubRoutine(ChargeRoutine());
        else chargeDone = true;

        // 1. 어두워짐 + 경고
        yield return Darken();

        List<Vector2Int> cells = grid.GetRandomBlocks(strikeCount, strikeSize);
        List<BossTelegraph> telegraphs = new List<BossTelegraph>();
        foreach (Vector2Int cell in cells)
        {
            if (!showTelegraph)
            {
                telegraphs.Add(null);
                continue;
            }

            BossTelegraph telegraph = CreateTelegraph(grid.GetBlockShape(cell, strikeSize), telegraphStyle);
            telegraph.SetVisible(true);
            telegraphs.Add(telegraph);
        }

        yield return new WaitForSeconds(warningDuration);

        // 기 모으기 이펙트가 다 끝난 뒤 번개 시작
        while (!chargeDone) yield return null;

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

                ZoneShape shape = grid.GetBlockShape(cells[i], strikeSize);
                float cellX = shape.Center.x;
                bool passed = fromLeft ? cellX <= edgeX : cellX >= edgeX;
                if (!passed) continue;

                struck[i] = true;
                remain--;
                if (telegraphs[i] != null) telegraphs[i].Release();
                BossDamageZone zone = SpawnDamageZone(shape, null, damage, zoneDuration, tickInterval, lightningVfx);
                if (zone.Vfx != null)
                {
                    zone.Vfx.transform.position = shape.Center + lightningVfxOffset;
                    float length = GetClipLength(zone.Vfx);
                    if (length > 0f) zone.HideVfxAfter(length + vfxHideDelay);
                    StartSubRoutine(LightningVfxRoutine(zone.Vfx, shape.Center));
                }
                else if (mouthPoint != null && mouthBoltVfx != null) StartSubRoutine(MouthBoltRoutine(shape.Center));
                if (lightningVfx == null) StartSubRoutine(StrikeRoutine(shape));
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

    #region 보스 입 기 모으기

    private readonly List<GameObject> chargeVisuals = new List<GameObject>();
    private bool chargeDone;

    /// <summary> 입 번개 시작점 = 기 모으기 구슬 중심 </summary>
    private Vector2 ChargePoint => mouthPoint.position;

    /// <summary> chargeVfx -> chargeBurstDelay 후 chargeBurstVfx, 둘 다 클립 1회 재생 후 반납 </summary>
    private IEnumerator ChargeRoutine()
    {
        float chargeLength = 0f;
        float burstLength = 0f;

        GameObject charge = SpawnChargeVisual(chargeVfx, chargeVfxCenter, chargeVfxScale, out chargeLength);
        float elapsed = 0f;
        while (elapsed < chargeBurstDelay)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        GameObject burst = SpawnChargeVisual(chargeBurstVfx, chargeBurstVfxCenter, chargeBurstVfxScale, out burstLength);
        float end = Mathf.Max(chargeLength, chargeBurstDelay + burstLength);
        while (elapsed < end)
        {
            elapsed += Time.deltaTime;
            if (charge != null && elapsed >= chargeLength) ReleaseChargeVisual(ref charge);
            yield return null;
        }

        ReleaseChargeVisual(ref charge);
        ReleaseChargeVisual(ref burst);
        chargeDone = true;
    }

    /// <param name="center">스프라이트 안 이펙트 중심 - 이 점이 ChargePoint 에 오도록 배치</param>
    /// <param name="length">클립 1회 재생 시간</param>
    private GameObject SpawnChargeVisual(GameObject prefab, Vector2 center, float scale, out float length)
    {
        length = 0f;
        if (prefab == null) return null;

        Vector3 pos = ChargePoint - center * scale;
        GameObject go = ObjectPoolManager.Spawn(prefab, pos, Quaternion.identity);
        go.transform.localScale = Vector3.one * scale;
        chargeVisuals.Add(go);

        length = GetClipLength(go) + vfxHideDelay;
        return go;
    }

    /// <summary> 전조 프레임은 크게 -> 이후 원래 크기, 마지막 프레임 시작 시 입 번개 </summary>
    private IEnumerator LightningVfxRoutine(GameObject vfx, Vector2 target)
    {
        float frameTime = 1f / GetClipFrameRate(vfx);
        float warnEnd = lightningWarnFrames * frameTime;
        float boltTime = Mathf.Max(0f, GetClipLength(vfx) - frameTime);
        bool boltSpawned = mouthPoint == null || mouthBoltVfx == null;

        float elapsed = 0f;
        while (vfx != null && vfx.activeInHierarchy)
        {
            // Animator 는 코루틴 이후 갱신 - 이번 프레임 애니 시간 기준으로 맞춤
            float animTime = elapsed + Time.deltaTime;
            vfx.transform.localScale = Vector3.one * (animTime < warnEnd ? lightningWarnScale : lightningVfxScale);

            if (!boltSpawned && animTime >= boltTime)
            {
                boltSpawned = true;
                StartSubRoutine(MouthBoltRoutine(target));
            }
            if (boltSpawned && animTime >= warnEnd) yield break;

            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    private static float GetClipFrameRate(GameObject go)
    {
        Animator animator = go != null ? go.GetComponent<Animator>() : null;
        if (animator == null || animator.runtimeAnimatorController == null) return 12f;

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.frameRate > 0f) return clip.frameRate;
        }
        return 12f;
    }

    /// <summary> 이펙트 애니 1회 재생 시간 ( 풀 재활성화 시 Animator 는 처음부터 재생 ) </summary>
    private static float GetClipLength(GameObject go)
    {
        Animator animator = go != null ? go.GetComponent<Animator>() : null;
        if (animator == null || animator.runtimeAnimatorController == null) return 0f;

        float length = 0f;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            length = Mathf.Max(length, clip.length);
        }
        return length;
    }

    private void ReleaseChargeVisual(ref GameObject go)
    {
        if (go == null) return;
        chargeVisuals.Remove(go);
        ObjectPoolManager.Despawn(go);
        go = null;
    }

    private void ClearChargeVisuals()
    {
        foreach (GameObject go in chargeVisuals)
        {
            if (go != null) ObjectPoolManager.Despawn(go);
        }
        chargeVisuals.Clear();
        chargeDone = true;
    }

    #endregion

    #region 보스 입 번개

    private readonly List<GameObject> mouthBolts = new List<GameObject>();

    /// <summary>
    /// 이펙트의 윗끝 -> 아래 끝 ( mouthBoltTop -> mouthBoltBottom ) 을 입 -> target 에 맞춤
    /// 부모 : 입 위치, -Y 가 target 방향, Y 스케일 = 거리 / 자식 : 윗끝 -> 아래 끝을 부모 (0,0) -> (0,-1) 로 정규화
    /// </summary>
    private IEnumerator MouthBoltRoutine(Vector2 target)
    {
        Vector2 mouth = ChargePoint;
        Vector2 dir = target - mouth;
        Vector2 local = mouthBoltBottom - mouthBoltTop;
        if (dir.sqrMagnitude < 0.0001f || local.sqrMagnitude < 0.0001f) yield break;

        GameObject pivot = new GameObject("MouthBolt");
        pivot.transform.SetPositionAndRotation(mouth, Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.down, dir)));
        pivot.transform.localScale = new Vector3(local.magnitude * mouthBoltWidth, dir.magnitude, 1f);
        mouthBolts.Add(pivot);

        float k = 1f / local.magnitude;
        Quaternion childRot = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(local, Vector2.down));
        GameObject bolt = ObjectPoolManager.Spawn(mouthBoltVfx, pivot.transform.position, pivot.transform.rotation, pivot.transform);
        bolt.transform.localRotation = childRot;
        bolt.transform.localScale = Vector3.one * k;
        bolt.transform.localPosition = childRot * (-(Vector3)mouthBoltTop * k);

        yield return new WaitForSeconds(GetClipLength(bolt) + vfxHideDelay);
        ReleaseMouthBolt(pivot);
    }

    private void ReleaseMouthBolt(GameObject pivot)
    {
        if (pivot == null) return;
        mouthBolts.Remove(pivot);

        // 이펙트는 풀 반납 후 부모 제거
        for (int i = pivot.transform.childCount - 1; i >= 0; i--)
        {
            ObjectPoolManager.Despawn(pivot.transform.GetChild(i).gameObject);
        }
        Destroy(pivot);
    }

    private void ClearMouthBolts()
    {
        for (int i = mouthBolts.Count - 1; i >= 0; i--)
        {
            ReleaseMouthBolt(mouthBolts[i]);
        }
        mouthBolts.Clear();
    }

    #endregion

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
        ClearMouthBolts();
        ClearChargeVisuals();
    }
}
