using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.VersionControl.Asset;

/// <summary>
/// 적 피격 전용 인터페이스
/// </summary>
public interface IEnemyDamageable
{
    public void TakeDamage(int damage);
}

/// <summary>
/// 적 공통 
/// </summary>
public abstract class EnemyBase : MonoBehaviour, IEnemyDamageable, IPoolable
{
    protected EnemyContext enemyContext;
    protected EnemyFSM enemyFSM;
    protected EnemySkillManager skillManager;

    public static Action<float> OnEnemyDieExpEvent; 

    /// <summary>
    /// 개별 적 사망 이벤트 ( 던전 진행 관리자 등이 구독 )
    /// </summary>
    public event Action<EnemyBase> OnEnemyDied;

    public bool IsDie => enemyContext != null && enemyContext.EnemyHelath.IsDie;

    [SerializeField]
    protected MonsterData monsterData;
    public MonsterData MonsterData => monsterData;

    [SerializeField, Tooltip("사망 후 제거 ( 풀 반납 ) 까지 대기 시간 - 사망 애니 길이")]
    protected float despawnDelay = 1.5f;

    private bool isSpawnedOnce; //첫 생성은 Awake 에서 초기화 완료

    protected virtual void Awake()
    {
        enemyContext = CreateCTX();
        enemyContext.Animator.Play(EnemyAnimHash.Spawn);

        skillManager = CreateSkillManager();

        enemyFSM = CreateFSM();
    }

    protected virtual void OnEnable()
    {
        //적 사망 이벤트
        enemyContext.EnemyHelath.OnDie += OnDied;
    }
    protected virtual void OnDisable()
    {
        enemyContext.EnemyHelath.OnDie -= OnDied;
    }

    /// <summary>
    /// 적 Skill 개별 생성
    /// </summary>
    /// <returns></returns>
    protected abstract EnemySkillManager CreateSkillManager();
    /// <summary>
    /// 적 FSM 개별 생성
    /// </summary>
    /// <returns></returns>
    public abstract EnemyFSM CreateFSM();
    /// <summary>
    /// 적 컴포넌트 개별 조립
    /// </summary>
    /// <returns></returns>
    public abstract EnemyContext CreateCTX();

    protected virtual void Update()
    {
        if (enemyContext.EnemyHelath.IsDie)
            return;

        enemyFSM.UpdateTick();
    }
    /// <summary>
    /// 적 피격
    /// </summary>
    /// <param name="damage"></param>
    public virtual void TakeDamage(int damage)
    {
        if (enemyContext.EnemyHelath.IsDie)
            return;

        enemyContext.EnemyHelath.ApplyDamaged(damage);

        if (enemyContext.EnemyHelath.IsDie) 
            return;

        enemyContext.Animator.Play(EnemyAnimHash.Hit, 0, 0f);
    }

    /// <summary>
    /// 적 사망
    /// </summary>
    public virtual void OnDied()
    {
        //진행 중인 상태(공격/스킬 코루틴) 정리 - 코루틴이 Die 애니를 덮어쓰지 않게
        enemyFSM.Stop();
        StopAllCoroutines();

        ResetAnimParams();

        enemyContext.Animator.SetTrigger(EnemyAnimHash.DieTrigger);

        OnEnemyDieExpEvent?.Invoke(monsterData.EXP);
        OnEnemyDied?.Invoke(this);

        //사망 애니 후 풀 반납 ( 풀 오브젝트가 아니면 Destroy )
        ObjectPoolManager.Despawn(gameObject, despawnDelay);
    }

    /// <summary>
    /// 풀 재사용 - 체력 / 애니 / 스킬 / FSM 초기화
    /// </summary>
    public virtual void OnSpawned()
    {
        if (!isSpawnedOnce)
        {
            isSpawnedOnce = true;
            return;
        }

        enemyContext.EnemyHelath.ResetHelath();
        ResetAnimParams();
        enemyContext.Animator.Play(EnemyAnimHash.Spawn, 0, 0f);

        skillManager = CreateSkillManager();
        enemyFSM = CreateFSM();
    }

    public virtual void OnDespawned()
    {
        enemyFSM.Stop();
        StopAllCoroutines();
    }

    /// <summary>
    /// 남아있는 트리거/Bool 초기화 - 사망 후 다른 상태로 전환 방지
    /// </summary>
    protected virtual void ResetAnimParams()
    {
        Animator animator = enemyContext.Animator;

        animator.ResetTrigger(EnemyAnimHash.IdleTrigger);
        animator.ResetTrigger(EnemyAnimHash.BaseAttackTrigger);
        animator.ResetTrigger(EnemyAnimHash.SkillTrigger);

        animator.SetBool(EnemyAnimHash.IsWalk, false);
        animator.SetBool(EnemyAnimHash.IsCharge, false);
        animator.SetBool(EnemyAnimHash.IsStun, false);
    }


}
/// <summary>
/// 적 공통 - 상태 전환 정의 
/// </summary>
public abstract class EnemyState
{
    protected EnemyFSM fsm;
    protected EnemyContext ctx;
    protected EnemySkillManager skillManager;

    protected MonoBehaviour mono;
    public EnemyState(EnemyContext ctx, EnemySkillManager skillManager, EnemyFSM fsm,MonoBehaviour mono)
    {
        this.ctx = ctx;
        this.skillManager = skillManager;
        this.fsm = fsm;
        this.mono = mono;
    }

    public abstract void Enter();
    public abstract void Update();
    public abstract void Exit();
}

/// <summary>
/// 적 상태 관리
/// </summary>
public class EnemyFSM
{
    private EnemyState currentState;
    private EnemyContext ctx;
    private Dictionary<System.Type, EnemyState> states = new(); //딕셔너리로 상태 관리 및 매번 객체 생성으로 상태 전환하는거 최적화

    public EnemyFSM(EnemyContext enemyContext)
    {
        this.ctx = enemyContext;
    }
    
    public void UpdateTick()
    {
        currentState?.Update();
    }
    public void RegisterState(EnemyState state)
    {
        states[state.GetType()] = state; //딕셔너리에 각 상태 클래스의 타입를 Key로 하여, 각 상태 클래스 자체를 구독
    }

    public T GetState<T>() where T : EnemyState
    {
        return states.TryGetValue(typeof(T), out var state) ? (T)state : null;
    }

    /// <summary>
    /// FSM 정지 ( 사망 시 ) - 현재 상태 Exit 호출 후 비움
    /// </summary>
    public void Stop()
    {
        currentState?.Exit();
        currentState = null;
    }

    public void ChangeState<T>() where T : EnemyState
    {
        if (states.TryGetValue(typeof(T), out var newState))
        {
            currentState?.Exit();
            currentState = newState;
            currentState.Enter();
        }
    }
}
