using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상자 보관 UI.
/// 상자 슬롯만 표시하고, 넣기는 기존 HUD 인벤토리 패널을 재사용한다.
/// 열려 있는 동안 HUD 인벤토리 칸 클릭 -> 상자에 넣기, 상자 칸 클릭 -> 인벤토리로 꺼내기.
/// 슬롯은 HUDQuickSlot 템플릿을 복제해서 사용한다.
/// 상자 1개당 ChestUI 1개를 사용한다 (ChestObject가 자신의 ChestUI를 소유).
/// </summary>
public class ChestUI : MonoBehaviour
{
    [Header("패널")]
    [SerializeField]
    private GameObject panelRoot;
    [SerializeField]
    private Button closeButton;

    [Header("상자 슬롯")]
    [SerializeField]
    private RectTransform chestContent;
    [SerializeField]
    private HUDQuickSlot chestSlotTemplate;

    [Header("보유 아이템 UI")]
    [Tooltip("상자와 같이 열리는 보유 아이템 UI. 칸 클릭 시 상자에 넣기")]
    [SerializeField]
    private HUDOwnedItemsView ownedItemsView;

    [Header("이동 수량")]
    [Tooltip("켜면 클릭 시 1개씩 이동, 끄면 클릭한 칸 전체 이동")]
    [SerializeField]
    private bool moveOneByOne = false;

    private readonly List<HUDQuickSlot> chestSlotViews = new List<HUDQuickSlot>();

    private ChestObject currentChest;
    private PlayerInventory playerInventory;
    private HUDBottomPanel hudPanel;

