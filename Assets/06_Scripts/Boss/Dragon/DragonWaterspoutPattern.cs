using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 3번 용오름
/// 마름모 ( 바깥 -> 안쪽 ) 순서로 점멸, 바깥이 nextStartAfterBlinks 회 점멸하면 다음 마름모 점멸 시작
/// 각 마름모 blinkCount 회 점멸 후 해당 구역에 용오름 ( 피해 )
/// </summary>
public class DragonWaterspoutPattern : BossPattern
{
    [Header("구역")]
    [SerializeField, Tooltip("바깥 -> 안쪽 순서, 마름모는 angle 45")]
    private List<ZoneShape> rings = new List<ZoneShape>();
    [SerializeField, Tooltip("true: 안쪽 마름모 영역은 피해 제외 ( 테두리 띠만 피해 )")]
    private bool ringOnly = true;

    [Header("경고")]
    [SerializeField] private TelegraphStyle telegraphStyle = new TelegraphStyle();
    [SerializeField] private int blinkCount = 3;
    [SerializeField, Tooltip("이전 마름모가 몇 번 점멸한 뒤 다음 마름모 시작")]
    private int nextStartAfterBlinks = 2;
    [SerializeField] private float blinkOnTime = 0.25f;
    [SerializeField] private float blinkOffTime = 0.2f;

    [Header("용오름")]
    [SerializeField] private float eruptDuration = 1.5f;
    [SerializeField] private float damage = 15f;
    [SerializeField] private float tickInterval = 0.5f;
    [SerializeField, Tooltip("마름모 이펙트 ( Animator : 전조 / 공격 / 공격끝 ) - 마름모마다 크기 맞춰 생성")]
    private GameObject waterspoutVfx;

    [Header("용오름 이펙트")]
    [SerializeField, Tooltip("이펙트 스케일 1 일 때 마름모 가로 / 세로 ( 월드 단위 ) - 마름모 크기에 맞춰 스케일 계산")]
    private Vector2 vfxDiamondSize = new Vector2(11.4f, 9.2f);
    [SerializeField, Tooltip("이펙트 있으면 기존 경고 ( 점멸 박스 ) 도 같이 표시")]
    private bool showTelegraphWithVfx;
    [SerializeField] private string vfxTellState = "AttackTell";
    [SerializeField] private string vfxAttackState = "Attack";
    [SerializeField] private string vfxEndState = "AttackEnd";

    [Header("용오름 기본 연출 ( waterspoutVfx 없을 때 )")]
    [SerializeField] private Color eruptFlashColor = new Color(0.85f, 0.95f, 1f, 0.9f);
    [SerializeField] private Color eruptZoneColor = new Color(0.3f, 0.6f, 1f, 0.45f);
    [SerializeField] private float eruptFlashTime = 0.2f;

    [Header("기믹 시도 중 가중치")]
    [SerializeField, Tooltip("비우면 씬의 BossStunGimmick 자동 탐색")]
    private BossStunGimmick gimmick;
    [SerializeField, Min(0f), Tooltip("기믹 시도 중일 때 가중치 배율")]
    private float gimmickWeightMultiplier = 3f;

    private readonly List<GameObject> eruptVisuals = new List<GameObject>();
    private readonly List<GameObject> spawnedVfx = new List<GameObject>();

    public override float Weight
    {
        get
        {
            if (gimmick == null) gimmick = FindAnyObjectByType<BossStunGimmick>(FindObjectsInactive.Include);
            return gimmick != null && gimmick.IsAttempting ? base.Weight * gimmickWeightMultiplier : base.Weight;
        }
    }

    protected override IEnumerator Execute()
    {
        if (rings.Count == 0) yield break;

        float blinkPeriod = blinkOnTime + blinkOffTime;
        float stagger = nextStartAfterBlinks * blinkPeriod;

        for (int i = 0; i < rings.Count; i++)
        {
            ZoneShape hole = ringOnly && i + 1 < rings.Count ? rings[i + 1] : null;
            StartSubRoutine(RunRing(rings[i], hole));

            if (i < rings.Count - 1) yield return new WaitForSeconds(stagger);
        }

        // 마지막 마름모 점멸 + 용오름 종료까지 대기
        // 마지막 마름모 점멸 + 용오름 + 공격끝 애니 종료까지 대기 ( 패턴 종료 시 Cleanup 으로 이펙트 반납 )
        yield return new WaitForSeconds(blinkCount * blinkPeriod + eruptDuration + GetVfxEndLength());
    }

