using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 적 개별 - 슬라임
/// </summary>
public class EnemySlime : EnemyBase
{
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

    private EnemySlimeContext slimeContext;

    [SerializeField]
    private MonsterSlimeData monsterSlimeData;

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
    /// 슬라임 스킬 저장소 생성 및 스킬 등록
    /// </summary>
    /// <returns></returns>
    protected override EnemySkillManager CreateSkillManager()
    {
        skillManager = new EnemySkillManager();
       
        //등록 순서 = 우선순위
        skillManager.RegisterSkill(new SlimeInflateSkill(enemyContext, this, monsterSlimeData));
       
        return skillManager;
    }

    /// <summary>
    /// 슬라임 FSM 생성 및 상태 등록 및 초기 상태 설정
    /// </summary>
    /// <returns></returns>
    public override EnemyFSM CreateFSM()
    {
        enemyFSM = new EnemyFSM(enemyContext);
      
        enemyFSM.RegisterState(new SlimeIdleState(slimeContext, skillManager,enemyFSM , this , monsterSlimeData));
        enemyFSM.RegisterState(new SlimeWalkState(slimeContext, skillManager, enemyFSM , this, monsterSlimeData));
        enemyFSM.RegisterState(new SlimeChargeState(slimeContext, skillManager, enemyFSM, this, monsterSlimeData));
        enemyFSM.RegisterState(new SlimeBaseAttackState(slimeContext, skillManager, enemyFSM,this, monsterSlimeData));
        enemyFSM.RegisterState(new SlimeSkillAttackState(slimeContext, skillManager, enemyFSM, this, monsterSlimeData));

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
            new EnemyHelath(monsterData.HP), //TODO KJ - SO에서 읽어온 최대 체력값으로
            GetComponentInChildren<SpriteRenderer>(),
            GetComponentInChildren<Rigidbody2D>(),
            GetComponentInChildren<Animator>(),
            GetComponentInChildren<EnemySlimeDetector>(),
            monsterData
        );

        return slimeContext;
    }

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);

        //기타 처리할 것 있으면 
    }
    public override void OnDied()
    {
        base.OnDied();

        slimeContext.Rigidbody2D.linearVelocity = Vector2.zero;
        //TODO KJ - 드랍 아이템
    }

}

/// <summary>
/// 슬라임 Idle 상태
/// Spawn -> Idle
/// </summary>
public class SlimeIdleState : EnemyState
{

    private float idleDuration;
    private float idleTimer = 0f;

    public SlimeIdleState(EnemyContext ctx, EnemySkillManager skillManager, EnemyFSM fsm, 
        MonoBehaviour mono, MonsterSlimeData monsterSlimeData)
       : base(ctx, skillManager, fsm, mono)
    {
        //ctx.Animator.SetBool(EnemyAnimHash.IsSpawn, false);

        idleDuration = monsterSlimeData.IdleDuration;
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
            fsm.ChangeState<SlimeWalkState>();
        }
    }

    public override void Exit() { }
}
/// <summary>
/// 슬라임 Walk 상태 ( 좌/우로 왔다갔다 )
/// Idle -> Walk
/// </summary>
public class SlimeWalkState : EnemyState
{
    private EnemySlimeContext slimectx;

    private Vector2 walkDirection = Vector2.left; //스프라이트 기준 기본적으로 어딜 바라보고 있는지
    private float walkSpeed;

    private float patrolDistance;
    private Vector2 patrolStart;

    private float walkDuration;
    private float walkTimer = 0f;
    public SlimeWalkState(EnemyContext ctx, EnemySkillManager skillManager, EnemyFSM fsm, MonoBehaviour mono,MonsterSlimeData monsterSlimeData)
       : base(ctx, skillManager, fsm, mono)
    {
        slimectx = (EnemySlimeContext)ctx;
       
        walkSpeed = monsterSlimeData.WalkSpeed;
        patrolDistance = monsterSlimeData.PatrolDistance;
        walkDuration = monsterSlimeData.WalkDruation;
    }

    public override void Enter()
    {
        slimectx.Animator.SetBool(EnemyAnimHash.IsWalk,true);

        walkTimer = 0f;
        patrolStart = slimectx.Rigidbody2D.position;
    }

