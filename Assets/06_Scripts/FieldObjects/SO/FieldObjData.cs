using UnityEngine;

/// <summary>
/// 단순 채취 자원 데이터
/// </summary>
[CreateAssetMenu(fileName = "FieldObjData", menuName = "Game/FieldObjData")]
public class FieldObjData : ObjBaseData
{
    public string ObjectID;
    public string ObjectName;
    public string ObjectType;
    public string Description;
    public int DropGroupID;
    public string InteractionRange;
}
