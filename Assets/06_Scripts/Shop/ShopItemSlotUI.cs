using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ShopListSlot 프리팹에 붙는 스크립트.
/// 구조: ShopListSlot(root) > Img_TopBG, Img_Icon, TMP_Count, Grp_ItemInformation(Img_BG, TMP_ItemQuantity), Pnl_Botton(Img_BlueDot, TMP_Price)
/// </summary>
public class ShopItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image imgIcon;
    [SerializeField] private TMP_Text tmpPrice;
    [SerializeField] private TMP_Text tmpCount;

    private InvenItemData itemData;
    private int price;
    private bool isDiamondMode;

    public event Action<ShopItemSlotUI> OnClicked;
    public InvenItemData ItemData => itemData;
    public int Price => price;

    public void Setup(InvenItemData item, bool diamond)
    {
        itemData = item;
        isDiamondMode = diamond;
        price = diamond ? item.BuyDiamond : item.BaseBuyGold;

        if (imgIcon != null && item.Icon != null)
            imgIcon.sprite = item.Icon;

        if (tmpPrice != null)
            tmpPrice.text = price.ToString("N0");

        if (tmpCount != null)
            tmpCount.text = "";
    }

    private void Awake()
    {
        // 자동 바인딩: 에디터에서 연결 안 되어 있으면 이름으로 찾기
        if (imgIcon == null) imgIcon = FindInChildren<Image>("Img_Icon");
        if (tmpPrice == null) tmpPrice = FindInChildren<TMP_Text>("TMP_Price");
        if (tmpCount == null) tmpCount = FindInChildren<TMP_Text>("TMP_Count");

        Button btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(() => OnClicked?.Invoke(this));
    }

    private T FindInChildren<T>(string childName) where T : Component
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == childName)
            {
                T comp = child.GetComponent<T>();
                if (comp != null) return comp;
            }
        }
        return null;
    }
}
