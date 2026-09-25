using UnityEditor;
using UnityEngine;

[System.Serializable]
public class FarmObjDTO
{
    public string CropID;
    public string CropName;
    public float TimePerStageSec;
    public int Regrowable;
    public int DropGroupID;
    public string CropSeason;
}
public class FarmObjDTOImporter : BaseDataImporter<FarmObjDTO, FarmObjData>
{
    protected override string JsonFilePath => "/02_Data/Json/FarmObj/ParmObj.json";
    public override string SOFolderPath => "Assets/02_Data/SO/FarmObj/";

    protected override void MapDTOToSO(FarmObjDTO dto, FarmObjData so)
    {
        so.CropID = dto.CropID;
        so.CropName = dto.CropName;
        so.TimePerStageSec = dto.TimePerStageSec;
        so.DropGroupID = dto.DropGroupID;
        so.CropSeason = dto.CropSeason;
        so.Regrowable = dto.Regrowable;
    }

    protected override string GetDTOID(FarmObjDTO dto)
    {
        return dto.CropID;
    }

    [MenuItem("Tools/FarmObj 가져오기")]
    public static void Import()
    {
        BaseDataImporter<FarmObjDTO, FarmObjData> importer = new FarmObjDTOImporter();
        importer.Import();
    }
}