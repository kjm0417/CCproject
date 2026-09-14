using UnityEngine;

[CreateAssetMenu(fileName = "FieldObjData", menuName = "Game/FieldObjData")]
public class FieldObjData : ScriptableObject
{
    public string ObjectID;
    public string ObjectName;
    public string ObjectType;
    public int HP;
    public string Description;
    public int RespawnTimeMin;
    public int RespawnTimeMax;
    public string InteractionRange;
    public int DropGroupID;
}
