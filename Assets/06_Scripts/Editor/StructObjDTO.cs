using UnityEditor;
using UnityEngine;

[System.Serializable]
public class StructObjDTO 
{
    public string StructID;
    public string StructName;
    public string InteractionDetail;
}

public class StructObjImporter : BaseDataImporter<StructObjDTO, StructObjData>
{
    protected override string JsonFilePath => "/02_Data/Json/StructObj/StructObj.json";
    public override string SOFolderPath => "Assets/02_Data/SO/StructObj/";

    protected override void MapDTOToSO(StructObjDTO dto, StructObjData so)
    {
        so.StructID = dto.StructID;
        so.StructName = dto.StructName;
        so.InteractionDetail = dto.InteractionDetail;
    }

    protected override string GetDTOID(StructObjDTO dto)
    {
        return dto.StructID;
    }

    [MenuItem("Tools/StructObj 가져오기")]
    public static void Import()
    {
        BaseDataImporter<StructObjDTO, StructObjData> importer = new StructObjImporter();
        importer.Import();
    }
}