using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

[Serializable]
public class CookingRecipeDTO
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
}

public static class CookingRecipeDataImporter
{
    public const string JsonPath = "Assets/02_Data/Json/Receipe/Receipe.json";
    public const string RecipeFolder = "Assets/02_Data/SO/Recipes";
    public const string CookingAssetPath = "Assets/02_Data/SO/Cooking/CookingData.asset";
    private const string ItemFolder = "Assets/02_Data/SO/InvenItem";
    // 레시피 JSON에는 작업대 제작식도 함께 있다. 결과물이 이 타입인 행만 요리 레시피로 가져온다.
    private const string CookedItemType = "요리 아이템";

    [Serializable]
    private class RecipeRows
    {
        public List<CookingRecipeDTO> items;
    }

    private class ImportEntry
    {
        public CookingRecipeDTO Row;
        public InvenItemData Result;
        public List<RecipeIngredient> Ingredients;
    }

    [MenuItem("Tools/Cooking/Import Recipes")]
    public static void ImportFromMenu()
    {
        try
        {
            Import();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[CookingRecipeDataImporter] 가져오기 실패: {exception.Message}");
        }
    }

    public static CookingData Import()
    {
        string json = File.ReadAllText(JsonPath);
        RecipeRows data = JsonUtility.FromJson<RecipeRows>("{\"items\":" + json + "}");
        if (data == null || data.items == null)
            throw new InvalidDataException("레시피 JSON은 행 배열이어야 합니다.");

        Dictionary<string, InvenItemData> items = LoadItems();
        var ids = new HashSet<int>();
        var entries = new List<ImportEntry>();
        int skipped = 0;

        // 모든 행을 먼저 검증한다. 잘못된 재료를 조용히 빼서 더 싼 레시피를 만들지 않는다.
        foreach (CookingRecipeDTO row in data.items)
        {
            if (row == null || row.CraftRecipeID <= 0)
                throw new InvalidDataException("CraftRecipeID는 양수여야 합니다.");
            if (!ids.Add(row.CraftRecipeID))
                throw new InvalidDataException($"레시피 ID 중복: {row.CraftRecipeID}");
            if (row.ResultCount <= 0 || row.CraftFacilityID <= 0 || row.CraftTimeSec < 0f
                || float.IsNaN(row.CraftTimeSec) || float.IsInfinity(row.CraftTimeSec))
                throw new InvalidDataException($"레시피 {row.CraftRecipeID}: 결과 수량/시설 ID/제작 시간을 확인하세요.");

            InvenItemData result = FindItem(items, row.ResultItemID, row.CraftRecipeID);
            if (result.ItemType != CookedItemType)
            {
                skipped++;
                continue;
            }

            var ingredients = new List<RecipeIngredient>();
            AddIngredient(ingredients, items, row.CraftRecipeID, row.Material1ID, row.Material1Count);
            AddIngredient(ingredients, items, row.CraftRecipeID, row.Material2ID, row.Material2Count);
            AddIngredient(ingredients, items, row.CraftRecipeID, row.Material3ID, row.Material3Count);
            if (ingredients.Count == 0)
                throw new InvalidDataException($"레시피 {row.CraftRecipeID}: 재료가 하나 이상 필요합니다.");

            entries.Add(new ImportEntry
            {
                Row = row,
                Result = result,
                Ingredients = ingredients
            });
            ValidateAssetPath<CookingRecipeData>(RecipePath(row.CraftRecipeID));
        }
        ValidateAssetPath<CookingData>(CookingAssetPath);

        EnsureFolder(RecipeFolder);
        EnsureFolder("Assets/02_Data/SO/Cooking");
        var imported = new List<CookingRecipeData>();
        foreach (ImportEntry entry in entries)
        {
            string path = RecipePath(entry.Row.CraftRecipeID);
            CookingRecipeData recipe = AssetDatabase.LoadAssetAtPath<CookingRecipeData>(path);
            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<CookingRecipeData>();
                AssetDatabase.CreateAsset(recipe, path);
            }

            recipe.CraftRecipeID = entry.Row.CraftRecipeID;
            recipe.ResultItem = entry.Result;
            recipe.ResultCount = entry.Row.ResultCount;
            recipe.CraftFacilityID = entry.Row.CraftFacilityID;
            recipe.CraftTimeSec = entry.Row.CraftTimeSec;
            recipe.Ingredients = entry.Ingredients; // 재임포트 시 누적하지 않고 교체한다.
            EditorUtility.SetDirty(recipe);
            imported.Add(recipe);
        }

