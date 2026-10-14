using Pathfinding;
using UnityEngine;

/// <summary>
/// 던전 길찾기 그리드 - 맵 이동 시 현재 맵 범위로 그리드를 옮기고 다시 스캔
/// 비활성 맵의 콜라이더는 물리에서 제외되므로 맵에 들어갈 때마다 스캔 필요
/// </summary>
[RequireComponent(typeof(AstarPath))]
public class DungeonPathGrid : MonoBehaviour
{
    [SerializeField, Tooltip("칸 크기 - 적 몸 크기 정도")]
    private float nodeSize = 0.5f;
    [SerializeField, Tooltip("적 몸 지름 ( 칸 단위, 1 = 칸 하나 ) - 가장 큰 적의 WallBody 지름 / nodeSize 이상. 적이 벽 모서리에 걸리면 키우기")]
    private float diameter = 2.6f;
    [SerializeField, Tooltip("벽/구조물 레이어")]
    private LayerMask obstacleMask;
    [SerializeField, Tooltip("맵 범위 바깥 여유 거리")]
    private float margin = 1f;

    /// <summary>
    /// 맵 범위로 그리드 이동 후 스캔
    /// </summary>
    public void ScanMap(DungeonMap map)
    {
        if (map == null || AstarPath.active == null)
            return;

        if (!TryGetMapBounds(map, out Bounds bounds))
        {
            Debug.LogWarning($"[DungeonPathGrid] {map.MapID} 벽/CamBound 없음 - 스캔 생략");
            return;
        }

        GridGraph grid = AstarPath.active.data.gridGraph;
        if (grid == null)
        {
            Debug.LogError("[DungeonPathGrid] Pathfinder 에 Grid Graph 없음");
            return;
        }

        //2D 설정 - 인스펙터 값과 상관없이 코드 기준으로 고정
        grid.is2D = true;
        grid.collision.use2D = true;
        grid.collision.collisionCheck = true;
        grid.collision.heightCheck = false;
        grid.collision.type = ColliderType.Sphere;
        grid.collision.diameter = diameter;
        grid.collision.mask = obstacleMask;

        grid.center = bounds.center;
        int width = Mathf.CeilToInt(bounds.size.x / nodeSize);
        int depth = Mathf.CeilToInt(bounds.size.y / nodeSize);
        grid.SetDimensions(width, depth, nodeSize);

        AstarPath.active.Scan(grid);
    }

    /// <summary>
    /// 맵 범위 = 맵의 벽 콜라이더 + 맵 자식인 CamBound 를 합친 영역
    /// </summary>
    private bool TryGetMapBounds(DungeonMap map, out Bounds bounds)
    {
        bounds = default;
        bool found = false;

        foreach (Collider2D col in map.GetComponentsInChildren<Collider2D>())
        {
            if (col.isTrigger || ((1 << col.gameObject.layer) & obstacleMask) == 0)
                continue;

            Encapsulate(ref bounds, ref found, col.bounds);
        }

        //다른 맵의 CamBound 를 참조하는 경우 제외
        if (map.CamBound != null && map.CamBound.transform.IsChildOf(map.transform))
        {
            Encapsulate(ref bounds, ref found, map.CamBound.bounds);
        }

        if (found)
        {
            bounds.Expand(margin * 2f);
        }

        return found;
    }

    private static void Encapsulate(ref Bounds bounds, ref bool found, Bounds add)
    {
        if (!found)
        {
            bounds = add;
            found = true;
            return;
        }

        bounds.Encapsulate(add);
    }
}
