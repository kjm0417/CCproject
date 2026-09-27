using UnityEngine;

public class HUDInventoryPanelView : MonoBehaviour
{
    [SerializeField] private HUDQuickSlot[] quickSlots;
    [SerializeField] private RectTransform itemsContent;
    [SerializeField] private HUDQuickSlot itemSlotTemplate;

    public HUDQuickSlot[] QuickSlots => quickSlots;
    public RectTransform ItemsContent => itemsContent;
    public HUDQuickSlot ItemSlotTemplate => itemSlotTemplate;
}
