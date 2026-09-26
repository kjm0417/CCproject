using UnityEngine;
using UnityEngine.UI;
using System;

public class HUDBottomPanel : PlayerHUDPanelBase
{
    private const int QuickSlotCount = 8;
    private const int FirstGeneralSlotIndex = 3;
    private const int LastGeneralSlotIndex = 5;
    private const int FoodSlotIndex = 6;
    private const int InventorySlotIndex = 7;

    [Header("��ȣ�ۿ� �� ���� Ű")]
    [SerializeField] private UIAttackButton attackButton;
    [SerializeField] private Button btnAutoTarget;

    [Header("Quick Slots")]
    [SerializeField] private HUDQuickSlot[] quickSlots = new HUDQuickSlot[QuickSlotCount];
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Button btnInventory;

    [Header("Tool Item IDs")]
    [SerializeField] private string swordItemId = "25101";
    [SerializeField] private string axeItemId = "22101";
    [SerializeField] private string pickaxeItemId = "21101";

    [Header("Quick Slot Icons")]
    [SerializeField] private Sprite swordIcon;
    [SerializeField] private Sprite axeIcon;
    [SerializeField] private Sprite pickaxeIcon;
    [SerializeField] private Sprite foodFallbackIcon;
    [SerializeField] private Sprite inventoryIcon;

    [Header("Food Rule")]
    [SerializeField] private string foodSubType = "음식 아이템";

    private PlayerInventory inventory;

    public event Action OnInventoryRequested;

    // 2. Context ���� �� 1ȸ �ʱ�ȭ
    protected override void OnInitialize()
    {
        AutoAssignReferences();
        inventory = Context.Inventory;

        if (attackButton != null)
        {
            attackButton.Initialize(Context);
        }

        if (quickSlots != null)
        {
            for (int i = 0; i < quickSlots.Length; i++)
            {
                if (quickSlots[i] == null) continue;
                quickSlots[i].Initialize(i + 1);
            }
        }

        if (btnInventory != null)
        {
            btnInventory.onClick.RemoveListener(ToggleInventory);
            btnInventory.onClick.AddListener(ToggleInventory);
        }
    }

    // 3. Show �� �̺�Ʈ ����
    protected override void SubscribeEvents()
    {
        if (Context.Interaction != null)
        {
            Context.Interaction.OnTargetChanged += HandleTargetChanged;
        }

        if (inventory != null)
        {
            inventory.OnInventoryChanged += RefreshQuickSlots;
        }
    }

    // 4. Show �� ��� UI ����
    protected override void Refresh()
    {
        if (Context.Interaction != null && attackButton != null)
        {
            attackButton.UpdateTarget(Context.Interaction.CurrentTarget, Context.Interaction.CurrentTargetToolType);
        }

        RefreshQuickSlots();
    }

    // 5. Hide �� �̺�Ʈ ����
    protected override void UnsubscribeEvents()
    {
        if (Context == null) return;

        if (Context.Interaction != null)
        {
            Context.Interaction.OnTargetChanged -= HandleTargetChanged;
        }

        if (inventory != null)
        {
            inventory.OnInventoryChanged -= RefreshQuickSlots;
        }
    }

    // 6. Release �� ����
    protected override void OnRelease()
    {
        if (btnInventory != null)
        {
            btnInventory.onClick.RemoveListener(ToggleInventory);
        }

        if (attackButton != null)
        {
            attackButton.Release();
        }

        if (quickSlots != null)
        {
            for (int i = 0; i < quickSlots.Length; i++)
            {
                quickSlots[i]?.Release();
            }
        }

        inventory = null;
    }

    private void HandleTargetChanged(IInteractable target, ToolType toolType)
    {
        if (attackButton == null) return;
        attackButton.UpdateTarget(target, toolType);
    }

    private void RefreshQuickSlots()
    {
        if (quickSlots != null)
        {
            for (int i = 0; i < quickSlots.Length; i++)
            {
                quickSlots[i]?.Clear();
            }
        }

        SetToolSlot(0, swordItemId, "칼", GetIcon(swordIcon, "Equipment/Sword"));
        SetToolSlot(1, axeItemId, "도끼", GetIcon(axeIcon, "Equipment/Axe"));
        SetToolSlot(2, pickaxeItemId, "곡괭이", GetIcon(pickaxeIcon, "Equipment/Pick"));
        SetGeneralItemSlots();
        SetFoodSlot(FoodSlotIndex);
        SetInventorySlot();
    }

    private void AutoAssignReferences()
    {
        if (attackButton == null)
        {
            attackButton = GetComponentInChildren<UIAttackButton>(true);
        }

        HUDQuickSlot[] foundSlots = GetComponentsInChildren<HUDQuickSlot>(true);
        if (foundSlots.Length > 0)
        {
            Transform slotParent = foundSlots[0].transform.parent;

            foundSlots[0].gameObject.SetActive(true);
            for (int i = foundSlots.Length; i < QuickSlotCount; i++)
            {
                HUDQuickSlot clone = Instantiate(foundSlots[0], slotParent);
                clone.gameObject.name = $"Btn_QuickSlot_{i + 1}";
                clone.gameObject.SetActive(true);
            }

            foundSlots = GetComponentsInChildren<HUDQuickSlot>(true);
            Array.Sort(foundSlots, CompareHierarchyOrder);
            quickSlots = new HUDQuickSlot[QuickSlotCount];
            Array.Copy(foundSlots, quickSlots, Mathf.Min(QuickSlotCount, foundSlots.Length));

            for (int i = QuickSlotCount; i < foundSlots.Length; i++)
            {
                foundSlots[i].gameObject.SetActive(false);
            }

            ResizeSlotGrid(slotParent);
        }
    }

