using UnityEditor;
using UnityEngine;

[System.Serializable]
public class FieldObjDTO
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

public class FieldObjImporter : BaseDataImporter<FieldObjDTO, FieldObjData>
{
    protected override string JsonFilePath => "/02_Data/Json/FieldObj/FieldObj.json";
    protected override string SOFolderPath => "Assets/02_Data/SO/FieldObjData/"; 

    protected override void MapDTOToSO(FieldObjDTO dto, FieldObjData so)
    {
        so.ObjectID = dto.ObjectID;
        so.ObjectName = dto.ObjectName;
        so.ObjectType = dto.ObjectType;
        so.HP = dto.HP;
        so.Description = dto.Description;
        so.RespawnTimeMin = dto.RespawnTimeMin;
        so.RespawnTimeMax = dto.RespawnTimeMax;
        so.InteractionRange = dto.InteractionRange;
        so.DropGroupID = dto.DropGroupID;
    }

    protected override string GetDTOID(FieldObjDTO dto)
    {
        return dto.ObjectID;
    }

    [MenuItem("Tools/FieldObj 가져오기")]
    public static void Import()
    {
        BaseDataImporter<FieldObjDTO, FieldObjData> importer = new FieldObjImporter();
        importer.Import();
    }
}