    public override void Update()
    {
        //이동
        slimectx.Rigidbody2D.linearVelocity = walkDirection * walkSpeed;

        //거리 체크 - 반대 방향으로
        float distance = Vector2.Distance(slimectx.Rigidbody2D.position, patrolStart);
        if (distance >= patrolDistance)
        {
            walkDirection *= -1; // 방향 전환
            patrolStart = slimectx.Rigidbody2D.position; // 기준점 갱신

            slimectx.SpriteRenderer.flipX = walkDirection.x > 0;
        }

        //WalkTimer 시간 경과 시 다시 Idle
        walkTimer += Time.deltaTime;
        if (walkTimer >= walkDuration)
        {
            fsm.ChangeState<SlimeIdleState>();
        }

        //추격 감지
        if(slimectx.EnemyDetector.IsCharge())
        {
            fsm.ChangeState<SlimeChargeState>();
        }

    }

    public override void Exit()
    {
        ctx.Animator.SetBool(EnemyAnimHash.IsWalk, false);
        slimectx.Rigidbody2D.linearVelocity = Vector2.zero;
    }
}

/// <summary>
/// 슬라임 추격 상태 ( 공격 범위 안에 들어올 때 까지 추격 )
/// Walk -> Charge
/// </summary>
public class SlimeChargeState : EnemyState
{
    private EnemySlimeContext slimectx;

    private float chargeSpeed;
    public SlimeChargeState(EnemyContext ctx, EnemySkillManager skillManager, EnemyFSM fsm, MonoBehaviour mono,MonsterSlimeData monsterSlimeData)
       : base(ctx, skillManager, fsm, mono)
    {
        slimectx = (EnemySlimeContext)ctx;

        chargeSpeed = monsterSlimeData.ChargeSpeed;
    }

    public override void Enter()
    {
        slimectx.Animator.SetBool(EnemyAnimHash.IsCharge, true);
    }

    public override void Update()
    {
        if (slimectx.EnemyDetector.chargeToPlayer != null)
        {
            // 플레이어 쫓아가기
            Vector2 direction = ((Vector2)slimectx.EnemyDetector.chargeToPlayer.transform.position - slimectx.Rigidbody2D.position).normalized;

            slimectx.Rigidbody2D.linearVelocity = direction * chargeSpeed;
            slimectx.SpriteRenderer.flipX = direction.x > 0;

            //공격 범위 안 - 스킬 우선, 스킬 불가능하면 기본 공격
            if (slimectx.EnemyDetector.IsAttack())
            {
                if (skillManager.TryGetReadySkill(out _))
                {
                    fsm.ChangeState<SlimeSkillAttackState>();
                    return;
                }

                if (fsm.GetState<SlimeBaseAttackState>().IsReady)
                {
                    fsm.ChangeState<SlimeBaseAttackState>();
                    return;
                }

                //둘 다 쿨타임 - 제자리 대기
                slimectx.Rigidbody2D.linearVelocity = Vector2.zero;
            }
        }

        //플레이어 못 찾음 
        if (!slimectx.EnemyDetector.IsCharge())
        {
            fsm.ChangeState<SlimeWalkState>();
        }
    }
    public override void Exit()
    {
        slimectx.Animator.SetBool(EnemyAnimHash.IsCharge, false);
    }

}

/// <summary>
/// 슬라임 기본 공격 상태 ( 뒤로 뺏다가 돌진 )
/// Walk -> Charge
/// </summary>
public class SlimeBaseAttackState : EnemyState
{
    private EnemySlimeContext slimectx;
    private EnemySlimeDetector slimeDetector; //슬라임의 감지 기능

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

    public SlimeBaseAttackState(EnemyContext ctx, EnemySkillManager skillManager, EnemyFSM fsm, MonoBehaviour mono,MonsterSlimeData monsterSlimeData)
       : base(ctx, skillManager, fsm , mono)
    {
        this.slimectx = (EnemySlimeContext)ctx;
        slimeDetector = (EnemySlimeDetector)ctx.EnemyDetector;

        backupDuration = monsterSlimeData.BackupDuration;
        backupSpeed = monsterSlimeData.BackupSpeed;

        chargeDuration = monsterSlimeData.ChargeDuration;
        chargeSpeed = monsterSlimeData.ChargeSpeed;

        cooldown = monsterSlimeData.BaseAttackCooldown;

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

        //뒤로 빼기
        slimectx.Animator.SetTrigger(EnemyAnimHash.BaseAttackTrigger);
        Vector2 backupDir = -((Vector2)slimectx.EnemyDetector.attackToPlayer.transform.position
    - slimectx.Rigidbody2D.position).normalized;

        while (elapsed < backupDuration)
        {
         
            slimectx.Rigidbody2D.linearVelocity = backupDir * backupSpeed;
           
            //slimectx.SpriteRenderer.flipX = backupDir.x > 0;

            elapsed += Time.deltaTime;
            yield return null;
        }

        //돌진
        bool hasAttack = false;

        Vector2 direction = ((Vector2)slimectx.EnemyDetector.attackToPlayer.transform.position
    - slimectx.Rigidbody2D.position).normalized;
        while (elapsed < chargeDuration)
        {
           
            slimectx.Rigidbody2D.linearVelocity = direction * chargeSpeed;

           // slimectx.SpriteRenderer.flipX = direction.x > 0;

            elapsed += Time.deltaTime;

            if (!hasAttack)
            {
                hasAttack = slimeDetector.BaseAttack();
            }

            yield return null;
        }

        //공격 끝
        attackRoutine = null;
        fsm.ChangeState<SlimeIdleState>();
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

        slimectx.Animator.ResetTrigger(EnemyAnimHash.BaseAttackTrigger);
        slimectx.Rigidbody2D.linearVelocity = Vector2.zero;
    }

}

