using UnityEngine;

public class EnemySkeletonDetector : EnemyDetector
{
    #region 스켈레톤 범위 - 기본 공격 범위 및 스킬 전방 베기 데미지 범위
    [Header("스켈레톤 스킬 공격 /스플래시 범위 - 노란색 호")]
    [SerializeField]
    private float splashRadius = 1.5f;
    [SerializeField, Range(0f, 360f)]
    private float splashAngle = 120f; //바라보는 방향 기준 전체 각도

    public float SplashRadius => splashRadius;
    [Header("스켈레톤 기본 공격  /기본 공격 범위 - 파란색 원")]
    [SerializeField]
    private float baseAttackRadius;

    public float BaseAttackRadius => baseAttackRadius;
    #endregion

    //적 공통 데이터 - 공격력
    private float ATK;

    //바라보는 방향 판별용 - 스프라이트 기본 왼쪽, flipX면 오른쪽
    private SpriteRenderer spriteRenderer;

    protected override void Awake()
    {
        base.Awake();

        EnemyBase enemyBase = GetComponentInParent<EnemyBase>();
        ATK = enemyBase.MonsterData.ATK;
        spriteRenderer = enemyBase.GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>
    /// 현재 바라보는 방향
    /// </summary>
    private Vector2 FacingDirection
    {
        get
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInParent<EnemyBase>()?.GetComponentInChildren<SpriteRenderer>();

            return (spriteRenderer != null && spriteRenderer.flipX) ? Vector2.right : Vector2.left;
        }
    }

    /// <summary>
    /// 호 범위 안의 플레이어 찾기 - 원으로 먼저 거르고 각도로 한번 더 거름
    /// </summary>
    private PlayerVitals FindPlayerInArc(float radius, float angle)
    {
        colliders.Clear();

        int count = Physics2D.OverlapCircle(transform.position, radius, contactFilter2D, colliders);

        Vector2 origin = transform.position;
        Vector2 facing = FacingDirection;

        for (int i = 0; i < count; i++)
        {
            //콜라이더 중심이 아닌 가장 가까운 점 기준 - 호 가장자리에 걸친 경우도 판정
            Vector2 toTarget = colliders[i].ClosestPoint(origin) - origin;

            if (toTarget.sqrMagnitude > 0.0001f && Vector2.Angle(facing, toTarget) > angle * 0.5f)
                continue;

            PlayerVitals vitals = colliders[i].GetComponentInParent<PlayerVitals>();
            if (vitals != null)
                return vitals;
        }

        return null;
    }

    /// <summary>
    /// 스켈레톤 기본 공격
    /// </summary>
    public bool BaseAttack()
    {
        colliders.Clear();

        int count = Physics2D.OverlapCircle(transform.position, baseAttackRadius, contactFilter2D, colliders);

        if (count > 0)
        {
            PlayerVitals vitals = colliders[0].GetComponentInParent<PlayerVitals>();
            if (vitals != null)
            {
                vitals.TakeDamage(ATK);
                Debug.Log("플레이어에게 기본 공격 피해를 입힘!");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 스켈레톤 스킬 공격
    /// </summary>
    public bool SpinningSlash()
    {
        PlayerVitals vitals = FindPlayerInArc(splashRadius, splashAngle);

        if (vitals != null)
        {
            vitals.TakeDamage(ATK);
            Debug.Log("플레이어에게 스킬 공격 피해를 입힘!");
            return true;
        }

        return false;

    }


    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        #region 스켈레톤
        Gizmos.color = Color.yellow;
        DrawArcGizmo(splashRadius, splashAngle);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, baseAttackRadius);
        #endregion
    }

    /// <summary>
    /// 호 범위 기즈모 - 양 끝 선 + 호
    /// </summary>
    private void DrawArcGizmo(float radius, float angle)
    {
        const int segments = 24;

        Vector3 origin = transform.position;
        float baseAngle = Mathf.Atan2(FacingDirection.y, FacingDirection.x) * Mathf.Rad2Deg;
        float startAngle = baseAngle - angle * 0.5f;
        float step = angle / segments;

        Vector3 prev = origin + (Vector3)(Quaternion.Euler(0f, 0f, startAngle) * Vector3.right) * radius;
        Gizmos.DrawLine(origin, prev);

        for (int i = 1; i <= segments; i++)
        {
            Vector3 next = origin + (Vector3)(Quaternion.Euler(0f, 0f, startAngle + step * i) * Vector3.right) * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        Gizmos.DrawLine(prev, origin);
    }
}
