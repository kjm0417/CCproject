using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 생활 분류 버튼 아래에 펼쳐지는 레시피(생활 도구) 한 줄
/// </summary>
public class CraftRecipeEntryView : MonoBehaviour
{
    [SerializeField]
    private Button button;
    [SerializeField]
    private Image iconImage;
    [SerializeField]
    private TMP_Text nameText;
    [Tooltip("선택 시 켜지는 강조 표시 (선택)")]
    [SerializeField]
    private GameObject selectedMark;

    /// <summary>
    /// 아이콘 / 이름 / 클릭 콜백 설정
    /// </summary>
    public void Setup(Sprite icon, string displayName, Action onClick)
    {
        if (button == null) button = GetComponent<Button>();
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }
        if (nameText != null) nameText.text = displayName;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }
        SetSelected(false);
    }

    /// <summary>
    /// 선택 강조 표시
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (selectedMark != null) selectedMark.SetActive(selected);
    }
}
