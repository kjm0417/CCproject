using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class TestDataDTO 
{
    public string id;
    public string objectName;
    public int maxHP;
    public string dropItemId;
    public float dropRate;
}

public class TestDataImporter : BaseDataImporter<TestDataDTO, TestData>
{
    protected override string JsonFilePath => "/02_Data/Json/TestData.json";
    protected override string SOFolderPath => "Assets/02_Data/SO/Data/";

    protected override void MapDTOToSO(TestDataDTO dto, TestData so)
    {
        so.id = dto.id;
        so.objectName = dto.objectName;
        so.maxHP = dto.maxHP;
        so.dropItemId = dto.dropItemId;
        so.dropRate = dto.dropRate;
    }

    protected override string GetDTOID(TestDataDTO dto)
    {
        return dto.id;
    }

    [MenuItem("Tools/TestData 가져오기")]
    public static void Import()
    {
        BaseDataImporter<TestDataDTO, TestData> importer = new TestDataImporter();
        importer.Import();
    }
}
