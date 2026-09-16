using UnityEngine;

/// <summary>
/// 복합 채취 자원 데이터
/// </summary>
[CreateAssetMenu(fileName = "FieldObjData", menuName = "Game/FieldComplexObjData")]
public class FieldComplexObjData : ObjBaseData
{
    public string ObjectID;
    public string ObjectName;
    public string ObjectType;
    public string Description;
    public string InteractionRange;
    public int CropID;
}
