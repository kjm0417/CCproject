using UnityEditor;
using UnityEngine;

[System.Serializable]
public class FieldComplexObjDTO 
{
    public string ObjectID;
    public string ObjectName;
    public string ObjectType;
    public int HP;
    public string Description;
    public int RespawnTimeMin;
    public int RespawnTimeMax;
    public string InteractionRange;
    public int CropID;
}
public class FieldComplexObjImporter : BaseDataImporter<FieldComplexObjDTO, FieldComplexObjData>
{
    protected override string JsonFilePath => "/02_Data/Json/FieldObjComplexData/FieldComplexObj.json";
    public override string SOFolderPath => "Assets/02_Data/SO/FieldObjComplexData/";

    protected override void MapDTOToSO(FieldComplexObjDTO dto, FieldComplexObjData so)
    {
        so.ObjectID = dto.ObjectID;
        so.ObjectName = dto.ObjectName;
        so.ObjectType = dto.ObjectType;
        so.HP = dto.HP;
        so.Description = dto.Description;
        so.RespawnTimeMin = dto.RespawnTimeMin;
        so.RespawnTimeMax = dto.RespawnTimeMax;
        so.InteractionRange = dto.InteractionRange;
        so.CropID = dto.CropID;
    }

    protected override string GetDTOID(FieldComplexObjDTO dto)
    {
        return dto.ObjectID;
    }

    [MenuItem("Tools/FieldObjComplexData 가져오기")]
    public static void Import()
    {
        BaseDataImporter<FieldComplexObjDTO, FieldComplexObjData> importer = new FieldComplexObjImporter();
        importer.Import();
    }
}