/// <summary>
/// 슬라임 스킬 공격 상태 ( 쿨타임 끝난 스킬 중 우선순위 높은 스킬 실행 )
/// Charge -> SkillAttack
/// </summary>
public class SlimeSkillAttackState : EnemyState
{
    private EnemySlimeContext slimectx;
    private Coroutine skillRoutine;
    private EnemySkill currentSkill;

    public SlimeSkillAttackState(EnemyContext ctx, EnemySkillManager skillManager, EnemyFSM fsm, MonoBehaviour mono , MonsterSlimeData monsterSlimeData)
       : base(ctx, skillManager, fsm, mono)
    {
        slimectx = (EnemySlimeContext)ctx;
    }

    public override void Enter()
    {
        slimectx.Rigidbody2D.linearVelocity = Vector2.zero;

        if (!skillManager.TryGetReadySkill(out EnemySkill skill))
        {
            fsm.ChangeState<SlimeIdleState>();
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
        fsm.ChangeState<SlimeIdleState>();
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

        slimectx.Rigidbody2D.linearVelocity = Vector2.zero;
    }
}

/// <summary>
/// 슬라임 스킬 - 몸을 부풀렸다 줄어들며 주변 범위 스플래시 데미지, 이후 어지러워함
/// </summary>
public class SlimeInflateSkill : EnemySkill
{
    private EnemySlimeContext slimectx;
    private EnemySlimeDetector slimeDetector; //슬라임의 감지 기능

    private float stunDuration;

    private RigidbodyConstraints2D originConstraints;
    private const float StartTimeout = 0.5f;

    public SlimeInflateSkill(EnemyContext ctx, MonoBehaviour owner, MonsterSlimeData monsterSlimeData) : base(ctx, owner)
    {
        slimectx = (EnemySlimeContext)ctx;
        slimeDetector = (EnemySlimeDetector)ctx.EnemyDetector;

        stunDuration = monsterSlimeData.StunDuration;
        SkillAttackCoolDown = monsterSlimeData.SkillAttackCooldown ;
    }

   // public override float GetCooldown() => 6f; //TODO KJ - Excel

    protected override IEnumerator Execute()
    {
        //추격/기본 공격 이동 정지 + 스킬, 스턴 동안 밀리지 않게 고정
        originConstraints = slimectx.Rigidbody2D.constraints;
        slimectx.Rigidbody2D.linearVelocity = Vector2.zero;
        slimectx.Rigidbody2D.constraints = RigidbodyConstraints2D.FreezeAll;

        //부풀어 오름 = 스플래시 판정
        slimectx.Animator.SetTrigger(EnemyAnimHash.SkillTrigger);
        slimeDetector.Splash();

        //Skill 애니메이션 진입 대기
        float t = 0f;
        while (!IsSkillPlaying())
        {
            t += Time.deltaTime;
            if (t >= StartTimeout)
                break;
            yield return null;
        }

        //Skill 애니메이션 끝날 때까지 대기
        while (IsSkillPlaying() &&
               slimectx.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

        slimectx.Animator.ResetTrigger(EnemyAnimHash.IdleTrigger); //남아 있던 트리거 제거
        slimectx.Animator.SetBool(EnemyAnimHash.IsStun, true);
        slimectx.Animator.Play(EnemyAnimHash.Stun, 0, 0f);

        yield return new WaitForSeconds(stunDuration);

        slimectx.Animator.SetBool(EnemyAnimHash.IsStun, false);
    }

    public override void OnEnd()
    {
        //고정 해제
        slimectx.Rigidbody2D.constraints = originConstraints;
        slimectx.Animator.SetBool(EnemyAnimHash.IsStun, false);
    }

    private bool IsSkillPlaying()
    {
        return slimectx.Animator.GetCurrentAnimatorStateInfo(0).shortNameHash == EnemyAnimHash.Skill;
    }
}

