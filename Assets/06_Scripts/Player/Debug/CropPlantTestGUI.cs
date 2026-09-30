using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// [TEMP] 작물 심기 테스트용 uGUI 패널. 삭제 예정.
// (OnGUI는 Device Simulator 터치 입력을 못 받아서 uGUI로 런타임 생성)
// HUD 퀵슬롯 번호(화면 표시 1~8) 버튼 탭 -> 그 퀵슬롯 아이템 1개 소모, 발밑 파낸 흙 칸에 작물 생성.
// Player(PlayerContext와 같은 오브젝트)에 붙여서 Play.
[RequireComponent(typeof(PlayerContext))]
public class CropPlantTestGUI : MonoBehaviour
{
    private const string DefaultFontPath = "Assets/08_Pont/yuhanKimbery SDF.asset";

    [Serializable]
    private class SeedCropMapping
    {
        public string SeedItemId; //씨앗 아이템 ID
        public PlantedCropObject CropPrefab; //심을 작물
    }

    [Tooltip("씨앗 아이템 ID -> 작물 prefab. 나중에 데이터로 옮길 임시 매핑")]
    [SerializeField]
    private List<SeedCropMapping> seedCropMappings = new List<SeedCropMapping>();

    [Tooltip("심을 위치 기준(발밑). 비워두면 플레이어 위치 사용")]
    [SerializeField]
    private Transform plantOrigin;

    [SerializeField]
    private int quickSlotCount = 8;

    [Header("패널 UI")]
    [Tooltip("한글 표시용 TMP 폰트. 비어 있으면 에디터에서 yuhanKimbery SDF 자동 할당")]
    [SerializeField]
    private TMP_FontAsset font;

    [SerializeField]
    private bool showPanel = true;

    private PlayerContext context;
    private HUDBottomPanel hudBottomPanel;

    private GameObject canvasRoot;
    private TMP_Text slotListText;
    private TMP_Text messageText;
    private float refreshTimer;

    // 작물이 심긴 칸 (구역 ID, 칸)
    private readonly HashSet<(int, Vector3Int)> plantedCells = new HashSet<(int, Vector3Int)>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (font == null)
        {
            font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultFontPath);
        }
    }
