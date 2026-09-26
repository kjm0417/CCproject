using UnityEngine;
using static Unity.Cinemachine.IInputAxisOwner.AxisDescriptor;

public class EnemySlimeDetector : EnemyDetector
{
    #region 슬라임 범위 - 스킬 스플래시 데미지 범위
    [Header("슬라임 스킬 /스플래시 범위 - 노란색 원")]
    [SerializeField]
    private float splashRadius = 1.5f;

    public float SplashRadius => splashRadius;
    [Header("슬라임 기본 공격  /기본 공격 범위 - 파란색 원")]
    [SerializeField]
    private float baseAttackRadius;

    public float BaseAttackRadius => baseAttackRadius;
    #endregion

    //적 공통 데이터 - 공격력
    private float ATK;

    protected override void Awake()
    {
        base.Awake();

        ATK = GetComponentInParent<EnemyBase>().MonsterData.ATK;
    }

    /// <summary>
    /// 슬라임 기본 공격
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
    /// 슬라임 스킬 공격
    /// </summary>
    public void Splash()
    {
        colliders.Clear();

        int count = Physics2D.OverlapCircle(transform.position,splashRadius, contactFilter2D, colliders);

        if(count > 0)
        {
            PlayerVitals vitals = colliders[0].GetComponentInParent<PlayerVitals>();
            if (vitals != null)
            {
                vitals.TakeDamage(ATK);
                Debug.Log("플레이어에게 스킬 공격 피해를 입힘!");
            }
        }
        
    }


    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        #region 슬라임
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, splashRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, baseAttackRadius);
        #endregion
    }
}
