using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 제작 UI 우측 생활 분류 (산업 / 농업 / 경제 / 마법)
/// </summary>
public enum CraftLifeType
{
    None,
    Industry,
    Agriculture,
    Economy,
    Magic,
}

/// <summary>
/// 레시피 재료 한 줄 (아이템 ID + 필요 개수)
/// </summary>
[Serializable]
public struct CraftMaterial
{
    public string ItemID;
    public int Count;

    public CraftMaterial(string itemId, int count)
    {
        ItemID = itemId;
        Count = count;
    }
}

/// <summary>
/// 조합식&레시피 시트 한 행. 결과물(ResultItemID)은 도구(2xxxx)면 ToolData, 그 외는 InvenItemData로 찾는다.
/// </summary>
[CreateAssetMenu(fileName = "RecipeData", menuName = "Game/Craft/RecipeData")]
public class RecipeData : ScriptableObject
{
    public string CraftRecipeID;
    public string ResultItemID;
    [Min(1)] public int ResultCount = 1;
    [Tooltip("이 레시피를 제작할 수 있는 시설 오브젝트 ID (예: 42003 작업대)")]
    public string CraftFacilityID;
    [Min(0f)] public float CraftTimeSec;
    public List<CraftMaterial> Materials = new List<CraftMaterial>();
    public CraftLifeType LifeType;
    [Tooltip("Spring / Summer / Autumn / Winter / All")]
    public string Season;

    /// <summary>
    /// 결과물이 도구인지 (ID 체계: 첫째 자리 2 = 도구)
    /// </summary>
    public bool IsToolResult => !string.IsNullOrEmpty(ResultItemID) && ResultItemID[0] == '2';

    /// <summary>
    /// 해당 계절에 제작 가능한지 (Season이 비어 있거나 All이면 항상 가능)
    /// </summary>
    public bool MatchesSeason(string season)
    {
        if (string.IsNullOrEmpty(Season) || Season == "All") return true;
        return string.Equals(Season, season, StringComparison.OrdinalIgnoreCase);
    }
}
