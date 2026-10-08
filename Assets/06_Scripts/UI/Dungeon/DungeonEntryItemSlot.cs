using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 던전 입장 UI 아이템 칸 ( 제출 아이템 / 보상 아이템 공용 )
/// 아이콘 + 우하단 수량 배지 ( x2 ) + 하단 캡션 ( 보유/필요 )
/// </summary>
public class DungeonEntryItemSlot : MonoBehaviour
{
    [SerializeField]
    private Image icon;
    [SerializeField, Tooltip("우하단 수량 배지 ( 보상 아이템 x2 )")]
    private TextMeshProUGUI badgeText;
    [SerializeField, Tooltip("칸 아래 문구 ( 제출 아이템 보유/필요 )")]
    private TextMeshProUGUI captionText;

    public void Set(Sprite sprite, string badge, string caption, Color captionColor)
    {
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        if (badgeText != null)
        {
            badgeText.gameObject.SetActive(!string.IsNullOrEmpty(badge));
            badgeText.text = badge;
        }

        if (captionText != null)
        {
            captionText.gameObject.SetActive(!string.IsNullOrEmpty(caption));
            captionText.text = caption;
            captionText.color = captionColor;
        }
    }
}
