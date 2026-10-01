using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씨앗을 파낸 흙 칸에 심고, 작물이 심긴 칸을 기억하는 컴포넌트.
/// TerritoryManager, GroundTileModifier와 같은 오브젝트에 붙인다.
/// </summary>
[RequireComponent(typeof(GroundTileModifier))]
public class CropPlanter : MonoBehaviour
{
    [Serializable]
    private class SeedCropMapping
    {
        public InvenItemData Seed; //씨앗 아이템
        public PlantedCropObject CropPrefab; //심을 작물
    }

    [Tooltip("씨앗 아이템 -> 심을 작물 prefab")]
    [SerializeField]
    private List<SeedCropMapping> seedCropMappings = new List<SeedCropMapping>();

    [Tooltip("켜두면 심기 실패 이유를 로그로 출력")]
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

    // 작물이 심긴 칸 (구역 ID, 칸)
    private readonly HashSet<(int, Vector3Int)> plantedCells = new HashSet<(int, Vector3Int)>();

    /// <summary>
    /// 해당 씨앗을 심을 수 있는 작물이 등록되어 있는지
    /// </summary>
    public bool IsPlantable(InvenItemData seed)
    {
        return FindCropPrefab(seed) != null;
    }

    /// <summary>
    /// 씨앗 아이콘으로 쓸 작물 prefab의 Sprite (매핑 없으면 null)
    /// </summary>
    public Sprite GetSeedIcon(InvenItemData seed)
    {
        PlantedCropObject cropPrefab = FindCropPrefab(seed);
        return cropPrefab != null ? cropPrefab.IconSprite : null;
    }

    /// <summary>
    /// 월드 좌표가 속한 칸에 씨앗 1개를 소모해 작물을 심는다
    /// </summary>
    public bool TryPlant(Vector3 worldPosition, InvenItemData seed, PlayerInventory inventory)
    {
        if (seed == null || inventory == null) return false;

        PlantedCropObject cropPrefab = FindCropPrefab(seed);
        if (cropPrefab == null)
        {
            Log($"{seed.ItemName}({seed.ItemID}) 매핑된 작물 없음");
            return false;
        }

        if (!TileModifier.TryGetModifiedCell(worldPosition, out TerritoryZone zone, out Vector3Int cell))
        {
            Log($"{worldPosition} 파낸 흙이 아님");
            return false;
        }

        var cellKey = (zone.TerritoryZoneData.ZoneId, cell);
        if (plantedCells.Contains(cellKey))
        {
            Log($"{zone.name} {cell} 이미 작물이 심겨 있음");
            return false;
        }

        if (inventory.Remove(seed, 1) <= 0)
        {
            Log($"{seed.ItemName} 소모 실패");
            return false;
        }

        Vector3 spawnPosition = zone.TerritoryTileMapGround.GetCellCenterWorld(cell);
        PlantedCropObject crop = Instantiate(cropPrefab, spawnPosition, Quaternion.identity);

        plantedCells.Add(cellKey);
        crop.OnRemoved += _ => plantedCells.Remove(cellKey);
        return true;
    }

    private PlantedCropObject FindCropPrefab(InvenItemData seed)
    {
        if (seed == null) return null;

        foreach (SeedCropMapping mapping in seedCropMappings)
        {
            if (mapping.Seed == null || mapping.CropPrefab == null) continue;

            // 같은 SO가 아니어도 ID가 같으면 같은 씨앗으로 취급
            if (mapping.Seed == seed || mapping.Seed.ItemID == seed.ItemID)
                return mapping.CropPrefab;
        }
        return null;
    }

    private void Log(string message)
    {
        if (debugLog) Debug.Log($"[CropPlanter] {message}");
    }
}
