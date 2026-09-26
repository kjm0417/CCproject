using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 공통 - 플레이어 감지
/// </summary>
public abstract class EnemyDetector : MonoBehaviour
{
    protected ContactFilter2D contactFilter2D;
    protected List<Collider2D> colliders = new();

    protected virtual void Awake()
    {
        contactFilter2D.useLayerMask = true;
        contactFilter2D.layerMask = LayerMask.GetMask("Player");
    }

    #region 추격 감지 관련 - 추격 상태 전이 조건
    [Header("추격 감지 관련 - 초록색 선")]
    [SerializeField]
    private float chargeRadius;

    public PlayerMovement chargeToPlayer { get; private set; } //추격 당하는 플레이어
    /// <summary>
    /// 추격 감지 여부
    /// </summary>
    /// <returns></returns>
    public bool IsCharge()
    {
        colliders.Clear();

        int count = Physics2D.OverlapCircle(transform.position, chargeRadius, contactFilter2D, colliders);

        if(count > 0)
        {
            chargeToPlayer = colliders[0].GetComponent<PlayerMovement>();
            return true;
        }

        chargeToPlayer = null;
        return false;
    }
    #endregion
    #region 공격 감지 관련 - 공격 상태 전이 조건
    [Header("공격 감지 관련 - 빨간색 선")]
    [SerializeField]
    private float attackRadius;

    public PlayerMovement attackToPlayer { get; private set; } //적이 공격하려고 하는 플레이어
    /// <summary>
    /// 추격 감지 여부
    /// </summary>
    /// <returns></returns>
    public bool IsAttack()
    {
        colliders.Clear();

        int count = Physics2D.OverlapCircle(transform.position, attackRadius, contactFilter2D, colliders);

        if (count > 0)
        {
            attackToPlayer = colliders[0].GetComponent<PlayerMovement>();
            return true;
        }

        attackToPlayer = null;
        return false;
    }
    #endregion
    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, chargeRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}
