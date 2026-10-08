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

    [Tooltip("발밑 칸이 변환 불가일 때 상하좌우 인접 칸을 찾을 거리 (칸 단위, 칸 중심까지 거리). 0이면 발밑 칸만")]
    [SerializeField]
    private float neighborReach = 1f;

    private static readonly Vector3Int[] NeighborOffsets =
    {
        Vector3Int.left, Vector3Int.right, Vector3Int.up, Vector3Int.down
    };

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
    private struct ModifiedCell
    {
        public TileBase Original; //변환 전 타일 (되돌릴 때 사용)
        public TileBase Result;   //변환 후 타일
    }

    private Dictionary<int, Dictionary<Vector3Int, ModifiedCell>> modifiedCells = new Dictionary<int, Dictionary<Vector3Int, ModifiedCell>>();

    /// <summary>
    /// 월드 좌표가 속한 칸의 타일을 도구에 맞게 변환 시도
    /// </summary>
    public bool TryConvertTile(Vector3 worldPosition, ToolType toolType)
    {
        // 발밑 칸이 이미 변환됐거나 불가하면 가장 가까운 인접 칸
        if (!TryFindConversion(worldPosition, toolType, true, out TerritoryZone zone, out Vector3Int cell, out TileBase resultTile)
            && !TryFindNeighborConversion(worldPosition, toolType, out zone, out cell, out resultTile))
            return false;
        #region [이전] 발밑 칸만 변환
        // if (!TryFindConversion(worldPosition, toolType, true, out TerritoryZone zone, out Vector3Int cell, out TileBase resultTile))
        //     return false;
        #endregion

        TileBase originalTile = zone.TerritoryTileMapGround.GetTile(cell);
        zone.TerritoryTileMapGround.SetTile(cell, resultTile);
        RecordCell(zone.TerritoryZoneData.ZoneId, cell, originalTile, resultTile);
        return true;
    }

    /// <summary>
    /// 변환된 칸을 변환 전 타일로 되돌리고 기록 삭제
    /// </summary>
    public bool TryRevertTile(TerritoryZone zone, Vector3Int cell)
    {
        if (!modifiedCells.TryGetValue(zone.TerritoryZoneData.ZoneId, out var cells)) return false;
        if (!cells.TryGetValue(cell, out ModifiedCell modified)) return false;

        zone.TerritoryTileMapGround.SetTile(cell, modified.Original);
        cells.Remove(cell);
        return true;
    }

    /// <summary>
    /// 월드 좌표가 속한 칸을 도구로 변환할 수 있는지 확인만 (타일은 바꾸지 않음)
    /// </summary>
    public bool CanConvertTile(Vector3 worldPosition, ToolType toolType)
    {
        return TryFindConversion(worldPosition, toolType, false, out _, out _, out _)
            || TryFindNeighborConversion(worldPosition, toolType, out _, out _, out _);
    }

    /// <summary>
    /// 발밑 칸 기준 상하좌우 인접 칸 중 neighborReach 안에서 가까운 순으로 변환 가능한 칸 찾기
    /// </summary>
    private bool TryFindNeighborConversion(Vector3 worldPosition, ToolType toolType,
        out TerritoryZone zone, out Vector3Int cell, out TileBase resultTile)
    {
        zone = null;
        cell = default;
        resultTile = null;

        if (neighborReach <= 0f) return false;

        TerritoryZone footZone = FindUnlockedZoneAt(worldPosition, out Vector3Int footCell);
        if (footZone == null) return false;

        Tilemap ground = footZone.TerritoryTileMapGround;
        float maxDistance = neighborReach * ground.layoutGrid.cellSize.x;

        List<(float distance, Vector3 center)> candidates = new List<(float, Vector3)>();
        foreach (Vector3Int offset in NeighborOffsets)
        {
            Vector3 center = ground.GetCellCenterWorld(footCell + offset);
            float distance = Vector2.Distance(worldPosition, center);
            if (distance <= maxDistance) candidates.Add((distance, center));
        }
        candidates.Sort((a, b) => a.distance.CompareTo(b.distance));

        // 인접 칸이 다른 구역일 수 있어 칸 중심 월드 좌표로 다시 판정
        foreach (var candidate in candidates)
        {
            if (TryFindConversion(candidate.center, toolType, false, out zone, out cell, out resultTile))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 변환 가능 여부 판정. 가능하면 대상 구역/칸/결과 타일을 돌려준다.
    /// </summary>
    private bool TryFindConversion(Vector3 worldPosition, ToolType toolType, bool logFailure,
        out TerritoryZone zone, out Vector3Int cell, out TileBase resultTile)
    {
        resultTile = null;
        cell = default;
        zone = null;

        if (conversionData == null)
        {
            if (logFailure) Log("TileConversionData 미연결");
            return false;
        }

        zone = FindUnlockedZoneAt(worldPosition, out cell);
        if (zone == null)
        {
            if (logFailure) Log($"{worldPosition} 위치에 해금된 구역의 Ground 타일 없음");
            return false;
        }

        TileBase currentTile = zone.TerritoryTileMapGround.GetTile(cell);
        if (!conversionData.TryGetResult(toolType, currentTile, out resultTile))
        {
            if (logFailure) Log($"{zone.name} {cell} 타일 '{currentTile.name}' 은(는) {toolType} 변환 규칙 없음");
            return false;
        }

        // 자원이 올라가 있는 칸은 변환 불가
        ResourceSpawner spawner = zone.GetComponent<ResourceSpawner>();
        if (spawner != null && spawner.IsOccupied(cell))
        {
            if (logFailure) Log($"{zone.name} {cell} 자원이 있어 변환 불가");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 월드 좌표가 속한 칸이 도구로 변환된 칸(파낸 흙 등)이면 구역/칸을 돌려준다
    /// </summary>
    public bool TryGetModifiedCell(Vector3 worldPosition, out TerritoryZone zone, out Vector3Int cell)
    {
        zone = FindUnlockedZoneAt(worldPosition, out cell);
        return zone != null && IsModified(zone, cell);
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

    private void RecordCell(int zoneId, Vector3Int cell, TileBase originalTile, TileBase resultTile)
    {
        if (!modifiedCells.TryGetValue(zoneId, out var cells))
        {
            cells = new Dictionary<Vector3Int, ModifiedCell>();
            modifiedCells.Add(zoneId, cells);
        }

        // 여러 번 변환돼도 최초 원본 타일 유지
        if (cells.TryGetValue(cell, out ModifiedCell existing))
            originalTile = existing.Original;

        cells[cell] = new ModifiedCell { Original = originalTile, Result = resultTile };
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
                    TileName = cellPair.Value.Result.name
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
            // 씬에 배치된 타일이 원본 (저장 데이터에는 결과 타일만 있음)
            TileBase originalTile = zone.TerritoryTileMapGround.GetTile(cell);
            zone.TerritoryTileMapGround.SetTile(cell, tile);
            RecordCell(tileData.ZoneId, cell, originalTile, tile);
        }
    }
    #endregion
}