    private IEnumerator RunRing(ZoneShape ring, ZoneShape hole)
    {
        if (waterspoutVfx != null)
        {
            yield return RunRingVfx(ring.Snapshot(), hole);
            yield break;
        }

        BossTelegraph telegraph = CreateTelegraph(ring.Snapshot(), telegraphStyle);
        yield return telegraph.Blink(blinkCount, blinkOnTime, blinkOffTime);
        telegraph.Release();

        SpawnDamageZone(ring, hole, damage, eruptDuration, tickInterval);
        yield return EruptRoutine(ring.Snapshot(), hole?.Snapshot());
    }

    /// <summary> 이펙트 마름모를 해당 마름모 크기로 생성 -> 전조 -> 공격 ( 피해 ) -> 공격끝 -> 반납 </summary>
    private IEnumerator RunRingVfx(ZoneShape ring, ZoneShape hole)
    {
        GameObject vfx = ObjectPoolManager.Spawn(waterspoutVfx, ring.Center, Quaternion.identity);
        vfx.transform.localScale = GetVfxScale(ring);
        spawnedVfx.Add(vfx);
        Animator animator = vfx.GetComponentInChildren<Animator>();

        // 전조
        PlayVfx(animator, vfxTellState);
        float tellTime = blinkCount * (blinkOnTime + blinkOffTime);
        if (showTelegraphWithVfx)
        {
            BossTelegraph telegraph = CreateTelegraph(ring, telegraphStyle);
            yield return telegraph.Blink(blinkCount, blinkOnTime, blinkOffTime);
            telegraph.Release();
        }
        else
        {
            yield return new WaitForSeconds(tellTime);
        }

        // 공격 ( 피해 유지 )
        PlayVfx(animator, vfxAttackState);
        SpawnDamageZone(ring, hole, damage, eruptDuration, tickInterval);
        yield return new WaitForSeconds(eruptDuration);

        // 공격끝
        PlayVfx(animator, vfxEndState);
        yield return new WaitForSeconds(GetVfxEndLength());

        spawnedVfx.Remove(vfx);
        ObjectPoolManager.Despawn(vfx);
    }

    /// <summary> 마름모 ( 회전 사각형 ) 의 화면상 가로 / 세로 에 이펙트 마름모를 맞춤 </summary>
    private Vector3 GetVfxScale(ZoneShape ring)
    {
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;
        foreach (Vector3 corner in ring.GetCorners())
        {
            min = Vector3.Min(min, corner);
            max = Vector3.Max(max, corner);
        }
        Vector3 extent = max - min;
        return new Vector3(extent.x / Mathf.Max(0.01f, vfxDiamondSize.x), extent.y / Mathf.Max(0.01f, vfxDiamondSize.y), 1f);
    }

    private static void PlayVfx(Animator animator, string state)
    {
        if (animator != null && !string.IsNullOrEmpty(state)) animator.Play(state, 0, 0f);
    }

    private float GetVfxEndLength()
    {
        if (waterspoutVfx == null) return 0f;

        Animator animator = waterspoutVfx.GetComponentInChildren<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null) return 0f;

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == vfxEndState) return clip.length;
        }
        return 0f;
    }

    /// <summary> 번쩍임 -> 피해 유지 동안 채움 -> 사라짐 </summary>
    private IEnumerator EruptRoutine(ZoneShape ring, ZoneShape hole)
    {
        BossZoneFill fill = BossZoneFill.Create(ring, hole, eruptFlashColor, telegraphStyle.sortingLayerName, telegraphStyle.sortingOrder - 1);
        eruptVisuals.Add(fill.gameObject);

        float elapsed = 0f;
        while (elapsed < eruptDuration)
        {
            elapsed += Time.deltaTime;
            Color color = elapsed < eruptFlashTime
                ? Color.Lerp(eruptFlashColor, eruptZoneColor, elapsed / eruptFlashTime)
                : eruptZoneColor;

            float remain = eruptDuration - elapsed;
            if (remain < 0.3f) color.a *= Mathf.Clamp01(remain / 0.3f);

            fill.SetColor(color);
            yield return null;
        }

        eruptVisuals.Remove(fill.gameObject);
        Destroy(fill.gameObject);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        foreach (GameObject go in eruptVisuals)
        {
            if (go != null) Destroy(go);
        }
        eruptVisuals.Clear();

        foreach (GameObject vfx in spawnedVfx)
        {
            if (vfx != null) ObjectPoolManager.Despawn(vfx);
        }
        spawnedVfx.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        foreach (ZoneShape ring in rings)
        {
            ring.DrawGizmo(Color.red);
        }

        #region [이전] 이펙트를 피해 영역에 붙여 마름모 각도로 회전 생성
        // SpawnDamageZone(ring, hole, damage, eruptDuration, tickInterval, waterspoutVfx);
        // if (waterspoutVfx == null) yield return EruptRoutine(ring.Snapshot(), hole?.Snapshot());
        #endregion
    }
}
