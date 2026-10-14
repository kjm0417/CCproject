using UnityEngine;

/// <summary>
/// 도구 및 장비 시트 한 행. 인벤토리 아이템과 별도로 PlayerToolBag에서 보유를 관리한다.
/// </summary>
[CreateAssetMenu(fileName = "ToolData", menuName = "Game/Craft/ToolData")]
public class ToolData : ScriptableObject
{
    public string ToolID;
    public string ToolName;
    [Tooltip("Pickaxe / Axe / Shovel / Sword / Hoe / FishingRod / Staff ...")]
    public string ToolType;
    public string ToolGrade;
    public string Description;
    public float Power;
    public string CraftRecipeID;

    [Header("표시")]
    [Tooltip("제작 UI / HUD 아이콘. 비우면 Resources/Equipment/{ToolType} 로드 시도")]
    public Sprite Icon;

    /// <summary>
    /// 아이콘 반환. 지정 아이콘이 없으면 Resources에서 ToolType 이름으로 찾는다.
    /// </summary>
    public Sprite GetIcon()
    {
        if (Icon == null && !string.IsNullOrEmpty(ToolType)) Icon = Resources.Load<Sprite>($"Equipment/{ToolType}");
        return Icon;
    }
}
