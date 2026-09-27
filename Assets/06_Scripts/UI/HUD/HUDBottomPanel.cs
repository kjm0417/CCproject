using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

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
    [SerializeField] private HUDInventoryPanelView inventoryPanelView;
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
    [SerializeField, Min(0f)] private float defaultFoodHungerRecovery = 20f;

    private PlayerInventory inventory;
    private bool isInventoryOpen;
    private HUDQuickSlot quickSlotTemplate;
    private HUDQuickSlot inventoryItemSlotTemplate;
    private HUDQuickSlot[] inventoryQuickSlots;
    private RectTransform inventoryItemsContent;
    private readonly List<HUDQuickSlot> inventoryItemSlots = new List<HUDQuickSlot>();

    public event Action OnInventoryRequested;
    public bool IsInventoryOpen => isInventoryOpen;

    // 2. Context ���� �� 1ȸ �ʱ�ȭ
    protected override void OnInitialize()
    {
        AutoAssignReferences();
        inventory = Context.Inventory;
        SetInventoryOpen(false);

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

        if (inventoryQuickSlots != null)
        {
            for (int i = 0; i < inventoryQuickSlots.Length; i++)
            {
                inventoryQuickSlots[i]?.Initialize(i + 1);
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

        SetInventoryOpen(false);

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
        SetInventoryOpen(false);

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

        if (inventoryQuickSlots != null)
        {
            for (int i = 0; i < inventoryQuickSlots.Length; i++)
            {
                inventoryQuickSlots[i]?.Release();
            }
        }

        for (int i = 0; i < inventoryItemSlots.Count; i++)
        {
            inventoryItemSlots[i]?.Release();
        }

        inventory = null;
    }

    /// <summary>
    /// 화면에 보이는 퀵슬롯 번호(1부터)에 들어 있는 아이템. 없으면 null
    /// </summary>
    public InvenItemData GetQuickSlotItem(int slotNumber)
    {
        int index = slotNumber - 1;
        if (quickSlots == null || index < 0 || index >= quickSlots.Length || quickSlots[index] == null) return null;

        return quickSlots[index].Item;
    }

    private void HandleTargetChanged(IInteractable target, ToolType toolType)
    {
        if (attackButton == null) return;
        attackButton.UpdateTarget(target, toolType);
    }

    private void RefreshQuickSlots()
    {
        HashSet<InvenItemData> registeredItems = PopulateQuickSlotSet(quickSlots);

        if (inventoryQuickSlots != null)
        {
            PopulateQuickSlotSet(inventoryQuickSlots);
        }

        RefreshInventoryItems(registeredItems);
    }

    private HashSet<InvenItemData> PopulateQuickSlotSet(HUDQuickSlot[] targetSlots)
    {
        HashSet<InvenItemData> registeredItems = new HashSet<InvenItemData>();
        if (targetSlots == null) return registeredItems;

        for (int i = 0; i < targetSlots.Length; i++)
        {
            targetSlots[i]?.Clear();
        }

        SetToolSlot(targetSlots, 0, swordItemId, "칼", GetIcon(swordIcon, "Equipment/Sword"), registeredItems);
        SetToolSlot(targetSlots, 1, axeItemId, "도끼", GetIcon(axeIcon, "Equipment/Axe"), registeredItems);
        SetToolSlot(targetSlots, 2, pickaxeItemId, "곡괭이", GetIcon(pickaxeIcon, "Equipment/Pick"), registeredItems);
        SetGeneralItemSlots(targetSlots, registeredItems);
        SetFoodSlot(targetSlots, FoodSlotIndex, registeredItems);
        SetInventorySlot(targetSlots);
        return registeredItems;
    }

    private void AutoAssignReferences()
    {
        if (attackButton == null)
        {
            attackButton = GetComponentInChildren<UIAttackButton>(true);
        }

        BindInventoryPanel();

        HUDQuickSlot[] allSlots = GetComponentsInChildren<HUDQuickSlot>(true);
        List<HUDQuickSlot> mainSlots = new List<HUDQuickSlot>();
        for (int i = 0; i < allSlots.Length; i++)
        {
            if (allSlots[i].GetComponentInParent<HUDInventoryPanelView>(true) == null)
            {
                mainSlots.Add(allSlots[i]);
            }
        }

        HUDQuickSlot[] foundSlots = mainSlots.ToArray();
        if (foundSlots.Length > 0)
        {
            Transform slotParent = foundSlots[0].transform.parent;
            quickSlotTemplate = foundSlots[0];

            foundSlots[0].gameObject.SetActive(true);
            for (int i = foundSlots.Length; i < QuickSlotCount; i++)
            {
                HUDQuickSlot clone = Instantiate(foundSlots[0], slotParent);
                clone.gameObject.name = $"Btn_QuickSlot_{i + 1}";
                clone.gameObject.SetActive(true);
            }

            allSlots = GetComponentsInChildren<HUDQuickSlot>(true);
            mainSlots.Clear();
            for (int i = 0; i < allSlots.Length; i++)
            {
                if (allSlots[i].GetComponentInParent<HUDInventoryPanelView>(true) == null)
                {
                    mainSlots.Add(allSlots[i]);
                }
            }

            foundSlots = mainSlots.ToArray();
            Array.Sort(foundSlots, CompareHierarchyOrder);
            quickSlots = new HUDQuickSlot[QuickSlotCount];
            Array.Copy(foundSlots, quickSlots, Mathf.Min(QuickSlotCount, foundSlots.Length));

            for (int i = QuickSlotCount; i < foundSlots.Length; i++)
            {
                foundSlots[i].gameObject.SetActive(false);
            }

            ResizeSlotGrid(slotParent);

            if (slotParent.parent != null)
            {
                slotParent.parent.SetAsLastSibling();
            }
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

    private void BindInventoryPanel()
    {
        if (inventoryPanelView == null && inventoryPanel != null)
        {
            inventoryPanelView = inventoryPanel.GetComponent<HUDInventoryPanelView>();
        }

        if (inventoryPanelView == null) return;

        inventoryPanel = inventoryPanelView.gameObject;
        inventoryQuickSlots = inventoryPanelView.QuickSlots;
        inventoryItemsContent = inventoryPanelView.ItemsContent;
        inventoryItemSlotTemplate = inventoryPanelView.ItemSlotTemplate;
    }

    private void RefreshInventoryItems(HashSet<InvenItemData> registeredItems)
    {
        if (inventoryItemsContent == null || inventory == null) return;

        int visibleCount = 0;
        for (int i = 0; i < inventory.Slots.Count; i++)
        {
            InventorySlot inventorySlot = inventory.Slots[i];
            InvenItemData item = inventorySlot.Item;
            if (item == null || registeredItems.Contains(item)) continue;

            HUDQuickSlot slot = GetOrCreateInventoryItemSlot(visibleCount);
            slot.gameObject.SetActive(true);
            slot.Initialize(0);
            slot.SetNumberVisible(false);
            slot.SetItem(item, inventorySlot.Count, ResolveInventoryIcon(item));
            slot.SetItemName(item.ItemName);
            visibleCount++;
        }

        for (int i = visibleCount; i < inventoryItemSlots.Count; i++)
        {
            inventoryItemSlots[i].Clear();
            inventoryItemSlots[i].gameObject.SetActive(false);
        }
    }

    private HUDQuickSlot GetOrCreateInventoryItemSlot(int index)
    {
        while (inventoryItemSlots.Count <= index)
        {
            HUDQuickSlot template = inventoryItemSlotTemplate != null
                ? inventoryItemSlotTemplate
                : quickSlotTemplate;
            HUDQuickSlot slot = Instantiate(template, inventoryItemsContent);
            slot.gameObject.name = $"Inventory_ItemSlot_{inventoryItemSlots.Count + 1}";
            slot.gameObject.SetActive(true);
            slot.Initialize(0);
            slot.SetNumberVisible(false);
            inventoryItemSlots.Add(slot);
        }

        return inventoryItemSlots[index];
    }

    private void SetToolSlot(
        HUDQuickSlot[] targetSlots,
        int index,
        string itemId,
        string itemName,
        Sprite icon,
        HashSet<InvenItemData> registeredItems)
    {
        if (!IsValidSlot(targetSlots, index)) return;

        InventorySlot inventorySlot = FindItemSlot(itemId, itemName);
        targetSlots[index].SetTool(inventorySlot != null, icon);
        if (inventorySlot?.Item != null) registeredItems.Add(inventorySlot.Item);
    }

    private void SetGeneralItemSlots(HUDQuickSlot[] targetSlots, HashSet<InvenItemData> registeredItems)
    {
        if (inventory == null) return;

        int quickSlotIndex = FirstGeneralSlotIndex;
        for (int i = 0; i < inventory.Slots.Count && quickSlotIndex <= LastGeneralSlotIndex; i++)
        {
            InventorySlot inventorySlot = inventory.Slots[i];
            InvenItemData item = inventorySlot.Item;
            if (item == null || IsTool(item) || IsFood(item)) continue;
            if (!IsValidSlot(targetSlots, quickSlotIndex)) break;

            targetSlots[quickSlotIndex].SetItem(item, inventorySlot.Count, ResolveItemIcon(item));
            registeredItems.Add(item);
            quickSlotIndex++;
        }
    }

    private void SetFoodSlot(
        HUDQuickSlot[] targetSlots,
        int index,
        HashSet<InvenItemData> registeredItems)
    {
        if (!IsValidSlot(targetSlots, index)) return;

        InventorySlot foodSlot = FindFirstFoodSlot();
        if (foodSlot == null)
        {
            targetSlots[index].SetItem(null, 0, null);
            return;
        }

        InvenItemData foodItem = foodSlot.Item;
        targetSlots[index].SetItem(
            foodItem,
            foodSlot.Count,
            ResolveFoodIcon(foodItem),
            () => ConsumeFood(foodItem));
        registeredItems.Add(foodItem);
    }

    private void ConsumeFood(InvenItemData foodItem)
    {
        if (inventory == null || Context == null || Context.Vitals == null || foodItem == null) return;

        float hungerRecovery = foodItem.HungerRecovery > 0f
            ? foodItem.HungerRecovery
            : defaultFoodHungerRecovery;

        if (hungerRecovery <= 0f || Context.Vitals.HungerValue >= Context.Vitals.MaxHunger) return;

        if (inventory.Remove(foodItem, 1) > 0)
        {
            Context.Vitals.RecoverHunger(hungerRecovery);
        }
    }

    private InventorySlot FindItemSlot(string itemId, string itemName)
    {
        if (inventory == null) return null;

        for (int i = 0; i < inventory.Slots.Count; i++)
        {
            InvenItemData item = inventory.Slots[i].Item;
            if (item == null) continue;

            if (!string.IsNullOrEmpty(itemId) && item.ItemID == itemId) return inventory.Slots[i];
            if (!string.IsNullOrEmpty(itemName) && item.ItemName == itemName) return inventory.Slots[i];
        }

        return null;
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

    private void SetInventorySlot(HUDQuickSlot[] targetSlots)
    {
        if (!IsValidSlot(targetSlots, InventorySlotIndex)) return;

        Sprite icon = inventoryIcon;
        if (icon == null && btnInventory != null && btnInventory.targetGraphic is Image image)
        {
            icon = image.sprite;
        }

        targetSlots[InventorySlotIndex].SetCommand(icon, ToggleInventory);
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

    private Sprite ResolveInventoryIcon(InvenItemData item)
    {
        if (item == null) return null;

        if (item.ItemID == swordItemId || item.ItemName == "칼")
        {
            return GetIcon(swordIcon, "Equipment/Sword");
        }

        if (item.ItemID == axeItemId || item.ItemName == "도끼")
        {
            return GetIcon(axeIcon, "Equipment/Axe");
        }

        if (item.ItemID == pickaxeItemId || item.ItemName == "곡괭이")
        {
            return GetIcon(pickaxeIcon, "Equipment/Pick");
        }

        return ResolveItemIcon(item);
    }

    private Sprite ResolveFoodIcon(InvenItemData item)
    {
        return ResolveItemIcon(item);
    }

    private Sprite GetIcon(Sprite assignedIcon, string resourcesPath)
    {
        return assignedIcon != null ? assignedIcon : Resources.Load<Sprite>(resourcesPath);
    }

    private static bool IsValidSlot(HUDQuickSlot[] targetSlots, int index)
    {
        return targetSlots != null
            && index >= 0
            && index < targetSlots.Length
            && targetSlots[index] != null;
    }

    private void ToggleInventory()
    {
        if (inventoryPanel != null)
        {
            SetInventoryOpen(!inventoryPanel.activeSelf);
        }

        OnInventoryRequested?.Invoke();
    }

    public void SetInventoryOpen(bool isOpen)
    {
        isInventoryOpen = isOpen;

        if (inventoryPanel != null && inventoryPanel.activeSelf != isOpen)
        {
            inventoryPanel.SetActive(isOpen);
        }

        if (isOpen)
        {
            inventoryPanel?.transform.SetAsLastSibling();
            RefreshQuickSlots();
        }

        if (Context != null && Context.Movement != null)
        {
            Context.Movement.SetInputBlocked(isOpen);
        }
    }
}
