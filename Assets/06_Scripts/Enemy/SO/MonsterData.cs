using UnityEngine;

/// <summary>
/// 적 공통 데이터 
/// 적 개별 데이터는 별도의 SO를 만들어서 사용
/// </summary>
[CreateAssetMenu(fileName = "MonsterData", menuName = "Game/MonsterData")]
public class MonsterData : ScriptableObject
{
    public string MonsterID;
    public string MonsterName;
    public int Level; //몬스터의 레벨
    public int HP;
    public float ATK; //몬스터의 공격 뎀지
    public float EXP; //몬스터 사망 시, 플레이어에게 줄 경험치
    public int DropGroupID;
}