#endif

    private void Awake()
    {
        context = GetComponent<PlayerContext>();
        if (plantOrigin == null) plantOrigin = transform;
    }

    private void Start()
    {
        if (showPanel) BuildPanel();
    }

    private void OnDestroy()
    {
        if (canvasRoot != null) Destroy(canvasRoot);
    }

    private void Update()
    {
        if (slotListText == null) return;

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer > 0f) return;
        refreshTimer = 0.25f;

        slotListText.text = BuildSlotListText();
    }

    private void OnPlantButton(int slotNumber)
    {
        string result = TryPlant(slotNumber);
        if (messageText != null) messageText.text = result;
        refreshTimer = 0f;
    }

    private string BuildSlotListText()
    {
        if (context == null || context.Inventory == null) return "PlayerInventory 없음";

        if (hudBottomPanel == null) hudBottomPanel = FindAnyObjectByType<HUDBottomPanel>(FindObjectsInactive.Include);
        if (hudBottomPanel == null) return "HUDBottomPanel 없음";

        List<string> lines = new List<string>();
        for (int number = 1; number <= quickSlotCount; number++)
        {
            InvenItemData item = hudBottomPanel.GetQuickSlotItem(number);
            if (item == null) continue; //도구/음식/인벤토리 버튼 등 아이템 아닌 칸은 생략

            string plantable = FindCropPrefab(item.ItemID) != null ? "  <color=#7CFC00>[심기 가능]</color>" : "";
            lines.Add($"{number}. {item.ItemName} ({item.ItemID}) x{CountItem(item)}{plantable}");
        }
        return lines.Count > 0 ? string.Join("\n", lines) : "(퀵슬롯에 아이템 없음)";
    }

    private string TryPlant(int slotNumber)
    {
        if (hudBottomPanel == null) hudBottomPanel = FindAnyObjectByType<HUDBottomPanel>(FindObjectsInactive.Include);
        if (hudBottomPanel == null) return "HUDBottomPanel 없음";
        if (slotNumber < 1 || slotNumber > quickSlotCount) return $"퀵슬롯은 1~{quickSlotCount}";

        InvenItemData item = hudBottomPanel.GetQuickSlotItem(slotNumber);
        if (item == null) return $"{slotNumber}번 퀵슬롯에 아이템 없음";

        PlantedCropObject cropPrefab = FindCropPrefab(item.ItemID);
        if (cropPrefab == null) return $"{item.ItemName}({item.ItemID}) 은(는) 심을 수 없음 (매핑 없음)";

        GroundTileModifier tileModifier = TerritoryManager.Instance != null ? TerritoryManager.Instance.TileModifier : null;
        if (tileModifier == null) return "GroundTileModifier 없음";

        if (!tileModifier.TryGetModifiedCell(plantOrigin.position, out TerritoryZone zone, out Vector3Int cell))
            return "발밑이 파낸 흙이 아님";

        var cellKey = (zone.TerritoryZoneData.ZoneId, cell);
        if (plantedCells.Contains(cellKey)) return "이미 작물이 심겨 있음";

        if (context.Inventory.Remove(item, 1) <= 0) return "씨앗 소모 실패";

        Vector3 spawnPos = zone.TerritoryTileMapGround.GetCellCenterWorld(cell);
        PlantedCropObject crop = Instantiate(cropPrefab, spawnPos, Quaternion.identity);

        plantedCells.Add(cellKey);
        crop.OnRemoved += _ => plantedCells.Remove(cellKey);

        return $"{item.ItemName} 심음 → {zone.name} {cell}";
    }

    private int CountItem(InvenItemData item)
    {
        int total = 0;
        foreach (InventorySlot slot in context.Inventory.Slots)
        {
            if (slot.Item == item) total += slot.Count;
        }
        return total;
    }

    private PlantedCropObject FindCropPrefab(string itemId)
    {
        foreach (SeedCropMapping mapping in seedCropMappings)
        {
            if (mapping.SeedItemId == itemId && mapping.CropPrefab != null)
                return mapping.CropPrefab;
        }
        return null;
    }

    #region 패널 생성 (런타임 uGUI)
    private void BuildPanel()
    {
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            Debug.LogWarning("[CropPlantTestGUI] EventSystem이 없어 버튼 입력이 안 됨");
        }

        if (font == null)
        {
            Debug.LogWarning("[CropPlantTestGUI] font 미지정 - 한글이 깨질 수 있음");
        }

        canvasRoot = new GameObject("[TEMP] CropPlantTestCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        // 우상단 패널
        RectTransform panel = CreateRect("Panel", canvasRoot.transform);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1f, 1f);
        panel.anchoredPosition = new Vector2(-20f, -20f);
        panel.sizeDelta = new Vector2(520f, 0f);
        panel.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        VerticalLayoutGroup vertical = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vertical.padding = new RectOffset(16, 16, 12, 12);
        vertical.spacing = 8f;
        vertical.childControlWidth = true;
        vertical.childControlHeight = true;
        vertical.childForceExpandHeight = false;
        panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateText(panel, "== [TEMP] 작물 심기 (퀵슬롯 번호 탭) ==", 26f);
        slotListText = CreateText(panel, "", 24f);

        // 번호 버튼 줄 (1~8)
        RectTransform row = CreateRect("Buttons", panel);
        HorizontalLayoutGroup horizontal = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        horizontal.spacing = 6f;
        horizontal.childControlWidth = true;
        horizontal.childControlHeight = true;
        horizontal.childForceExpandWidth = true;
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;

        for (int number = 1; number <= quickSlotCount; number++)
        {
            int slotNumber = number;
            CreateButton(row, slotNumber.ToString(), () => OnPlantButton(slotNumber));
        }

        messageText = CreateText(panel, "", 24f);
        messageText.color = new Color(1f, 0.85f, 0.3f);
    }

    private RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private TMP_Text CreateText(Transform parent, string text, float size)
    {
        RectTransform rect = CreateRect("Text", parent);
        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = size;
        tmp.text = text;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    private void CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rect = CreateRect($"Btn_{label}", parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.9f);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        TMP_Text text = CreateText(rect, label, 30f);
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Center;
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
    }
    #endregion
}
