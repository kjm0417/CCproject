using UnityEngine;

/// <summary>
/// Shop_Object에 붙이는 상호작용 스크립트.
/// 플레이어가 상호작용하면 청설모(ShopkeeperAI)가 달려오고,
/// 도착(IsReadyToServe) 후 상점 UI를 연다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ShopObject : MonoBehaviour, IInteractable
{
    [Header("상점 연결")]
    [SerializeField] private ShopkeeperAI shopkeeper;
    [SerializeField] private ShopArea shopArea;

    private PlayerContext waitingPlayer;
    private ShopPanelUI shopUI;
    private bool waitingForShopkeeper;

    // ── IInteractable ──

    public bool CanInteract(InteractionContext context)
    {
        return context.Player != null && !waitingForShopkeeper;
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

        // 청설모가 이미 상점에 있으면 바로 UI 열기
        if (shopkeeper.IsReadyToServe)
        {
            OpenShopUI();
            return;
        }

        // 청설모가 아직 안 왔으면 대기, 이동 잠금
        waitingForShopkeeper = true;
        if (waitingPlayer.Movement != null)
            waitingPlayer.Movement.SetInputBlocked(true);
    }

    private void Update()
    {
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
            OpenShopUI();
        }
    }

    // ── 상점 UI ──

    private void OpenShopUI()
    {
        if (shopUI == null)
            shopUI = FindAnyObjectByType<ShopPanelUI>(FindObjectsInactive.Include);

        if (shopUI == null || waitingPlayer == null)
        {
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
        ReleasePlayer();
    }

    private void ReleasePlayer()
    {
        waitingForShopkeeper = false;
        if (waitingPlayer != null && waitingPlayer.Movement != null)
            waitingPlayer.Movement.SetInputBlocked(false);
        waitingPlayer = null;
    }

    private void OnDisable()
    {
        if (shopUI != null)
            shopUI.OnShopClosed -= HandleShopClosed;
        ReleasePlayer();
    }
}
