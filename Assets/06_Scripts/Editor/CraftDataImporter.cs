using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class RecipeDTO
{
    public int CraftRecipeID;
    public int ResultItemID;
    public int ResultCount;
    public int CraftFacilityID;
    public float CraftTimeSec;
    public int Material1ID;
    public int Material1Count;
    public int Material2ID;
    public int Material2Count;
    public int Material3ID;
    public int Material3Count;
    public string LifeType;
    public string Season;
}

[System.Serializable]
public class ToolDTO
{
    public int ToolID;
    public string ToolName;
    public string ToolType;
    public string ToolGrade;
    public string Description;
    public float Power;
    public int CraftRecipeID;
}

public class RecipeDataImporter : BaseDataImporter<RecipeDTO, RecipeData>
{
    protected override string JsonFilePath => "/02_Data/Json/Receipe/Receipe.json";
    public override string SOFolderPath => "Assets/02_Data/SO/Receipe/";

    protected override void MapDTOToSO(RecipeDTO dto, RecipeData so)
    {
        so.CraftRecipeID = dto.CraftRecipeID.ToString();
        so.ResultItemID = dto.ResultItemID.ToString();
        so.ResultCount = Mathf.Max(1, dto.ResultCount);
        so.CraftFacilityID = dto.CraftFacilityID.ToString();
        so.CraftTimeSec = dto.CraftTimeSec;
        so.Season = dto.Season;
        so.LifeType = Enum.TryParse(dto.LifeType, true, out CraftLifeType lifeType) ? lifeType : CraftLifeType.None;

        so.Materials = new List<CraftMaterial>();
        AddMaterial(so, dto.Material1ID, dto.Material1Count);
        AddMaterial(so, dto.Material2ID, dto.Material2Count);
        AddMaterial(so, dto.Material3ID, dto.Material3Count);
    }

    private static void AddMaterial(RecipeData so, int itemId, int count)
    {
        if (itemId <= 0 || count <= 0) return;
        so.Materials.Add(new CraftMaterial(itemId.ToString(), count));
    }

    protected override string GetDTOID(RecipeDTO dto)
    {
        return dto.CraftRecipeID.ToString();
    }
}

public class ToolDataImporter : BaseDataImporter<ToolDTO, ToolData>
{
    protected override string JsonFilePath => "/02_Data/Json/Equiment/Equiment.json";
    public override string SOFolderPath => "Assets/02_Data/SO/Equiment/";

    protected override void MapDTOToSO(ToolDTO dto, ToolData so)
    {
        so.ToolID = dto.ToolID.ToString();
        so.ToolName = dto.ToolName;
        so.ToolType = dto.ToolType;
        so.ToolGrade = dto.ToolGrade;
        so.Description = dto.Description;
        so.Power = dto.Power;
        so.CraftRecipeID = dto.CraftRecipeID > 0 ? dto.CraftRecipeID.ToString() : string.Empty;
    }

    protected override string GetDTOID(ToolDTO dto)
    {
        return dto.ToolID.ToString();
    }
}

/// <summary>
/// 레시피 / 도구 JSON -> SO 생성(기존 에셋은 값만 갱신해 GUID·아이콘 유지) 후 CraftDatabase에 등록
/// </summary>
public static class CraftDataImporter
{
    private const string DatabasePath = "Assets/02_Data/SO/CraftDatabase/CraftDatabase.asset";

    [MenuItem("Tools/Craft 데이터 가져오기 (레시피+도구)")]
    public static void Import()
    {
        List<RecipeData> recipes = ImportInto(new RecipeDataImporter(), d => d.CraftRecipeID.ToString());
        List<ToolData> tools = ImportInto(new ToolDataImporter(), d => d.ToolID.ToString());

        CraftDatabase database = AssetDatabase.LoadAssetAtPath<CraftDatabase>(DatabasePath);
        if (database == null)
        {
            EnsureFolder("Assets/02_Data/SO/CraftDatabase");
            database = ScriptableObject.CreateInstance<CraftDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }
        database.EditorSetData(recipes, tools);
        EditorUtility.SetDirty(database);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CraftDataImporter] 레시피 {recipes.Count}개, 도구 {tools.Count}개 가져오기 완료");
    }

    private static List<TSO> ImportInto<TDTO, TSO>(BaseDataImporter<TDTO, TSO> importer, Func<TDTO, string> getId)
        where TSO : ScriptableObject
    {
        EnsureFolder(importer.SOFolderPath.TrimEnd('/'));
        List<TSO> result = new List<TSO>();

        foreach (TDTO dto in importer.GetDTOList().items)
        {
            string assetPath = importer.SOFolderPath + getId(dto) + ".asset";
            TSO so = AssetDatabase.LoadAssetAtPath<TSO>(assetPath);
            bool isNew = so == null;
            if (isNew) so = ScriptableObject.CreateInstance<TSO>();

            importer.MapEntry(dto, so);

            if (isNew) AssetDatabase.CreateAsset(so, assetPath);
            else EditorUtility.SetDirty(so);
            result.Add(so);
        }
        return result;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
