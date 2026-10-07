using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보유 아이템 목록 UI (HUD Inventory Panel View의 아이템 영역과 같은 역할의 별도 View).
/// PlayerInventory.OnInventoryChanged를 구독해서 HUD 인벤토리와 항상 같은 내용으로 동기화된다.
/// 상자 UI 등에서 Show/Hide로 켜고 끄며, 칸 클릭 동작을 외부에서 넘겨받는다.
/// </summary>
public class HUDOwnedItemsView : MonoBehaviour
{
    // HUDBottomPanel의 퀵슬롯 아이템 칸 수 (InventorySlotIndex)와 같아야 한다.
    private const int QuickSlotItemCount = 7;

    [Header("패널")]
    [Tooltip("비워두면 이 오브젝트")]
    [SerializeField]
    private GameObject panelRoot;

    [Header("아이템 슬롯")]
    [SerializeField]
    private RectTransform itemsContent;
    [SerializeField]
    private HUDQuickSlot itemSlotTemplate;

    [Header("표시 규칙")]
    [Tooltip("켜면 퀵슬롯에 올라간 아이템은 제외 (HUD 인벤토리 아이템 영역과 동일). 끄면 전체 보유 아이템 표시")]
    [SerializeField]
    private bool excludeQuickSlotItems = true;

    private readonly List<HUDQuickSlot> itemSlotViews = new List<HUDQuickSlot>();
    private readonly HashSet<InvenItemData> quickSlotItems = new HashSet<InvenItemData>();

    private PlayerInventory inventory;
    private Action<InventorySlot> onItemClick;
    private HUDBottomPanel hudPanel;

    /// <summary>
    /// 현재 표시 중인지 여부
    /// </summary>
    public bool IsShown => inventory != null;

    /// <summary>
    /// 템플릿 숨김, 패널 초기 비활성화
    /// </summary>
    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
        if (itemSlotTemplate != null) itemSlotTemplate.gameObject.SetActive(false);

        // panelRoot 자신에 붙어 비활성으로 시작한 경우, Show 중 Awake가 호출되므로 그때는 숨기지 않는다.
        if (inventory == null) panelRoot.SetActive(false);
    }

    /// <summary>
    /// 보유 아이템 UI 표시. 인벤토리 변경 이벤트를 구독해서 동기화한다.
    /// onClick이 null이면 칸 클릭 비활성.
    /// </summary>
    public void Show(PlayerInventory targetInventory, Action<InventorySlot> onClick)
    {
        Unbind();
        inventory = targetInventory;
        onItemClick = onClick;

        if (inventory != null) inventory.OnInventoryChanged += Refresh;

        panelRoot.SetActive(true);
        Refresh();
    }

    /// <summary>
    /// 보유 아이템 UI 숨김, 이벤트 구독 해제
    /// </summary>
    public void Hide()
    {
        Unbind();
        inventory = null;
        onItemClick = null;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    /// <summary>
    /// 인벤토리 변경 이벤트 구독 해제
    /// </summary>
    private void Unbind()
    {
        if (inventory != null) inventory.OnInventoryChanged -= Refresh;
    }

    /// <summary>
    /// 파괴 시 이벤트 구독 해제
    /// </summary>
    private void OnDestroy()
    {
        Unbind();
    }

    /// <summary>
    /// 보유 아이템 슬롯을 다시 그린다. HUDBottomPanel.RefreshInventoryItems와 같은 규칙.
    /// </summary>
    private void Refresh()
    {
        if (inventory == null || itemsContent == null || itemSlotTemplate == null) return;

        CollectQuickSlotItems();

        int visibleCount = 0;
        for (int i = 0; i < inventory.Slots.Count; i++)
        {
            InventorySlot inventorySlot = inventory.Slots[i];
            InvenItemData item = inventorySlot.Item;
            if (item == null || inventorySlot.Count <= 0) continue;
            if (excludeQuickSlotItems && quickSlotItems.Contains(item)) continue;

            HUDQuickSlot view = GetOrCreateSlotView(visibleCount);
            view.gameObject.SetActive(true);
            view.Initialize(0);
            view.SetNumberVisible(false);
            UnityEngine.Events.UnityAction click = onItemClick != null
                ? () => onItemClick?.Invoke(inventorySlot)
                : null;
            view.SetItem(item, inventorySlot.Count, ResolveIcon(item), click);
            view.SetItemName(item.ItemName);
            visibleCount++;
        }

        for (int i = visibleCount; i < itemSlotViews.Count; i++)
        {
            itemSlotViews[i].Clear();
            itemSlotViews[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 퀵슬롯에 올라간 아이템 수집 (인벤토리 앞쪽 QuickSlotItemCount 칸)
    /// </summary>
    private void CollectQuickSlotItems()
    {
        quickSlotItems.Clear();
        if (!excludeQuickSlotItems) return;

        int count = Mathf.Min(inventory.Slots.Count, QuickSlotItemCount);
        for (int i = 0; i < count; i++)
        {
            InvenItemData item = inventory.Slots[i].Item;
            if (item != null) quickSlotItems.Add(item);
        }
    }

    /// <summary>
    /// index 위치의 슬롯 View 반환, 없으면 템플릿 복제로 생성
    /// </summary>
    private HUDQuickSlot GetOrCreateSlotView(int index)
    {
        while (itemSlotViews.Count <= index)
        {
            HUDQuickSlot view = Instantiate(itemSlotTemplate, itemsContent);
            view.gameObject.name = $"OwnedItem_Slot_{itemSlotViews.Count + 1}";
            itemSlotViews.Add(view);
        }

        return itemSlotViews[index];
    }

    /// <summary>
    /// HUDBottomPanel의 아이콘 규칙으로 아이템 아이콘 조회
    /// </summary>
    private Sprite ResolveIcon(InvenItemData item)
    {
        if (hudPanel == null) hudPanel = FindAnyObjectByType<HUDBottomPanel>(FindObjectsInactive.Include);
        return hudPanel != null ? hudPanel.GetItemIcon(item) : null;
    }
}
