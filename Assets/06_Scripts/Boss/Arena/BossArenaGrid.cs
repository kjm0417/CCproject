using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 전장 grid - transform 위치가 좌하단 기준
/// 물 / 빈 공간처럼 서 있을 수 없는 칸은 excludedCells 또는 excludedAreas 로 제외
/// </summary>
public class BossArenaGrid : MonoBehaviour
{
    [SerializeField] private Vector2Int gridSize = new Vector2Int(15, 9);
    [SerializeField] private Vector2 cellSize = Vector2.one;
    [SerializeField, Tooltip("패턴 대상에서 제외할 칸 ( 0,0 = 좌하단 )")]
    private List<Vector2Int> excludedCells = new List<Vector2Int>();
    [SerializeField, Tooltip("이 오브젝트 ( 자식 포함 ) 영역에 칸 중심이 들어가면 제외 Collider2D 가 있으면 콜라이더 모양, 없으면 Renderer 범위 기준")]
    private List<GameObject> excludedAreas = new List<GameObject>();

    private readonly List<Vector2Int> validCells = new List<Vector2Int>();

    public Vector2 CellSize => cellSize;
    public Vector2 MinWorld => transform.position;
    public Vector2 MaxWorld => (Vector2)transform.position + Vector2.Scale(gridSize, cellSize);

    private void Awake()
    {
        RebuildValidCells();
    }

    private void RebuildValidCells()
    {
        validCells.Clear();
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (!IsExcluded(cell)) validCells.Add(cell);
            }
        }
    }

    public bool IsExcluded(Vector2Int cell)
    {
        if (excludedCells.Contains(cell)) return true;

        Vector2 center = CellToWorld(cell);
        foreach (GameObject area in excludedAreas)
        {
            if (area != null && AreaContains(area, center)) return true;
        }
        return false;
    }

    private static bool AreaContains(GameObject area, Vector2 point)
    {
        Collider2D[] colliders = area.GetComponentsInChildren<Collider2D>();
        if (colliders.Length > 0)
        {
            foreach (Collider2D col in colliders)
            {
                if (col.OverlapPoint(point)) return true;
            }
            return false;
        }

        // 콜라이더가 없으면 Renderer 사각 범위로 판정
        foreach (Renderer renderer in area.GetComponentsInChildren<Renderer>())
        {
            Bounds bounds = renderer.bounds;
            if (point.x >= bounds.min.x && point.x <= bounds.max.x && point.y >= bounds.min.y && point.y <= bounds.max.y) return true;
        }
        return false;
    }

    public Vector2 CellToWorld(Vector2Int cell)
    {
        return (Vector2)transform.position + Vector2.Scale((Vector2)cell + Vector2.one * 0.5f, cellSize);
    }

    public ZoneShape GetCellShape(Vector2Int cell)
    {
        return ZoneShape.Fixed(CellToWorld(cell), cellSize);
    }

    /// <summary> 중복 없이 랜덤 칸 count 개 </summary>
    public List<Vector2Int> GetRandomCells(int count)
    {
        List<Vector2Int> pool = new List<Vector2Int>(validCells);
        List<Vector2Int> result = new List<Vector2Int>();

        count = Mathf.Min(count, pool.Count);
        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }
        return result;
    }

    /// <summary> origin ( 좌하단 칸 ) 부터 size 칸 묶음 영역 </summary>
    public ZoneShape GetBlockShape(Vector2Int origin, Vector2Int size)
    {
        Vector2 center = (Vector2)transform.position + Vector2.Scale((Vector2)origin + (Vector2)size * 0.5f, cellSize);
        return ZoneShape.Fixed(center, Vector2.Scale(size, cellSize));
    }

    /// <summary> 묶음 안 모든 칸이 유효한지 ( 제외 칸 / grid 밖 X ) </summary>
    public bool IsBlockValid(Vector2Int origin, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                if (!validCells.Contains(origin + new Vector2Int(x, y))) return false;
            }
        }
        return true;
    }

    /// <summary> 서로 겹치지 않는 랜덤 size 칸 묶음 count 개 ( 반환값 = 묶음 좌하단 칸 ) </summary>
    public List<Vector2Int> GetRandomBlocks(int count, Vector2Int size)
    {
        size = Vector2Int.Max(size, Vector2Int.one);

        List<Vector2Int> pool = new List<Vector2Int>();
        foreach (Vector2Int cell in validCells)
        {
            if (IsBlockValid(cell, size)) pool.Add(cell);
        }

        List<Vector2Int> result = new List<Vector2Int>();
        while (result.Count < count && pool.Count > 0)
        {
            int index = Random.Range(0, pool.Count);
            Vector2Int origin = pool[index];
            result.Add(origin);

            // 선택된 묶음과 겹치는 후보 제거
            pool.RemoveAll(o => Mathf.Abs(o.x - origin.x) < size.x && Mathf.Abs(o.y - origin.y) < size.y);
        }
        return result;
    }

    private void OnDrawGizmosSelected()
    {
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                Color color = IsExcluded(cell) ? new Color(0f, 0.5f, 1f, 0.6f) : new Color(1f, 1f, 1f, 0.3f);
                GetCellShape(cell).DrawGizmo(color);
            }
        }
    }
}
