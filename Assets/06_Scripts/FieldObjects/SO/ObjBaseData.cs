using UnityEngine;

/// <summary>
/// HP가 존재하는 자연 생성물 데이터 ( 부모 ) - 단순 채집 자원 / 복합 채집 자원
/// </summary>
[CreateAssetMenu(fileName = "ObjBaseData", menuName = "Game/ObjBaseData")]
public class ObjBaseData : ScriptableObject
{
    public int HP;
    public int RespawnTimeMin;
    public int RespawnTimeMax;
}
