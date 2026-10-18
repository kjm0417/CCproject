using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 설치 아이템(상자 / 작업대 / 모닥불)을 플레이어 발밑 칸에 배치하고, 배치된 칸을 기억하는 컴포넌트.
/// TerritoryManager, GroundTileModifier와 같은 오브젝트에 붙인다.
/// </summary>
[RequireComponent(typeof(GroundTileModifier))]
public class StructurePlacer : MonoBehaviour
{
    [Serializable]
    private class PlaceableMapping
    {
        public InvenItemData Item; //설치 아이템
        public GameObject Prefab; //배치될 오브젝트
    }

    [Tooltip("설치 아이템 -> 배치할 prefab")]
    [SerializeField]
    private List<PlaceableMapping> placeableMappings = new List<PlaceableMapping>();

    [Tooltip("배치한 상자가 사용할 보관 UI. 비워두면 씬에서 찾는다.")]
    [SerializeField]
    private ChestUI chestUI;

    [Tooltip("켜두면 배치 실패 이유를 로그로 출력")]
    [SerializeField]
    private bool debugLog;

    private GroundTileModifier tileModifier;
    private GroundTileModifier TileModifier
    {
        get
        {
            if (tileModifier == null) tileModifier = GetComponent<GroundTileModifier>();
            return tileModifier;
        }
    }

    // 배치된 칸 (구역 ID, 칸) -> 배치 오브젝트 / 아이템 ID
    private readonly Dictionary<(int, Vector3Int), (GameObject obj, string itemId)> placedCells
        = new Dictionary<(int, Vector3Int), (GameObject, string)>();

    /// <summary>
    /// 해당 구역의 칸에 설치물이 있는지
    /// </summary>
    public bool IsPlaced(TerritoryZone zone, Vector3Int cell)
    {
        return zone != null && placedCells.ContainsKey((zone.TerritoryZoneData.ZoneId, cell));
    }

    /// <summary>
    /// 배치 가능한 설치 아이템인지
    /// </summary>
    public bool IsPlaceable(InvenItemData item)
    {
        return FindPrefab(item) != null;
    }

    /// <summary>
    /// 월드 좌표가 속한 칸에 설치 아이템 1개를 소모해 배치한다
    /// </summary>
    public bool TryPlace(Vector3 worldPosition, InvenItemData item, PlayerInventory inventory)
    {
        if (item == null || inventory == null) return false;

        GameObject prefab = FindPrefab(item);
        if (prefab == null)
        {
            Log($"{item.ItemName}({item.ItemID}) 매핑된 prefab 없음");
            return false;
        }

        TerritoryZone zone = TileModifier.FindZoneAt(worldPosition, out Vector3Int cell);
        if (zone == null)
        {
            Log($"{worldPosition} 해금된 영지가 아님");
            return false;
        }

        if (!CanPlaceAt(zone, cell)) return false;

        if (inventory.Remove(item, 1) <= 0)
        {
            Log($"{item.ItemName} 소모 실패");
            return false;
        }

        Spawn(prefab, zone, cell, item.ItemID);
        return true;
    }

    /// <summary>
    /// 자원 / 작물 / 다른 설치물이 없는 칸인지
    /// </summary>
    private bool CanPlaceAt(TerritoryZone zone, Vector3Int cell)
    {
        if (IsPlaced(zone, cell))
        {
            Log($"{zone.name} {cell} 이미 설치물이 있음");
            return false;
        }

        ResourceSpawner spawner = zone.GetComponent<ResourceSpawner>();
        if (spawner != null && spawner.IsOccupied(cell))
        {
            Log($"{zone.name} {cell} 자원이 있어 배치 불가");
            return false;
        }

        CropPlanter cropPlanter = TerritoryManager.Instance != null ? TerritoryManager.Instance.CropPlanter : null;
        if (cropPlanter != null && cropPlanter.IsPlanted(zone, cell))
        {
            Log($"{zone.name} {cell} 작물이 있어 배치 불가");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 칸 중앙에 설치물 생성 및 칸 점유 등록
    /// </summary>
    private GameObject Spawn(GameObject prefab, TerritoryZone zone, Vector3Int cell, string itemId)
    {
        Vector3 position = zone.TerritoryTileMapGround.GetCellCenterWorld(cell);
        GameObject obj = Instantiate(prefab, position, Quaternion.identity);
        // 상자 저장 키(이름 + 위치)가 로드 후에도 같도록 이름 고정
        obj.name = $"Placed_{itemId}";

        ChestObject chest = obj.GetComponent<ChestObject>();
        if (chest != null)
        {
            if (chestUI == null) chestUI = FindAnyObjectByType<ChestUI>(FindObjectsInactive.Include);
            chest.SetChestUI(chestUI);
        }

        placedCells.Add((zone.TerritoryZoneData.ZoneId, cell), (obj, itemId));
        return obj;
    }

    #region 저장 및 불러오기
    /// <summary>
    /// 배치한 설치물 저장
    /// </summary>
    public List<PlacedStructureData> Save()
    {
        List<PlacedStructureData> data = new List<PlacedStructureData>();

        foreach (var pair in placedCells)
        {
            if (pair.Value.obj == null) continue;

            data.Add(new PlacedStructureData
            {
                ZoneId = pair.Key.Item1,
                CellX = pair.Key.Item2.x,
                CellY = pair.Key.Item2.y,
                ItemID = pair.Value.itemId
            });
        }
        return data;
    }

    /// <summary>
    /// 저장된 설치물 다시 배치 (아이템 소모 없음)
    /// </summary>
    public void Load(List<PlacedStructureData> data)
    {
        if (data == null) return;

        TerritoryManager manager = TerritoryManager.Instance;
        if (manager == null) return;

        foreach (PlacedStructureData placed in data)
        {
            TerritoryZone zone = manager.GetZone(placed.ZoneId);
            if (zone == null) continue;

            GameObject prefab = FindPrefab(placed.ItemID);
            if (prefab == null)
            {
                Debug.LogWarning($"[StructurePlacer] 설치 아이템 {placed.ItemID} 매핑된 prefab 없음 - 로드 건너뜀");
                continue;
            }

            Vector3Int cell = new Vector3Int(placed.CellX, placed.CellY, 0);
            if (placedCells.ContainsKey((placed.ZoneId, cell))) continue;

            Spawn(prefab, zone, cell, placed.ItemID);
        }
    }
    #endregion

    private GameObject FindPrefab(InvenItemData item)
    {
        return item != null ? FindPrefab(item.ItemID) : null;
    }

    private GameObject FindPrefab(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;

        foreach (PlaceableMapping mapping in placeableMappings)
        {
            if (mapping.Item == null || mapping.Prefab == null) continue;

            // 같은 SO가 아니어도 ID가 같으면 같은 아이템으로 취급
            if (mapping.Item.ItemID == itemId)
                return mapping.Prefab;
        }
        return null;
    }

    private void Log(string message)
    {
        if (debugLog) Debug.Log($"[StructurePlacer] {message}");
    }
}
