using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Canvas - Shop 프리팹에 붙는 스크립트.
/// 2화면 구조:
///   Grp_ShopMain  (아이템 목록 그리드 + 탭 버튼)
///   Grp_ShopChange (아이템 상세 + 수량 + 구매) — 기본 비활성
/// </summary>
public class ShopPanelUI : MonoBehaviour
{
    [Header("화면 그룹")]
    [SerializeField] private GameObject grpShopMain;
    [SerializeField] private GameObject grpShopChange;

    [Header("Grp_ShopMain - 아이템 목록")]
    [SerializeField] private Transform itemGridContainer; // Grp_ShopMain > Pnl_Center > Container
    [SerializeField] private GameObject shopSlotPrefab;    // ShopListSlot 프리팹

    [Header("Grp_ShopMain - 탭 버튼")]
    [SerializeField] private Button btnGoldShop;
    [SerializeField] private Button btnDiamondShop;
    [SerializeField] private Button btnSale;
    [SerializeField] private Button btnBackMain; // Grp_ShopMain의 Btn_Back

    [Header("Grp_ShopChange - 상세 정보 (Pnl_Top)")]
    [SerializeField] private Image detailIcon;        // Img_Icon
    [SerializeField] private TMP_Text detailName;     // TMP_Name (Img_Name 안)
    [SerializeField] private TMP_Text detailPrice;    // TMP_Price
    [SerializeField] private TMP_Text detailCount;    // TMP_Count (Img_Count 안)
    [SerializeField] private TMP_Text detailShopCount;// TMP_Count (Img_ShopCount 안)
    [SerializeField] private TMP_Text detailDescription; // TMP_Description

    [Header("Grp_ShopChange - 구매 컨트롤 (Pnl_Botton)")]
    [SerializeField] private Slider quantitySlider;
    [SerializeField] private Button btnUp;      // +1
    [SerializeField] private Button btnDown;    // -1
    [SerializeField] private Button btnUps;     // +5
    [SerializeField] private Button btnDowns;   // -5
    [SerializeField] private Button btnOK;
    [SerializeField] private Button btnClose;
    [SerializeField] private Button btnBackChange; // Grp_ShopChange의 Btn_Back

    [Header("매각")]
    [Tooltip("매각하기 버튼을 한 번 누를 때 지급하는 골드 (임시: 아이템 판매 구현 전)")]
    [SerializeField, Min(0)] private int saleGoldAmount = 100;

    [Header("데이터")]
    [SerializeField] private ItemDatabase itemDatabase;

    public event Action OnShopClosed;

    // 이번에 상점을 연 뒤 한 번이라도 구매했는지 (청설모 퇴장 연출 분기용)
    public bool PurchasedThisVisit { get; private set; }

    private PlayerWallet wallet;
    private PlayerInventory inventory;
    private InvenItemData selectedItem;
    private int selectedPrice;
    private int quantity = 1;
    private bool isDiamondMode;
    private readonly List<ShopItemSlotUI> slotInstances = new List<ShopItemSlotUI>();

    public void Open(PlayerWallet playerWallet, PlayerInventory playerInventory)
    {
        wallet = playerWallet;
        inventory = playerInventory;
        isDiamondMode = false;
        quantity = 1;
        selectedItem = null;
        PurchasedThisVisit = false;

        gameObject.SetActive(true);
        ShowMain();
        BuildItemList();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        OnShopClosed?.Invoke();
    }

    private void Awake()
    {
        AutoBind();

        // 탭 버튼
        if (btnGoldShop != null) btnGoldShop.onClick.AddListener(() => SwitchTab(false));
        if (btnDiamondShop != null) btnDiamondShop.onClick.AddListener(() => SwitchTab(true));
        if (btnSale != null) btnSale.onClick.AddListener(OnSaleTabClicked);

        // 메인 뒤로가기 = 상점 닫기
        if (btnBackMain != null) btnBackMain.onClick.AddListener(Close);

        // 상세 뒤로가기 = 메인으로 복귀
        if (btnBackChange != null) btnBackChange.onClick.AddListener(ShowMain);

        // 구매 컨트롤
        if (btnUp != null) btnUp.onClick.AddListener(() => ChangeQuantity(1));
        if (btnDown != null) btnDown.onClick.AddListener(() => ChangeQuantity(-1));
        if (btnUps != null) btnUps.onClick.AddListener(() => ChangeQuantity(5));
        if (btnDowns != null) btnDowns.onClick.AddListener(() => ChangeQuantity(-5));
        if (btnOK != null) btnOK.onClick.AddListener(OnBuyClicked);
        if (btnClose != null) btnClose.onClick.AddListener(Close);

        if (quantitySlider != null)
        {
            quantitySlider.wholeNumbers = true;
            quantitySlider.onValueChanged.AddListener(OnSliderChanged);
        }
    }

