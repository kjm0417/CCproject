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
[System.Serializable]
public class TestDataDtoList
{
    public List<TestDataDTO> items;
}

public class TestDataImporter
{
    [MenuItem("Tools/TestData 가져오기")]
    public static void Import()
    {
        string path = Application.dataPath + "/02_Data/Json/TestData.json";
        string json = System.IO.File.ReadAllText(path);
        string wrappedJson = "{\"items\":" + json + "}";

        TestDataDtoList data = JsonUtility.FromJson<TestDataDtoList>(wrappedJson);

        foreach (TestDataDTO dto in data.items)
        {
            TestData so = ScriptableObject.CreateInstance<TestData>();
            so.id = dto.id;
            so.objectName = dto.objectName;
            so.maxHP = dto.maxHP;
            so.dropItemId = dto.dropItemId;
            so.dropRate = dto.dropRate;

            string assetPath = "Assets/02_Data/SO/Data/" + dto.id + ".asset";
            AssetDatabase.CreateAsset(so, assetPath);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
