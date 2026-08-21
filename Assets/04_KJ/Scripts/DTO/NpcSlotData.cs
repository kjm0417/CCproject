using UnityEngine;

/// <summary>
/// NPC 배치 전용 데이터 클래스
/// </summary>
[System.Serializable]
public class NpcSlotData
{
    public GameObject NpcPrefab; //어떤 NPC인지
    public Vector2 NpcSpawnPos;//NPC 스폰 위치
}
