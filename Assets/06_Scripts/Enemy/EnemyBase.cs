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
public abstract class EnemyBase : MonoBehaviour, IEnemyDamageable
{
    protected EnemyContext enemyContext;
    protected EnemyFSM enemyFSM;
    protected EnemySkillManager skillManager;

    public static Action<float> OnEnemyDieExpEvent; 

    [SerializeField]
    protected MonsterData monsterData;
    public MonsterData MonsterData => monsterData;

    private void OnGUI()
    {
        if (enemyContext == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 220, 150), GUI.skin.box);

        GUILayout.Label($"{name}");
        GUILayout.Label($"HP: {enemyContext.EnemyHelath.CurrentHealth} / {enemyContext.EnemyHelath.MaxHealth}");
        GUILayout.Label($"IsDie: {enemyContext.EnemyHelath.IsDie}");

        if (GUILayout.Button("TakeDamage 10"))
            TakeDamage(10);

        if (GUILayout.Button("TakeDamage 100"))
            TakeDamage(100);

        GUILayout.EndArea();
    }
    protected virtual void Awake()
    {
        enemyContext = CreateCTX();
        enemyContext.Animator.SetBool(EnemyAnimHash.IsSpawn, true);

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