    /// <summary>에디터에서 연결이 안 된 필드를 이름으로 자동 탐색.</summary>
    private void AutoBind()
    {
        Transform root = transform;

        // 화면 그룹
        if (grpShopMain == null) grpShopMain = FindGO(root, "Grp_ShopMain");
        if (grpShopChange == null) grpShopChange = FindGO(root, "Grp_ShopChange");

        // Grp_ShopMain
        if (grpShopMain != null)
        {
            Transform main = grpShopMain.transform;
            if (itemGridContainer == null)
            {
                Transform pnlCenter = FindDeep(main, "Pnl_Center");
                if (pnlCenter != null) itemGridContainer = FindDeep(pnlCenter, "Container");
            }
            if (btnGoldShop == null) btnGoldShop = FindBtn(main, "Btn_GoldShop");
            if (btnDiamondShop == null) btnDiamondShop = FindBtn(main, "Btn_DiamondShop");
            if (btnSale == null) btnSale = FindBtn(main, "Btn_Sale");
            if (btnBackMain == null) btnBackMain = FindBtn(main, "Btn_Back");
        }

        // Grp_ShopChange
        if (grpShopChange != null)
        {
            Transform change = grpShopChange.transform;
            if (btnBackChange == null) btnBackChange = FindBtn(change, "Btn_Back");

            Transform pnlCenter = FindDeep(change, "Pnl_Center");
            if (pnlCenter != null)
            {
                // Pnl_Top — 상세 정보
                Transform pnlTop = FindDeep(pnlCenter, "Pnl_Top");
                if (pnlTop != null)
                {
                    if (detailIcon == null)
                    {
                        Transform t = FindDirect(pnlTop, "Img_Icon");
                        if (t != null) detailIcon = t.GetComponent<Image>();
                    }
                    if (detailName == null)
                    {
                        Transform imgName = FindDirect(pnlTop, "Img_Name");
                        if (imgName != null) detailName = FindTMP(imgName, "TMP_Name");
                    }
                    if (detailPrice == null) detailPrice = FindTMP(pnlTop, "TMP_Price");
                    if (detailDescription == null)
                    {
                        Transform imgDesc = FindDirect(pnlTop, "Img_Description");
                        if (imgDesc != null) detailDescription = FindTMP(imgDesc, "TMP_Description");
                    }
                    if (detailCount == null)
                    {
                        Transform imgCount = FindDirect(pnlTop, "Img_Count");
                        if (imgCount != null) detailCount = FindTMP(imgCount, "TMP_Count");
                    }
                    if (detailShopCount == null)
                    {
                        Transform imgShopCount = FindDirect(pnlTop, "Img_ShopCount");
                        if (imgShopCount != null) detailShopCount = FindTMP(imgShopCount, "TMP_Count");
                    }
                }

                // Pnl_Botton — 구매 컨트롤
                Transform pnlBot = FindDeep(pnlCenter, "Pnl_Botton");
                if (pnlBot != null)
                {
                    if (quantitySlider == null)
                    {
                        Transform t = FindDirect(pnlBot, "Slider");
                        if (t != null) quantitySlider = t.GetComponent<Slider>();
                    }
                    if (btnUp == null) btnUp = FindBtn(pnlBot, "Btn_Up");
                    if (btnDown == null) btnDown = FindBtn(pnlBot, "Btn_Down");
                    if (btnUps == null) btnUps = FindBtn(pnlBot, "Btn_Ups");
                    if (btnDowns == null) btnDowns = FindBtn(pnlBot, "Btn_Downs");
                    if (btnOK == null) btnOK = FindBtn(pnlBot, "Btn_OK");
                    if (btnClose == null) btnClose = FindBtn(pnlBot, "Btn_Close");
                }
            }
        }

        // ShopListSlot 프리팹 — 없으면 Resources에서 찾기
        if (shopSlotPrefab == null)
            shopSlotPrefab = Resources.Load<GameObject>("ShopListSlot");
    }

    private static GameObject FindGO(Transform parent, string name)
    {
        Transform t = FindDeep(parent, name);
        return t != null ? t.gameObject : null;
    }

    private static Button FindBtn(Transform parent, string name)
    {
        Transform t = FindDirect(parent, name);
        return t != null ? t.GetComponent<Button>() : null;
    }

