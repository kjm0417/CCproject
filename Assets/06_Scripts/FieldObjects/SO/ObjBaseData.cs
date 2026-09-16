using UnityEngine;

/// <summary>
/// HP가 존재하는 자원 데이터 - 단순 채집 자원 / 복합 채집 자원
/// </summary>
[CreateAssetMenu(fileName = "FieldObjData", menuName = "Game/ObjBaseData")]
public class ObjBaseData : ScriptableObject
{
    public int HP;
    public int DropGroupID;
    public int RespawnTimeMin;
    public int RespawnTimeMax;
}
