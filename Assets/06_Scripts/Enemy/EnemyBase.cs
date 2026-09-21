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

    protected virtual void Awake()
    {
        enemyContext = CreateCTX();
        
        enemyFSM = CreateFSM();

        skillManager = CreateSkillManager();
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
    }

    /// <summary>
    /// 적 사망
    /// </summary>
    public abstract void OnDied();


}
/// <summary>
/// 적 공통 - 상태 전환 정의 
/// </summary>
public abstract class EnemyState
{
    protected EnemyContext ctx;
    protected EnemySkillManager skillManager;

    public virtual void SetContext(EnemyContext context, EnemySkillManager manager)
    {
        ctx = context;
        skillManager = manager;
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
/// <summary>
/// 적 공통 - 스킬 실행 및 쿨타임 
/// </summary>
public abstract class EnemySkill
{
    protected EnemyContext ctx;
    protected MonoBehaviour owner; //모노에서 지원하는 메서드나 기능들 사용하기 위함

    public EnemySkill(EnemyContext ctx, MonoBehaviour owner)
    {
        this.ctx = ctx;
        this.owner = owner;
    }

    /// <summary>
    /// 스킬 Tick 실행
    /// </summary>
    public abstract void Execute();

    /// <summary>
    /// 스킬 쿨타임 설정 ( 각 스킬 클래스가 재정의 )
    /// </summary>
    /// <returns></returns>
    public virtual float GetCooldown() => 1f;
}
/// <summary>
/// 적 스킬 저장소
/// </summary>
public class EnemySkillManager
{
    private Dictionary<string, EnemySkill> skills = new();

    public void RegisterSkill(string name, EnemySkill skill)
    {
        skills[name] = skill;
    }

    public void Execute(string skillName)
    {
        if (skills.TryGetValue(skillName, out var skill))
            skill.Execute();
    }
}