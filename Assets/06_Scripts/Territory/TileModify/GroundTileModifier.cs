using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 도구로 영지 Ground 타일을 다른 타일로 바꾸고, 바뀐 칸을 기억해 저장/로드하는 컴포넌트.
/// TerritoryManager와 같은 오브젝트에 붙인다.
/// </summary>
[RequireComponent(typeof(TerritoryManager))]
public class GroundTileModifier : MonoBehaviour
{
    [SerializeField]
    private TileConversionData conversionData;

    [Tooltip("켜두면 타일 변환 실패 이유와 발밑 타일 이름을 로그로 출력")]
    [SerializeField]
    private bool debugLog;

    private TerritoryManager territoryManager;
    private TerritoryManager Manager
    {
        get
        {
            // 로드가 Awake보다 먼저 불릴 수 있어서 필요할 때 찾는다
            if (territoryManager == null) territoryManager = GetComponent<TerritoryManager>();
            return territoryManager;
        }
    }

    // ZoneId -> (바뀐 칸 -> 바뀐 후 타일)
    private Dictionary<int, Dictionary<Vector3Int, TileBase>> modifiedCells = new Dictionary<int, Dictionary<Vector3Int, TileBase>>();

    /// <summary>
    /// 월드 좌표가 속한 칸의 타일을 도구에 맞게 변환 시도
    /// </summary>
    public bool TryConvertTile(Vector3 worldPosition, ToolType toolType)
    {
        if (conversionData == null)
        {
            Log("TileConversionData 미연결");
            return false;
        }

        TerritoryZone zone = FindUnlockedZoneAt(worldPosition, out Vector3Int cell);
        if (zone == null)
        {
            Log($"{worldPosition} 위치에 해금된 구역의 Ground 타일 없음");
            return false;
        }

        Tilemap ground = zone.TerritoryTileMapGround;
        TileBase currentTile = ground.GetTile(cell);
        if (!conversionData.TryGetResult(toolType, currentTile, out TileBase resultTile))
        {
            Log($"{zone.name} {cell} 타일 '{currentTile.name}' 은(는) {toolType} 변환 규칙 없음");
            return false;
        }

        // 자원이 올라가 있는 칸은 변환 불가
        ResourceSpawner spawner = zone.GetComponent<ResourceSpawner>();
        if (spawner != null && spawner.IsOccupied(cell))
        {
            Log($"{zone.name} {cell} 자원이 있어 변환 불가");
            return false;
        }

        ground.SetTile(cell, resultTile);
        RecordCell(zone.TerritoryZoneData.ZoneId, cell, resultTile);
        return true;
    }

    /// <summary>
    /// 해당 구역의 칸이 변환된 칸인지 확인
    /// </summary>
    public bool IsModified(TerritoryZone zone, Vector3Int cell)
    {
        return modifiedCells.TryGetValue(zone.TerritoryZoneData.ZoneId, out var cells) && cells.ContainsKey(cell);
    }

    /// <summary>
    /// 해금된 구역 중 해당 위치에 Ground 타일이 있는 구역 찾기
    /// </summary>
    private TerritoryZone FindUnlockedZoneAt(Vector3 worldPosition, out Vector3Int cell)
    {
        foreach (TerritoryZone zone in Manager.Zones)
        {
            if (zone.IsLocked || zone.TerritoryTileMapGround == null) continue;

            Vector3Int zoneCell = zone.TerritoryTileMapGround.WorldToCell(worldPosition);
            if (zone.TerritoryTileMapGround.HasTile(zoneCell))
            {
                cell = zoneCell;
                return zone;
            }
        }

        cell = default;
        return null;
    }

    private void Log(string message)
    {
        if (debugLog) Debug.Log($"[GroundTileModifier] {message}");
    }

    private void RecordCell(int zoneId, Vector3Int cell, TileBase tile)
    {
        if (!modifiedCells.TryGetValue(zoneId, out var cells))
        {
            cells = new Dictionary<Vector3Int, TileBase>();
            modifiedCells.Add(zoneId, cells);
        }
        cells[cell] = tile;
    }

    #region 저장 및 불러오기
    public List<ModifiedTileData> Save()
    {
        List<ModifiedTileData> result = new List<ModifiedTileData>();
        foreach (var zonePair in modifiedCells)
        {
            foreach (var cellPair in zonePair.Value)
            {
                result.Add(new ModifiedTileData
                {
                    ZoneId = zonePair.Key,
                    CellX = cellPair.Key.x,
                    CellY = cellPair.Key.y,
                    TileName = cellPair.Value.name
                });
            }
        }
        return result;
    }

    public void Load(List<ModifiedTileData> data)
    {
        modifiedCells.Clear();
        if (data == null || conversionData == null) return;

        foreach (ModifiedTileData tileData in data)
        {
            TerritoryZone zone = Manager.GetZone(tileData.ZoneId);
            if (zone == null || zone.TerritoryTileMapGround == null) continue;

            TileBase tile = conversionData.FindResultTile(tileData.TileName);
            if (tile == null)
            {
                Debug.LogWarning($"변환 타일을 찾을 수 없음: {tileData.TileName}");
                continue;
            }

            Vector3Int cell = new Vector3Int(tileData.CellX, tileData.CellY, 0);
            zone.TerritoryTileMapGround.SetTile(cell, tile);
            RecordCell(tileData.ZoneId, cell, tile);
        }
    }
    #endregion
}
