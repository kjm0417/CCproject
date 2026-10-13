using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 제작에 필요한 레시피 / 도구 SO 목록. 에디터 메뉴 Tools/Craft 데이터 가져오기 로 자동 채워진다.
/// </summary>
[CreateAssetMenu(fileName = "CraftDatabase", menuName = "Game/Craft/CraftDatabase")]
public class CraftDatabase : ScriptableObject
{
    [SerializeField]
    private List<RecipeData> recipes = new List<RecipeData>();
    [SerializeField]
    private List<ToolData> tools = new List<ToolData>();

    public IReadOnlyList<RecipeData> Recipes => recipes;
    public IReadOnlyList<ToolData> Tools => tools;

    /// <summary>
    /// 시설 / 생활 분류 / 계절 조건에 맞는 레시피 목록
    /// </summary>
    public List<RecipeData> GetRecipes(string facilityId, CraftLifeType lifeType, string season)
    {
        List<RecipeData> result = new List<RecipeData>();
        for (int i = 0; i < recipes.Count; i++)
        {
            RecipeData recipe = recipes[i];
            if (recipe == null) continue;
            if (!string.IsNullOrEmpty(facilityId) && recipe.CraftFacilityID != facilityId) continue;
            if (recipe.LifeType != lifeType) continue;
            if (!recipe.MatchesSeason(season)) continue;
            result.Add(recipe);
        }
        return result;
    }

    /// <summary>
    /// ID로 도구 데이터 찾기 (없으면 null)
    /// </summary>
    public ToolData FindTool(string toolId)
    {
        if (string.IsNullOrEmpty(toolId)) return null;
        for (int i = 0; i < tools.Count; i++)
        {
            if (tools[i] != null && tools[i].ToolID == toolId) return tools[i];
        }
        return null;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터 임포터 전용: 목록 교체
    /// </summary>
    public void EditorSetData(List<RecipeData> newRecipes, List<ToolData> newTools)
    {
        recipes = newRecipes;
        tools = newTools;
    }
#endif
}
