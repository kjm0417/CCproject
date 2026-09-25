using UnityEngine;

/// <summary>
/// 구조물 배치 전용 데이터 클래스 ( 던전 입구 , 상점 등등 구조물)
/// </summary>
[System.Serializable]
public class StructSlotData
{
    public GameObject StructPrefab; //어떤 구조물인지
    public Vector2 StructSpawnPos;//구조물 스폰 위치
}
