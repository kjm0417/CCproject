using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 퀘스트 UI(트래커 + 사이드 패널) prefab 생성. 목업 기준 1280x720 레이아웃
/// </summary>
public static class QuestUIBuilder
{
    private const string PrefabPath = "Assets/04_Prefabs/UI-Canvas/Canvas - QuestUI.prefab";
    private const string FontPath = "Assets/08_Pont/yuhanKimbery SDF.asset";
    private const string CoinPath = "Assets/03_Sprite/UI/DungeonEntry/Icon_Coin.png";

    private static readonly Color Paper = Hex(0xFFFAF0);
    private static readonly Color Paper2 = Hex(0xF3ECDC);
    private static readonly Color Ink = Hex(0x2E3338);
    private static readonly Color Sky = Hex(0x6FC7F2);
    private static readonly Color SkyDark = Hex(0x2F8FC4);
    private static readonly Color Gold = Hex(0xF2C75C);
    private static readonly Color Red = Hex(0xE5534B);
    private static readonly Color BarBack = Hex(0xE2E9EC);

    private static TMP_FontAsset font;
    private static Sprite rounded;
    private static Sprite circle;

    [MenuItem("Tools/Quest/Build Quest UI Prefab")]
    private static void Build()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        GameObject root = new GameObject("Canvas - QuestUI", typeof(RectTransform));
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        QuestPanelUI panel = BuildPanel(root.transform);
        BuildTracker(root.transform, panel);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        Debug.Log($"[QuestUIBuilder] {PrefabPath} 생성");
    }

    #region 트래커
    private static void BuildTracker(Transform parent, QuestPanelUI panel)
    {
        RectTransform tracker = Rect("Tracker", parent);
        tracker.anchorMin = tracker.anchorMax = tracker.pivot = new Vector2(1f, 1f);
        tracker.anchoredPosition = new Vector2(-16f, -112f);
        tracker.sizeDelta = new Vector2(250f, 0f);
        VerticalLayoutGroup layout = tracker.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        tracker.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 제목 줄
        RectTransform head = Panel("Head", tracker, Paper, 3f);
        Height(head, 44f);
        Button headButton = head.gameObject.AddComponent<Button>();
        TMP_Text title = Text("Txt_Title", head, "퀘스트", 24, Ink, TextAlignmentOptions.MidlineLeft);
        Stretch(title.rectTransform, 14f, 0f, 70f, 0f);
        TMP_Text open = Text("Txt_Open", head, "열기 >", 14, SkyDark, TextAlignmentOptions.MidlineRight);
        Stretch(open.rectTransform, 0f, 0f, 12f, 0f);

        RectTransform badge = Circle("Badge", head, Red);
        badge.anchorMin = badge.anchorMax = new Vector2(0f, 1f);
        badge.sizeDelta = new Vector2(24f, 24f);
        badge.anchoredPosition = new Vector2(84f, -2f);
        badge.gameObject.AddComponent<Outline>().effectColor = Color.white;
        TMP_Text badgeText = Text("Txt_Badge", badge, "0", 14, Color.white, TextAlignmentOptions.Center);
        Stretch(badgeText.rectTransform, 0f, 0f, 0f, 0f);

        // 2줄
        QuestTrackerRowUI[] rows = new QuestTrackerRowUI[2];
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = BuildTrackerRow(tracker, i);
        }

        TMP_Text more = Text("Txt_More", tracker, "+0개 더", 15, Hex(0xDFE7EA), TextAlignmentOptions.MidlineRight);
        Height(more.rectTransform, 20f);

        QuestTrackerUI trackerUI = parent.gameObject.AddComponent<QuestTrackerUI>();
        SerializedObject so = new SerializedObject(trackerUI);
        so.FindProperty("trackerRoot").objectReferenceValue = tracker.gameObject;
        so.FindProperty("headButton").objectReferenceValue = headButton;
        so.FindProperty("badgeRoot").objectReferenceValue = badge.gameObject;
        so.FindProperty("txtBadge").objectReferenceValue = badgeText;
        SerializedProperty rowsProp = so.FindProperty("rows");
        rowsProp.arraySize = rows.Length;
        for (int i = 0; i < rows.Length; i++) rowsProp.GetArrayElementAtIndex(i).objectReferenceValue = rows[i];
        so.FindProperty("txtMore").objectReferenceValue = more;
        so.FindProperty("panel").objectReferenceValue = panel;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static QuestTrackerRowUI BuildTrackerRow(Transform parent, int index)
    {
        RectTransform row = Panel($"Row_{index + 1}", parent, Paper, 3f);
        Height(row, 50f);
        Button button = row.gameObject.AddComponent<Button>();

        Image icon = Img("Icon", row, null, Color.white);
        icon.preserveAspect = true;
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.sizeDelta = new Vector2(32f, 32f);
        iconRect.anchoredPosition = new Vector2(24f, 0f);

        TMP_Text condition = Text("Txt_Condition", row, "조건", 16, Ink, TextAlignmentOptions.MidlineLeft);
        condition.overflowMode = TextOverflowModes.Ellipsis;
        Stretch(condition.rectTransform, 46f, 14f, 72f, 4f);

        RectTransform countBack = Panel("CountBack", row, Red, 0f);
        countBack.anchorMin = countBack.anchorMax = new Vector2(1f, 0.5f);
        countBack.pivot = new Vector2(1f, 0.5f);
        countBack.sizeDelta = new Vector2(58f, 24f);
        countBack.anchoredPosition = new Vector2(-8f, 6f);
        TMP_Text count = Text("Txt_Count", countBack, "0/0", 16, Ink, TextAlignmentOptions.Center);
        Stretch(count.rectTransform, 0f, 0f, 0f, 0f);

        Image barFill = Bar(row, out RectTransform barRect);
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(1f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.offsetMin = new Vector2(46f, 7f);
        barRect.offsetMax = new Vector2(-8f, 14f);

        QuestTrackerRowUI rowUI = row.gameObject.AddComponent<QuestTrackerRowUI>();
        SerializedObject so = new SerializedObject(rowUI);
        so.FindProperty("button").objectReferenceValue = button;
        so.FindProperty("background").objectReferenceValue = row.GetComponent<Image>();
        so.FindProperty("outline").objectReferenceValue = row.GetComponent<Outline>();
        so.FindProperty("icon").objectReferenceValue = icon;
        so.FindProperty("txtCondition").objectReferenceValue = condition;
        so.FindProperty("txtCount").objectReferenceValue = count;
        so.FindProperty("countBackground").objectReferenceValue = countBack.GetComponent<Image>();
        so.FindProperty("barFill").objectReferenceValue = barFill;
        so.ApplyModifiedPropertiesWithoutUndo();
        return rowUI;
    }
    #endregion

    #region 패널
    private static QuestPanelUI BuildPanel(Transform parent)
    {
        // 패널 바깥 터치 영역 (투명)
        Image outside = Img("OutsideButton", parent, null, new Color(0f, 0f, 0f, 0.001f));
        Stretch(outside.rectTransform, 0f, 0f, 0f, 0f);
        Button outsideButton = outside.gameObject.AddComponent<Button>();
        outsideButton.transition = Selectable.Transition.None;

        RectTransform sheet = Panel("Panel", parent, Paper, 4f);
        sheet.anchorMin = new Vector2(1f, 0f);
        sheet.anchorMax = new Vector2(1f, 1f);
        sheet.pivot = new Vector2(1f, 0.5f);
        sheet.sizeDelta = new Vector2(600f, 0f);
        sheet.offsetMin = new Vector2(-615f, 29f);
        sheet.offsetMax = new Vector2(-15f, -100f);

        // 헤더
        RectTransform head = Panel("Head", sheet, Sky, 0f);
        head.anchorMin = new Vector2(0f, 1f);
        head.anchorMax = new Vector2(1f, 1f);
        head.pivot = new Vector2(0.5f, 1f);
        head.offsetMin = new Vector2(4f, -64f);
        head.offsetMax = new Vector2(-4f, -4f);

        TMP_Text title = Text("Txt_Title", head, "퀘스트", 30, Ink, TextAlignmentOptions.MidlineLeft);
        Stretch(title.rectTransform, 18f, 0f, 380f, 0f);

        RectTransform chip = Panel("ReadyChip", head, Paper, 2f);
        chip.anchorMin = chip.anchorMax = new Vector2(0f, 0.5f);
        chip.pivot = new Vector2(0f, 0.5f);
        chip.sizeDelta = new Vector2(120f, 28f);
        chip.anchoredPosition = new Vector2(130f, 0f);
        TMP_Text readyCount = Text("Txt_ReadyCount", chip, "모두 받음", 15, Ink, TextAlignmentOptions.Center);
        Stretch(readyCount.rectTransform, 0f, 0f, 0f, 0f);

        RectTransform claimAll = Panel("Btn_ClaimAll", head, Gold, 3f);
        claimAll.anchorMin = claimAll.anchorMax = new Vector2(1f, 0.5f);
        claimAll.pivot = new Vector2(1f, 0.5f);
        claimAll.sizeDelta = new Vector2(110f, 38f);
        claimAll.anchoredPosition = new Vector2(-62f, 0f);
        Button claimAllButton = claimAll.gameObject.AddComponent<Button>();
        NoDisabledTint(claimAllButton);
        TMP_Text claimAllText = Text("Txt", claimAll, "모두 받기", 17, Ink, TextAlignmentOptions.Center);
        Stretch(claimAllText.rectTransform, 0f, 0f, 0f, 0f);

        RectTransform close = Circle("Btn_Close", head, Paper);
        close.anchorMin = close.anchorMax = new Vector2(1f, 0.5f);
        close.pivot = new Vector2(1f, 0.5f);
        close.sizeDelta = new Vector2(40f, 40f);
        close.anchoredPosition = new Vector2(-12f, 0f);
        close.gameObject.AddComponent<Outline>().effectColor = Ink;
        Button closeButton = close.gameObject.AddComponent<Button>();
        TMP_Text closeText = Text("Txt", close, "X", 20, Ink, TextAlignmentOptions.Center);
        Stretch(closeText.rectTransform, 0f, 0f, 0f, 0f);

        // 목록 (스크롤)
        RectTransform scroll = Rect("Scroll", sheet);
        Stretch(scroll, 4f, 4f, 4f, 68f);
        ScrollRect scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        RectTransform viewport = Rect("Viewport", scroll);
        Stretch(viewport, 0f, 0f, 0f, 0f);
        viewport.gameObject.AddComponent<RectMask2D>();
        Image viewportHit = viewport.gameObject.AddComponent<Image>();
        viewportHit.color = new Color(1f, 1f, 1f, 0f);

        RectTransform content = Rect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = Vector2.zero;
        VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(14, 14, 14, 14);
        contentLayout.spacing = 12f;
        contentLayout.childControlWidth = contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport;
        scrollRect.content = content;

        QuestCardUI cardTemplate = BuildCard(content);

        QuestPanelUI panelUI = parent.gameObject.AddComponent<QuestPanelUI>();
        SerializedObject so = new SerializedObject(panelUI);
        so.FindProperty("panelRoot").objectReferenceValue = sheet.gameObject;
        so.FindProperty("outsideButton").objectReferenceValue = outsideButton;
        so.FindProperty("closeButton").objectReferenceValue = closeButton;
        so.FindProperty("claimAllButton").objectReferenceValue = claimAllButton;
        so.FindProperty("claimAllImage").objectReferenceValue = claimAll.GetComponent<Image>();
        so.FindProperty("txtReadyCount").objectReferenceValue = readyCount;
        so.FindProperty("cardRoot").objectReferenceValue = content;
        so.FindProperty("cardTemplate").objectReferenceValue = cardTemplate;
        so.ApplyModifiedPropertiesWithoutUndo();
        return panelUI;
    }

    private static QuestCardUI BuildCard(Transform parent)
    {
        RectTransform card = Panel("Card_Template", parent, Color.white, 3f);
        Height(card, 132f);
        CanvasGroup group = card.gameObject.AddComponent<CanvasGroup>();

        // ① 퀘스트명 + 태그
        // 태그가 퀘스트명 길이에 맞춰 바로 옆에 붙도록 가로 정렬
        RectTransform nameRow = Rect("NameRow", card);
        TopLeft(nameRow, new Vector2(16f, -10f), new Vector2(360f, 34f));
        HorizontalLayoutGroup nameLayout = nameRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        nameLayout.spacing = 10f;
        nameLayout.childAlignment = TextAnchor.MiddleLeft;
        nameLayout.childControlWidth = nameLayout.childControlHeight = true;
        nameLayout.childForceExpandWidth = nameLayout.childForceExpandHeight = false;

        TMP_Text name = Text("Txt_Name", nameRow, "퀘스트명", 24, Ink, TextAlignmentOptions.MidlineLeft);
        TMP_Text tag = Text("Txt_Tag", nameRow, "1회", 15, Ink, TextAlignmentOptions.Center);

        // ② 지급 횟수
        TMP_Text paid = Text("Txt_Paid", card, "지급 0/1", 15, Hex(0x9AA3AA), TextAlignmentOptions.TopRight);
        RectTransform paidRect = paid.rectTransform;
        paidRect.anchorMin = paidRect.anchorMax = paidRect.pivot = new Vector2(1f, 1f);
        paidRect.sizeDelta = new Vector2(110f, 24f);
        paidRect.anchoredPosition = new Vector2(-80f, -14f);

        RectTransform pipRoot = Rect("Pips", card);
        pipRoot.anchorMin = pipRoot.anchorMax = pipRoot.pivot = new Vector2(1f, 1f);
        pipRoot.sizeDelta = new Vector2(64f, 16f);
        pipRoot.anchoredPosition = new Vector2(-14f, -18f);
        HorizontalLayoutGroup pipLayout = pipRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        pipLayout.spacing = 4f;
        pipLayout.childAlignment = TextAnchor.MiddleRight;
        pipLayout.childControlWidth = pipLayout.childControlHeight = false;
        RectTransform pip = Circle("Pip", pipRoot, Color.white);
        pip.sizeDelta = new Vector2(14f, 14f);
        pip.gameObject.AddComponent<Outline>().effectColor = Ink;

        // ③ 완료 조건 (아이콘 + 바 + 문구)
        Image icon = Img("Icon", card, null, Color.white);
        icon.preserveAspect = true;
        TopLeft(icon.rectTransform, new Vector2(16f, -50f), new Vector2(30f, 30f));

        Image barFill = Bar(card, out RectTransform barRect);
        TopLeft(barRect, new Vector2(54f, -58f), new Vector2(300f, 14f));

        TMP_Text condition = Text("Txt_Condition", card, "조건 0/0", 17, Ink, TextAlignmentOptions.MidlineLeft);
        TopLeft(condition.rectTransform, new Vector2(364f, -50f), new Vector2(190f, 30f));

        // ④ 보상 + 받기
        RectTransform rewardChip = Panel("RewardChip", card, Paper2, 0f);
        TopLeft(rewardChip, new Vector2(16f, -90f), new Vector2(110f, 30f));
        Image coin = Img("Coin", rewardChip, AssetDatabase.LoadAssetAtPath<Sprite>(CoinPath), Color.white);
        coin.preserveAspect = true;
        TopLeft(coin.rectTransform, new Vector2(4f, -3f), new Vector2(24f, 24f));
        TMP_Text reward = Text("Txt_Reward", rewardChip, "0 G", 17, Ink, TextAlignmentOptions.MidlineLeft);
        Stretch(reward.rectTransform, 32f, 0f, 4f, 0f);

        RectTransform claim = Panel("Btn_Claim", card, Gold, 3f);
        claim.anchorMin = claim.anchorMax = claim.pivot = new Vector2(1f, 0f);
        claim.sizeDelta = new Vector2(100f, 38f);
        claim.anchoredPosition = new Vector2(-14f, 12f);
        Button claimButton = claim.gameObject.AddComponent<Button>();
        NoDisabledTint(claimButton);
        TMP_Text claimText = Text("Txt", claim, "받기", 18, Ink, TextAlignmentOptions.Center);
        Stretch(claimText.rectTransform, 0f, 0f, 0f, 0f);

        Image flash = Img("Flash", card, rounded, new Color(Gold.r, Gold.g, Gold.b, 0.5f));
        flash.type = Image.Type.Sliced;
        flash.raycastTarget = false;
        Stretch(flash.rectTransform, 0f, 0f, 0f, 0f);

        QuestCardUI cardUI = card.gameObject.AddComponent<QuestCardUI>();
        SerializedObject so = new SerializedObject(cardUI);
        so.FindProperty("background").objectReferenceValue = card.GetComponent<Image>();
        so.FindProperty("outline").objectReferenceValue = card.GetComponent<Outline>();
        so.FindProperty("canvasGroup").objectReferenceValue = group;
        so.FindProperty("txtName").objectReferenceValue = name;
        so.FindProperty("txtTag").objectReferenceValue = tag;
        so.FindProperty("txtPaid").objectReferenceValue = paid;
        so.FindProperty("pipRoot").objectReferenceValue = pipRoot;
        so.FindProperty("pipTemplate").objectReferenceValue = pip.GetComponent<Image>();
        so.FindProperty("icon").objectReferenceValue = icon;
        so.FindProperty("barFill").objectReferenceValue = barFill;
        so.FindProperty("txtCondition").objectReferenceValue = condition;
        so.FindProperty("txtReward").objectReferenceValue = reward;
        so.FindProperty("claimButton").objectReferenceValue = claimButton;
        so.FindProperty("claimButtonImage").objectReferenceValue = claim.GetComponent<Image>();
        so.FindProperty("txtClaim").objectReferenceValue = claimText;
        so.FindProperty("flash").objectReferenceValue = flash;
        so.ApplyModifiedPropertiesWithoutUndo();
        return cardUI;
    }
    #endregion

    #region 생성 헬퍼
    private static RectTransform Rect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image Img(string name, Transform parent, Sprite sprite, Color color)
    {
        RectTransform rect = Rect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        return image;
    }

    /// <summary>
    /// 둥근 배경 + 테두리 (border 0이면 테두리 없음)
    /// </summary>
    private static RectTransform Panel(string name, Transform parent, Color color, float border)
    {
        Image image = Img(name, parent, rounded, color);
        image.type = Image.Type.Sliced;
        if (border > 0f)
        {
            Outline outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = Ink;
            outline.effectDistance = new Vector2(border, -border);
        }
        return image.rectTransform;
    }

    private static RectTransform Circle(string name, Transform parent, Color color)
    {
        return Img(name, parent, circle, color).rectTransform;
    }

    private static Image Bar(Transform parent, out RectTransform barRect)
    {
        Image back = Img("Bar", parent, rounded, BarBack);
        back.type = Image.Type.Sliced;
        barRect = back.rectTransform;

        Image fill = Img("Fill", barRect, null, Sky);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = 0f;
        fill.raycastTarget = false;
        Stretch(fill.rectTransform, 2f, 2f, 2f, 2f);
        return fill;
    }

    private static TMP_Text Text(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions alignment)
    {
        RectTransform rect = Rect(name, parent);
        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        return tmp;
    }

    private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void TopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    /// <summary>
    /// 비활성 색은 스크립트가 직접 칠하므로 버튼 틴트로 어둡게 하지 않음
    /// </summary>
    private static void NoDisabledTint(Button button)
    {
        ColorBlock colors = button.colors;
        colors.disabledColor = Color.white;
        button.colors = colors;
    }

    private static void Height(RectTransform rect, float height)
    {
        LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = height;
        element.minHeight = height;
    }

    private static Color Hex(int rgb)
    {
        return new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 0xFF);
    }
    #endregion
}
