using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 구역 전용 - 플레이어 감지 기능 (개별 구역에 부착)
/// 감지 기능 : 화살표 생성 + 카메라 연출
/// </summary>
public class TerritoryApproachTrigger : MonoBehaviour
{
    private TerritoryZone currentZone;

    private ContactFilter2D contactFilter2D;
    private List<Collider2D> colliders;
   
    [SerializeField]
    private float radius;
    [SerializeField]
    Vector2 offset;

    private bool isCollider;
    private void Start()
    {
        currentZone = GetComponent<TerritoryZone>();

        contactFilter2D.layerMask = LayerMask.GetMask("Player");
        contactFilter2D.useLayerMask = true;

    }

    private void Update()
    {
        if (!currentZone.IsLocked) return;

        TerritoryDetection();
    }

    private void TerritoryDetection()
    {

        int colliderCount =  Physics2D.OverlapCircle(transform.position + (Vector3)offset, radius, contactFilter2D, colliders);

        if (colliderCount >= 1 && !isCollider)
        {
            currentZone.ApproachZone();
            isCollider = true;
        }
    }

    private void OnDrawGizmos()
    {
        if (currentZone == null) return;
        if (!currentZone.IsLocked) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + (Vector3)offset, radius);
    }
}
