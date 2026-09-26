using UnityEngine;

public class PlayerInteraction : MonoBehaviour, IPlayerComponent
{
    [Header("상호작용 대상 찾기")]
    [SerializeField]
    private Transform interactionOrigin;

    [Tooltip("나중에 필드 오브젝트 레이어로 설정하면 됩니다. 지금은 테스트용으로 Everything 상태여도 e됨.")]
    [SerializeField]
    private LayerMask interactableLayers = ~0;

    [Header("타일 상호작용")]
    [Tooltip("삽질 등 타일 상호작용 기준 위치(발밑). 비워두면 플레이어 위치 사용.")]
    [SerializeField]
    private Transform tileCheckOrigin;

    [Header("임시 도구 테스트")]
    [Tooltip("임시 테스트 값입니다. 나중에는 장착한 도구 데이터에서 자동으로 설정되게 하면 됨.")]
    [SerializeField]
    private ToolType currentToolType = ToolType.None;

    [Tooltip("켜두면 장비 판정이 없을 때 오브젝트가 원하는 도구를 들고 있다고 가정.")]
    [SerializeField]
    private bool assumeCorrectToolInRange = true;

    private PlayerContext context;
    private PlayerStats stats;

    public void Initialize(PlayerContext playerContext)
    {
        context = playerContext;
        stats = playerContext.Stats;

        if (interactionOrigin == null)
        {
            interactionOrigin = transform;
        }

        if (tileCheckOrigin == null)
        {
            tileCheckOrigin = transform;
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
        // 삽을 들고 있으면 발밑 타일 먼저 시도, 실패하면 오브젝트 상호작용
        if (currentToolType == ToolType.Shovel && TryInteractTile(currentToolType)) return true;

        IInteractable target = FindNearestInteractable();
        if (target == null) return false;

        ToolType toolType = ResolveToolType(target);
        InteractionContext interactionContext = CreateInteractionContext(toolType);
        if (!target.CanInteract(interactionContext)) return false;

        context.Animation?.PlayToolInteraction(toolType);
        target.Interact(interactionContext);
        return true;
    }

    private bool TryInteractTile(ToolType toolType)
    {
        GroundTileModifier tileModifier = TerritoryManager.Instance != null ? TerritoryManager.Instance.TileModifier : null;
        if (tileModifier == null) return false;
        if (!tileModifier.TryConvertTile(tileCheckOrigin.position, toolType)) return false;

        context.Animation?.PlayToolInteraction(toolType);
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
        if (currentToolType != ToolType.None) return currentToolType;
        if (!assumeCorrectToolInRange) return ToolType.None;

        IToolInteractionTarget toolTarget = target as IToolInteractionTarget;
        return toolTarget != null ? toolTarget.PreferredToolType : ToolType.None;
    }

    private InteractionContext CreateInteractionContext(ToolType toolType)
    {
        float damage = stats != null ? stats.AttackPower : 1f;
        Vector3 hitPoint = interactionOrigin != null ? interactionOrigin.position : transform.position;
        return new InteractionContext(context, toolType, damage, hitPoint);
    }
}