    private static TMP_Text FindTMP(Transform parent, string name)
    {
        Transform t = FindDirect(parent, name);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    private static Transform FindDirect(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            if (parent.GetChild(i).name == name) return parent.GetChild(i);
        }
        return null;
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindDeep(parent.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    private void OnSliderChanged(float value)
    {
        quantity = Mathf.Max(1, (int)value);
        RefreshDetail();
    }

    // ─── 화면 전환 ───

    private void ShowMain()
    {
        if (grpShopMain != null) grpShopMain.SetActive(true);
        if (grpShopChange != null) grpShopChange.SetActive(false);
    }

    private void ShowChange()
    {
        if (grpShopMain != null) grpShopMain.SetActive(false);
        if (grpShopChange != null) grpShopChange.SetActive(true);
    }

    // ─── 탭 ───

    private void SwitchTab(bool diamond)
    {
        isDiamondMode = diamond;
        selectedItem = null;
        quantity = 1;
        BuildItemList();
    }

    // 임시 매각: 누를 때마다 골드를 saleGoldAmount만큼 지급한다. 아이템은 소모하지 않는다.
    private void OnSaleTabClicked()
    {
        if (wallet == null || saleGoldAmount <= 0) return;
        wallet.Add(CurrencyType.Gold, saleGoldAmount);
    }

    // ─── 아이템 목록 ───

    private void BuildItemList()
    {
        foreach (ShopItemSlotUI slot in slotInstances)
        {
            if (slot != null) Destroy(slot.gameObject);
        }
        slotInstances.Clear();

        if (itemDatabase == null || shopSlotPrefab == null || itemGridContainer == null) return;

        foreach (InvenItemData item in itemDatabase.Items)
        {
            if (item == null || !item.ShopListed) continue;
            if (isDiamondMode && item.BuyDiamond <= 0) continue;
            if (!isDiamondMode && item.BaseBuyGold <= 0) continue;

            GameObject go = Instantiate(shopSlotPrefab, itemGridContainer);
            go.SetActive(true);
            ShopItemSlotUI slotUI = go.GetComponent<ShopItemSlotUI>();
            if (slotUI == null) slotUI = go.AddComponent<ShopItemSlotUI>();

            slotUI.Setup(item, isDiamondMode);
            slotUI.OnClicked += OnSlotClicked;
            slotInstances.Add(slotUI);
        }
    }

    // ─── 슬롯 클릭 → 상세 화면 ───

    private void OnSlotClicked(ShopItemSlotUI slot)
    {
        selectedItem = slot.ItemData;
        selectedPrice = slot.Price;
        quantity = 1;
        ShowChange();
        RefreshDetail();
    }

    private void RefreshDetail()
    {
        if (selectedItem == null) return;

        // 아이콘
        if (detailIcon != null)
        {
            detailIcon.sprite = selectedItem.Icon;
            detailIcon.enabled = selectedItem.Icon != null;
        }

        // 이름
        if (detailName != null)
            detailName.text = selectedItem.ItemName ?? "";

        // 설명
        if (detailDescription != null)
            detailDescription.text = selectedItem.Description ?? "";

        // 가격 (총액)
        int totalPrice = selectedPrice * quantity;
        if (detailPrice != null)
            detailPrice.text = totalPrice.ToString("N0");

        // 수량
        string countText = quantity.ToString();
        if (detailCount != null) detailCount.text = countText;
        if (detailShopCount != null) detailShopCount.text = countText;

        // 슬라이더
        if (quantitySlider != null)
        {
            int max = GetMaxBuyable(selectedPrice);
            quantitySlider.minValue = 1;
            quantitySlider.maxValue = Mathf.Max(1, max);
            quantitySlider.SetValueWithoutNotify(quantity);
        }
    }

    // ─── 수량 조절 ───

    private void ChangeQuantity(int delta)
    {
        if (selectedItem == null) return;
        int max = GetMaxBuyable(selectedPrice);
        quantity = Mathf.Clamp(quantity + delta, 1, Mathf.Max(1, max));
        RefreshDetail();
    }

    private int GetMaxBuyable(int unitPrice)
    {
        if (wallet == null || unitPrice <= 0) return 99;
        CurrencyType type = isDiamondMode ? CurrencyType.Gem : CurrencyType.Gold;
        int balance = wallet.GetBalance(type);
        return balance > 0 && unitPrice > 0 ? Mathf.Max(1, balance / unitPrice) : 1;
    }

    // ─── 구매 ───

    private void OnBuyClicked()
    {
        if (selectedItem == null || wallet == null || inventory == null) return;

        int totalPrice = selectedPrice * quantity;
        CurrencyType type = isDiamondMode ? CurrencyType.Gem : CurrencyType.Gold;

        if (!wallet.TrySpend(type, totalPrice))
        {
            Debug.Log("ShopPanelUI: 재화가 부족합니다.");
            return;
        }

        int leftover = inventory.Add(selectedItem, quantity);
        if (leftover < quantity) PurchasedThisVisit = true;
        if (leftover > 0)
        {
            int refundAmount = selectedPrice * leftover;
            wallet.Add(type, refundAmount);
            Debug.Log($"ShopPanelUI: 인벤토리 공간 부족. {leftover}개 환불.");
        }

        // 구매 완료 → 목록으로 복귀
        quantity = 1;
        selectedItem = null;
        ShowMain();
        BuildItemList();
    }
}
