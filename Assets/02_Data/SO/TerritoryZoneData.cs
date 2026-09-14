using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 구역 데이터
/// </summary>
[CreateAssetMenu(fileName = "TerritoryZoneData", menuName = "Scriptable Objects/TerritoryZoneData")]
public class TerritoryZoneData : ScriptableObject
{
    public int ZoneId; //구역 ID
    public float RequireGold; //해금 시 필요 골드
    public bool IsSpecialZone;//특별 구역 여부

    public List<ResourceSpawnEntry> ResourceSpawnTable; //자원 스폰 테이블
    public List<NpcSlotData> NpcSlots; //NPC 스폰 테이블

    // 인접 Zone ID (-1 = 없음)
    public int AdjacentZoneId_Up = -1;
    public int AdjacentZoneId_Down = -1;
    public int AdjacentZoneId_Left = -1;
    public int AdjacentZoneId_Right = -1;
}
