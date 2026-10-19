using UnityEngine;

/// <summary>
/// Shop_Object에 붙이는 상호작용 스크립트.
/// 플레이어가 상호작용하면 청설모(ShopkeeperAI)가 달려오고,
/// 도착(IsReadyToServe)하면 Standing 자세를 취한 뒤 openDelay초 후 상점 UI를 연다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ShopObject : MonoBehaviour, IInteractable
{
    [Header("상점 연결")]
    [SerializeField] private ShopkeeperAI shopkeeper;
    [SerializeField] private ShopArea shopArea;

    [Header("연출")]
    [Tooltip("청설모가 Standing 자세를 취한 뒤 상점 UI가 열리기까지의 시간 (초)")]
    [SerializeField, Min(0f)] private float openDelay = 1.5f;

    private PlayerContext waitingPlayer;
    private ShopPanelUI shopUI;
    private bool waitingForShopkeeper;
    private bool preparingShop;
    private float openTimer;

    /// <summary>
    /// 청설모 / 상점 감지 영역 연결 (상점 섬 해금으로 청설모가 생성될 때 호출)
    /// </summary>
    public void Connect(ShopkeeperAI keeper, ShopArea area)
    {
        shopkeeper = keeper;
        shopArea = area;
    }

    // ── IInteractable ──

    public bool CanInteract(InteractionContext context)
    {
        return context.Player != null && !waitingForShopkeeper && !preparingShop;
    }

    public void Interact(InteractionContext context)
    {
        waitingPlayer = context.Player;

        // 청설모가 없으면 바로 UI 열기
        if (shopkeeper == null)
        {
            OpenShopUI();
            return;
        }

        // 청설모가 이미 상점에 있으면 Standing 후 UI 열기
        if (shopkeeper.IsReadyToServe)
        {
            BeginServing();
            return;
        }

        // 청설모가 아직 안 왔으면 불러서 대기, 이동 잠금
        shopkeeper.RequestService();
        waitingForShopkeeper = true;
        if (waitingPlayer.Movement != null)
            waitingPlayer.Movement.SetInputBlocked(true);
    }

    private void Update()
    {
        if (preparingShop)
        {
            UpdatePreparing();
            return;
        }

        if (!waitingForShopkeeper) return;

        // 청설모가 없어졌으면 바로 열기
        if (shopkeeper == null)
        {
            waitingForShopkeeper = false;
            OpenShopUI();
            return;
        }

        // 청설모가 도착했는지 매 프레임 확인
        if (shopkeeper.IsReadyToServe)
        {
            waitingForShopkeeper = false;
            BeginServing();
        }
    }

    // ── 청설모 응대 (Standing → openDelay 후 UI) ──

    private void BeginServing()
    {
        if (waitingPlayer.Movement != null)
            waitingPlayer.Movement.SetInputBlocked(true);

        shopkeeper.SetServing(true);
        preparingShop = true;
        openTimer = openDelay;
    }

    private void UpdatePreparing()
    {
        // 청설모가 없어졌거나 자리를 떠났으면 기다리지 않고 바로 열기
        if (shopkeeper == null || !shopkeeper.IsServing)
        {
            preparingShop = false;
            OpenShopUI();
            return;
        }

        openTimer -= Time.deltaTime;
        if (openTimer > 0f) return;

        preparingShop = false;
        OpenShopUI();
    }

    // ── 상점 UI ──

    private void OpenShopUI()
    {
        if (shopUI == null)
            shopUI = FindAnyObjectByType<ShopPanelUI>(FindObjectsInactive.Include);

        if (shopUI == null || waitingPlayer == null)
        {
            CancelShopkeeperServing();
            ReleasePlayer();
            return;
        }

        if (waitingPlayer.Movement != null)
            waitingPlayer.Movement.SetInputBlocked(true);

        shopUI.OnShopClosed += HandleShopClosed;
        shopUI.Open(waitingPlayer.Wallet, waitingPlayer.Inventory);
    }

    private void HandleShopClosed()
    {
        if (shopUI != null)
            shopUI.OnShopClosed -= HandleShopClosed;

        // 구매 여부에 따라 청설모 퇴장 연출이 갈린다
        if (shopkeeper != null)
            shopkeeper.FinishServing(shopUI != null && shopUI.PurchasedThisVisit);
        ReleasePlayer();
    }

    private void CancelShopkeeperServing()
    {
        if (shopkeeper != null)
            shopkeeper.SetServing(false);
    }

    private void ReleasePlayer()
    {
        waitingForShopkeeper = false;
        preparingShop = false;
        if (waitingPlayer != null && waitingPlayer.Movement != null)
            waitingPlayer.Movement.SetInputBlocked(false);
        waitingPlayer = null;
    }

    private void OnDisable()
    {
        if (shopUI != null)
            shopUI.OnShopClosed -= HandleShopClosed;
        CancelShopkeeperServing();
        ReleasePlayer();
    }
}
