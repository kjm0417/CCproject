using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 개별 - 해골
/// </summary>
public class EnemySkeleton : EnemyBase
{
    private void OnGUI()
    {
        if (enemyContext == null) return;

        GUILayout.BeginArea(new Rect(310, 10, 220, 150), GUI.skin.box);

        GUILayout.Label($"{name}");
        GUILayout.Label($"HP: {enemyContext.EnemyHelath.CurrentHealth} / {enemyContext.EnemyHelath.MaxHealth}");
        GUILayout.Label($"IsDie: {enemyContext.EnemyHelath.IsDie}");

        if (GUILayout.Button("TakeDamage 10"))
            TakeDamage(10);

        if (GUILayout.Button("TakeDamage 100"))
            TakeDamage(100);

        GUILayout.EndArea();
    }
    private EnemySkeltetonContext skeletonContext;

    [SerializeField]
    private MonsterSkeletonData monsterSkeletonData;
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

    /// <summary>
    /// 스켈레톤 스킬 저장소 생성 및 스킬 등록
    /// </summary>
    /// <returns></returns>
    protected override EnemySkillManager CreateSkillManager()
    {
        skillManager = new EnemySkillManager();

        //등록 순서 = 우선순위
        skillManager.RegisterSkill(new SkeletonInflateSkill(enemyContext, this, monsterSkeletonData));

        return skillManager;
    }

    /// <summary>
    /// 스켈레톤 FSM 생성 및 상태 등록 및 초기 상태 설정
    /// </summary>
    /// <returns></returns>
    public override EnemyFSM CreateFSM()
    {
        enemyFSM = new EnemyFSM(enemyContext);

        enemyFSM.RegisterState(new SkeletonIdleState(skeletonContext, skillManager, enemyFSM, this, monsterSkeletonData));
        enemyFSM.RegisterState(new SkeletonWalkState(skeletonContext, skillManager, enemyFSM, this, monsterSkeletonData));
        enemyFSM.RegisterState(new SkeletonChargeState(skeletonContext, skillManager, enemyFSM, this, monsterSkeletonData));
        enemyFSM.RegisterState(new SkeletonBaseAttackState(skeletonContext, skillManager, enemyFSM, this, monsterSkeletonData));
        enemyFSM.RegisterState(new SkeletonSkillAttackState(skeletonContext, skillManager, enemyFSM, this, monsterSkeletonData));

        enemyFSM.ChangeState<SkeletonIdleState>();

        return enemyFSM;
    }
    /// <summary>
    /// 스켈레톤 컴포넌트 조립 - 필요 시 추가
    /// </summary>
    /// <returns></returns>
    public override EnemyContext CreateCTX()
    {
        skeletonContext = new EnemySkeltetonContext(
            new EnemyHelath(monsterData.HP), //TODO KJ - SO에서 읽어온 최대 체력값으로
            GetComponentInChildren<SpriteRenderer>(),
            GetComponentInChildren<Rigidbody2D>(),
            GetComponentInChildren<Animator>(),
            GetComponentInChildren<EnemySkeletonDetector>(),
            monsterData
        );

        return skeletonContext;
    }

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);

        //기타 처리할 것 있으면 
    }
    public override void OnDied()
    {
        base.OnDied();

        skeletonContext.Rigidbody2D.linearVelocity = Vector2.zero;
        //TODO KJ - 드랍 아이템
    }
}
/// <summary>
/// 스켈레톤 Idle 상태
/// Spawn -> Idle
/// </summary>
public class SkeletonIdleState : EnemyState
{

    private float idleDuration;
    private float idleTimer = 0f;

    public SkeletonIdleState(EnemyContext ctx, EnemySkillManager skillManager, EnemyFSM fsm,
        MonoBehaviour mono, MonsterSkeletonData monsterSkeletonData)
       : base(ctx, skillManager, fsm, mono)
    {
        //ctx.Animator.StopPlayback(EnemyAnimHash.Spawn);

        idleDuration = monsterSkeletonData.IdleDuration;
    }
    public override void Enter()
    {
        ctx.Animator.SetTrigger(EnemyAnimHash.IdleTrigger);

        idleTimer = 0f;
    }

