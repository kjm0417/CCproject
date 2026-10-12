using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class FoodSlotUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private Image nameBackground;
    [SerializeField] private TMP_Text nameText;
    private Action onSelected;
    private Color normalColor;
    public RecipeData Recipe { get; private set; }

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (nameBackground != null) normalColor = nameBackground.color;
        button.onClick.AddListener(Select);
    }

    public void Bind(RecipeData recipe, Action onClick)
    {
        Recipe = recipe;
        onSelected = onClick;
        if (nameText != null) nameText.text = recipe.ResultItem.ItemName;
        if (icon != null)
        {
            icon.sprite = CookingItemIcons.Resolve(recipe.ResultItem);
            icon.enabled = icon.sprite != null;
        }
    }

    public void Refresh(bool selected, bool canSelect)
    {
        button.interactable = canSelect;
        if (nameBackground != null)
            nameBackground.color = selected ? new Color(1f, 0.8f, 0.3f) : normalColor;
    }

    private void Select() => onSelected?.Invoke();
    private void OnDestroy() { if (button != null) button.onClick.RemoveListener(Select); }
}
