using UnityEngine;

/// <summary>
/// 스켈레톤 스킬 이펙트 - 이펙트에 닿은 플레이어 피격
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SkeletonSkillFX : MonoBehaviour
{
    [Tooltip("이펙트 유지 시간")]
    [SerializeField]
    private float lifeTime = 0.3f;

    private float damage;
    private bool hasHit = false; //한 번만 피격

    private int playerLayer;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        playerLayer = LayerMask.NameToLayer("Player");
    }

    /// <summary>
    /// 이펙트 초기화 - 데미지, 바라보는 방향
    /// </summary>
    public void Init(float damage, Vector2 facing)
    {
        this.damage = damage;

        //이펙트 원본은 왼쪽 기준 - 오른쪽이면 콜라이더까지 같이 좌우 반전
        if (facing.x > 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit)
            return;

        //플레이어 본체만 - 줍기 범위 등 다른 콜라이더 제외
        if (other.gameObject.layer != playerLayer)
            return;

        PlayerVitals vitals = other.GetComponentInParent<PlayerVitals>();
        if (vitals == null)
            return;

        hasHit = true;
        vitals.TakeDamage(damage);
        Debug.Log("플레이어가 스켈레톤 스킬 이펙트에 맞음!");
    }
}
