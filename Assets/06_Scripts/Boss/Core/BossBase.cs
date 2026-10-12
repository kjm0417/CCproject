using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 공통 - HP / 스턴 / 사망 / 패턴 실행 관리
/// 판단 ( 어떤 패턴을 언제 ) 은 Behavior Graph 가 담당, 이 클래스는 실행만 담당
/// 새 보스는 상속 후 연출 훅 ( OnPatternStarted 등 ) 만 override
/// </summary>
public abstract class BossBase : MonoBehaviour, IEnemyDamageable
{
    [SerializeField] protected BossData data;
    [SerializeField, Tooltip("비어 있으면 자식 포함 BossPattern 자동 수집")]
    private List<BossPattern> patterns = new List<BossPattern>();
    [SerializeField, Tooltip("사망 시 비활성화할 AI 컴포넌트 ( BehaviorGraphAgent )")]
    private Behaviour aiAgent;
    [SerializeField, Tooltip("보스 애니메이션 ( Idle / Hit / Stun / Die / 패턴별 트리거 )")]
    private BossAnimator anim = new BossAnimator();

    private BossPatternSelector selector;
    private PlayerVitals player;

    private int currentHp;
    private bool isDead;
    private bool isStunned;
    private float stunEndTime;

    private Coroutine patternRoutine;
    private BossPattern runningPattern;

    public event Action<int, int> OnHpChanged; // current, max
    public event Action<BossPattern> OnPatternStartedEvent;
    public event Action OnStunStartedEvent;
    public event Action OnStunEndedEvent;
    public event Action OnDiedEvent;

    public BossData Data => data;
    public int CurrentHp => currentHp;
    public float HpRatio => data.maxHp > 0 ? (float)currentHp / data.maxHp : 0f;
    public bool IsDead => isDead;
    public bool IsStunned => isStunned;
    public bool IsPatternRunning => patternRoutine != null;
    public BossPattern SelectedPattern { get; private set; }

    public bool HasPlayer => player != null && !player.IsDead;
    public Vector2 PlayerPosition => player != null ? (Vector2)player.transform.position : Vector2.zero;

    protected virtual void Awake()
    {
        currentHp = data.maxHp;

        if (patterns.Count == 0)
        {
            GetComponentsInChildren(true, patterns);
        }
        foreach (BossPattern pattern in patterns)
        {
            pattern.Initialize(this);
        }

        selector = new BossPatternSelector(patterns, data.avoidRepeatPattern);
        anim.Initialize();
    }

    protected virtual void Start()
    {
        player = FindAnyObjectByType<PlayerVitals>();
        OnHpChanged?.Invoke(currentHp, data.maxHp);
    }

    protected virtual void Update()
    {
        if (isStunned && Time.time >= stunEndTime)
        {
            EndStun();
        }
    }

    #region 패턴

    public bool SelectNextPattern()
    {
        SelectedPattern = selector.Select();
        return SelectedPattern != null;
    }

    public bool SelectPatternById(string patternId)
    {
        SelectedPattern = selector.FindById(patternId);
        if (SelectedPattern != null) selector.NotifyUsed(SelectedPattern);
        return SelectedPattern != null;
    }

    public bool StartSelectedPattern()
    {
        if (isDead || isStunned || IsPatternRunning || SelectedPattern == null) return false;

        runningPattern = SelectedPattern;
        patternRoutine = StartCoroutine(RunPattern(runningPattern));
        return true;
    }

    /// <summary> 실행 중인 패턴 강제 종료 ( 스턴 / 사망 / BT 중단 ) </summary>
    public void CancelPattern()
    {
        if (patternRoutine == null) return;

        StopCoroutine(patternRoutine);
        FinishPattern();
    }

    private IEnumerator RunPattern(BossPattern pattern)
    {
        anim.SetTrigger(pattern.AnimTrigger);
        OnPatternStarted(pattern);
        OnPatternStartedEvent?.Invoke(pattern);

        yield return pattern.Run();

        FinishPattern();
    }

    private void FinishPattern()
    {
        BossPattern pattern = runningPattern;
        patternRoutine = null;
        runningPattern = null;

        if (pattern == null) return;
        anim.ResetTrigger(pattern.AnimTrigger); // 스턴 등으로 소비 못한 트리거 정리
        pattern.Cleanup();
        pattern.MarkEnded();
        OnPatternEnded(pattern);
    }

    #endregion

    #region 스턴

    public void ApplyStun(float duration)
    {
        if (isDead) return;

        CancelPattern();
        stunEndTime = Mathf.Max(stunEndTime, Time.time + duration);

        if (isStunned) return;
        isStunned = true;
        anim.SetStunned(true);
        OnStunStarted();
        OnStunStartedEvent?.Invoke();
    }

    private void EndStun()
    {
        isStunned = false;
        anim.SetStunned(false);
        OnStunEnded();
        OnStunEndedEvent?.Invoke();
    }

    #endregion

    #region 피격 / 사망

    public virtual void TakeDamage(int damage)
    {
        if (isDead) return;
        if (data.invulnerableUnlessStunned && !isStunned) return;

        int finalDamage = isStunned ? Mathf.RoundToInt(damage * data.stunDamageMultiplier) : damage;
        currentHp = Mathf.Max(0, currentHp - finalDamage);
        OnHpChanged?.Invoke(currentHp, data.maxHp);

        if (currentHp <= 0) Die();
        else anim.PlayHit();
    }

    protected virtual void Die()
    {
        if (isDead) return;

        isDead = true;
        CancelPattern();
        if (aiAgent != null) aiAgent.enabled = false;
        anim.PlayDie();

        OnDied();
        OnDiedEvent?.Invoke();
    }

    /// <summary> 즉시 사망 ( 디버그 / 연출용 ) - 무적 / 스턴 조건 무시 </summary>
    public void Kill()
    {
        if (isDead) return;

        currentHp = 0;
        OnHpChanged?.Invoke(currentHp, data.maxHp);
        Die();
    }

    public void DamagePlayer(float damage)
    {
        if (HasPlayer) player.TakeDamage(damage);
    }

    #endregion

    #region 하위 보스 연출 훅

    protected virtual void OnPatternStarted(BossPattern pattern) { }
    protected virtual void OnPatternEnded(BossPattern pattern) { }
    protected virtual void OnStunStarted() { }
    protected virtual void OnStunEnded() { }
    protected virtual void OnDied() { }

    #endregion
}
