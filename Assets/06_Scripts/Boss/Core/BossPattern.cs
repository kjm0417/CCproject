using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 패턴 공통 - 보스 오브젝트 ( 또는 자식 ) 에 컴포넌트로 부착
/// 새 패턴은 상속 후 Execute 만 구현
/// 실행 / 취소는 BossBase 가 관리 ( 스턴·사망 시 Cleanup 호출 )
/// </summary>
public abstract class BossPattern : MonoBehaviour
{
    [Header("패턴 선택")]
    [SerializeField, Tooltip("BT 에서 특정 패턴 지정 실행 시 사용")]
    private string patternId;
    [SerializeField, Min(0f), Tooltip("가중치 랜덤 비율")]
    private float weight = 1f;
    [SerializeField, Min(0f), Tooltip("패턴 종료 후 재사용 대기 시간")]
    private float cooldown;
    [SerializeField, Tooltip("사용 가능한 HP 비율 구간 ( 페이즈 구분용 )")]
    private Vector2 hpRatioRange = new Vector2(0f, 1f);

    [Header("애니메이션")]
    [SerializeField, Tooltip("패턴 시작 시 보스 Animator 트리거 ( 비우면 재생 X )")]
    private string animTrigger;

    private float lastEndTime = float.NegativeInfinity;

    private readonly List<BossTelegraph> telegraphs = new List<BossTelegraph>();
    private readonly List<BossDamageZone> damageZones = new List<BossDamageZone>();
    private readonly List<Coroutine> subRoutines = new List<Coroutine>();

    protected BossBase Boss { get; private set; }

    public string PatternId => string.IsNullOrEmpty(patternId) ? GetType().Name : patternId;
    public float Weight => weight;
    public string AnimTrigger => animTrigger;

    public void Initialize(BossBase boss)
    {
        Boss = boss;
    }

    public virtual bool CanUse()
    {
        if (Time.time < lastEndTime + cooldown) return false;

        float hpRatio = Boss.HpRatio;
        return hpRatio >= hpRatioRange.x && hpRatio <= hpRatioRange.y;
    }

    /// <summary> BossBase 전용 실행 진입점 </summary>
    public IEnumerator Run()
    {
        yield return Execute();
    }

    protected abstract IEnumerator Execute();

    /// <summary>
    /// 패턴 종료 / 취소 시 호출 - 경고·피해 영역·하위 코루틴 정리
    /// 패턴이 직접 만든 오브젝트가 있으면 override 후 base 호출
    /// </summary>
    public virtual void Cleanup()
    {
        foreach (Coroutine routine in subRoutines)
        {
            if (routine != null) Boss.StopCoroutine(routine);
        }
        subRoutines.Clear();

        foreach (BossTelegraph telegraph in telegraphs)
        {
            if (telegraph != null) telegraph.Release();
        }
        telegraphs.Clear();

        foreach (BossDamageZone zone in damageZones)
        {
            if (zone != null) zone.Release();
        }
        damageZones.Clear();
    }

    public void MarkEnded()
    {
        lastEndTime = Time.time;
    }

    #region 하위 클래스용 헬퍼

    protected BossTelegraph CreateTelegraph(ZoneShape shape, TelegraphStyle style)
    {
        BossTelegraph telegraph = BossTelegraph.Create(shape, style, transform);
        telegraphs.Add(telegraph);
        return telegraph;
    }

    protected BossDamageZone SpawnDamageZone(ZoneShape shape, ZoneShape hole, float damage, float duration, float tickInterval, GameObject vfxPrefab = null)
    {
        BossDamageZone zone = BossDamageZone.Spawn(Boss, shape, hole, damage, duration, tickInterval, vfxPrefab);
        damageZones.Add(zone);
        return zone;
    }

    /// <summary> 취소 시 같이 정지되는 병렬 코루틴 </summary>
    protected Coroutine StartSubRoutine(IEnumerator routine)
    {
        Coroutine coroutine = Boss.StartCoroutine(routine);
        subRoutines.Add(coroutine);
        return coroutine;
    }

    /// <summary>
    /// 영역 안 플레이어에게 피해 - nextHitTime 으로 틱 간격 관리
    /// </summary>
    protected bool TryDamagePlayer(ZoneShape shape, float damage, float tickInterval, ref float nextHitTime)
    {
        if (Time.time < nextHitTime) return false;
        if (!Boss.HasPlayer || !shape.Contains(Boss.PlayerPosition)) return false;

        Boss.DamagePlayer(damage);
        nextHitTime = Time.time + tickInterval;
        return true;
    }

    #endregion
}
