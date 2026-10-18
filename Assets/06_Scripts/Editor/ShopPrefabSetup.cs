using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Canvas - Shop 프리팹에 ShopPanelUI를 자동 연결하고,
/// ShopListSlot 프리팹에 ShopItemSlotUI를 붙이는 에디터 도구.
/// </summary>
public static class ShopPrefabSetup
{
    private const string ShopCanvasPath = "Assets/04_Prefabs/UI-Canvas/Canvas - Shop.prefab";
    private const string ShopSlotPath = "Assets/04_Prefabs/ListSlot/ShopListSlot.prefab";

    [MenuItem("Tools/Shop/Setup Shop Prefabs")]
    public static void Setup()
    {
        SetupShopListSlot();
        SetupCanvasShop();
        AssetDatabase.SaveAssets();
        Debug.Log("ShopPrefabSetup: 설정 완료!");
    }

    private static void SetupShopListSlot()
    {
        using (var scope = new PrefabUtility.EditPrefabContentsScope(ShopSlotPath))
        {
            GameObject root = scope.prefabContentsRoot;
            ShopItemSlotUI slotUI = root.GetComponent<ShopItemSlotUI>();
            if (slotUI == null) slotUI = root.AddComponent<ShopItemSlotUI>();

            // SerializedObject로 private/serialized 필드 연결
            SerializedObject so = new SerializedObject(slotUI);

            // Img_Icon
            Transform imgIconT = FindDeep(root.transform, "Img_Icon");
            if (imgIconT != null)
                so.FindProperty("imgIcon").objectReferenceValue = imgIconT.GetComponent<Image>();

            // TMP_Price (Pnl_Botton > TMP_Price)
            Transform tmpPriceT = FindDeep(root.transform, "TMP_Price");
            if (tmpPriceT != null)
                so.FindProperty("tmpPrice").objectReferenceValue = tmpPriceT.GetComponent<TMP_Text>();

            // TMP_Count
            Transform tmpCountT = FindDeep(root.transform, "TMP_Count");
            if (tmpCountT != null)
                so.FindProperty("tmpCount").objectReferenceValue = tmpCountT.GetComponent<TMP_Text>();

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("ShopPrefabSetup: ShopListSlot에 ShopItemSlotUI 연결 완료.");
        }
    }

    private static void SetupCanvasShop()
    {
        // ShopListSlot 프리팹 로드 (슬롯 프리팹 참조용)
        GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShopSlotPath);

