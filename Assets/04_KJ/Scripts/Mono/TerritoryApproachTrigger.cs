using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 구역을 해금하려고 했을 때, 해당 구역이 플레이어 기준 상/하/좌/우 중 어디에 위치한지
/// </summary>
public enum TerrirotyDirection { Up, Down, Left, Right }

/// <summary>
/// 구역 전용 - 플레이어 감지 기능 (개별 구역에 부착)
/// 감지 기능 : 화살표 생성 + 카메라 연출
/// </summary>
public class TerritoryApproachTrigger : MonoBehaviour
{
    private TerritoryZone currentZone;

    private ContactFilter2D contactFilter2D;
    private List<Collider2D> colliders = new List<Collider2D>();
   
    [SerializeField]
    private float radius;
    [SerializeField]
    Vector2 offset;

    private bool isColliderApproach; //구역의 접근 여부

    /// <summary>
    /// 구역 접근 설정. fase : 구역 접근 가능, true : 구역 접근 불가
    /// </summary>
    /// <param name="isApproach"></param>
    public void SetApproach(bool isApproach)
    {
        isColliderApproach = isApproach;
    }
    private void Awake()
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
        colliders.Clear();

        int colliderCount =  Physics2D.OverlapCircle(transform.position + (Vector3)offset, radius, contactFilter2D, colliders);

        bool isNowColliding = colliderCount > 0;

        if (isNowColliding && !isColliderApproach)
        {
            TerrirotyDirection dir = GetDirectionFrom(colliders[colliderCount - 1].transform.position);
            currentZone.ApproachZone(dir);
            isColliderApproach = true;
        }
        else if (!isNowColliding && isColliderApproach)
        {
            currentZone.LeaveZone(); 
            isColliderApproach = false;
        }

    }
    private TerrirotyDirection GetDirectionFrom(Vector2 playerPos)
    {
        Vector2 zonePos = (Vector2)transform.position + offset;

        Vector2 delta = zonePos - playerPos; // 구역 위치 - 플레이어 위치
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return delta.x > 0 ? TerrirotyDirection.Right : TerrirotyDirection.Left;
        else
            return delta.y > 0 ? TerrirotyDirection.Up : TerrirotyDirection.Down;
    }
    private void OnDrawGizmos()
    {
        if (currentZone == null) return;
        if (!currentZone.IsLocked) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + (Vector3)offset, radius);
    }
}
