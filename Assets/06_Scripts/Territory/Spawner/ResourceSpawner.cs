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

    // 재생성 예정 자원: (entry, 시작시간, 대기시간)
    private List<(ResourceSpawnEntry entry, float startTime, float delay)> respawningResources = new List<(ResourceSpawnEntry, float, float)>();

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
    /// 해당 칸에 자원이 올라가 있는지 확인
    /// </summary>
    public bool IsOccupied(Vector3Int cell)
    {
        return occupiedCells.ContainsKey(cell);
    }

    /// <summary>
    /// 현재 스폰된 모든 오브젝트 반환
    /// </summary>
    public List<GameObject> GetSpawnedObjects()
    {
        return new List<GameObject>(occupiedCells.Values);
    }

    /// <summary>
    /// 자원 생성 및 위치 설정
    /// </summary>
    private GameObject CreateResourceAtPosition(ResourceSpawnEntry entry, Vector3 worldPosition, bool applyOffset = true)
    {
        GameObject resourceObj = Instantiate(entry.ResourcePrefab, Vector3.zero, Quaternion.identity);

        GridCenterPoint centerPoint = resourceObj.GetComponent<GridCenterPoint>();
        if (centerPoint != null && applyOffset)
        {
            resourceObj.transform.position = worldPosition + centerPoint.offset;
        }
        else
        {
            resourceObj.transform.position = worldPosition;
        }

        return resourceObj;
    }

    /// <summary>
    /// 자원 구독 및 셀 등록
    /// </summary>
    private void RegisterResource(GameObject resourceObj, ResourceSpawnEntry entry, Vector3Int cellPos)
    {
        var provider = resourceObj.GetComponent<IRespawnProvier>();
        if (provider != null)
        {
            provider.InfoResource(entry);
            provider.OnDestroyed += OnResourceDestroyed;
        }

        var cellsList = new List<Vector3Int> { cellPos };
        occupiedCells.Add(cellPos, resourceObj);

        BoundsInt bounds = territoryZone.TerritoryTileMapGround.cellBounds;
        for (int x = 0; x < entry.GridWidth; x++)
        {
            for (int y = 0; y < entry.GridHeight; y++)
            {
                if (x == 0 && y == 0) continue;

                Vector3Int adjacentCell = cellPos + new Vector3Int(x, y, 0);
                if (!bounds.Contains(adjacentCell)) continue;
                if (!occupiedCells.ContainsKey(adjacentCell))
                {
                    occupiedCells.Add(adjacentCell, resourceObj);
                    cellsList.Add(adjacentCell);
                }
            }
        }

        occupiedCellsByObject.Add(resourceObj, cellsList);
    }

    /// <summary>
    /// 구역이 해금된 직후 한 번 호출되는, 실제로 자원을 배치하는 메서드
    /// </summary>
    public void SpawnInitial()
    {
        List<ResourceSpawnEntry> table = territoryZone.TerritoryZoneData.ResourceSpawnTable;

        List<Vector3Int> availableCells = GetVaildSpawnCells();

        // 추가: StructSlots 생성
        if (territoryZone.TerritoryZoneData.StructSlots.Count > 0)
        {
            foreach (var structSlot in territoryZone.TerritoryZoneData.StructSlots)
            {
                Instantiate(structSlot.StructPrefab, structSlot.StructSpawnPos, Quaternion.identity);
            }
        }

        // 추가: NpcSlots 생성
        if (territoryZone.TerritoryZoneData.NpcSlots.Count > 0)
        {
            foreach (var npcSlot in territoryZone.TerritoryZoneData.NpcSlots)
            {
                // NpcSlotData 구조 확인 필요
                Instantiate(npcSlot.NpcPrefab, npcSlot.NpcSpawnPos, Quaternion.identity);
            }
        }

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
                GameObject resourceObj = CreateResourceAtPosition(entry, spawnPos);
                RegisterResource(resourceObj, entry, pickedCell);

                availableCells.Remove(pickedCell);
                for (int x = 0; x < entry.GridWidth; x++)
                {
                    for (int y = 0; y < entry.GridHeight; y++)
                    {
                        if (x == 0 && y == 0) continue;
                        Vector3Int adjacentCell = pickedCell + new Vector3Int(x, y, 0);
                        availableCells.Remove(adjacentCell);
                    }
                }
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
                Vector3 spawnPos = territoryZone.TerritoryTileMapGround.GetCellCenterWorld(pickedCell);
                GameObject resourceObj = CreateResourceAtPosition(entry, spawnPos);
                RegisterResource(resourceObj, entry, pickedCell);
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

                // 도구로 변환한 타일(파낸 흙 등)이면 불가
                GroundTileModifier tileModifier = TerritoryManager.Instance != null ? TerritoryManager.Instance.TileModifier : null;
                if (tileModifier != null && tileModifier.IsModified(territoryZone, checkCell))
                    return false;

                // 구조물과 충돌하면 불가
                Vector3 cellWorldPos = territoryZone.TerritoryTileMapGround.GetCellCenterWorld(checkCell);
                foreach (var structSlot in territoryZone.TerritoryZoneData.StructSlots)
                {
                    float distance = Vector2.Distance(structSlot.StructSpawnPos, cellWorldPos);
                    if (distance < 4.0f)
                    {
                        Debug.Log($"Too close to struct! Blocked.");
                        return false;
                    }
                }
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

        // 구독 해제
        var damageable = destroyedObj.GetComponent<IRespawnProvier>();
        if (damageable != null)
        {
            damageable.OnDestroyed -= OnResourceDestroyed;
        }

        // 재생성 예정 자원 기록
        respawningResources.Add((entry, Time.time, respawnTime));

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
        var index = respawningResources.FindIndex(r => r.entry == entry);
        if (index >= 0)
        {
            respawningResources.RemoveAt(index);
        }
        RespawnResource(entry);
    }

    /// <summary>
    /// 로드 시 occupiedCells 초기화
    /// </summary>
    public void ClearSpawnedResources()
    {
        occupiedCells.Clear();
        occupiedCellsByObject.Clear();
    }

    /// <summary>
    /// 재생성 예정 자원 모두 반환 (각 자원별)
    /// </summary>
    public List<(ResourceSpawnEntry entry, float remainingTime)> GetAllRespawningResources()
    {
        var result = new List<(ResourceSpawnEntry, float)>();
        foreach (var item in respawningResources)
        {
            float remainingTime = (item.startTime + item.delay) - Time.time;
            if (remainingTime > 0)
            {
                result.Add((item.entry, remainingTime));
            }
        }
        return result;
    }

    /// <summary>
    /// 재생성 예정 자원 남은 시간 반환 (entry별 최소값)
    /// </summary>
    public Dictionary<ResourceSpawnEntry, float> GetRespawningResources()
    {
        var allResources = GetAllRespawningResources();
        var result = new Dictionary<ResourceSpawnEntry, float>();

        foreach (var (entry, remainingTime) in allResources)
        {
            if (!result.ContainsKey(entry) || result[entry] > remainingTime)
            {
                result[entry] = remainingTime;
            }
        }

        return result;
    }

    /// <summary>
    /// 재생성 코루틴 재시작 (남은시간으로)
    /// </summary>
    public void RestartRespawnCoroutines(List<(ResourceSpawnEntry entry, float remainingTime)> respawningData)
    {
        if (respawningData == null) return;

        foreach (var (entry, remainingTime) in respawningData)
        {
            respawningResources.Add((entry, Time.time, remainingTime));
            StartCoroutine(RespawnAfterDelay(entry, remainingTime));
        }
    }

    /// <summary>
    /// 로드 시 저장된 위치에 자원 생성
    /// </summary>
    public GameObject SpawnResourceAtPosition(ResourceSpawnEntry entry, Vector3 worldPosition)
    {
        GameObject resourceObj = CreateResourceAtPosition(entry, worldPosition,false);

        Vector3Int cellPos = territoryZone.TerritoryTileMapGround.WorldToCell(worldPosition);

        // 범위 체크
        BoundsInt bounds = territoryZone.TerritoryTileMapGround.cellBounds;
        if (!bounds.Contains(cellPos))
        {
            Debug.LogWarning($"Cell position {cellPos} outside bounds {bounds}");
            return resourceObj;
        }

        RegisterResource(resourceObj, entry, cellPos);
        return resourceObj;
    }
}
