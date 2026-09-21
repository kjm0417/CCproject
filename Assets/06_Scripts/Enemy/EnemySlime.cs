using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySlime : EnemyBase
{
    private EnemySlimeContext slimeContext;

    protected override void Awake()
    {
        base.Awake();
    }
    protected override void OnEnable()
    {
        //적 사망 이벤트
        base.OnEnable();
    }
    protected override void OnDisable()
    {
        base.OnDisable();
    }
    protected override void Update()
    {
        base.Update();
    }

    protected override EnemySkillManager CreateSkillManager()
    {
        skillManager = new EnemySkillManager();
       
        skillManager.RegisterSkill("JumpAttack", new SlimeJumpAttack(enemyContext, this));
       
        return skillManager;
    }

    /// <summary>
    /// 슬라임 FSM 생성 및 상태 등록 및 초기 상태 설정
    /// </summary>
    /// <returns></returns>
    public override EnemyFSM CreateFSM()
    {
        enemyFSM = new EnemyFSM(enemyContext);
      
        enemyFSM.RegisterState(new SlimeIdleState());
        enemyFSM.RegisterState(new SlimeJumpState());
       
        enemyFSM.ChangeState<SlimeIdleState>();

        return enemyFSM;
    }
    /// <summary>
    /// 슬라임 컴포넌트 조립 - 필요 시 추가
    /// </summary>
    /// <returns></returns>
    public override EnemyContext CreateCTX()
    {
        slimeContext = new EnemySlimeContext(
            new EnemyHelath(100f), //TODO KJ - SO에서 읽어온 최대 체력값으로
            GetComponentInChildren<SpriteRenderer>(),
            GetComponentInChildren<Rigidbody2D>(),
            GetComponentInChildren<Animator>(),
            GetComponent<EnemyDetector>()
        );

        return slimeContext;
    }

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);

        //TODO KJ - 피격 애님 재생
    }
    public override void OnDied()
    {

    }

}

public class SlimeIdleState : EnemyState
{
    public override void Enter()
    {
       
    }

    public override void Update()
    {
    
    }

    public override void Exit()
    {

    }
}
public class SlimeJumpState : EnemyState
{
    public override void Enter()
    {
        
    }

    public override void Update()
    {
        
    }

    public override void Exit()
    {

    }

}

public class SlimeJumpAttack : EnemySkill
{
    private float attackDamage = 10f;
    private float attackRange = 1.5f;

    public SlimeJumpAttack(EnemyContext ctx, MonoBehaviour owner)
        : base(ctx, owner) //부모 클래스 생성자 호출하여 Context와 Mono 초기화
    {
        //초기화 할 것들. 없으면 안해도 됨. 
    }

    public override void Execute()
    {
       
    }

    public override float GetCooldown()
    {
        return 1.0f;
    }
}