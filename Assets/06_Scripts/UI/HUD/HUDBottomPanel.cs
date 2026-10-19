using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class HUDBottomPanel : PlayerHUDPanelBase
{
    private const int QuickSlotCount = 8;
    private const int InventorySlotIndex = 7;
    private const float QuickSlotMaxWidth = 700f;
    private const float QuickSlotMaxHeight = 80f;
    private const float QuickSlotHorizontalMargin = 16f;

    [Header("상호작용 버튼")]
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
    [SerializeField] private string shovelItemId = "23101";

    [Header("Quick Slot Icons")]
    [SerializeField] private Sprite swordIcon;
    [SerializeField] private Sprite axeIcon;
    [SerializeField] private Sprite pickaxeIcon;
    [SerializeField] private Sprite shovelIcon;
    [SerializeField] private Sprite foodFallbackIcon;
    [SerializeField] private Sprite inventoryIcon;

    [Header("Food Rule")]
    [SerializeField] private string foodSubType = "음식 아이템";
    [SerializeField, Min(0f)] private float defaultFoodHungerRecovery = 20f;

    [Header("Seed Rule")]
    [SerializeField] private string seedSubType = "씨앗 아이템";

    private PlayerInventory inventory;
    private bool isInventoryOpen;
    private HUDQuickSlot quickSlotTemplate;
    private Transform quickSlotGridTransform;
    private HUDQuickSlot inventoryItemSlotTemplate;
    private HUDQuickSlot[] inventoryQuickSlots;
    private RectTransform inventoryItemsContent;
    private readonly List<HUDQuickSlot> inventoryItemSlots = new List<HUDQuickSlot>();
    private InvenItemData selectedQuickSlotItem;
    private string selectedQuickSlotItemId;

    // 상자 등 외부 UI가 열려 있을 때 인벤토리 칸 클릭 동작을 대체 (null이면 기본 동작)
    private Action<InventorySlot> itemClickOverride;

    public event Action OnInventoryRequested;
    public bool IsInventoryOpen => isInventoryOpen;

    private void OnRectTransformDimensionsChange()
    {
        if (quickSlotGridTransform != null)
        {
            ResizeSlotGrid(quickSlotGridTransform);
        }
    }

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

        if (Context.Combat != null)
        {
            Context.Combat.OnTargetAvailabilityChanged += HandleAttackTargetAvailabilityChanged;
        }

        if (inventory != null)
        {
            inventory.OnInventoryChanged += RefreshQuickSlots;
        }
    }

    // 4. Show �� ��� UI ����
    protected override void Refresh()
    {
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

        if (Context.Combat != null)
        {
            Context.Combat.OnTargetAvailabilityChanged -= HandleAttackTargetAvailabilityChanged;
        }

        if (inventory != null)
        {
            inventory.OnInventoryChanged -= RefreshQuickSlots;
        }

        Context.HeldTool?.Hide();
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

    private void HandleTargetChanged(IInteractable target, ToolType toolType)
    {
        if (attackButton == null) return;

        if (selectedQuickSlotItem != null)
        {
            RefreshInteractionButton();
            return;
        }

        attackButton.UpdateTarget(target, toolType);
        UpdateHeldTool();
    }

    private void HandleAttackTargetAvailabilityChanged(bool hasTarget)
    {
        RefreshInteractionButton();
    }

    private void RefreshQuickSlots()
    {
        ValidateSelectedQuickSlotItem();

        HashSet<InventorySlot> registeredSlots = PopulateQuickSlotSet(quickSlots);

        if (inventoryQuickSlots != null)
        {
            PopulateQuickSlotSet(inventoryQuickSlots);
        }

        RefreshInventoryItems(registeredSlots);
        RefreshInteractionButton();
    }

    // 퀵슬롯에 표시한 인벤토리 칸 목록 반환 (같은 아이템이 여러 칸이어도 칸 단위로 구분)
    private HashSet<InventorySlot> PopulateQuickSlotSet(HUDQuickSlot[] targetSlots)
    {
        HashSet<InventorySlot> registeredSlots = new HashSet<InventorySlot>();
        if (targetSlots == null) return registeredSlots;


        for (int i = 0; i < targetSlots.Length; i++)
        {
            targetSlots[i]?.Clear();
        }

        if (inventory != null)
        {
            int visibleItemCount = Mathf.Min(inventory.Slots.Count, InventorySlotIndex);
            for (int i = 0; i < visibleItemCount; i++)
            {
                if (!IsValidSlot(targetSlots, i)) continue;

                InventorySlot inventorySlot = inventory.Slots[i];
                InvenItemData item = inventorySlot.Item;
                if (item == null) continue;

                int displayCount = item.MaxStack > 1 || inventorySlot.Count > 1
                    ? inventorySlot.Count
                    : 0;
                Sprite icon = ResolveInventoryIcon(item);
                targetSlots[i].SetItem(item, displayCount, icon, () => OnQuickSlotItemClicked(inventorySlot, item));

                targetSlots[i].SetSelected(IsSelectedQuickSlotItem(item));

                registeredSlots.Add(inventorySlot);

            }
        }

        SetInventorySlot(targetSlots);

        return registeredSlots;

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
            quickSlotGridTransform = slotParent;

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

    private void ResizeSlotGrid(Transform slotParent)
    {
        if (slotParent == null) return;

        GridLayoutGroup grid = slotParent.GetComponent<GridLayoutGroup>();
        RectTransform rect = slotParent as RectTransform;
        if (grid == null || rect == null) return;

        RectTransform quickSlotRoot = rect.parent as RectTransform;
        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null
            ? canvas.rootCanvas.transform as RectTransform
            : null;

        if (quickSlotRoot != null && canvasRect != null && canvasRect.rect.width > 0f)
        {
            float targetWidth = Mathf.Min(
                QuickSlotMaxWidth,
                Mathf.Max(QuickSlotCount, canvasRect.rect.width - QuickSlotHorizontalMargin * 2f));
            float sizeRatio = targetWidth / QuickSlotMaxWidth;

            quickSlotRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
            quickSlotRoot.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                QuickSlotMaxHeight * sizeRatio);
        }

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


    private void RefreshInventoryItems(HashSet<InventorySlot> registeredSlots)
    {
        if (inventoryItemsContent == null || inventory == null) return;

        // 퀵슬롯은 바로가기이며, 하단에는 빈 칸까지 전체 인벤토리를 표시한다.
        for (int i = 0; i < inventory.MaxSlots; i++)
        {
            HUDQuickSlot slot = GetOrCreateInventoryItemSlot(i);
            slot.gameObject.SetActive(true);

            if (i >= inventory.Slots.Count || inventory.Slots[i].Item == null)
            {
                slot.Clear();
                continue;
            }

            InventorySlot inventorySlot = inventory.Slots[i];
            InvenItemData item = inventorySlot.Item;

            UnityEngine.Events.UnityAction onClick = itemClickOverride != null
                ? () => itemClickOverride?.Invoke(inventorySlot)
                : null;

            slot.SetItem(item, inventorySlot.Count, ResolveInventoryIcon(item), onClick);
            slot.SetItemName(item.ItemName);
        }

        for (int i = inventory.MaxSlots; i < inventoryItemSlots.Count; i++)
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
            RefreshInteractionButton();
        }
    }

    private bool IsFood(InvenItemData item)
    {
        return item != null && item.SubType == foodSubType;
    }

    private bool IsSeed(InvenItemData item)
    {
        return item != null && item.SubType == seedSubType;
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
        targetSlots[InventorySlotIndex].SetSelected(false);
    }

    /// <summary>
    /// 외부 UI(상자 등)가 인벤토리 칸 클릭 동작을 대체하도록 등록. null이면 기본 동작(퀵슬롯 선택)으로 복구.
    /// </summary>
    public void SetItemClickOverride(Action<InventorySlot> handler)
    {
        itemClickOverride = handler;
        RefreshQuickSlots();
    }

    /// <summary>
    /// 퀵슬롯 아이템 클릭. 대체 동작이 등록돼 있으면 그쪽으로, 아니면 아이템 선택.
    /// </summary>
    private void OnQuickSlotItemClicked(InventorySlot inventorySlot, InvenItemData item)
    {
        if (itemClickOverride != null)
        {
            itemClickOverride.Invoke(inventorySlot);
            return;
        }

        SelectQuickSlotItem(item);
    }

    private void SelectQuickSlotItem(InvenItemData item)
    {
        if (item == null) return;

        if (IsSelectedQuickSlotItem(item))
        {
            ClearQuickSlotSelection();
            return;
        }

        selectedQuickSlotItem = item;
        selectedQuickSlotItemId = item.ItemID;
        Context?.Interaction?.SetCurrentTool(ResolveToolType(item), true);
        RefreshQuickSlotSelection();
        RefreshInteractionButton();
    }

    private void ClearQuickSlotSelection()
    {
        selectedQuickSlotItem = null;
        selectedQuickSlotItemId = null;
        Context?.Interaction?.SetCurrentTool(ToolType.None, false);
        RefreshQuickSlotSelection();
        RefreshInteractionButton();
    }

    private void ValidateSelectedQuickSlotItem()
    {
        if (selectedQuickSlotItem == null && string.IsNullOrEmpty(selectedQuickSlotItemId)) return;
        if (inventory != null)
        {
            for (int i = 0; i < inventory.Slots.Count; i++)
            {
                InvenItemData item = inventory.Slots[i].Item;
                if (IsSelectedQuickSlotItem(item))
                {
                    selectedQuickSlotItem = item;
                    return;
                }
            }
        }

        selectedQuickSlotItem = null;
        selectedQuickSlotItemId = null;
        Context?.Interaction?.SetCurrentTool(ToolType.None, false);
        attackButton?.ClearQuickSlotOverride();
    }

    private bool IsSelectedQuickSlotItem(InvenItemData item)
    {
        if (item == null) return false;
        if (item == selectedQuickSlotItem) return true;

        return !string.IsNullOrEmpty(selectedQuickSlotItemId)
            && item.ItemID == selectedQuickSlotItemId;
    }

    private void RefreshQuickSlotSelection()
    {
        ApplyQuickSlotSelection(quickSlots);
        ApplyQuickSlotSelection(inventoryQuickSlots);
    }

    private void ApplyQuickSlotSelection(HUDQuickSlot[] targetSlots)
    {
        if (targetSlots == null || inventory == null) return;

        int itemSlotCount = Mathf.Min(inventory.Slots.Count, InventorySlotIndex);
        for (int i = 0; i < targetSlots.Length; i++)
        {
            bool isSelected = i < itemSlotCount
                && IsSelectedQuickSlotItem(inventory.Slots[i].Item);
            targetSlots[i]?.SetSelected(isSelected);
        }
    }

    private ToolType ResolveToolType(InvenItemData item)
    {
        if (item == null) return ToolType.None;
        // 강화 도구도 같은 종류로 판정 (21201 -> 21101)
        string baseId = ToolItemIds.ToBaseId(item.ItemID);
        if (baseId == swordItemId || item.ItemName == "칼") return ToolType.Sword;
        if (baseId == axeItemId || item.ItemName == "도끼") return ToolType.Axe;
        if (baseId == pickaxeItemId || item.ItemName == "곡괭이") return ToolType.Pickaxe;
        if (baseId == shovelItemId || item.ItemName == "삽") return ToolType.Shovel;
        return ToolType.None;
    }

    private void RefreshInteractionButton()
    {
        ApplyInteractionButton();
        UpdateHeldTool();
    }

    /// <summary>
    /// 상호작용 버튼에 표시 중인 도구를 플레이어 손에도 표시 ( 버튼에 도구가 없으면 숨김 )
    /// </summary>
    private void UpdateHeldTool()
    {
        PlayerHeldTool heldTool = Context != null ? Context.HeldTool : null;
        if (heldTool == null) return;

        heldTool.Show(ResolveHeldToolSprite());
    }

    private Sprite ResolveHeldToolSprite()
    {
        if (attackButton == null) return null;

        // 퀵슬롯 선택 중: 선택한 아이템이 도구일 때만 ( 버튼 아이콘과 같은 스프라이트 )
        if (selectedQuickSlotItem != null)
        {
            return ToolItemIds.IsTool(selectedQuickSlotItem.ItemID)
                ? ResolveInventoryIcon(selectedQuickSlotItem)
                : null;
        }

        // 공격 범위에 적이 있으면 버튼이 검으로 바뀜
        PlayerCombat combat = Context != null ? Context.Combat : null;
        if (combat != null && combat.HasTargetInAttackArea())
        {
            return ResolveOwnedToolSprite(swordItemId, GetIcon(swordIcon, "Equipment/Sword"));
        }

        // 대상에 맞춰 자동 선택된 도구
        PlayerInteraction interaction = Context != null ? Context.Interaction : null;
        if (interaction == null) return null;

        switch (interaction.CurrentTargetToolType)
        {
            case ToolType.Sword:
                return ResolveOwnedToolSprite(swordItemId, GetIcon(swordIcon, "Equipment/Sword"));
            case ToolType.Axe:
                return ResolveOwnedToolSprite(axeItemId, GetIcon(axeIcon, "Equipment/Axe"));
            case ToolType.Pickaxe:
                return ResolveOwnedToolSprite(pickaxeItemId, GetIcon(pickaxeIcon, "Equipment/Pick"));
            case ToolType.Shovel:
                return ResolveOwnedToolSprite(shovelItemId, GetIcon(shovelIcon, "Equipment/Shovel"));
            default:
                return null;
        }
    }

    /// <summary>
    /// 보유한 같은 종류 도구 중 가장 높은 등급의 아이콘 ( 강화 도구 우선 ), 없으면 기본 아이콘
    /// </summary>
    private Sprite ResolveOwnedToolSprite(string baseToolId, Sprite fallback)
    {
        InvenItemData bestItem = null;
        if (inventory != null)
        {
            for (int i = 0; i < inventory.Slots.Count; i++)
            {
                InvenItemData item = inventory.Slots[i].Item;
                if (item == null || !ToolItemIds.IsSameTool(item.ItemID, baseToolId)) continue;

                // 등급 자리 ( 2 X G YY 의 G ) 비교
                if (bestItem == null || item.ItemID[2] > bestItem.ItemID[2])
                {
                    bestItem = item;
                }
            }
        }

        Sprite icon = bestItem != null ? ResolveInventoryIcon(bestItem) : null;
        return icon != null ? icon : fallback;
    }

    private void ApplyInteractionButton()
    {
        if (attackButton == null) return;

        PlayerInteraction interaction = Context != null ? Context.Interaction : null;
        if (selectedQuickSlotItem == null)
        {
            PlayerCombat combat = Context != null ? Context.Combat : null;
            if (combat != null && combat.HasTargetInAttackArea())
            {
                Sprite swordAttackIcon = GetIcon(swordIcon, "Equipment/Sword");
                attackButton.SetQuickSlotOverride(swordAttackIcon, () => combat.TryAttack(), true);
                return;
            }

            attackButton.ClearQuickSlotOverride();
            if (interaction != null)
            {
                attackButton.UpdateTarget(interaction.CurrentTarget, interaction.CurrentTargetToolType);
            }
            return;
        }

        InvenItemData item = selectedQuickSlotItem;
        Sprite icon = ResolveInventoryIcon(item);
        if (IsFood(item))
        {
            bool canConsume = Context != null
                && Context.Vitals != null
                && Context.Vitals.HungerValue < Context.Vitals.MaxHunger;
            attackButton.SetQuickSlotOverride(icon, () => ConsumeFood(item), canConsume);
            return;
        }

        if (IsSeed(item))
        {
            // 발밑 칸은 이동할 때마다 바뀌므로 버튼은 켜두고, 심을 수 있는지는 누를 때 판정
            CropPlanter cropPlanter = TerritoryManager.Instance != null ? TerritoryManager.Instance.CropPlanter : null;
            bool canPlant = interaction != null
                && ((cropPlanter != null && cropPlanter.IsPlantable(item))
                    || interaction.CurrentTarget is DungeonSeedTile); // 던전 씨앗 타일 근처
            bool canHarvest = interaction != null
                && interaction.CurrentTarget is PlantedCropObject crop
                && crop.IsHarvestable;
            attackButton.SetQuickSlotOverride(icon, () => interaction.TryHarvestOrPlantSeed(item), canPlant || canHarvest);

            #region [이전] 씨앗 선택 시 심기만 (수확 불가)
            // attackButton.SetQuickSlotOverride(icon, () => interaction.TryPlantSeed(item), canPlant);
            #endregion
            return;
        }

        // 설치 아이템(상자 / 작업대 / 모닥불): 발밑 칸에 배치. 배치 가능 여부는 누를 때 판정
        StructurePlacer placer = TerritoryManager.Instance != null ? TerritoryManager.Instance.StructurePlacer : null;
        if (placer != null && placer.IsPlaceable(item))
        {
            attackButton.SetQuickSlotOverride(icon, () => interaction.TryPlaceStructure(item), interaction != null);
            return;
        }

        ToolType toolType = ResolveToolType(item);
        if (toolType == ToolType.None || interaction == null)
        {
            attackButton.SetQuickSlotOverride(icon, null, false);
            return;
        }

        if (toolType == ToolType.Sword)
        {
            PlayerCombat combat = Context != null ? Context.Combat : null;
            attackButton.SetQuickSlotOverride(icon, () => combat.TryAttack(), combat != null);
            return;
        }

        // 삽은 흙 타일이 있는 씬 ( 영지 ) 에서만 단독 사용 가능 - 던전은 근처 상호작용 대상이 있을 때만
        bool canDig = toolType == ToolType.Shovel
            && TerritoryManager.Instance != null && TerritoryManager.Instance.TileModifier != null;
        bool canInteract = canDig
            || (interaction.CurrentTarget != null
                && interaction.CurrentTargetToolType == toolType);
        attackButton.SetQuickSlotOverride(icon, interaction.Interact, canInteract);
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
            case "13003":
                resourcesPath = "Inven_Drop/Garlic";
                break;
            case "13005":
                resourcesPath = "Inven_Drop/Berly";
                break;
            case "13006":
                resourcesPath = "Inven_Drop/Onion";
                break;
            case "13007":
                resourcesPath = "Inven_Drop/Mullbery";
                break;
            default:
                return IsFood(item) ? GetFoodFallbackIcon() : null;
        }

        Sprite loadedIcon = Resources.Load<Sprite>(resourcesPath);
        return loadedIcon != null ? loadedIcon : GetFoodFallbackIcon();
    }

    /// <summary>
    /// 외부 UI(상자 등)에서 인벤토리와 같은 아이콘을 쓰기 위한 공개 진입점
    /// </summary>
    public Sprite GetItemIcon(InvenItemData item)
    {
        return ResolveInventoryIcon(item);
    }

    private Sprite ResolveInventoryIcon(InvenItemData item)
    {
        if (item == null) return null;

        // 아이템 ID별 전용 아이콘 (Resources/ItemIcons/{ItemID}) 우선
        Sprite idIcon = Resources.Load<Sprite>(ToolItemIds.IconPath(item.ItemID));
        if (idIcon != null) return idIcon;

        // 전용 아이콘 없는 강화 도구는 기본 도구 아이콘 사용
        string baseId = ToolItemIds.ToBaseId(item.ItemID);
        if (baseId == swordItemId || item.ItemName == "칼")
        {
            return GetIcon(swordIcon, "Equipment/Sword");
        }

        if (baseId == axeItemId || item.ItemName == "도끼")
        {
            return GetIcon(axeIcon, "Equipment/Axe");
        }

        if (baseId == pickaxeItemId || item.ItemName == "곡괭이")
        {
            return GetIcon(pickaxeIcon, "Equipment/Pick");
        }

        if (baseId == shovelItemId || item.ItemName == "삽")
        {
            return GetIcon(shovelIcon, "Equipment/Shovel");
        }

        // 던전 기믹 씨앗은 SO에 지정한 아이콘
        if (item is GimmickSeedData gimmickSeed && gimmickSeed.Icon != null)
        {
            return gimmickSeed.Icon;
        }

        if (IsSeed(item))
        {
            // 씨앗은 심을 작물 prefab의 Sprite로 표시
            CropPlanter cropPlanter = TerritoryManager.Instance != null ? TerritoryManager.Instance.CropPlanter : null;
            Sprite seedIcon = cropPlanter != null ? cropPlanter.GetSeedIcon(item) : null;
            if (seedIcon != null) return seedIcon;
        }

        return ResolveItemIcon(item);
    }

    private Sprite GetIcon(Sprite assignedIcon, string resourcesPath)
    {
        return assignedIcon != null ? assignedIcon : Resources.Load<Sprite>(resourcesPath);
    }

    private Sprite GetFoodFallbackIcon()
    {
        return foodFallbackIcon != null
            ? foodFallbackIcon
            : Resources.Load<Sprite>("Inven_Drop/Apple");
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
            // 상자 등 외부 UI가 열려 있으면 인벤토리를 닫아도 이동 정지 유지
            Context.Movement.SetInputBlocked(isOpen || itemClickOverride != null);
        }
    }
}
