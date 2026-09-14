using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class DropTableDTO
{
    public string DropGroupID;
    public string ItemID;
    public int DropRate;
    public int MinCount;
    public int MaxCount;
}

public class DropTableImporter : BaseDataImporter<DropTableDTO, DropTableData>
{
    protected override string JsonFilePath => "/02_Data/Json/DropTable/DropTable.json";

    public override string SOFolderPath => "Assets/02_Data/SO/DropTable/DropTableData.asset";

    protected override string GetDTOID(DropTableDTO dto)
    {
        return dto.DropGroupID;
    }

    protected override void MapDTOToSO(DropTableDTO dto, DropTableData so)
    {
        so.DropGroupID = dto.DropGroupID;
        so.ItemID = dto.ItemID;
        so.DropRate = dto.DropRate;
        so.MinCount = dto.MinCount;
        so.MaxCount = dto.MaxCount;

        DropTableEntry entry = new DropTableEntry
        {
            DropGroupID = int.Parse(dto.DropGroupID),
            ItemID = int.Parse(dto.ItemID),
            DropRate = dto.DropRate,
            MinCount = dto.MinCount,
            MaxCount = dto.MaxCount
        };
        so.AddEntry(entry);
    }

    private static BaseDataImporter<DropTableDTO, DropTableData> importer;
    
    [MenuItem("Tools/1. DropTable 가져오기")]
    public static void Import()
    {
        importer = new DropTableImporter();
        
        DTOList<DropTableDTO> data = importer.GetDTOList();

        Dictionary<string, List<DropTableDTO>> groupedData = new Dictionary<string, List<DropTableDTO>>();

        foreach (DropTableDTO dto in data.items)
        {
            if (!groupedData.ContainsKey(dto.DropGroupID))
                groupedData[dto.DropGroupID] = new List<DropTableDTO>();

            groupedData[dto.DropGroupID].Add(dto);
        }

        foreach (var group in groupedData)
        {
            DropTableData dropTableData = ScriptableObject.CreateInstance<DropTableData>();

            foreach (DropTableDTO dto in group.Value)
            {
                importer.MapEntry(dto, dropTableData);  // AddEntry 포함
            }

            string assetPath = importer.SOFolderPath + "DropTableData_" + group.Key + ".asset";
            AssetDatabase.CreateAsset(dropTableData, assetPath);
            EditorUtility.SetDirty(dropTableData);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

}
