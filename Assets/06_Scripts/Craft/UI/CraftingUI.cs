using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 제작 UI.
/// 우측 생활 버튼(산업/농업/경제/마법) 클릭 -> 버튼 아래에 해당 생활의 레시피 목록 펼침 -> 레시피 선택 시 중앙 상세 패널 표시.
/// 상세 패널: 결과 아이콘/이름, 재료 3칸(보유/필요), 제작 개수(화살표, 1~max), 소요시간, 제작 버튼, 진행 슬라이더.
/// 제작 시작 시 재료를 소모하고, 1개씩 소요시간이 지나면 결과물 지급. 모두 끝나면 자동으로 UI를 닫는다.
/// </summary>
public class CraftingUI : MonoBehaviour
{
    [Serializable]
    private class CategoryButton
    {
        public CraftLifeType LifeType;
        public Button Button;
    }

    [Header("데이터")]
    [SerializeField]
    private CraftDatabase craftDatabase;
    [SerializeField]
    private ItemDatabase itemDatabase;

    [Header("패널")]
    [SerializeField]
    private GameObject panelRoot;
    [SerializeField]
    private Button closeButton;

    [Header("생활 분류 (우측)")]
    [SerializeField]
    private List<CategoryButton> categoryButtons = new List<CategoryButton>();
    [Tooltip("생활 버튼들과 같은 부모(VerticalLayoutGroup) 아래에 있는 레시피 목록 컨테이너. 선택한 버튼 바로 아래로 이동한다.")]
    [SerializeField]
    private RectTransform recipeListRoot;
    [SerializeField]
    private CraftRecipeEntryView recipeEntryTemplate;

    [Header("상세 (중앙)")]
    [SerializeField]
    private GameObject detailPanel;
    [SerializeField]
    private Image resultIcon;
    [SerializeField]
    private TMP_Text resultNameText;
    [SerializeField]
    private CraftMaterialSlotView[] materialSlots = new CraftMaterialSlotView[3];
    [Tooltip("재료 충족도 (0~1)")]
    [SerializeField]
    private Slider materialFillSlider;
    [Tooltip("강화 레시피일 때만 켜지는 필요 도구 영역 (슬롯 + 화살표)")]
    [SerializeField]
    private GameObject requiredToolRoot;
    [SerializeField]
    private CraftMaterialSlotView requiredToolSlot;

    [Header("개수 / 시간 / 제작")]
    [SerializeField]
    private TMP_Text countText;
    [SerializeField]
    private Button prevCountButton;
    [SerializeField]
    private Button nextCountButton;
    [Tooltip("개수 밑 제작 진행 슬라이더")]
    [SerializeField]
    private Slider craftProgressSlider;
    [SerializeField]
    private TMP_Text timeText;
    [SerializeField]
    private Button craftButton;
    [Tooltip("재료가 없을 때 최대 제작 수 상한")]
    [SerializeField, Min(1)]
    private int maxCraftCountCap = 99;

    private readonly List<CraftRecipeEntryView> entryViews = new List<CraftRecipeEntryView>();
    private List<RecipeData> currentRecipes = new List<RecipeData>();

    private CraftingTable currentTable;
    private PlayerContext player;
    private HUDBottomPanel hudPanel;

    private CraftLifeType openedCategory = CraftLifeType.None;
    private RecipeData selectedRecipe;
    private int craftCount = 1;

    private Coroutine craftRoutine;
    private RecipeData craftingRecipe;
    private int craftingRemaining;

    /// <summary>
    /// 현재 제작 진행 중인지
    /// </summary>
    public bool IsCrafting => craftingRecipe != null;

    /// <summary>
    /// 버튼 연결, 템플릿 숨김, 패널 초기 비활성화
    /// </summary>
    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
        if (recipeEntryTemplate != null) recipeEntryTemplate.gameObject.SetActive(false);

        if (closeButton != null) closeButton.onClick.AddListener(OnCloseButton);
        if (prevCountButton != null) prevCountButton.onClick.AddListener(() => ChangeCount(-1));
        if (nextCountButton != null) nextCountButton.onClick.AddListener(() => ChangeCount(1));
        if (craftButton != null) craftButton.onClick.AddListener(OnCraftButton);

        foreach (CategoryButton category in categoryButtons)
        {
            if (category.Button == null) continue;
            CraftLifeType lifeType = category.LifeType;
            category.Button.onClick.AddListener(() => OnCategoryButton(lifeType));
        }

