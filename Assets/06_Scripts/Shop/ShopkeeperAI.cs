using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ShopkeeperAI : MonoBehaviour
{
    public enum ShopkeeperState
    {
        Idle,
        Wander,
        ReturnToShop,
        AttendShop
    }

    [Header("목적지")]
    [SerializeField] private Transform[] wanderPoints;
    [SerializeField] private Transform shopStandPoint;
    [SerializeField] private ShopArea shopArea;

    [Header("이동")]
    [SerializeField, Min(0.01f)] private float walkSpeed = 1.5f;
    [SerializeField, Min(0.01f)] private float runSpeed = 4f;
    [SerializeField, Min(0.01f)] private float arrivalDistance = 0.15f;
    [SerializeField, Min(0.01f)] private float navMeshSampleDistance = 0.5f;
    [SerializeField, Min(0.1f)] private float retryInterval = 1f;
    [SerializeField, Min(0.1f)] private float stuckTimeout = 3f;

    [Header("대기")]
    [SerializeField] private Vector2 idleDuration = new Vector2(1f, 3f);
    [SerializeField, Min(0f)] private float leaveShopDelay = 2f;

    [Header("상점 응대 후")]
    [Tooltip("구매 후 상점을 닫았을 때 Standing을 유지하는 시간 (초). 이후 Idle → 기본 로직")]
    [SerializeField, Min(0f)] private float purchaseStandingTime = 1.5f;
    [Tooltip("구매 없이 상점을 닫았을 때 플레이어를 무시하고 돌아다니는 시간 (초)")]
    [SerializeField, Min(0f)] private float noPurchaseIgnoreTime = 5f;

    [Header("애니메이션")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool spriteDefaultFacesRight = true;

    private static readonly int IdleAnimation = Animator.StringToHash("Squirrel_Idle");
    private static readonly int WalkAnimation = Animator.StringToHash("Squirrel_Walk");
    private static readonly int RunAnimation = Animator.StringToHash("Squirrel_Run");
    private static readonly int StandingAnimation = Animator.StringToHash("Squirrel_Standing");

    private NavMeshAgent agent;
    private NavMeshPath navigationPath;
    private Vector3 destination;
    private Vector3 lastProgressPosition;
    private float idleTimer;
    private float retryTimer;
    private float stuckTimer;
    private float leaveTimer;
    private int lastWanderPoint = -1;
    private int currentAnimation;
    private bool hasDestination;
    private bool warnedAboutNavMesh;
    private bool isServing;
    private float standingTimer;
    private float ignorePlayerTimer;

    public ShopkeeperState CurrentState { get; private set; }
    public bool IsReadyToServe => CurrentState == ShopkeeperState.AttendShop;
    public bool IsServing => isServing;

    public void Configure(Transform[] points, Transform standPoint, ShopArea area)
    {
        wanderPoints = points;
        shopStandPoint = standPoint;
        shopArea = area;
    }

    // 상점 상호작용 중 Standing 자세로 손님을 맞는다. 상점에 도착한 상태(AttendShop)에서만 켤 수 있다.
    public void SetServing(bool serving)
    {
        if (serving && !IsReadyToServe) return;
        standingTimer = 0f;
        if (isServing == serving) return;

        isServing = serving;
        PlayAnimation(isServing ? StandingAnimation : IdleAnimation);
    }

    // 상점 UI가 닫혔을 때 호출.
    // 구매함: Standing을 잠시 유지 → Idle → 기본 로직 (플레이어가 있으면 계속 상점 대기)
    // 구매 안 함: 바로 걸어서 돌아다닌다 (noPurchaseIgnoreTime 동안 플레이어 무시)
    public void FinishServing(bool purchased)
    {
        if (!isServing) return;

        if (purchased && purchaseStandingTime > 0f)
        {
            standingTimer = purchaseStandingTime;
            return;
        }

        isServing = false;
        standingTimer = 0f;
        if (purchased)
        {
            PlayAnimation(IdleAnimation);
            return;
        }

        ignorePlayerTimer = noPurchaseIgnoreTime;
        StopMovement();
        CurrentState = ShopkeeperState.Idle;
        TryBeginWander();
        if (CurrentState != ShopkeeperState.Wander) BeginIdle();
    }

    // 플레이어가 상점에 상호작용했는데 청설모가 자리에 없을 때 호출. 무시 시간을 풀고 바로 복귀시킨다.
    public void RequestService()
    {
        ignorePlayerTimer = 0f;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.enabled = false;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.baseOffset = 0f;
        agent.stoppingDistance = arrivalDistance;
        agent.autoBraking = true;
        navigationPath = new NavMeshPath();

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        retryTimer = 0f;
        ignorePlayerTimer = 0f;
        warnedAboutNavMesh = false;
        currentAnimation = 0;
        BeginIdle();
    }

    private void Start()
    {
        if (shopStandPoint == null || shopArea == null || wanderPoints == null || wanderPoints.Length == 0)
        {
            Debug.LogWarning("ShopkeeperAI: 배회 지점, 상점 복귀 지점, 상점 감지 영역을 연결해 주세요.", this);
        }
    }

    private void Update()
    {
        retryTimer -= Time.deltaTime;
        if (ignorePlayerTimer > 0f) ignorePlayerTimer -= Time.deltaTime;
        bool hasPlayer = shopArea != null && shopArea.HasPlayer && ignorePlayerTimer <= 0f;

        if (hasPlayer && (CurrentState == ShopkeeperState.Idle || CurrentState == ShopkeeperState.Wander))
        {
            StopMovement();
            CurrentState = ShopkeeperState.ReturnToShop;
            retryTimer = 0f;
        }

        if (!EnsureAgentOnNavMesh()) return;

        switch (CurrentState)
        {
            case ShopkeeperState.Idle:
                idleTimer -= Time.deltaTime;
                if (idleTimer <= 0f && retryTimer <= 0f) TryBeginWander();
                break;
            case ShopkeeperState.Wander:
                UpdateTravel(false);
                break;
            case ShopkeeperState.ReturnToShop:
                if (!hasDestination && retryTimer <= 0f)
                {
                    retryTimer = retryInterval;
                    if (shopStandPoint != null) TrySetDestination(shopStandPoint.position, runSpeed);
                }
                UpdateTravel(true);
                break;
            case ShopkeeperState.AttendShop:
                UpdatePurchaseStanding();
                // 손님 응대 중에는 자리를 뜨지 않는다
                leaveTimer = hasPlayer || isServing ? leaveShopDelay : leaveTimer - Time.deltaTime;
                if (!hasPlayer && leaveTimer <= 0f) BeginIdle();
                break;
        }

        UpdateMovementAnimation();
    }

    private void UpdatePurchaseStanding()
    {
        if (!isServing || standingTimer <= 0f) return;

        standingTimer -= Time.deltaTime;
        if (standingTimer > 0f) return;

        isServing = false;
        PlayAnimation(IdleAnimation);
    }

    private bool EnsureAgentOnNavMesh()
    {
        if (agent.enabled && agent.isOnNavMesh) return true;
        if (retryTimer > 0f) return false;

        agent.enabled = false;
        hasDestination = false;
        if (CurrentState == ShopkeeperState.Wander) BeginIdle();
        PlayAnimation(IdleAnimation);
        retryTimer = retryInterval;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSampleDistance, GetQueryFilter()))
        {
            transform.position = hit.position;
            agent.enabled = true;
            agent.Warp(hit.position);
            if (agent.isOnNavMesh)
            {
                warnedAboutNavMesh = false;
                return true;
            }
        }

        if (!warnedAboutNavMesh)
        {
            warnedAboutNavMesh = true;
            Debug.LogWarning("ShopkeeperAI: 현재 위치에 NavMesh가 없습니다. 2D NavMesh를 Bake하고 다람쥐를 이동 가능 영역에 배치해 주세요.", this);
        }

        return false;
    }

    private NavMeshQueryFilter GetQueryFilter()
    {
        return new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
    }

    private void TryBeginWander()
    {
        retryTimer = retryInterval;
        if (wanderPoints == null || wanderPoints.Length == 0) return;

        int firstIndex = Random.Range(0, wanderPoints.Length);
        for (int offset = 0; offset < wanderPoints.Length; offset++)
        {
            int pointIndex = (firstIndex + offset) % wanderPoints.Length;
            Transform point = wanderPoints[pointIndex];
            if (point == null || (wanderPoints.Length > 1 && pointIndex == lastWanderPoint)) continue;
            if (Vector2.Distance(transform.position, point.position) <= arrivalDistance) continue;
            if (!TrySetDestination(point.position, walkSpeed)) continue;

            lastWanderPoint = pointIndex;
            CurrentState = ShopkeeperState.Wander;
            return;
        }
    }

    private bool TrySetDestination(Vector3 position, float speed)
    {
        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, navMeshSampleDistance, GetQueryFilter())) return false;
        if (!agent.CalculatePath(hit.position, navigationPath)
            || navigationPath.status != NavMeshPathStatus.PathComplete) return false;

        agent.speed = speed;
        agent.isStopped = false;
        if (!agent.SetPath(navigationPath))
        {
            agent.isStopped = true;
            return false;
        }

        destination = hit.position;
        hasDestination = true;
        stuckTimer = 0f;
        lastProgressPosition = transform.position;
        return true;
    }

    private void UpdateTravel(bool returningToShop)
    {
        if (!hasDestination || agent.pathPending) return;

        if (Vector2.Distance(transform.position, destination) <= arrivalDistance + 0.02f)
        {
            StopMovement();
            if (returningToShop)
            {
                CurrentState = ShopkeeperState.AttendShop;
                leaveTimer = leaveShopDelay;
            }
            else
            {
                BeginIdle();
            }
            return;
        }

        stuckTimer += Time.deltaTime;
        if (Vector2.Distance(transform.position, lastProgressPosition) > 0.05f)
        {
            lastProgressPosition = transform.position;
            stuckTimer = 0f;
        }

        if (agent.pathStatus == NavMeshPathStatus.PathComplete && agent.hasPath && stuckTimer < stuckTimeout) return;

        StopMovement();
        retryTimer = retryInterval;
        if (!returningToShop) BeginIdle();
    }

    private void BeginIdle()
    {
        isServing = false;
        standingTimer = 0f;
        StopMovement();
        CurrentState = ShopkeeperState.Idle;
        idleTimer = Random.Range(Mathf.Max(0f, idleDuration.x), Mathf.Max(idleDuration.x, idleDuration.y, 0f));
    }

    private void StopMovement()
    {
        hasDestination = false;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        PlayAnimation(IdleAnimation);
    }

    private void UpdateMovementAnimation()
    {
        if (isServing)
        {
            PlayAnimation(StandingAnimation);
            return;
        }

        Vector2 velocity = agent.velocity;
        if (!hasDestination || velocity.sqrMagnitude <= 0.0001f)
        {
            PlayAnimation(IdleAnimation);
            return;
        }

        if (spriteRenderer != null && Mathf.Abs(velocity.x) > 0.0001f)
        {
            spriteRenderer.flipX = (velocity.x > 0f) != spriteDefaultFacesRight;
        }
        PlayAnimation(CurrentState == ShopkeeperState.ReturnToShop ? RunAnimation : WalkAnimation);
    }

    private void PlayAnimation(int animationHash)
    {
        if (animator == null || currentAnimation == animationHash) return;
        animator.Play(animationHash, 0, 0f);
        currentAnimation = animationHash;
    }

    private void OnDisable()
    {
        StopMovement();
        if (agent != null) agent.enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (wanderPoints != null)
        {
            foreach (Transform point in wanderPoints)
            {
                if (point != null) Gizmos.DrawWireSphere(point.position, arrivalDistance);
            }
        }
        Gizmos.color = Color.yellow;
        if (shopStandPoint != null) Gizmos.DrawWireSphere(shopStandPoint.position, arrivalDistance);
    }
}
