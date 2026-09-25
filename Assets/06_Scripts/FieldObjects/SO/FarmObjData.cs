using UnityEngine;

[CreateAssetMenu(fileName = "FieldObjData", menuName = "Game/FieldComplexObjData")]
public class FarmObjData : ScriptableObject
{
    public string CropID;
    public string CropName;
    public float TimePerStageSec;
    public int Regrowable;
    public int DropGroupID;
    public string CropSeason;
}