        // ItemDatabase 찾기
        string[] dbGuids = AssetDatabase.FindAssets("t:ItemDatabase");
        ItemDatabase itemDB = null;
        if (dbGuids.Length > 0)
        {
            string dbPath = AssetDatabase.GUIDToAssetPath(dbGuids[0]);
            itemDB = AssetDatabase.LoadAssetAtPath<ItemDatabase>(dbPath);
        }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(ShopCanvasPath))
        {
            GameObject root = scope.prefabContentsRoot;

            ShopPanelUI panelUI = root.GetComponent<ShopPanelUI>();
            if (panelUI == null) panelUI = root.AddComponent<ShopPanelUI>();

            SerializedObject so = new SerializedObject(panelUI);

            // ─── 화면 그룹 ───
            Transform grpMainT = FindDeep(root.transform, "Grp_ShopMain");
            Transform grpChangeT = FindDeep(root.transform, "Grp_ShopChange");
            so.FindProperty("grpShopMain").objectReferenceValue = grpMainT != null ? grpMainT.gameObject : null;
            so.FindProperty("grpShopChange").objectReferenceValue = grpChangeT != null ? grpChangeT.gameObject : null;

            // ─── Grp_ShopMain 아이템 그리드 ───
            // Grp_ShopMain > Pnl_Center > Container
            if (grpMainT != null)
            {
                Transform pnlCenterMain = FindDirectChild(grpMainT, "Pnl_Center");
                if (pnlCenterMain != null)
                {
                    Transform container = FindDirectChild(pnlCenterMain, "Container");
                    so.FindProperty("itemGridContainer").objectReferenceValue = container;
                }

                // Btn_Back (Grp_ShopMain 직속)
                Transform btnBackMainT = FindDirectChild(grpMainT, "Btn_Back");
                if (btnBackMainT != null)
                    so.FindProperty("btnBackMain").objectReferenceValue = btnBackMainT.GetComponent<Button>();

                // Pnl_Left > 탭 버튼들
                Transform pnlLeftMain = FindDirectChild(grpMainT, "Pnl_Left");
                if (pnlLeftMain != null)
                {
                    Transform btnGoldT = FindDirectChild(pnlLeftMain, "Btn_GoldShop");
                    Transform btnDiamondT = FindDirectChild(pnlLeftMain, "Btn_DiamondShop");
                    Transform btnSaleT = FindDirectChild(pnlLeftMain, "Btn_Sale");

                    if (btnGoldT != null)
                        so.FindProperty("btnGoldShop").objectReferenceValue = btnGoldT.GetComponent<Button>();
                    if (btnDiamondT != null)
                        so.FindProperty("btnDiamondShop").objectReferenceValue = btnDiamondT.GetComponent<Button>();
                    if (btnSaleT != null)
                        so.FindProperty("btnSale").objectReferenceValue = btnSaleT.GetComponent<Button>();
                }
            }

            // ─── Grp_ShopChange 상세 정보 ───
            if (grpChangeT != null)
            {
                // Btn_Back (Grp_ShopChange 직속)
                Transform btnBackChangeT = FindDirectChild(grpChangeT, "Btn_Back");
                if (btnBackChangeT != null)
                    so.FindProperty("btnBackChange").objectReferenceValue = btnBackChangeT.GetComponent<Button>();

                Transform pnlCenterChange = FindDirectChild(grpChangeT, "Pnl_Center");
                if (pnlCenterChange != null)
                {
                    Transform pnlTop = FindDeep(pnlCenterChange, "Pnl_Top");
                    Transform pnlBotton = FindDeep(pnlCenterChange, "Pnl_Botton");

                    if (pnlTop != null)
                    {
                        // Img_Icon
                        Transform iconT = FindDirectChild(pnlTop, "Img_Icon");
                        if (iconT != null)
                            so.FindProperty("detailIcon").objectReferenceValue = iconT.GetComponent<Image>();

                        // Img_Name > TMP_Name
                        Transform imgNameT = FindDirectChild(pnlTop, "Img_Name");
                        if (imgNameT != null)
                        {
                            Transform tmpNameT = FindDirectChild(imgNameT, "TMP_Name");
                            if (tmpNameT != null)
                                so.FindProperty("detailName").objectReferenceValue = tmpNameT.GetComponent<TMP_Text>();
                        }

                        // TMP_Price
                        Transform tmpPriceT = FindDirectChild(pnlTop, "TMP_Price");
                        if (tmpPriceT != null)
                            so.FindProperty("detailPrice").objectReferenceValue = tmpPriceT.GetComponent<TMP_Text>();

                        // Img_Count > TMP_Count
                        Transform imgCountT = FindDirectChild(pnlTop, "Img_Count");
                        if (imgCountT != null)
                        {
                            Transform tmpCountT = FindDirectChild(imgCountT, "TMP_Count");
                            if (tmpCountT != null)
                                so.FindProperty("detailCount").objectReferenceValue = tmpCountT.GetComponent<TMP_Text>();
                        }

                        // Img_ShopCount > TMP_Count
                        Transform imgShopCountT = FindDirectChild(pnlTop, "Img_ShopCount");
                        if (imgShopCountT != null)
                        {
                            Transform tmpShopCountT = FindDirectChild(imgShopCountT, "TMP_Count");
                            if (tmpShopCountT != null)
                                so.FindProperty("detailShopCount").objectReferenceValue = tmpShopCountT.GetComponent<TMP_Text>();
                        }

                        // Img_Description > TMP_Description
                        Transform imgDescT = FindDirectChild(pnlTop, "Img_Description");
                        if (imgDescT != null)
                        {
                            Transform tmpDescT = FindDirectChild(imgDescT, "TMP_Description");
                            if (tmpDescT != null)
                                so.FindProperty("detailDescription").objectReferenceValue = tmpDescT.GetComponent<TMP_Text>();
                        }
                    }

                    if (pnlBotton != null)
                    {
                        // Slider
                        Transform sliderT = FindDirectChild(pnlBotton, "Slider");
                        if (sliderT != null)
                            so.FindProperty("quantitySlider").objectReferenceValue = sliderT.GetComponent<Slider>();

                        // 버튼들
                        SetButton(so, "btnUp", pnlBotton, "Btn_Up");
                        SetButton(so, "btnDown", pnlBotton, "Btn_Down");
                        SetButton(so, "btnUps", pnlBotton, "Btn_Ups");
                        SetButton(so, "btnDowns", pnlBotton, "Btn_Downs");
                        SetButton(so, "btnOK", pnlBotton, "Btn_OK");
                        SetButton(so, "btnClose", pnlBotton, "Btn_Close");
                    }
                }
            }

            // ─── 데이터 ───
            so.FindProperty("shopSlotPrefab").objectReferenceValue = slotPrefab;
            if (itemDB != null)
                so.FindProperty("itemDatabase").objectReferenceValue = itemDB;
            else
                Debug.LogWarning("ShopPrefabSetup: ItemDatabase 에셋을 찾을 수 없습니다. 수동으로 연결해주세요.");

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("ShopPrefabSetup: Canvas - Shop에 ShopPanelUI 연결 완료.");
        }
    }

    private static void SetButton(SerializedObject so, string propName, Transform parent, string childName)
    {
        Transform t = FindDirectChild(parent, childName);
        if (t != null)
            so.FindProperty(propName).objectReferenceValue = t.GetComponent<Button>();
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            if (parent.GetChild(i).name == childName)
                return parent.GetChild(i);
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
}