        if (currentTable == null) panelRoot.SetActive(false);
    }

    /// <summary>
    /// 제작 UI 열기
    /// </summary>
    public void Open(CraftingTable table, PlayerContext playerContext)
    {
        Unbind();
        currentTable = table;
        player = playerContext;
        if (player != null && player.Inventory != null) player.Inventory.OnInventoryChanged += RefreshDetail;

        panelRoot.SetActive(true);
        openedCategory = CraftLifeType.None;
        selectedRecipe = null;
        if (recipeListRoot != null) recipeListRoot.gameObject.SetActive(false);
        if (detailPanel != null) detailPanel.SetActive(false);
        SetProgress(0f);
        SetInteractable(true);
    }

    /// <summary>
    /// 제작 UI 닫기. 제작 중이었다면 끝나지 않은 개수만큼 재료를 돌려준다.
    /// </summary>
    public void Close(CraftingTable table)
    {
        if (currentTable != table) return;

        if (IsCrafting)
        {
            if (craftRoutine != null) StopCoroutine(craftRoutine);
            craftRoutine = null;
            RefundMaterials(craftingRecipe, craftingRemaining);
            craftingRecipe = null;
            craftingRemaining = 0;
        }

        Unbind();
        currentTable = null;
        player = null;
        panelRoot.SetActive(false);
    }

    /// <summary>
    /// 닫기 버튼 -> 제작대 Close (이동 정지 해제 + UI 닫기)
    /// </summary>
    private void OnCloseButton()
    {
        if (IsCrafting) return;
        if (currentTable != null) currentTable.Close();
        else panelRoot.SetActive(false);
    }

    /// <summary>
    /// 인벤토리 변경 이벤트 구독 해제
    /// </summary>
    private void Unbind()
    {
        if (player != null && player.Inventory != null) player.Inventory.OnInventoryChanged -= RefreshDetail;
    }

    private void OnDestroy()
    {
        Unbind();
    }

    #region 생활 분류 / 레시피 목록
    /// <summary>
    /// 생활 버튼 클릭: 같은 버튼이면 접고, 다른 버튼이면 그 아래에 레시피 목록을 펼친다.
    /// </summary>
    private void OnCategoryButton(CraftLifeType lifeType)
    {
        if (IsCrafting) return;

        if (openedCategory == lifeType)
        {
            openedCategory = CraftLifeType.None;
            if (recipeListRoot != null) recipeListRoot.gameObject.SetActive(false);
            return;
        }

        openedCategory = lifeType;
        currentRecipes = craftDatabase != null && currentTable != null
            ? craftDatabase.GetRecipes(currentTable.FacilityId, lifeType, currentTable.Season)
            : new List<RecipeData>();

        PlaceRecipeListUnder(lifeType);
        BuildRecipeEntries();

        if (currentRecipes.Count > 0) SelectRecipe(currentRecipes[0]);
        else
        {
            selectedRecipe = null;
            if (detailPanel != null) detailPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 레시피 목록 컨테이너를 선택한 생활 버튼 바로 아래 순서로 이동
    /// </summary>
    private void PlaceRecipeListUnder(CraftLifeType lifeType)
    {
        if (recipeListRoot == null) return;

        recipeListRoot.gameObject.SetActive(true);
        foreach (CategoryButton category in categoryButtons)
        {
            if (category.LifeType != lifeType || category.Button == null) continue;

            Transform buttonTransform = category.Button.transform;
            if (buttonTransform.parent != recipeListRoot.parent) break;

            int target = buttonTransform.GetSiblingIndex() + 1;
            if (recipeListRoot.GetSiblingIndex() < target) target--;
            recipeListRoot.SetSiblingIndex(target);
            break;
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(recipeListRoot.parent as RectTransform);
    }

    /// <summary>
    /// 현재 레시피 목록으로 항목 View 생성/재사용
    /// </summary>
    private void BuildRecipeEntries()
    {
        if (recipeEntryTemplate == null || recipeListRoot == null) return;

        for (int i = 0; i < currentRecipes.Count; i++)
        {
            RecipeData recipe = currentRecipes[i];
            CraftRecipeEntryView view;
            if (i < entryViews.Count) view = entryViews[i];
            else
            {
                view = Instantiate(recipeEntryTemplate, recipeListRoot);
                entryViews.Add(view);
            }

            view.gameObject.SetActive(true);
            view.Setup(GetResultIcon(recipe), GetResultName(recipe), () => SelectRecipe(recipe));
        }

        for (int i = currentRecipes.Count; i < entryViews.Count; i++) entryViews[i].gameObject.SetActive(false);
    }
    #endregion

    #region 상세 패널
    /// <summary>
    /// 레시피 선택: 개수 1로 초기화하고 상세 패널 갱신
    /// </summary>
    private void SelectRecipe(RecipeData recipe)
    {
        if (IsCrafting) return;

        selectedRecipe = recipe;
        craftCount = 1;
        for (int i = 0; i < currentRecipes.Count && i < entryViews.Count; i++)
        {
            entryViews[i].SetSelected(currentRecipes[i] == recipe);
        }

        if (detailPanel != null) detailPanel.SetActive(true);
        SetProgress(0f);
        RefreshDetail();
    }

    /// <summary>
    /// 화살표로 제작 개수 변경 (1 ~ max)
    /// </summary>
    private void ChangeCount(int delta)
    {
        if (selectedRecipe == null || IsCrafting) return;

        int max = Mathf.Max(1, GetMaxCraftCount(selectedRecipe));
        craftCount = Mathf.Clamp(craftCount + delta, 1, max);
        RefreshDetail();
    }

    /// <summary>
    /// 상세 패널 전체 갱신 (재료 보유량 / 개수 / 소요시간 / 버튼 상태)
    /// </summary>
    private void RefreshDetail()
    {
        if (selectedRecipe == null) return;

        RecipeData recipe = selectedRecipe;
        int max = GetMaxCraftCount(recipe);
        if (!IsCrafting) craftCount = Mathf.Clamp(craftCount, 1, Mathf.Max(1, max));

        if (resultIcon != null)
        {
            Sprite icon = GetResultIcon(recipe);
            resultIcon.sprite = icon;
            resultIcon.enabled = icon != null;
        }
        if (resultNameText != null) resultNameText.text = GetResultName(recipe);

        // 강화 레시피: 필요 도구 아이콘 + 보유 여부(0/1, 1/1)
        if (requiredToolRoot != null) requiredToolRoot.SetActive(recipe.IsUpgrade);
        if (recipe.IsUpgrade && requiredToolSlot != null && !IsCrafting)
        {
            requiredToolSlot.Set(GetItemIcon(recipe.RequiredToolID), Mathf.Min(1, GetItemCount(recipe.RequiredToolID)), 1);
        }

        // 재료: 제작 개수만큼 곱한 필요량 기준 (제작 중이면 이미 소모했으므로 갱신하지 않는다)
        if (!IsCrafting)
        {
            float fillSum = 0f;
            for (int i = 0; i < materialSlots.Length; i++)
            {
                CraftMaterialSlotView slot = materialSlots[i];
                if (slot == null) continue;

                if (i >= recipe.Materials.Count)
                {
                    slot.Clear();
                    continue;
                }

                CraftMaterial material = recipe.Materials[i];
                int need = material.Count * craftCount;
                int have = GetItemCount(material.ItemID);
                slot.Set(GetItemIcon(material.ItemID), have, need);
                fillSum += need > 0 ? Mathf.Clamp01((float)have / need) : 1f;
            }

            if (materialFillSlider != null)
                materialFillSlider.value = recipe.Materials.Count > 0 ? fillSum / recipe.Materials.Count : 1f;
        }

        if (IsCrafting) return;

        if (countText != null) countText.text = $"{craftCount}/{Mathf.Max(0, max)}";
        if (timeText != null)
        {
            // 도구 보유 조건 때문에 막혔으면 소요시간 대신 사유 표시
            string blockReason = GetToolBlockReason(recipe);
            timeText.text = blockReason ?? $"소요시간 : {FormatSeconds(recipe.CraftTimeSec * craftCount)}";
        }

        if (!IsCrafting)
        {
            if (prevCountButton != null) prevCountButton.interactable = craftCount > 1;
            if (nextCountButton != null) nextCountButton.interactable = craftCount < max;
            if (craftButton != null) craftButton.interactable = max >= craftCount && max > 0;
        }
    }

    private static string FormatSeconds(float seconds)
    {
        return Mathf.Approximately(seconds, Mathf.Round(seconds)) ? $"{Mathf.RoundToInt(seconds)}s" : $"{seconds:0.#}s";
    }
    #endregion

    #region 제작
    /// <summary>
    /// 제작 버튼: 재료 소모 후 제작 코루틴 시작
    /// </summary>
    private void OnCraftButton()
    {
        if (selectedRecipe == null || IsCrafting || player == null) return;
        if (GetMaxCraftCount(selectedRecipe) < craftCount) return;

        // 재료 소모 전에 제작 상태로 전환 (소모 시 인벤토리 이벤트로 개수가 다시 계산되지 않도록)
        int count = craftCount;
        craftingRecipe = selectedRecipe;
        craftingRemaining = count;
        ConsumeMaterials(craftingRecipe, count);
        SetInteractable(false);
        craftRoutine = StartCoroutine(CraftRoutine(craftingRecipe, count));
    }

    /// <summary>
    /// 1개씩 소요시간이 지날 때마다 결과물 지급, 진행 슬라이더 갱신. 끝나면 UI 자동 닫기.
    /// </summary>
    private IEnumerator CraftRoutine(RecipeData recipe, int count)
    {
        float unitTime = Mathf.Max(0f, recipe.CraftTimeSec);

        for (int i = 0; i < count; i++)
        {
            float elapsed = 0f;
            while (elapsed < unitTime)
            {
                elapsed += Time.deltaTime;
                SetProgress((i + Mathf.Clamp01(elapsed / unitTime)) / count);
                yield return null;
            }

            GiveResult(recipe);
            craftingRemaining--;
            SetProgress((float)(i + 1) / count);
        }

        craftRoutine = null;
        craftingRecipe = null;
        SetInteractable(true);

        // 제작 완료 -> 자동 닫기 (제작대 Close가 이동 정지 해제 후 Close(table) 호출)
        if (currentTable != null) currentTable.Close();
    }

    /// <summary>
    /// 결과물 지급. 도구도 인벤토리 아이템(도구 아이템)으로 지급한다.
    /// 강화 레시피는 기존 도구를 제거하고 강화 도구로 교체한다.
    /// </summary>
    private void GiveResult(RecipeData recipe)
    {
        if (player == null) return;

        InvenItemData item = itemDatabase != null ? itemDatabase.FindById(recipe.ResultItemID) : null;
        if (item == null || player.Inventory == null)
        {
            Debug.LogWarning($"[CraftingUI] 아이템 지급 실패: {recipe.ResultItemID}");
            return;
        }

        if (recipe.IsUpgrade)
        {
            InvenItemData oldTool = FindInventoryItem(recipe.RequiredToolID);
            if (oldTool != null) player.Inventory.Remove(oldTool, 1);
        }

        int left = player.Inventory.Add(item, recipe.ResultCount);
        if (left > 0) Debug.LogWarning($"[CraftingUI] 인벤토리가 가득 차 {item.ItemName} {left}개를 받지 못했습니다.");
    }

    /// <summary>
    /// 재료 소모 (count 개 분량)
    /// </summary>
    private void ConsumeMaterials(RecipeData recipe, int count)
    {
        PlayerInventory inventory = player != null ? player.Inventory : null;
        if (inventory == null) return;

        foreach (CraftMaterial material in recipe.Materials)
        {
            int toRemove = material.Count * count;
            // 같은 ID라도 SO 인스턴스가 다를 수 있으므로(세이브 로드 런타임 아이템) ID로 찾아 제거
            while (toRemove > 0)
            {
                InvenItemData item = FindInventoryItem(material.ItemID);
                if (item == null) break;
                int removed = inventory.Remove(item, toRemove);
                if (removed <= 0) break;
                toRemove -= removed;
            }
        }
    }

    /// <summary>
    /// 제작 중 강제로 닫힌 경우, 끝나지 않은 개수만큼 재료 반환
    /// </summary>
    private void RefundMaterials(RecipeData recipe, int count)
    {
        if (recipe == null || count <= 0 || player == null || player.Inventory == null) return;

        foreach (CraftMaterial material in recipe.Materials)
        {
            InvenItemData item = itemDatabase != null ? itemDatabase.FindById(material.ItemID) : null;
            if (item != null) player.Inventory.Add(item, material.Count * count);
        }
    }

    /// <summary>
    /// 제작 중엔 생활 버튼/화살표/제작/닫기 버튼 잠금
    /// </summary>
    private void SetInteractable(bool interactable)
    {
        if (closeButton != null) closeButton.interactable = interactable;
        if (craftButton != null) craftButton.interactable = interactable;
        if (prevCountButton != null) prevCountButton.interactable = interactable;
        if (nextCountButton != null) nextCountButton.interactable = interactable;
        foreach (CategoryButton category in categoryButtons)
        {
            if (category.Button != null) category.Button.interactable = interactable;
        }
        if (interactable) RefreshDetail();
    }

    private void SetProgress(float value)
    {
        if (craftProgressSlider != null) craftProgressSlider.value = Mathf.Clamp01(value);
    }
    #endregion

    #region 조회
    /// <summary>
    /// 현재 보유 재료로 만들 수 있는 최대 개수
    /// </summary>
    private int GetMaxCraftCount(RecipeData recipe)
    {
        if (recipe == null) return 0;
        if (GetToolBlockReason(recipe) != null) return 0;

        // 도구는 1개만 보유하므로 한 번에 1개
        int max = recipe.IsToolResult ? 1 : maxCraftCountCap;
        foreach (CraftMaterial material in recipe.Materials)
        {
            if (material.Count <= 0) continue;
            max = Mathf.Min(max, GetItemCount(material.ItemID) / material.Count);
        }
        return max;
    }

    /// <summary>
    /// 도구 보유 조건으로 제작이 막힌 사유. 막히지 않았으면 null.
    /// 기본 도구: 같은 종류 도구(강화 포함)를 이미 가지고 있으면 불가.
    /// 강화 도구: 기존 도구(RequiredToolID)를 가지고 있어야 가능.
    /// </summary>
    private string GetToolBlockReason(RecipeData recipe)
    {
        if (recipe == null || !recipe.IsToolResult) return null;

        if (recipe.IsUpgrade)
        {
            if (GetItemCount(recipe.ResultItemID) > 0) return "이미 보유한 도구";
            if (GetItemCount(recipe.RequiredToolID) <= 0) return $"{GetItemName(recipe.RequiredToolID)} 필요";
            return null;
        }

        return HasSameTool(recipe.ResultItemID) ? "이미 보유한 도구" : null;
    }

    /// <summary>
    /// 같은 종류 도구(등급 무관)를 인벤토리에 가지고 있는지
    /// </summary>
    private bool HasSameTool(string toolId)
    {
        PlayerInventory inventory = player != null ? player.Inventory : null;
        if (inventory == null) return false;

        string baseId = ToolItemIds.ToBaseId(toolId);
        for (int i = 0; i < inventory.Slots.Count; i++)
        {
            InventorySlot slot = inventory.Slots[i];
            if (slot.Item != null && slot.Count > 0 && ToolItemIds.IsSameTool(slot.Item.ItemID, baseId)) return true;
        }
        return false;
    }

    /// <summary>
    /// 인벤토리에서 해당 ID 아이템 총 개수
    /// </summary>
    private int GetItemCount(string itemId)
    {
        PlayerInventory inventory = player != null ? player.Inventory : null;
        if (inventory == null) return 0;

        int total = 0;
        for (int i = 0; i < inventory.Slots.Count; i++)
        {
            InventorySlot slot = inventory.Slots[i];
            if (slot.Item != null && slot.Item.ItemID == itemId) total += slot.Count;
        }
        return total;
    }

    /// <summary>
    /// 인벤토리 슬롯에서 해당 ID의 아이템 SO 인스턴스 찾기
    /// </summary>
    private InvenItemData FindInventoryItem(string itemId)
    {
        PlayerInventory inventory = player != null ? player.Inventory : null;
        if (inventory == null) return null;

        for (int i = 0; i < inventory.Slots.Count; i++)
        {
            InventorySlot slot = inventory.Slots[i];
            if (slot.Item != null && slot.Item.ItemID == itemId && slot.Count > 0) return slot.Item;
        }
        return null;
    }

    private string GetResultName(RecipeData recipe)
    {
        return GetItemName(recipe.ResultItemID);
    }

    private string GetItemName(string itemId)
    {
        InvenItemData item = itemDatabase != null ? itemDatabase.FindById(itemId) : null;
        return item != null ? item.ItemName : itemId;
    }

    /// <summary>
    /// 결과 아이콘. 도구는 ToolData 아이콘 우선, 없으면 HUD 아이콘 규칙
    /// </summary>
    private Sprite GetResultIcon(RecipeData recipe)
    {
        if (recipe.IsToolResult)
        {
            ToolData tool = craftDatabase != null ? craftDatabase.FindTool(recipe.ResultItemID) : null;
            Sprite icon = tool != null ? tool.GetIcon() : null;
            if (icon != null) return icon;
        }
        return GetItemIcon(recipe.ResultItemID);
    }

    /// <summary>
    /// 아이템 아이콘은 HUDBottomPanel의 아이콘 규칙 재사용
    /// </summary>
    private Sprite GetItemIcon(string itemId)
    {
        InvenItemData item = itemDatabase != null ? itemDatabase.FindById(itemId) : null;
        if (item == null) return null;

        if (hudPanel == null) hudPanel = FindAnyObjectByType<HUDBottomPanel>(FindObjectsInactive.Include);
        return hudPanel != null ? hudPanel.GetItemIcon(item) : null;
    }
    #endregion
}
