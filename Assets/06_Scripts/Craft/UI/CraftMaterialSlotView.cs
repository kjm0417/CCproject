using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 제작 재료 한 칸 (아이콘 + 보유/필요 개수)
/// </summary>
public class CraftMaterialSlotView : MonoBehaviour
{
    [SerializeField]
    private Image iconImage;
    [SerializeField]
    private TMP_Text countText;
    [SerializeField]
    private Color enoughColor = Color.white;
    [SerializeField]
    private Color lackColor = new Color(1f, 0.4f, 0.4f);

    /// <summary>
    /// 재료 표시. have/need 형식, 부족하면 lackColor
    /// </summary>
    public void Set(Sprite icon, int have, int need)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }
        if (countText != null)
        {
            countText.text = $"{have}/{need}";
            countText.color = have >= need ? enoughColor : lackColor;
        }
    }

    /// <summary>
    /// 빈 칸 표시 (재료 없음)
    /// </summary>
    public void Clear()
    {
        if (iconImage != null) iconImage.enabled = false;
        if (countText != null) countText.text = string.Empty;
    }
}