    public override void Update()
    {
        //idleTimer 시간 경과 시 Walk
        idleTimer += Time.deltaTime;
        if (idleTimer >= idleDuration)
        {
            fsm.ChangeState<SkeletonWalkState>();
        }
    }

    public override void Exit() { }
}
/// <summary>
/// 스켈레톤 Walk 상태 ( 좌/우로 왔다갔다 )
/// Idle -> Walk
/// </summary>
public class SkeletonWalkState : EnemyState
{
    private EnemySkeltetonContext skeletonctx;

    private Vector2 walkDirection = Vector2.left; //스프라이트 기준 기본적으로 어딜 바라보고 있는지
    private float walkSpeed;

    private float patrolDistance;
    private Vector2 patrolStart;

    private float walkDuration;
    private float walkTimer = 0f;
    public SkeletonWalkState(EnemyContext ctx, EnemySkillManager skillManager, EnemyFSM fsm, MonoBehaviour mono, MonsterSkeletonData monsterSkeletonData)
       : base(ctx, skillManager, fsm, mono)
    {
        skeletonctx = (EnemySkeltetonContext)ctx;

        walkSpeed = monsterSkeletonData.WalkSpeed;
        patrolDistance = monsterSkeletonData.PatrolDistance;
        walkDuration = monsterSkeletonData.WalkDruation;
    }

    public override void Enter()
    {
        skeletonctx.Animator.SetBool(EnemyAnimHash.IsWalk, true);

        walkTimer = 0f;
        patrolStart = skeletonctx.Rigidbody2D.position;
    }

    public override void Update()
    {
        //이동
        skeletonctx.Rigidbody2D.linearVelocity = walkDirection * walkSpeed;

        //거리 체크 - 반대 방향으로
        float distance = Vector2.Distance(skeletonctx.Rigidbody2D.position, patrolStart);
        if (distance >= patrolDistance)
        {
            walkDirection *= -1; // 방향 전환
            patrolStart = skeletonctx.Rigidbody2D.position; // 기준점 갱신

            skeletonctx.SpriteRenderer.flipX = walkDirection.x > 0;
        }

        //WalkTimer 시간 경과 시 다시 Idle
        walkTimer += Time.deltaTime;
        if (walkTimer >= walkDuration)
        {
            fsm.ChangeState<SkeletonIdleState>();
        }

        //추격 감지
        if (skeletonctx.EnemyDetector.IsCharge())
        {
            fsm.ChangeState<SkeletonChargeState>();
        }

    }

    public override void Exit()
    {
        ctx.Animator.SetBool(EnemyAnimHash.IsWalk, false);
        skeletonctx.Rigidbody2D.linearVelocity = Vector2.zero;
    }
}

/// <summary>
/// 스켈레톤 추격 상태 ( 공격 범위 안에 들어올 때 까지 추격 )
/// Walk -> Charge
/// </summary>
public class SkeletonChargeState : EnemyState
{
    private EnemySkeltetonContext skeletonctx;

    private float chargeSpeed;
    public SkeletonChargeState(EnemyContext ctx, EnemySkillManager skillManager, EnemyFSM fsm, MonoBehaviour mono, MonsterSkeletonData monsterSkeletonData)
       : base(ctx, skillManager, fsm, mono)
    {
        skeletonctx = (EnemySkeltetonContext)ctx;

        chargeSpeed = monsterSkeletonData.ChargeSpeed;
    }

    public override void Enter()
    {
        skeletonctx.Animator.SetBool(EnemyAnimHash.IsCharge, true);
    }

