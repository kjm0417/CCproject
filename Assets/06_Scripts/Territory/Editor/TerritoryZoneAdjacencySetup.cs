using System.Security.Policy;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Territory Zone 인접 관계 자동 설정 에디터 스크립트
/// </summary>
public class TerritoryZoneAdjacencySetup : EditorWindow
{
    private float adjacencyTolerance = 0.5f;

    [MenuItem("Window/Territory/Zone Adjacency Setup")]
    public static void ShowWindow()
    {
        GetWindow<TerritoryZoneAdjacencySetup>("Zone Adjacency Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Territory Zone 인접 관계 자동 설정", EditorStyles.boldLabel);

        adjacencyTolerance = EditorGUILayout.FloatField("인접 판정 오차 범위", adjacencyTolerance);

        if (GUILayout.Button("인접 Zone ID 자동 설정", GUILayout.Height(40)))
        {
            SetupAdjacentZones();
        }
    }

    private void SetupAdjacentZones()
    {
        TerritoryZone[] allZones = FindObjectsOfType<TerritoryZone>();

        if (allZones.Length == 0)
        {
            EditorUtility.DisplayDialog("알림", "씬에 TerritoryZone이 없습니다.", "확인");
            return;
        }

        foreach (TerritoryZone zone in allZones)
        {
            FindAdjacentZones(zone, allZones);
        }

        Debug.Log($"✓ {allZones.Length}개 Zone 인접 관계 설정 완료");
        EditorUtility.DisplayDialog("완료", $"{allZones.Length}개 Zone의 인접 관계가 설정되었습니다.", "확인");
    }

    private void FindAdjacentZones(TerritoryZone currentZone, TerritoryZone[] allZones)
    {
        Collider2D currentCollider = currentZone.GetComponentInChildren<Collider2D>();
        if (currentCollider == null)
            return;

        Bounds currentBounds = currentCollider.bounds;
        TerritoryZoneData currentData = currentZone.TerritoryZoneData;

        // 초기화
        currentData.AdjacentZoneId_Up = -1;
        currentData.AdjacentZoneId_Down = -1;
        currentData.AdjacentZoneId_Left = -1;
        currentData.AdjacentZoneId_Right = -1;

        foreach (TerritoryZone otherZone in allZones)
        {
            if (currentZone == otherZone)
                continue;

            Collider2D otherCollider = otherZone.GetComponentInChildren<Collider2D>();
            Debug.Log($"{otherZone.gameObject.name}: Collider = {otherCollider?.gameObject.name ?? "없음"}");

            if (otherCollider == null)
                continue;

            Bounds otherBounds = otherCollider.bounds;
            TerritoryZoneData otherData = otherZone.TerritoryZoneData;

            Debug.Log($"currentBounds: min={currentBounds.min}, max={currentBounds.max}");
            Debug.Log($"otherBounds: min={otherBounds.min}, max={otherBounds.max}");
            Debug.Log($"Right check: {currentBounds.max.x} vs {otherBounds.min.x}, diff={Mathf.Abs(currentBounds.max.x - otherBounds.min.x)}");
            // 우측 인접
            if (IsAdjacent(currentBounds.max.x, otherBounds.min.x) &&
                IsSameRow(currentBounds, otherBounds))
            {
                currentData.AdjacentZoneId_Right = otherData.ZoneId;
            }

            // 좌측 인접
            if (IsAdjacent(currentBounds.min.x, otherBounds.max.x) &&
                IsSameRow(currentBounds, otherBounds))
            {
                currentData.AdjacentZoneId_Left = otherData.ZoneId;
            }

            // 상단 인접
            if (IsAdjacent(currentBounds.max.y, otherBounds.min.y) &&
                IsSameColumn(currentBounds, otherBounds))
            {
                currentData.AdjacentZoneId_Up = otherData.ZoneId;
            }

            // 하단 인접
            if (IsAdjacent(currentBounds.min.y, otherBounds.max.y) &&
                IsSameColumn(currentBounds, otherBounds))
            {
                currentData.AdjacentZoneId_Down = otherData.ZoneId;
            }
        }

        EditorUtility.SetDirty(currentData);
    }

    private bool IsAdjacent(float pos1, float pos2)
    {
        return Mathf.Abs(pos1 - pos2) <= adjacencyTolerance;
    }

    private bool IsSameRow(Bounds bounds1, Bounds bounds2)
    {
        return !(bounds1.max.y < bounds2.min.y || bounds2.max.y < bounds1.min.y);
    }

    private bool IsSameColumn(Bounds bounds1, Bounds bounds2)
    {
        return !(bounds1.max.x < bounds2.min.x || bounds2.max.x < bounds1.min.x);
    }
}
