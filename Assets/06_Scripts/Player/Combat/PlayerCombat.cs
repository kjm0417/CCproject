using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour, IPlayerComponent
{
    [Header("공격 판정")]
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField, Range(0f, 360f)] private float attackAngle = 80f;

    [Header("타겟 감지")]
    [SerializeField, Min(0.02f)] private float targetScanInterval = 0.1f;

   

    private PlayerContext context;
    private float nextAttackTime;
    private float targetScanTimer;
    private bool hasTargetInAttackArea;
    private ContactFilter2D targetFilter;
    private readonly List<Collider2D> targetColliders = new List<Collider2D>();
    private readonly HashSet<IEnemyDamageable> damagedTargets = new HashSet<IEnemyDamageable>();

    public event Action<bool> OnTargetAvailabilityChanged;
    public bool HasTarget => hasTargetInAttackArea;

    public void Initialize(PlayerContext playerContext)
    {
        context = playerContext;

        if (attackOrigin == null) attackOrigin = transform;

        targetFilter = new ContactFilter2D();
        targetFilter.SetLayerMask(targetLayers);
        targetFilter.useTriggers = true;

        RefreshTargetAvailability(true);
    }

    private void Update()
    {
        if (context == null) return;

        targetScanTimer -= Time.deltaTime;
        if (targetScanTimer > 0f) return;

        targetScanTimer = targetScanInterval;
        RefreshTargetAvailability(false);
    }

    public bool TryAttack()
    {
        if (context == null || Time.time < nextAttackTime) return false;

        nextAttackTime = Time.time + context.BaseData.AttackCooldown;

        Vector2 origin = attackOrigin.position;
        Vector2 facing = GetAttackFacingDirection();
        float range = context.Stats.AttackRange;
        int damage = Mathf.RoundToInt(context.Stats.AttackPower);

        targetColliders.Clear();
        int hitCount = Physics2D.OverlapCircle(origin, range, targetFilter, targetColliders);

        damagedTargets.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = targetColliders[i];
            if (!TryGetLivingTarget(hit, out IEnemyDamageable target)) continue;
            if (!damagedTargets.Add(target)) continue;
            if (!IsInsideAttackArea(hit, origin, facing)) continue;

            target.TakeDamage(damage);
        }

        RefreshTargetAvailability(false);
        return true;
    }

    public bool HasTargetInAttackArea()
    {
        if (context == null || context.Stats == null || context.Movement == null) return false;

        Vector2 origin = attackOrigin != null ? attackOrigin.position : transform.position;
        Vector2 facing = GetAttackFacingDirection();
        float range = context.Stats.AttackRange;
        targetColliders.Clear();
        int hitCount = Physics2D.OverlapCircle(origin, range, targetFilter, targetColliders);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = targetColliders[i];
            if (!TryGetLivingTarget(hit, out _)) continue;
            if (IsInsideAttackArea(hit, origin, facing)) return true;
        }

        return false;
    }

    private void RefreshTargetAvailability(bool forceNotify)
    {
        bool hasTarget = HasTargetInAttackArea();
        if (!forceNotify && hasTarget == hasTargetInAttackArea) return;

        hasTargetInAttackArea = hasTarget;
        OnTargetAvailabilityChanged?.Invoke(hasTargetInAttackArea);
    }

    private bool TryGetLivingTarget(Collider2D targetCollider, out IEnemyDamageable target)
    {
        EnemyBase enemy = targetCollider.GetComponentInParent<EnemyBase>();
        if (enemy != null)
        {
            target = enemy;
            return !enemy.IsDie;
        }

        target = targetCollider.GetComponentInParent<IEnemyDamageable>();
        return target != null;
    }

    private Vector2 GetAttackFacingDirection()
    {
        if (context.Animation != null)
        {
            return context.Animation.FacingRight ? Vector2.right : Vector2.left;
        }

        return context.Movement != null && context.Movement.FacingDirection.x < 0f
            ? Vector2.left
            : Vector2.right;
    }

    private bool IsInsideAttackArea(Collider2D targetCollider, Vector2 origin, Vector2 facing)
    {
        Vector2 hitPosition = targetCollider.ClosestPoint(origin);
        Vector2 direction = hitPosition - origin;

        return direction.sqrMagnitude <= 0.0001f
            || Vector2.Angle(facing, direction) <= attackAngle * 0.5f;
    }



    [Header("공격 범위 표시")]
    [SerializeField] private bool showAttackRangeGizmo = true;
    [SerializeField, Range(3, 64)] private int gizmoSegments = 24;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.25f, 0.1f, 0.8f);
    private void OnDrawGizmos()
    {
        if (!showAttackRangeGizmo) return;

        Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;
        Vector2 facing = Vector2.right;
        float range = 2f;

        if (Application.isPlaying && context != null)
        {
            facing = GetAttackFacingDirection();
            if (context.Stats != null) range = context.Stats.AttackRange;
        }
        else
        {
            PlayerContext playerContext = GetComponent<PlayerContext>();
            if (playerContext != null && playerContext.BaseData != null)
            {
                range = playerContext.BaseData.AttackRange;
            }
        }

        int segmentCount = Mathf.Max(3, gizmoSegments);
        float startAngle = -attackAngle * 0.5f;
        Vector3 facingDirection = new Vector3(facing.x, facing.y, 0f).normalized;
        Vector3 previousPoint = origin
            + Quaternion.Euler(0f, 0f, startAngle) * facingDirection * range;

        Color previousColor = Gizmos.color;
        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(origin, previousPoint);

        for (int i = 1; i <= segmentCount; i++)
        {
            float angle = Mathf.Lerp(startAngle, attackAngle * 0.5f, i / (float)segmentCount);
            Vector3 nextPoint = origin
                + Quaternion.Euler(0f, 0f, angle) * facingDirection * range;
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }

        Gizmos.DrawLine(origin, previousPoint);
        Gizmos.color = previousColor;
    }
}