    public override void Update()
    {
        if (skeletonctx.EnemyDetector.chargeToPlayer != null)
        {
            // 플레이어 쫓아가기
            Vector2 direction = ((Vector2)skeletonctx.EnemyDetector.chargeToPlayer.transform.position - skeletonctx.Rigidbody2D.position).normalized;

            skeletonctx.Rigidbody2D.linearVelocity = direction * chargeSpeed;
            skeletonctx.SpriteRenderer.flipX = direction.x > 0;

            //공격 범위 안 - 스킬 우선, 스킬 불가능하면 기본 공격
            if (skeletonctx.EnemyDetector.IsAttack())
            {
                if (skillManager.TryGetReadySkill(out _))
                {
                    fsm.ChangeState<SkeletonSkillAttackState>();
                    return;
                }

                if (fsm.GetState<SkeletonBaseAttackState>().IsReady)
                {
                    fsm.ChangeState<SkeletonBaseAttackState>();
                    return;
                }

                //둘 다 쿨타임 - 제자리 대기
                skeletonctx.Rigidbody2D.linearVelocity = Vector2.zero;
            }
        }

        //플레이어 못 찾음 
        if (!skeletonctx.EnemyDetector.IsCharge())
        {
            fsm.ChangeState<SkeletonWalkState>();
        }
    }
    public override void Exit()
    {
        skeletonctx.Animator.SetBool(EnemyAnimHash.IsCharge, false);
    }

}

/// <summary>
/// 스켈레톤 기본 공격 상태 ( 가만히 서서 근접 공격 )
/// Any -> Attack
/// </summary>
public class SkeletonBaseAttackState : EnemyState
{
    private EnemySkeltetonContext skeletonctx;
    private EnemySkeletonDetector skeletonDetector; //슬라임의 감지 기능

    private float backupDuration;
    private float backupSpeed;

    private float chargeDuration;
    private float chargeSpeed;

    private bool isCharging = false;
    private Coroutine attackRoutine;

    private float cooldown;
    private float lastAttackTime = float.NegativeInfinity;

    /// <summary>
    /// 기본 공격 쿨타임 끝났는지 여부
    /// </summary>
    public bool IsReady => Time.time >= lastAttackTime + cooldown;

    public SkeletonBaseAttackState(EnemyContext ctx, EnemySkillManager skillManager, EnemyFSM fsm, MonoBehaviour mono, MonsterSkeletonData monsterSkeletonData)
       : base(ctx, skillManager, fsm, mono)
    {
        this.skeletonctx = (EnemySkeltetonContext)ctx;
        skeletonDetector = (EnemySkeletonDetector)ctx.EnemyDetector;


        chargeDuration = monsterSkeletonData.ChargeDuration;
        chargeSpeed = monsterSkeletonData.ChargeSpeed;

        cooldown = monsterSkeletonData.BaseAttackCooldown;

        isCharging = false;
    }

    public override void Enter()
    {
        lastAttackTime = Time.time;
        attackRoutine = mono.StartCoroutine(AttackRoutine());
    }
    private IEnumerator AttackRoutine()
    {
        float elapsed = 0f;

        //공격 시작
        skeletonctx.Animator.SetTrigger(EnemyAnimHash.BaseAttackTrigger);
        yield return null;
        bool hasAttack = false;

        Vector2 direction = ((Vector2)skeletonctx.EnemyDetector.attackToPlayer.transform.position
    - skeletonctx.Rigidbody2D.position).normalized;

        if (!hasAttack)
        {
            hasAttack = skeletonDetector.BaseAttack();
        }

        //공격 끝
        attackRoutine = null;
        fsm.ChangeState<SkeletonIdleState>();
    }

    public override void Update()
    {

    }

    public override void Exit()
    {
        //사망 등 외부에서 상태가 바뀌면 공격 중단
        if (attackRoutine != null)
        {
            mono.StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        skeletonctx.Animator.ResetTrigger(EnemyAnimHash.BaseAttackTrigger);
        skeletonctx.Rigidbody2D.linearVelocity = Vector2.zero;
    }

}

/// <summary>
/// 스켈레톤 스킬 공격 상태 ( 쿨타임 끝난 스킬 중 우선순위 높은 스킬 실행 )
/// Charge -> SkillAttack
/// </summary>
public class SkeletonSkillAttackState : EnemyState
{
    private EnemySkeltetonContext skeletonctx;
    private Coroutine skillRoutine;
    private EnemySkill currentSkill;

