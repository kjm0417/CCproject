using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CookingPanelUI : MonoBehaviour, IEscapeClosable
{
    [SerializeField] private CookingData cookingData;
    [SerializeField] private int craftFacilityID;
    [SerializeField] private RectTransform recipeContent;
    [SerializeField] private FoodSlotUI recipeSlotPrefab;
    [SerializeField] private RectTransform ingredientContent;
    [SerializeField] private NeedFoodSlotUI ingredientSlotPrefab;
    [SerializeField] private TMP_Text selectedName;
    [SerializeField] private Image selectedIcon;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button cookButton;
    [SerializeField] private Button autoCookButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private CookingMinigame minigame;
    private readonly List<FoodSlotUI> recipeSlots = new List<FoodSlotUI>();
    private readonly List<NeedFoodSlotUI> ingredientSlots = new List<NeedFoodSlotUI>();
    private PlayerInventory inventory;
    private PlayerCooking cooking;
    private bool initialized;
    private bool ownsSession;
    public event System.Action OnPanelClosed;
    public CookingRecipeData SelectedRecipe { get; private set; }

    private void Awake()
    {
        // 프리팹 편집용 미리보기 슬롯은 런타임 목록에서 제외한다.
        foreach (Transform child in recipeContent) child.gameObject.SetActive(false);
        foreach (Transform child in ingredientContent) child.gameObject.SetActive(false);
        cookButton.onClick.AddListener(Cook);
        autoCookButton.onClick.AddListener(AutoCook);
        closeButton.onClick.AddListener(Close);
    }

    private void Start()
    {
        if (!initialized) Initialize(FindAnyObjectByType<PlayerContext>());
    }

    public void Initialize(PlayerContext player)
    {
        CancelSession();
        Unsubscribe();
        inventory = player != null ? player.Inventory : null;
        cooking = player != null ? player.Cooking : null;
        if (cooking != null && cooking.Data != null) cookingData = cooking.Data;
        initialized = true;
        BuildRecipes();
        if (isActiveAndEnabled) Subscribe();
        Refresh();
    }

    public void Open(PlayerContext player, int facilityID = 0)
    {
        craftFacilityID = facilityID;
        gameObject.SetActive(true);   // Awake가 먼저 실행되어 미리보기 자식을 숨긴다.
        Initialize(player);           // 그 뒤에 BuildRecipes로 실제 슬롯을 생성한다.
    }

    public void Close() => gameObject.SetActive(false);

    // ESC: 요리 창 닫기 (조리·미니게임 중이면 취소되고 재료는 소모되지 않는다)
    public bool IsEscapeOpen => gameObject.activeInHierarchy;
    public bool BlocksShortcuts => true;
    public void CloseByEscape() => Close();

    // 외부 스테이션이 세션을 이어받을 때 호출. UI가 닫혀도 요리를 취소하지 않는다.
    public void DetachSession() => ownsSession = false;

    private void OnEnable() { UIEscapeStack.Push(this); if (initialized) { Subscribe(); Refresh(); } }
    private void OnDisable() { Unsubscribe(); CancelSession(); OnPanelClosed?.Invoke(); }

    private void Subscribe()
    {
        Unsubscribe();
        if (inventory != null) inventory.OnInventoryChanged += Refresh;
        if (cooking != null) cooking.OnCookingChanged += Refresh;
    }

    private void Unsubscribe()
    {
        if (inventory != null) inventory.OnInventoryChanged -= Refresh;
        if (cooking != null) cooking.OnCookingChanged -= Refresh;
    }

    private void CancelSession()
    {
        if (!ownsSession) return;
        ownsSession = false;
        if (minigame != null) minigame.Cancel();
        if (cooking != null) cooking.CancelCooking();
    }

    private void BuildRecipes()
    {
        foreach (FoodSlotUI slot in recipeSlots)
        {
            slot.gameObject.SetActive(false);
            Destroy(slot.gameObject);
        }
        recipeSlots.Clear();
        SelectedRecipe = null;
        if (cookingData != null)
            foreach (CookingRecipeData recipe in cookingData.GetUnlockedRecipes(craftFacilityID))
            {
                if (recipe.ResultItem == null) continue;
                FoodSlotUI slot = Instantiate(recipeSlotPrefab, recipeContent);
                slot.Bind(recipe, () => SelectRecipe(recipe));
                recipeSlots.Add(slot);
            }
        SelectRecipe(recipeSlots.Count > 0 ? recipeSlots[0].Recipe : null);
        ResizeContent(recipeContent, recipeSlots.Count, false);
    }

    public void SelectRecipe(CookingRecipeData recipe)
    {
        if (cooking != null && cooking.IsBusy) return;
        SelectedRecipe = recipe;
        if (selectedName != null) selectedName.text = recipe != null ? recipe.ResultItem.ItemName : string.Empty;
        if (selectedIcon != null)
        {
            selectedIcon.sprite = recipe != null ? CookingItemIcons.Resolve(recipe.ResultItem) : null;
            selectedIcon.enabled = selectedIcon.sprite != null;
        }
        int count = recipe != null ? recipe.Ingredients.Count : 0;
        while (ingredientSlots.Count < count)
            ingredientSlots.Add(Instantiate(ingredientSlotPrefab, ingredientContent));
        for (int i = 0; i < ingredientSlots.Count; i++)
        {
            ingredientSlots[i].gameObject.SetActive(i < count);
            if (i < count) ingredientSlots[i].Bind(recipe.Ingredients[i]);
        }
        ResizeContent(ingredientContent, count, true);
        Refresh();
    }

    private void Refresh()
    {
        bool busy = cooking != null && cooking.IsBusy;
        foreach (FoodSlotUI slot in recipeSlots) slot.Refresh(slot.Recipe == SelectedRecipe, !busy);
        foreach (NeedFoodSlotUI slot in ingredientSlots)
            if (slot.gameObject.activeSelf) slot.Refresh(inventory);
        string reason = string.Empty;
        bool canCook = cooking != null && cooking.CanCook(SelectedRecipe, false, out reason);
        cookButton.interactable = canCook && minigame != null;
        autoCookButton.interactable = canCook && cooking.CanAutoCook(SelectedRecipe);
        if (statusText != null)
        {
            if (cooking == null) statusText.text = "플레이어를 찾을 수 없습니다.";
            else if (busy || !string.IsNullOrEmpty(cooking.LastMessage)) statusText.text = cooking.LastMessage;
            else if (!canCook) statusText.text = reason;
            else statusText.text = cooking.CanAutoCook(SelectedRecipe)
                ? "자동 조리가 가능합니다." : "첫 조리는 미니게임을 성공해야 합니다.";
        }
    }

    public void Cook()
    {
        if (cooking == null || minigame == null) return;
        if (!cooking.TryStartManual(SelectedRecipe, out int requestID, out string reason))
        { if (statusText != null) statusText.text = reason; return; }
        ownsSession = true;
        PlayerCooking current = cooking;
        minigame.Begin(SelectedRecipe, success =>
        {
            ownsSession = false;
            if (current != null) current.CompleteManual(requestID, success);
        });
    }

    public void AutoCook()
    {
        if (cooking == null) return;
        if (cooking.TryAutoCook(SelectedRecipe, out string reason)) ownsSession = true;
        else if (statusText != null) statusText.text = reason;
    }

    private static void ResizeContent(RectTransform content, int count, bool horizontal)
    {
        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        RectTransform viewport = content.parent as RectTransform;
        if (grid == null || viewport == null) return;
        if (horizontal)
        {
            grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            grid.constraintCount = 1;
            float width = grid.padding.horizontal + count * grid.cellSize.x + Mathf.Max(0, count - 1) * grid.spacing.x;
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(viewport.rect.width, width));
        }
        else
        {
            int columns = Mathf.Max(1, grid.constraintCount);
            int rows = Mathf.CeilToInt((float)count / columns);
            float height = grid.padding.vertical + rows * grid.cellSize.y + Mathf.Max(0, rows - 1) * grid.spacing.y;
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(viewport.rect.height, height));
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }
}
