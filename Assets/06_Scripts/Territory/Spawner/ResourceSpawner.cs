using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 구역이 해금됐을 때, 정해진 자원 목록(TerritoryZoneData.ResourceSpawnTable)대로 자원 오브젝트를
/// 타일 영역안 빈 칸에 랜덤 배치하고, 파괴되면 정해진 시간 뒤 다시 채워 넣는 역할을 하는 컴포넌트.
/// </summary>
public class ResourceSpawner : MonoBehaviour
{
    private class SpawnedInfo
    {
        public GameObject Instance;
        public ResourceSpawnEntry Entry;
    }

    [SerializeField]
    private Tilemap territoryTileMap;

    private Dictionary<Vector3Int, GameObject> occupiedCells = new Dictionary<Vector3Int, GameObject>();

    private void Awake()
    {
        territoryTileMap.CompressBounds();

        BoundsInt bounds = territoryTileMap.cellBounds;
      
    }
    /// <summary>
    /// Ground Tilemap 전체 영역의 모든 칸 좌표를 뽑아서, "여기에 자원 스폰해도 되는 칸들"의 목록으로 돌려주는 메서드.
    /// </summary>
    /// <returns></returns>
    private List<Vector3Int> GetVaildSpawnCells()
    {
        BoundsInt bounds = territoryTileMap.cellBounds;
        Debug.Log($"Ground bounds min: {bounds.min}, max: {bounds.max}, size: {bounds.size}");

        List<Vector3Int> cellList = new List<Vector3Int>();
        foreach (var pos in bounds.allPositionsWithin)
        {
            cellList.Add(pos);
        }

        return cellList;
    }

    /// <summary>
    /// 구역이 해금된 직후 한 번 호출되는, 실제로 자원을 배치하는 메서드
    /// </summary>
    public void SpawnInitial()
    {
        List<ResourceSpawnEntry> table = GetComponent<TerritoryZone>().TerritoryZoneData.ResourceSpawnTable;
        List<Vector3Int> availableCells = GetVaildSpawnCells();

        foreach (ResourceSpawnEntry entry in table)
        {
            for (int i = 0; i < entry.MaxCount; i++)
            {
                if (availableCells.Count == 0) break; // 저번에 얘기한 안전장치

                int index =  Random.Range(0, availableCells.Count);

                Vector3Int pickedCell = availableCells[index]; //리스트에서 index 번째 자리에 있는 좌표값 = 우리가 사용할 자원의 스폰 위치

                Vector3 spawnPos = territoryTileMap.GetCellCenterWorld(pickedCell);
                //GetCellCenterWorld(pickedCell) :  좌표값(칸)에 정중앙에 해당하는 실제
                //월드 좌표로 바꿔주는 함수

                GameObject resourceObj =  Instantiate(entry.ResourcePrefab, spawnPos, Quaternion.identity);

                availableCells.Remove(pickedCell);
                //이건 지금 이메서드가 한 번 실행되는 동안만 의미 있는 거임. 나무 5개, 돌 3개를
                //연달아 스폰하는데, 방금 나무 하나 놓은 칸을 그 다음 돌 스폰할 때 또 뽑으면 같은 칸에 두 개가 겹쳐 놓임.그거 막으려고 뽑은
                //즉시 후보 목록에서 빼는 거임

                occupiedCells.Add(pickedCell, resourceObj);
                // 나중에 플레이어가 그 나무를캐서 파괴하면, "이 칸이 이제 다시 비었다"는 걸 알아야 새 나무를 리스폰시킬 수 있음.
                // 근데 어떤 오브젝트가 파괴됐을 때 "얘가 어느 칸에 있던 놈이었지?"를 알려면, 미리 "이 칸 = 이 오브젝트"라고 기록해둔 게 있어야 함
            }
        }
    }
}