    public SkeletonSkillAttackState(EnemyContext ctx, EnemySkillManager skillManager, EnemyFSM fsm, MonoBehaviour mono, MonsterSkeletonData monsterSkeletonData)
       : base(ctx, skillManager, fsm, mono)
    {
        skeletonctx = (EnemySkeltetonContext)ctx;
    }

    public override void Enter()
    {
        skeletonctx.Rigidbody2D.linearVelocity = Vector2.zero;

        if (!skillManager.TryGetReadySkill(out EnemySkill skill))
        {
            fsm.ChangeState<SkeletonIdleState>();
            return;
        }

        currentSkill = skill;
        skillRoutine = mono.StartCoroutine(SkillRoutine(skill));
    }

    private IEnumerator SkillRoutine(EnemySkill skill)
    {
        yield return skill.Use();

        //스킬 끝
        skillRoutine = null;
        fsm.ChangeState<SkeletonIdleState>();
    }

    public override void Update() { }

    public override void Exit()
    {
        //피격 등으로 상태가 바뀌면 스킬 중단
        if (skillRoutine != null)
        {
            mono.StopCoroutine(skillRoutine);
            skillRoutine = null;
        }

        currentSkill?.OnEnd();
        currentSkill = null;

        skeletonctx.Rigidbody2D.linearVelocity = Vector2.zero;
    }
}

/// <summary>
/// 스켈레톤 스킬 - 전방베기
/// </summary>
public class SkeletonInflateSkill : EnemySkill
{
    private EnemySkeltetonContext skeletonctx;
    private EnemySkeletonDetector skeletonDetector; //슬라임의 감지 기능

    private float chargeDuration;
    private float chargeSpeed;
    private RigidbodyConstraints2D originConstraints;
    private const float StartTimeout = 0.5f;

    public SkeletonInflateSkill(EnemyContext ctx, MonoBehaviour owner, MonsterSkeletonData monsterSkeletonData) : base(ctx, owner)
    {
        skeletonctx = (EnemySkeltetonContext)ctx;
        skeletonDetector = (EnemySkeletonDetector)ctx.EnemyDetector;

        chargeDuration = monsterSkeletonData.ChargeDuration;
        chargeSpeed = monsterSkeletonData.ChargeSpeed;

        SkillAttackCoolDown = monsterSkeletonData.SkillAttackCooldown;
    }

    // public override float GetCooldown() => 6f; //TODO KJ - Excel

    protected override IEnumerator Execute()
    {
        float elapsed = 0f;

        //추격/기본 공격 이동 정지 + 스킬, 스턴 동안 밀리지 않게 고정
        originConstraints = skeletonctx.Rigidbody2D.constraints;
        skeletonctx.Rigidbody2D.linearVelocity = Vector2.zero;
        skeletonctx.Rigidbody2D.constraints = RigidbodyConstraints2D.FreezeAll;

        //공격 시작
        skeletonctx.Animator.SetTrigger(EnemyAnimHash.SkillTrigger);
        Debug.Log("스켈레톤 전방베기!");

        bool hasAttack = false;

        if (!hasAttack)
        {
            hasAttack = skeletonDetector.SpinningSlash();
        }

        //공격 끝
        skeletonctx.Animator.ResetTrigger(EnemyAnimHash.IdleTrigger); //남아 있던 트리거 제거

        yield return null;
    }

    public override void OnEnd()
    {
        //고정 해제
        skeletonctx.Rigidbody2D.constraints = originConstraints;
        skeletonctx.Animator.SetBool(EnemyAnimHash.IsStun, false);
    }

    private bool IsSkillPlaying()
    {
        return skeletonctx.Animator.GetCurrentAnimatorStateInfo(0).shortNameHash == EnemyAnimHash.Skill;
    }
}