    private static void ResizeSlotGrid(Transform slotParent)
    {
        if (slotParent == null) return;

        GridLayoutGroup grid = slotParent.GetComponent<GridLayoutGroup>();
        RectTransform rect = slotParent as RectTransform;
        if (grid == null || rect == null) return;

        float availableWidth = rect.rect.width - grid.padding.left - grid.padding.right;
        availableWidth -= grid.spacing.x * (QuickSlotCount - 1);
        float width = availableWidth > 0f ? availableWidth / QuickSlotCount : grid.cellSize.x;

        float availableHeight = rect.rect.height - grid.padding.top - grid.padding.bottom;
        float height = availableHeight > 0f ? availableHeight : grid.cellSize.y;
        grid.cellSize = new Vector2(width, height);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = QuickSlotCount;
    }

    private static int CompareHierarchyOrder(HUDQuickSlot left, HUDQuickSlot right)
    {
        return left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex());
    }

    private void SetToolSlot(int index, string itemId, string itemName, Sprite icon)
    {
        if (!IsValidSlot(index)) return;

        bool isOwned = HasItem(itemId, itemName);
        quickSlots[index].SetTool(isOwned, icon);
    }

    private void SetGeneralItemSlots()
    {
        if (inventory == null) return;

        int quickSlotIndex = FirstGeneralSlotIndex;
        for (int i = 0; i < inventory.Slots.Count && quickSlotIndex <= LastGeneralSlotIndex; i++)
        {
            InventorySlot inventorySlot = inventory.Slots[i];
            InvenItemData item = inventorySlot.Item;
            if (item == null || IsTool(item) || IsFood(item)) continue;
            if (!IsValidSlot(quickSlotIndex)) break;

            quickSlots[quickSlotIndex].SetItem(item, inventorySlot.Count, ResolveItemIcon(item));
            quickSlotIndex++;
        }
    }

    private void SetFoodSlot(int index)
    {
        if (!IsValidSlot(index)) return;

        InventorySlot foodSlot = FindFirstFoodSlot();
        if (foodSlot == null)
        {
            quickSlots[index].SetItem(null, 0, null);
            return;
        }

        quickSlots[index].SetItem(foodSlot.Item, foodSlot.Count, ResolveFoodIcon(foodSlot.Item));
    }

    private bool HasItem(string itemId, string itemName)
    {
        if (inventory == null) return false;

        for (int i = 0; i < inventory.Slots.Count; i++)
        {
            InvenItemData item = inventory.Slots[i].Item;
            if (item == null) continue;

            if (!string.IsNullOrEmpty(itemId) && item.ItemID == itemId) return true;
            if (!string.IsNullOrEmpty(itemName) && item.ItemName == itemName) return true;
        }

        return false;
    }

    private InventorySlot FindFirstFoodSlot()
    {
        if (inventory == null) return null;

        for (int i = 0; i < inventory.Slots.Count; i++)
        {
            InvenItemData item = inventory.Slots[i].Item;
            if (item != null && item.SubType == foodSubType)
            {
                return inventory.Slots[i];
            }
        }

        return null;
    }

    private bool IsTool(InvenItemData item)
    {
        if (item == null) return false;

        return item.ItemID == swordItemId
            || item.ItemID == axeItemId
            || item.ItemID == pickaxeItemId
            || item.ItemName == "칼"
            || item.ItemName == "도끼"
            || item.ItemName == "곡괭이";
    }

    private bool IsFood(InvenItemData item)
    {
        return item != null && item.SubType == foodSubType;
    }

    private void SetInventorySlot()
    {
        if (!IsValidSlot(InventorySlotIndex)) return;

        Sprite icon = inventoryIcon;
        if (icon == null && btnInventory != null && btnInventory.targetGraphic is Image image)
        {
            icon = image.sprite;
        }

        quickSlots[InventorySlotIndex].SetCommand(icon, ToggleInventory);
    }

    private Sprite ResolveItemIcon(InvenItemData item)
    {
        if (item == null) return null;

        string resourcesPath;
        switch (item.ItemID)
        {
            case "11001":
                resourcesPath = "Inven_Drop/Log";
                break;
            case "11002":
                resourcesPath = "Inven_Drop/Gravel";
                break;
            case "11003":
                resourcesPath = "Inven_Drop/Coal";
                break;
            case "13001":
                resourcesPath = "Inven_Drop/Apple";
                break;
            case "13002":
                resourcesPath = "Inven_Drop/Broil_Apple";
                break;
            case "13004":
                resourcesPath = "Inven_Drop/Plum";
                break;
            default:
                return IsFood(item) ? foodFallbackIcon : null;
        }

        Sprite loadedIcon = Resources.Load<Sprite>(resourcesPath);
        return loadedIcon != null ? loadedIcon : foodFallbackIcon;
    }

    private Sprite ResolveFoodIcon(InvenItemData item)
    {
        return ResolveItemIcon(item);
    }

    private Sprite GetIcon(Sprite assignedIcon, string resourcesPath)
    {
        return assignedIcon != null ? assignedIcon : Resources.Load<Sprite>(resourcesPath);
    }

    private bool IsValidSlot(int index)
    {
        return quickSlots != null && index >= 0 && index < quickSlots.Length && quickSlots[index] != null;
    }

    private void ToggleInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        }

        OnInventoryRequested?.Invoke();
    }
}
