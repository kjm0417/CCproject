using System;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour, IPlayerComponent
{
    [Header("상호작용 대상 찾기")]
    [SerializeField]
    private Transform interactionOrigin;

    [Tooltip("나중에 필드 오브젝트 레이어로 설정하면 됩니다. 지금은 테스트용으로 Everything 상태여도 e됨.")]
    [SerializeField]
    private LayerMask interactableLayers = ~0;

    [Header("타겟 감지")]
    [Tooltip("타겟 탐색 주기(초). 0이면 매 프레임.")]
    [SerializeField]
    private float scanInterval = 0.15f;

    [Header("임시 도구 테스트")]
    [Tooltip("임시 테스트 값입니다. 나중에는 장착한 도구 데이터에서 자동으로 설정되게 하면 됨.")]
    [SerializeField]
    private ToolType currentToolType = ToolType.None;

    [Tooltip("켜두면 장비 판정이 없을 때 오브젝트가 원하는 도구를 들고 있다고 가정.")]
    [SerializeField]
    private bool assumeCorrectToolInRange = true;

    [Header("자동 장비 판정")]
    [SerializeField]
    private bool requireToolInInventory = true;
    [SerializeField]
    private string swordItemId = "25101";
    [SerializeField]
    private string axeItemId = "22101";
    [SerializeField]
    private string pickaxeItemId = "21101";
    [SerializeField]
    private string shovelItemId = "23101";

    // 현재 감지된 타겟이 바뀔 때 UI 등 외부에 알림 (타겟, 해당 타겟에 사용할 도구 타입)
    public event Action<IInteractable, ToolType> OnTargetChanged;
    public IInteractable CurrentTarget { get; private set; }
    public ToolType CurrentTargetToolType { get; private set; }

    private PlayerContext context;
    private PlayerStats stats;
    private float scanTimer;

    public void Initialize(PlayerContext playerContext)
    {
        context = playerContext;
        stats = playerContext.Stats;

        if (interactionOrigin == null)
        {
            interactionOrigin = transform;
        }
    }

    private void Update()
    {
        if (!context) return;

        scanTimer -= Time.deltaTime;
        if (scanTimer > 0f) return;
        scanTimer = scanInterval;

        IInteractable newTarget = FindNearestInteractable();
        ToolType newToolType = newTarget != null ? ResolveToolType(newTarget) : ToolType.None;

        if (newTarget != CurrentTarget || newToolType != CurrentTargetToolType)
        {
            CurrentTarget = newTarget;
            CurrentTargetToolType = newToolType;
            OnTargetChanged?.Invoke(CurrentTarget, CurrentTargetToolType);
        }
    }

    public void SetCurrentTool(ToolType toolType)
    {
        currentToolType = toolType;
    }

    public void Interact()
    {
        TryInteract();
    }

    public bool TryInteract()
    {
        if (CurrentTarget == null) return false;

        ToolType toolType = ResolveToolType(CurrentTarget);
        InteractionContext interactionContext = CreateInteractionContext(toolType);
        if (!CurrentTarget.CanInteract(interactionContext)) return false;

        context.Animation?.PlayToolInteraction(toolType);
        CurrentTarget.Interact(interactionContext);
        return true;
    }

    private IInteractable FindNearestInteractable()
    {
        float range = stats != null ? stats.AttackRange : 1f;
        Vector2 origin = interactionOrigin.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range, interactableLayers);

        IInteractable nearest = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            IInteractable interactable = hits[i].GetComponentInParent<IInteractable>();
            if (interactable == null) continue;

            float distance = Vector2.SqrMagnitude((Vector2)hits[i].transform.position - origin);
            if (distance < nearestDistance)
            {
                nearest = interactable;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private ToolType ResolveToolType(IInteractable target)
    {
        IToolInteractionTarget toolTarget = target as IToolInteractionTarget;
        ToolType preferredToolType = toolTarget != null ? toolTarget.PreferredToolType : ToolType.None;

        if (preferredToolType != ToolType.None)
        {
            if (!assumeCorrectToolInRange && currentToolType != preferredToolType)
            {
                return ToolType.None;
            }

            return HasTool(preferredToolType) ? preferredToolType : ToolType.None;
        }

        return currentToolType != ToolType.None && HasTool(currentToolType)
            ? currentToolType
            : ToolType.None;
    }

    private InteractionContext CreateInteractionContext(ToolType toolType)
    {
        float damage = stats != null ? stats.AttackPower : 1f;
        Vector3 hitPoint = interactionOrigin != null ? interactionOrigin.position : transform.position;
        return new InteractionContext(context, toolType, damage, hitPoint);
    }

    private bool HasTool(ToolType toolType)
    {
        if (toolType == ToolType.None) return true;
        if (!requireToolInInventory) return true;

        PlayerInventory inventory = context != null ? context.Inventory : null;
        if (inventory == null) return false;

        string itemId = GetToolItemId(toolType);
        string itemName = GetToolItemName(toolType);

        for (int i = 0; i < inventory.Slots.Count; i++)
        {
            InvenItemData item = inventory.Slots[i].Item;
            if (item == null) continue;

            if (!string.IsNullOrEmpty(itemId) && item.ItemID == itemId) return true;
            if (!string.IsNullOrEmpty(itemName) && item.ItemName == itemName) return true;
        }

        return false;
    }

    private string GetToolItemId(ToolType toolType)
    {
        switch (toolType)
        {
            case ToolType.Sword:
                return swordItemId;
            case ToolType.Axe:
                return axeItemId;
            case ToolType.Pickaxe:
                return pickaxeItemId;
            case ToolType.Shovel:
                return shovelItemId;
            default:
                return string.Empty;
        }
    }

    private string GetToolItemName(ToolType toolType)
    {
        switch (toolType)
        {
            case ToolType.Sword:
                return "칼";
            case ToolType.Axe:
                return "도끼";
            case ToolType.Pickaxe:
                return "곡괭이";
            case ToolType.Shovel:
                return "삽";
            default:
                return string.Empty;
        }
    }
}
