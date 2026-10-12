using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NeedFoodSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;
    private RecipeIngredient ingredient;
    public int OwnedCount { get; private set; }
    public int RequiredCount => ingredient != null ? ingredient.Count : 0;

    public void Bind(RecipeIngredient value)
    {
        ingredient = value;
        if (icon != null)
        {
            icon.sprite = CookingItemIcons.Resolve(value.Item);
            icon.enabled = icon.sprite != null;
        }
    }

    public void Refresh(PlayerInventory inventory)
    {
        OwnedCount = inventory != null && ingredient != null ? inventory.GetCount(ingredient.Item) : 0;
        if (countText == null) return;
        countText.text = $"{OwnedCount}/{RequiredCount}";
        countText.color = OwnedCount >= RequiredCount ? new Color(0.1f, 0.35f, 0.15f) : new Color(0.8f, 0.1f, 0.1f);
    }
}