        CookingData cooking = AssetDatabase.LoadAssetAtPath<CookingData>(CookingAssetPath);
        if (cooking == null)
        {
            cooking = ScriptableObject.CreateInstance<CookingData>();
            AssetDatabase.CreateAsset(cooking, CookingAssetPath);
        }

        var serialized = new SerializedObject(cooking);
        SerializedProperty recipes = serialized.FindProperty("recipes");
        recipes.arraySize = imported.Count;
        for (int i = 0; i < imported.Count; i++)
            recipes.GetArrayElementAtIndex(i).objectReferenceValue = imported[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(cooking);
        AssetDatabase.SaveAssets();
        Debug.Log($"[CookingRecipeDataImporter] 요리 레시피 {imported.Count}개 갱신 완료 (전체 해금), 요리가 아닌 제작식 {skipped}개 제외");
        return cooking;
    }

    private static Dictionary<string, InvenItemData> LoadItems()
    {
        if (!AssetDatabase.IsValidFolder(ItemFolder))
            throw new InvalidDataException("먼저 인벤토리 아이템 SO를 가져오세요.");
        var items = new Dictionary<string, InvenItemData>(StringComparer.Ordinal);
        foreach (string guid in AssetDatabase.FindAssets("t:InvenItemData", new[] { ItemFolder }))
        {
            InvenItemData item = AssetDatabase.LoadAssetAtPath<InvenItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (item == null || string.IsNullOrWhiteSpace(item.ItemID)) continue;
            if (items.ContainsKey(item.ItemID))
                throw new InvalidDataException($"아이템 SO의 ID 중복: {item.ItemID}");
            items.Add(item.ItemID, item);
        }
        return items;
    }

    private static InvenItemData FindItem(Dictionary<string, InvenItemData> items, int itemID, int recipeID)
    {
        if (itemID <= 0 || !items.TryGetValue(itemID.ToString(CultureInfo.InvariantCulture), out InvenItemData item))
            throw new InvalidDataException($"레시피 {recipeID}: 아이템 SO {itemID}를 찾을 수 없습니다.");
        return item;
    }

    private static void AddIngredient(List<RecipeIngredient> ingredients,
        Dictionary<string, InvenItemData> items, int recipeID, int itemID, int count)
    {
        if (itemID == 0 && count == 0) return;
        if (itemID <= 0 || count <= 0)
            throw new InvalidDataException($"레시피 {recipeID}: 재료 ID와 수량은 모두 양수, 빈 칸은 모두 0이어야 합니다.");
        InvenItemData item = FindItem(items, itemID, recipeID);
        foreach (RecipeIngredient ingredient in ingredients)
        {
            if (ingredient.Item != item) continue;
            ingredient.Count = checked(ingredient.Count + count);
            return;
        }
        ingredients.Add(new RecipeIngredient { Item = item, Count = count });
    }

    private static string RecipePath(int id) => $"{RecipeFolder}/{id}.asset";

    private static void ValidateAssetPath<T>(string path) where T : ScriptableObject
    {
        UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(path);
        if ((existing != null || File.Exists(path)) && !(existing is T))
            throw new InvalidDataException($"다른 형식 또는 읽을 수 없는 에셋이 있습니다: {path}");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
