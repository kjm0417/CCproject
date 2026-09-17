using DG.Tweening.Core.Easing;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEngine.EventSystems.EventTrigger;

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

    private TerritoryZone territoryZone;

    private Dictionary<Vector3Int, GameObject> occupiedCells = new Dictionary<Vector3Int, GameObject>();
    private Dictionary<GameObject, List<Vector3Int>> occupiedCellsByObject = new Dictionary<GameObject, List<Vector3Int>>();

    private void Awake()
    {
        territoryZone = GetComponent<TerritoryZone>();

        territoryZone.TerritoryTileMapGround.CompressBounds();

        BoundsInt bounds = territoryZone.TerritoryTileMapGround.cellBounds;
      
    }

    /// <summary>
    /// Ground Tilemap 전체 영역의 모든 칸 좌표를 뽑아서, "여기에 자원 스폰해도 되는 칸들"의 목록으로 돌려주는 메서드.
    /// </summary>
    /// <returns></returns>
    private List<Vector3Int> GetVaildSpawnCells()
    {
        BoundsInt bounds = territoryZone.TerritoryTileMapGround.cellBounds;
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
        List<ResourceSpawnEntry> table = territoryZone.TerritoryZoneData.ResourceSpawnTable;

        List<Vector3Int> availableCells = GetVaildSpawnCells();

        foreach (ResourceSpawnEntry entry in table)
        {
            Debug.Log($"Spawning {entry.ResourcePrefab.name}, GridWidth: {entry.GridWidth}, GridHeight: {entry.GridHeight}, MaxCount: {entry.MaxCount}");

            for (int i = 0; i < entry.MaxCount; i++)
            {
                if (availableCells.Count == 0) break; // 저번에 얘기한 안전장치

                int index =  Random.Range(0, availableCells.Count);

                Vector3Int pickedCell = availableCells[index]; //리스트에서 index 번째 자리에 있는 좌표값 = 우리가 사용할 자원의 스폰 위치

                if (!CanSpawnAt(pickedCell, entry.GridWidth, entry.GridHeight))
                {
                    availableCells.RemoveAt(index);
                    i--; // 이 반복 다시 시도
                    continue;
                }

                Vector3 spawnPos = territoryZone.TerritoryTileMapGround.GetCellCenterWorld(pickedCell);
                //GetCellCenterWorld(pickedCell) :  좌표값(칸)에 정중앙에 해당하는 실제
                //월드 좌표로 바꿔주는 함수

                if (entry.GridHeight > 1)
                {
                    Vector3 topCellCenter = territoryZone.TerritoryTileMapGround.GetCellCenterWorld(
                        pickedCell + new Vector3Int(0, entry.GridHeight - 1, 0)
                    );
                    spawnPos = (spawnPos + topCellCenter) / 2f;
                }

                GameObject resourceObj =  Instantiate(entry.ResourcePrefab, spawnPos, Quaternion.identity);

                var damageable = resourceObj.GetComponent<IRespawnProvier>();
                if (damageable != null)
                {
                    damageable.InfoResource(entry);
                    damageable.OnDestroyed += OnResourceDestroyed; //자원 재생성 이벤트 구독
                }

                var cellsList = new List<Vector3Int>();
                occupiedCells.Add(pickedCell, resourceObj);
                cellsList.Add(pickedCell);
                availableCells.Remove(pickedCell);

                for (int x = 0; x < entry.GridWidth; x++)
                {
                    for (int y = 0; y < entry.GridHeight; y++)
                    {
                        if (x == 0 && y == 0) continue; //이미 pickedCell 등록됨

                        Vector3Int adjacentCell = pickedCell + new Vector3Int(x, y, 0);

                        if (!occupiedCells.ContainsKey(adjacentCell))
                        {
                            occupiedCells.Add(adjacentCell, resourceObj);
                            cellsList.Add(adjacentCell);
                            availableCells.Remove(adjacentCell);
                        }
                    }
                }

                occupiedCellsByObject.Add(resourceObj, cellsList);
            }
        }
    }

    /// <summary>
    /// 자원 재생성
    /// </summary>
    /// <param name="resourceObj"></param>
    public void RespawnResource(ResourceSpawnEntry entry)
    {
        List<Vector3Int> availableCells = GetVaildSpawnCells();

        while (availableCells.Count > 0)
        {
            int index = Random.Range(0, availableCells.Count);
            Vector3Int pickedCell = availableCells[index];

            if (CanSpawnAt(pickedCell, entry.GridWidth, entry.GridHeight))
            {
                // 리스폰 실행 (SpawnInitial처럼)
                Vector3 spawnPos = territoryZone.TerritoryTileMapGround.GetCellCenterWorld(pickedCell);

                if (entry.GridHeight > 1)
                {
                    Vector3 topCellCenter = territoryZone.TerritoryTileMapGround.GetCellCenterWorld(
                        pickedCell + new Vector3Int(0, entry.GridHeight - 1, 0)
                    );
                    spawnPos = (spawnPos + topCellCenter) / 2f;
                }

                GameObject resourceObj = Instantiate(entry.ResourcePrefab, spawnPos, Quaternion.identity);

                var respawnProvider = resourceObj.GetComponent<IRespawnProvier>();
                if (respawnProvider != null)
                {
                    respawnProvider.InfoResource(entry);
                    respawnProvider.OnDestroyed += OnResourceDestroyed;
                }

                var cellsList = new List<Vector3Int>();
                occupiedCells.Add(pickedCell, resourceObj);
                cellsList.Add(pickedCell);

                for (int x = 0; x < entry.GridWidth; x++)
                {
                    for (int y = 0; y < entry.GridHeight; y++)
                    {
                        if (x == 0 && y == 0) continue;

                        Vector3Int adjacentCell = pickedCell + new Vector3Int(x, y, 0);

                        if (!occupiedCells.ContainsKey(adjacentCell))
                        {
                            occupiedCells.Add(adjacentCell, resourceObj);
                            cellsList.Add(adjacentCell);
                        }
                    }
                }

                occupiedCellsByObject.Add(resourceObj, cellsList);
                return;
            }

            availableCells.RemoveAt(index); // 이 위치는 안 되니까 제외
        }
    }


    /// <summary>
    /// 리스폰할 때, 리스폰 위치에 자원이 있는지 없는지 거르는 함수
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="gridWidth"></param>
    /// <param name="gridHeight"></param>
    /// <returns></returns>
    private bool CanSpawnAt(Vector3Int cell, int gridWidth, int gridHeight)
    {
        BoundsInt bounds = territoryZone.TerritoryTileMapGround.cellBounds;

        //이 위치의 Grid 범위 모두 비어있나?
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3Int checkCell = cell + new Vector3Int(x, y, 0);

                // bounds 밖이면 불가
                if (!bounds.Contains(checkCell))
                    return false;

                // 이미 점유됐으면 불가
                if (occupiedCells.ContainsKey(checkCell))
                    return false;
            }
        }
        return true;
    }

    void OnResourceDestroyed(GameObject destroyedObj,ResourceSpawnEntry entry, float respawnTime)
    {
        if (occupiedCellsByObject.TryGetValue(destroyedObj, out var cells))
        {
            foreach (var cell in cells)
                occupiedCells.Remove(cell);

            occupiedCellsByObject.Remove(destroyedObj);
        }
        // occupiedCells에서 제거
        StartCoroutine(RespawnAfterDelay(entry, respawnTime));
    }

    /// <summary>
    /// 일정 시간 후 리스폰
    /// </summary>
    /// <param name="entry"></param>
    /// <param name="delay"></param>
    /// <returns></returns>
    IEnumerator RespawnAfterDelay(ResourceSpawnEntry entry, float delay)
    {
        yield return new WaitForSeconds(delay);
        RespawnResource(entry);
    }
}
