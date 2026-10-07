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

    // 작물이 심긴 칸 (구역 ID, 칸) -> 심은 작물 / 씨앗 ID
    private readonly Dictionary<(int, Vector3Int), (PlantedCropObject crop, string seedItemId)> plantedCells
        = new Dictionary<(int, Vector3Int), (PlantedCropObject, string)>();

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
        if (plantedCells.ContainsKey(cellKey))
        {
            Log($"{zone.name} {cell} 이미 작물이 심겨 있음");
            return false;
        }

        if (inventory.Remove(seed, 1) <= 0)
        {
            Log($"{seed.ItemName} 소모 실패");
            return false;
        }

        SpawnCrop(cropPrefab, zone, cell, seed.ItemID);
        return true;
    }

    /// <summary>
    /// 칸 중앙에 작물 생성 및 칸 점유 등록
    /// </summary>
    private PlantedCropObject SpawnCrop(PlantedCropObject cropPrefab, TerritoryZone zone, Vector3Int cell, string seedItemId)
    {
        var cellKey = (zone.TerritoryZoneData.ZoneId, cell);

        Vector3 spawnPosition = zone.TerritoryTileMapGround.GetCellCenterWorld(cell);
        PlantedCropObject crop = ObjectPoolManager.Spawn(cropPrefab, spawnPosition, Quaternion.identity);
        #region [이전] Instantiate / Destroy
        // PlantedCropObject crop = Instantiate(cropPrefab, spawnPosition, Quaternion.identity);
        #endregion

        plantedCells.Add(cellKey, (crop, seedItemId));
        crop.OnRemoved += _ => plantedCells.Remove(cellKey);
        return crop;
    }

    #region 저장 및 불러오기
    /// <summary>
    /// 심은 작물 저장
    /// </summary>
    public List<PlantedCropData> Save()
    {
        List<PlantedCropData> data = new List<PlantedCropData>();

        foreach (var pair in plantedCells)
        {
            PlantedCropObject crop = pair.Value.crop;
            if (crop == null) continue;

            data.Add(new PlantedCropData
            {
                ZoneId = pair.Key.Item1,
                CellX = pair.Key.Item2.x,
                CellY = pair.Key.Item2.y,
                SeedItemID = pair.Value.seedItemId,
                GrowthStage = crop.GrowthStage,
                IsGrowthComplete = crop.IsGrowthComplete
            });
        }
        return data;
    }

    /// <summary>
    /// 저장된 작물 다시 심기 (씨앗 소모 없음, 저장된 단계부터 성장)
    /// </summary>
    public void Load(List<PlantedCropData> data)
    {
        if (data == null) return;

        TerritoryManager manager = TerritoryManager.Instance;
        if (manager == null) return;

        foreach (PlantedCropData cropData in data)
        {
            TerritoryZone zone = manager.GetZone(cropData.ZoneId);
            if (zone == null) continue;

            PlantedCropObject cropPrefab = FindCropPrefab(cropData.SeedItemID);
            if (cropPrefab == null)
            {
                Debug.LogWarning($"[CropPlanter] 씨앗 {cropData.SeedItemID} 매핑된 작물 없음 - 로드 건너뜀");
                continue;
            }

            Vector3Int cell = new Vector3Int(cropData.CellX, cropData.CellY, 0);
            if (plantedCells.ContainsKey((cropData.ZoneId, cell))) continue;

            PlantedCropObject crop = SpawnCrop(cropPrefab, zone, cell, cropData.SeedItemID);

            // Start 전에 채워두면 그 단계부터 이어서 성장
            crop.GrowthStage = cropData.GrowthStage > 0 ? cropData.GrowthStage : 1;
            crop.IsGrowthComplete = cropData.IsGrowthComplete;
        }
    }
    #endregion

    private PlantedCropObject FindCropPrefab(InvenItemData seed)
    {
        return seed != null ? FindCropPrefab(seed.ItemID) : null;
    }

    private PlantedCropObject FindCropPrefab(string seedItemId)
    {
        if (string.IsNullOrEmpty(seedItemId)) return null;

        foreach (SeedCropMapping mapping in seedCropMappings)
        {
            if (mapping.Seed == null || mapping.CropPrefab == null) continue;

            // 같은 SO가 아니어도 ID가 같으면 같은 씨앗으로 취급
            if (mapping.Seed.ItemID == seedItemId)
                return mapping.CropPrefab;
        }
        return null;
    }

    private void Log(string message)
    {
        if (debugLog) Debug.Log($"[CropPlanter] {message}");
    }
}
