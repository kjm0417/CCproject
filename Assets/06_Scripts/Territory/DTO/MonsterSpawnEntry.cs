/// <summary>
/// 구역에 유지할 몬스터 정보 (MaxCount = 동시에 존재할 최대 수)
/// </summary>
[System.Serializable]
public class MonsterSpawnEntry
{
    public EnemyBase EnemyPrefab; //생성할 몬스터

    public int MaxCount; //동시에 존재할 최대 수

    public float RespawnDelay = 30f; //처치 후 재출현 대기 시간 (초)

    public int KillGold; //처치할 때마다 지급할 골드
}
