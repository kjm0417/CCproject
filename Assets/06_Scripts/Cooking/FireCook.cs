using UnityEngine;

/// <summary>
/// 모닥불 요리 스테이션. 상호작용하면 요리 UI가 열리고,
/// 완성된 음식은 모닥불 위에 표시된다. 다시 상호작용하면 획득.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FireCook : MonoBehaviour, IInteractable
{
    private enum State { Idle, Cooking, Ready }

    [Header("요리 설정")]
    [SerializeField] private int facilityID = 42002;

    [Header("완성 음식 표시")]
    [SerializeField] private SpriteRenderer foodDisplay;
    [SerializeField] private Vector3 foodOffset = new Vector3(0f, 0.8f, 0f);

    private State state = State.Idle;
    private PlayerContext player;
    private CookingPanelUI cookingUI;
    private InvenItemData storedFood;
    private int storedCount;
    private bool subscribedToPlayer;

    private void Start()
    {
        if (foodDisplay == null)
        {
            var go = new GameObject("FoodDisplay");
            go.transform.SetParent(transform);
            go.transform.localPosition = foodOffset;
            foodDisplay = go.AddComponent<SpriteRenderer>();
            foodDisplay.sortingOrder = 10;
        }
        foodDisplay.enabled = false;
    }

    // ── IInteractable ──

    public bool CanInteract(InteractionContext context)
    {
        // 요리 진행 중에는 상호작용 불가
        return context.Player != null && state != State.Cooking;
    }

    public void Interact(InteractionContext context)
    {
        player = context.Player;

        if (state == State.Ready)
        {
            PickupFood();
            return;
        }

        OpenUI();
    }

    // ── UI 열기/닫기 ──

    private void OpenUI()
    {
        if (cookingUI == null)
            cookingUI = FindAnyObjectByType<CookingPanelUI>(FindObjectsInactive.Include);
        if (cookingUI == null || player == null) return;

        SubscribeToPlayer();
        cookingUI.OnPanelClosed += HandlePanelClosed;

        if (player.Movement != null) player.Movement.SetInputBlocked(true);
        cookingUI.Open(player, facilityID);
    }

    private void CloseUI()
    {
        if (cookingUI != null)
        {
            cookingUI.OnPanelClosed -= HandlePanelClosed;
            cookingUI.Close();
        }
        if (player != null && player.Movement != null)
            player.Movement.SetInputBlocked(false);
    }

    // ── 이벤트 구독 ──

    private void SubscribeToPlayer()
    {
        if (subscribedToPlayer || player?.Cooking == null) return;
        player.Cooking.OnCookSuccess += HandleCookSuccess;
        player.Cooking.OnCookingChanged += HandleCookingChanged;
        subscribedToPlayer = true;
    }

    private void UnsubscribeFromPlayer()
    {
        if (!subscribedToPlayer || player?.Cooking == null) return;
        player.Cooking.OnCookSuccess -= HandleCookSuccess;
        player.Cooking.OnCookingChanged -= HandleCookingChanged;
        subscribedToPlayer = false;
    }

    // ── 이벤트 핸들러 ──

    /// <summary>자동 요리 시작 시 UI를 닫고 Cooking 상태로 전환.</summary>
    private void HandleCookingChanged()
    {
        var cooking = player?.Cooking;
        if (cooking == null) return;

        // 자동 요리가 시작된 경우에만 UI를 닫는다 (미니게임은 UI 위에서 진행).
        if (cooking.IsBusy && cooking.IsAutomatic && state == State.Idle)
        {
            state = State.Cooking;
            if (cookingUI != null) cookingUI.DetachSession();
            CloseUI();
        }
    }

    /// <summary>요리 성공 시 인벤토리에서 결과를 회수하여 모닥불 위에 표시.</summary>
    private void HandleCookSuccess(CookingRecipeData recipe)
    {
        if (recipe == null || !subscribedToPlayer) return;

        // TryCook이 인벤토리에 넣은 결과물을 모닥불로 옮긴다.
        storedFood = recipe.ResultItem;
        storedCount = recipe.ResultCount;
        if (player?.Inventory != null)
            player.Inventory.Remove(recipe.ResultItem, recipe.ResultCount);

        ShowFood();
        CloseUI();
        state = State.Ready;
    }

    /// <summary>플레이어가 요리하지 않고 UI를 닫았을 때.</summary>
    private void HandlePanelClosed()
    {
        if (cookingUI != null) cookingUI.OnPanelClosed -= HandlePanelClosed;

        if (player != null && player.Movement != null)
            player.Movement.SetInputBlocked(false);

        // 요리를 시작하지 않은 채 닫았으면 구독 해제
        if (state == State.Idle)
            UnsubscribeFromPlayer();
    }

    // ── 음식 획득 ──

    private void PickupFood()
    {
        if (storedFood == null || player?.Inventory == null) return;

        int remaining = player.Inventory.Add(storedFood, storedCount);
        if (remaining > 0)
        {
            // 인벤토리가 꽉 찬 경우 남은 수량을 모닥불에 유지
            storedCount = remaining;
            return;
        }

        storedFood = null;
        storedCount = 0;
        HideFood();
        UnsubscribeFromPlayer();
        state = State.Idle;
    }

    // ── 음식 표시 ──

    private void ShowFood()
    {
        if (foodDisplay == null || storedFood == null) return;
        foodDisplay.sprite = CookingItemIcons.Resolve(storedFood);
        foodDisplay.enabled = foodDisplay.sprite != null;
    }

    private void HideFood()
    {
        if (foodDisplay == null) return;
        foodDisplay.sprite = null;
        foodDisplay.enabled = false;
    }

    private void OnDisable()
    {
        UnsubscribeFromPlayer();
    }
}
