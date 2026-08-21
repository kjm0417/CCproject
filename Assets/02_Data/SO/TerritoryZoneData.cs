using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 구역 데이터 클래스
/// </summary>
[CreateAssetMenu(fileName = "TerritoryZoneData", menuName = "Scriptable Objects/TerritoryZoneData")]
public class TerritoryZoneData : ScriptableObject
{
    public int ZoneId; //구역 고유 id
    public float RequireGold; //구역 해금하기 위한 필요 골드
    public bool IsSpecialZone;//특별한 구역인지 여부(npc 등)
    
    public List<ResourceSpawnEntry> ResourceSpawnTable; //이 구역에서 스폰되는 자원 오브젝트들의 목록
    public List<NpcSlotData> NpcSlots; //이 구역에 배치되는 NPC들의 목록
}
