using UnityEngine;

/// <summary>
/// 리소스 스폰 전용 데이터 클래스 -> 어떤 자원이 어떻게 스폰되는지에 대한 데이터
/// </summary>
[System.Serializable]
public class ResourceSpawnEntry
{
    public GameObject ResourcePrefab; //뭘 스폰할지 프리펩
    public float SpawnLongTime;//스폰 주기

    public int MaxCount; //자원을 최대 몇개 스폰할지.
}