    /// <summary>
    /// 템플릿 숨김, 닫기 버튼 연결, 패널 초기 비활성화
    /// </summary>
    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
        if (chestSlotTemplate != null) chestSlotTemplate.gameObject.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseButton);
        else Debug.LogWarning($"[{name}] closeButton이 비어 있어 상자를 닫을 수 없습니다.");

        // ChestUI가 panelRoot 자신에 붙어 비활성으로 시작한 경우, Open 중 Awake가 호출되므로 그때는 숨기지 않는다.
        if (currentChest == null) panelRoot.SetActive(false);
    }

    /// <summary>
    /// 상자 UI 열기. 상자 변경 이벤트 구독, HUD 인벤토리 클릭을 '상자에 넣기'로 전환하고 슬롯을 그린다.
    /// </summary>
    public void Open(ChestObject chest, PlayerInventory inventory)
    {
        #region 이전 코드 - UI 하나를 여러 상자가 공유할 때 기존 상자 닫기 (상자 1 : UI 1 구조로 불필요)
        //// 다른 상자가 열려 있으면 먼저 닫는다.
        //if (currentChest != null && currentChest != chest) currentChest.Close();
        #endregion

        Unbind();
        currentChest = chest;
        playerInventory = inventory;

        if (currentChest != null) currentChest.OnStorageChanged += Refresh;

        panelRoot.SetActive(true);
        PrepareChestSlots();
        if (ownedItemsView != null) ownedItemsView.Show(playerInventory, MoveToChest);

        HUDBottomPanel hud = GetHUDPanel();
        if (hud != null) hud.SetItemClickOverride(MoveToChest);

        Refresh();
    }

    /// <summary>
    /// 상자 UI 닫기. 현재 연 상자와 같을 때만 닫고 이벤트 구독 해제, HUD 인벤토리 클릭 원복.
    /// </summary>
    public void Close(ChestObject chest)
    {
        if (currentChest != chest) return;

        Unbind();
        currentChest = null;
        playerInventory = null;
        panelRoot.SetActive(false);
        if (ownedItemsView != null) ownedItemsView.Hide();

        HUDBottomPanel hud = GetHUDPanel();
        if (hud != null) hud.SetItemClickOverride(null);
    }

    /// <summary>
    /// 닫기 버튼 클릭 처리. 상자 Close로 Sprite 복구 + 이동 정지 해제 + UI 닫기까지 진행된다.
    /// </summary>
    private void OnCloseButton()
    {
        // 상자 쪽 Close가 Sprite 복구 후 다시 Close(chest)를 호출한다.
        if (currentChest != null) currentChest.Close();
        else panelRoot.SetActive(false);
    }

    /// <summary>
    /// 상자 변경 이벤트 구독 해제
    /// </summary>
    private void Unbind()
    {
        if (currentChest != null) currentChest.OnStorageChanged -= Refresh;
    }

    /// <summary>
    /// 파괴 시 이벤트 구독 해제
    /// </summary>
    private void OnDestroy()
    {
        Unbind();
    }

    /// <summary>
    /// 상자 슬롯을 다시 그린다. (인벤토리 쪽은 HUD가 OnInventoryChanged로 직접 갱신)
    /// </summary>
    private void Refresh()
    {
        if (currentChest == null) return;

        RefreshSlots(currentChest.Slots, chestContent, chestSlotTemplate, chestSlotViews, MoveToInventory, currentChest.Capacity);
    }

    /// <summary>
    /// 상자 칸 수(Capacity)만큼 슬롯을 미리 생성해 빈 칸으로 표시한다. (넣을 수 있는 공간 표시)
    /// </summary>
    private void PrepareChestSlots()
    {
        if (currentChest == null || chestContent == null || chestSlotTemplate == null) return;

        for (int i = 0; i < currentChest.Capacity; i++)
        {
            HUDQuickSlot view = GetOrCreateSlotView(chestSlotViews, i, chestContent, chestSlotTemplate);
            view.gameObject.SetActive(true);
            view.Initialize(0);
            view.SetNumberVisible(false);
            view.Clear();
        }
    }

    /// <summary>
    /// 슬롯 목록을 UI로 그린다. 빈 칸은 건너뛰고, minVisible까지는 빈 슬롯으로 채우고, 남는 슬롯 View는 숨긴다.
    /// </summary>
    private void RefreshSlots(IReadOnlyList<InventorySlot> source, RectTransform content, HUDQuickSlot template,
        List<HUDQuickSlot> views, System.Action<InventorySlot> onClick, int minVisible = 0)
    {
        if (content == null || template == null) return;

        int visibleCount = 0;
        for (int i = 0; i < source.Count; i++)
        {
            InventorySlot slot = source[i];
            if (slot.Item == null || slot.Count <= 0) continue;

            HUDQuickSlot view = GetOrCreateSlotView(views, visibleCount, content, template);
            view.gameObject.SetActive(true);
            view.Initialize(0);
            view.SetNumberVisible(false);
            view.SetItem(slot.Item, slot.Count, ResolveIcon(slot.Item), () => onClick(slot));
            view.SetItemName(slot.Item.ItemName);
            visibleCount++;
        }

        // 남은 공간은 빈 칸으로 표시
        while (visibleCount < minVisible)
        {
            HUDQuickSlot view = GetOrCreateSlotView(views, visibleCount, content, template);
            view.gameObject.SetActive(true);
            view.Initialize(0);
            view.SetNumberVisible(false);
            view.Clear();
            visibleCount++;
        }

        for (int i = visibleCount; i < views.Count; i++)
        {
            views[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// index 위치의 슬롯 View 반환, 없으면 템플릿 복제로 생성
    /// </summary>
    private static HUDQuickSlot GetOrCreateSlotView(List<HUDQuickSlot> views, int index, RectTransform content, HUDQuickSlot template)
    {
        if (index < views.Count) return views[index];

        HUDQuickSlot view = Instantiate(template, content);
        views.Add(view);
        return view;
    }

    /// <summary>
    /// 인벤토리 -> 상자 이동 (HUD 인벤토리 칸 클릭 시 호출). 상자에 실제로 들어간 만큼만 인벤토리에서 제거.
    /// </summary>
    private void MoveToChest(InventorySlot slot)
    {
        if (currentChest == null || playerInventory == null || slot.Item == null) return;

        InvenItemData item = slot.Item;
        int amount = moveOneByOne ? 1 : slot.Count;

        int remaining = currentChest.Add(item, amount);
        int moved = amount - remaining;
        if (moved > 0) playerInventory.Remove(item, moved);
    }

    /// <summary>
    /// 상자 -> 인벤토리 이동. 인벤토리에 실제로 들어간 만큼만 상자에서 제거.
    /// </summary>
    private void MoveToInventory(InventorySlot slot)
    {
        if (currentChest == null || playerInventory == null || slot.Item == null) return;

        InvenItemData item = slot.Item;
        int amount = moveOneByOne ? 1 : slot.Count;

        int remaining = playerInventory.Add(item, amount);
        int moved = amount - remaining;
        if (moved > 0) currentChest.Remove(item, moved);
    }

    /// <summary>
    /// HUDBottomPanel의 아이콘 규칙으로 아이템 아이콘 조회
    /// </summary>
    private Sprite ResolveIcon(InvenItemData item)
    {
        HUDBottomPanel hud = GetHUDPanel();
        return hud != null ? hud.GetItemIcon(item) : null;
    }

    /// <summary>
    /// 씬의 HUDBottomPanel 조회 (캐싱)
    /// </summary>
    private HUDBottomPanel GetHUDPanel()
    {
        if (hudPanel == null) hudPanel = FindAnyObjectByType<HUDBottomPanel>(FindObjectsInactive.Include);
        return hudPanel;
    }
}
