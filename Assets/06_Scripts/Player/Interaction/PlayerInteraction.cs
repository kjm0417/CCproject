using System;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour, IPlayerComponent
{
    [Header("상호작용 대상 찾기")]
    [SerializeField]
    private Transform interactionOrigin;

    [Tooltip("나중에 필드 오브젝트 레이어로 설정하면 됩니다. 지금은 테스트용으로 Everything 상태여도 됩니다.")]
    [SerializeField]
    private LayerMask interactableLayers = ~0;


    [Header("타겟 감지")]
    [Tooltip("타겟 탐색 주기(초). 0이면 매 프레임.")]
    [SerializeField]
    private float scanInterval = 0.15f;

    [Header("타일 상호작용")]
    [Tooltip("삽질 등 타일 상호작용 기준 위치(발밑). 비워두면 플레이어 위치 사용.")]
    [SerializeField]
    private Transform tileCheckOrigin;


    [Header("타겟 강조")]
    [Tooltip("도끼 / 곡괭이로 캘 대상의 알파 배율 (1이면 변화 없음)")]
    [SerializeField, Range(0f, 1f)]
    private float targetFadeAlpha = 0.5f;

    [Header("임시 도구 테스트")]
    [Tooltip("임시 테스트 값입니다. 나중에는 장착한 도구 데이터에서 자동으로 설정되게 하면 됨.")]
    [SerializeField]
    private ToolType currentToolType = ToolType.None;
    private bool isCurrentToolLocked;

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
    public ToolType CurrentToolType => currentToolType;
    public bool IsCurrentToolLocked => isCurrentToolLocked;

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

        if (tileCheckOrigin == null)
        {
            tileCheckOrigin = transform;
        }

        OnTargetChanged -= HandleTargetFade;
        OnTargetChanged += HandleTargetFade;
    }

    private void Update()
    {
        if (!context) return;

        UpdateTilePreview();

        scanTimer -= Time.deltaTime;
        if (scanTimer > 0f) return;
        scanTimer = scanInterval;

        IInteractable newTarget = FindNearestInteractable();
        ToolType newToolType = ResolveActiveToolType(newTarget);

        if (newTarget != CurrentTarget || newToolType != CurrentTargetToolType)
        {
            CurrentTarget = newTarget;
            CurrentTargetToolType = newToolType;
            OnTargetChanged?.Invoke(CurrentTarget, CurrentTargetToolType);
        }
    }

    public void SetCurrentTool(ToolType toolType)
    {
        SetCurrentTool(toolType, false);
    }

    public void SetCurrentTool(ToolType toolType, bool isLocked)
    {
        if (currentToolType == toolType && isCurrentToolLocked == isLocked) return;

        currentToolType = toolType;
        isCurrentToolLocked = isLocked;
        RefreshCurrentTargetTool();
    }

    private void RefreshCurrentTargetTool()
    {
        ToolType resolvedToolType = CurrentTarget != null
            ? ResolveToolType(CurrentTarget)
            : ResolveActiveToolType(null);
        if (resolvedToolType == CurrentTargetToolType) return;

        CurrentTargetToolType = resolvedToolType;
        OnTargetChanged?.Invoke(CurrentTarget, CurrentTargetToolType);
    }

    public void Interact()
    {
        TryInteract();
    }

    public bool TryInteract()
    {
        // 락 없이 도끼/곡괭이 대상이 있으면 오브젝트 먼저, 실패하면 삽질
        if (ShouldPrioritizeTarget())
        {
            if (TryInteractTarget()) return true;
            return currentToolType == ToolType.Shovel && TryInteractTile(currentToolType);
        }

        // 삽을 들고 있으면 발밑 타일 먼저 시도, 실패하면 오브젝트 상호작용
        if (currentToolType == ToolType.Shovel && TryInteractTile(currentToolType)) return true;

        return TryInteractTarget();
    }

    /// <summary>
    /// 락 없는 상태에서 도끼/곡괭이를 쓸 수 있는 대상인지 - 삽질보다 우선
    /// </summary>
    private bool ShouldPrioritizeTarget()
    {
        if (isCurrentToolLocked || CurrentTarget == null) return false;

        ToolType toolType = ResolveToolType(CurrentTarget);
        return toolType == ToolType.Axe || toolType == ToolType.Pickaxe;
    }

    private bool TryInteractTarget()
    {
        if (CurrentTarget == null) return false;

        IInteractable target = FindNearestInteractable();
        if (target == null) return false;

        ToolType toolType = ResolveToolType(CurrentTarget);
        if (isCurrentToolLocked && toolType == ToolType.None) return false;

        InteractionContext interactionContext = CreateInteractionContext(toolType);
        if (!CurrentTarget.CanInteract(interactionContext)) return false;

        context.Animation?.PlayToolInteraction(toolType);
        CurrentTarget.Interact(interactionContext);

        // 삽 효과음은 흙 타일 변경 ( TryInteractTile ) 에서만 - 오브젝트 상호작용 ( 발판 등 ) 은 무음
        if (toolType != ToolType.Shovel) PlayToolHitSFX(toolType);
        return true;
    }

    /// <summary>
    /// 도구로 오브젝트 타격 시 효과음
    /// </summary>
    private void PlayToolHitSFX(ToolType toolType)
    {
        if (SoundManager.Instance == null) return;

        switch (toolType)
        {
            case ToolType.Axe:
                SoundManager.Instance.PlaySFX(SFXType.AxeHit);
                break;
            case ToolType.Pickaxe:
                SoundManager.Instance.PlaySFX(SFXType.PickaxeHit);
                break;
            case ToolType.Shovel:
                SoundManager.Instance.PlaySFX(SFXType.ShovelHit);
                break;
        }
    }

    /// <summary>
    /// 삽을 들고 있으면 바뀔 흙 타일 잔상 표시 ( TryInteract 의 삽 조건과 동일 )
    /// </summary>
    private void UpdateTilePreview()
    {
        GroundTileModifier tileModifier = TerritoryManager.Instance != null ? TerritoryManager.Instance.TileModifier : null;
        if (tileModifier == null) return;

        if (currentToolType == ToolType.Shovel)
            tileModifier.UpdatePreview(tileCheckOrigin.position, currentToolType);
        else
            tileModifier.HidePreview();
    }

    private void OnDisable()
    {
        GroundTileModifier tileModifier = TerritoryManager.Instance != null ? TerritoryManager.Instance.TileModifier : null;
        if (tileModifier != null) tileModifier.HidePreview();

        RestoreTargetFade();
    }

    #region 타겟 강조
    private SpriteRenderer[] fadedRenderers;
    private float[] fadedOriginalAlphas;

    /// <summary>
    /// 도끼 / 곡괭이로 캘 대상(나무, 광물)이면 알파를 낮춰 표시
    /// </summary>
    private void HandleTargetFade(IInteractable target, ToolType toolType)
    {
        RestoreTargetFade();

        // 대상이 요구하는 도구가 도끼 / 곡괭이이고 그 도구로 칠 수 있을 때만
        // (요구 도구 없는 오브젝트는 들고 있는 도구가 그대로 넘어오므로 toolType 만으로 판정하면 안 됨)
        IToolInteractionTarget toolTarget = target as IToolInteractionTarget;
        if (toolTarget == null) return;

        ToolType preferredToolType = toolTarget.PreferredToolType;
        if (preferredToolType != ToolType.Axe && preferredToolType != ToolType.Pickaxe) return;
        if (toolType != preferredToolType) return;

        Component targetComponent = target as Component;
        if (targetComponent == null) return;

        fadedRenderers = targetComponent.GetComponentsInChildren<SpriteRenderer>();
        fadedOriginalAlphas = new float[fadedRenderers.Length];

        for (int i = 0; i < fadedRenderers.Length; i++)
        {
            Color color = fadedRenderers[i].color;
            fadedOriginalAlphas[i] = color.a;
            color.a *= targetFadeAlpha;
            fadedRenderers[i].color = color;
        }
    }

    private void RestoreTargetFade()
    {
        if (fadedRenderers == null) return;

        for (int i = 0; i < fadedRenderers.Length; i++)
        {
            // 파괴된 오브젝트는 건너뜀 (풀링으로 비활성된 건 복구)
            if (fadedRenderers[i] == null) continue;

            Color color = fadedRenderers[i].color;
            color.a = fadedOriginalAlphas[i];
            fadedRenderers[i].color = color;
        }

        fadedRenderers = null;
        fadedOriginalAlphas = null;
    }
    #endregion

    private bool TryInteractTile(ToolType toolType)
    {
        GroundTileModifier tileModifier = TerritoryManager.Instance != null ? TerritoryManager.Instance.TileModifier : null;
        if (tileModifier == null) return false;
        if (!tileModifier.TryConvertTile(tileCheckOrigin.position, toolType)) return false;

        context.Animation?.PlayToolInteraction(toolType);
        PlayToolHitSFX(toolType);
        return true;
    }

    /// <summary>
    /// 발밑 파낸 흙 칸에 씨앗 심기
    /// </summary>
    public bool TryPlantSeed(InvenItemData seed)
    {
        // 던전 씨앗 타일 근처면 타일에 심기
        if (CurrentTarget is DungeonSeedTile seedTile)
        {
            return context != null && seedTile.TryPlant(seed, context.Inventory);
        }

        CropPlanter cropPlanter = TerritoryManager.Instance != null ? TerritoryManager.Instance.CropPlanter : null;
        if (cropPlanter == null || context == null) return false;

        return cropPlanter.TryPlant(tileCheckOrigin.position, seed, context.Inventory);
    }

    /// <summary>
    /// 발밑 칸에 설치 아이템(상자 / 작업대 / 모닥불) 배치
    /// </summary>
    public bool TryPlaceStructure(InvenItemData item)
    {
        StructurePlacer placer = TerritoryManager.Instance != null ? TerritoryManager.Instance.StructurePlacer : null;
        if (placer == null || context == null) return false;

        return placer.TryPlace(tileCheckOrigin.position, item, context.Inventory);
    }

    /// <summary>
    /// 씨앗을 든 상태: 앞에 다 자란 작물이 있으면 수확, 없으면 발밑에 심기
    /// (씨앗 선택 시 도구가 None으로 잠겨 TryInteract로는 수확이 막히므로 별도 처리)
    /// </summary>
    public bool TryHarvestOrPlantSeed(InvenItemData seed)
    {
        if (CurrentTarget is PlantedCropObject crop && context != null)
        {
            InteractionContext interactionContext = CreateInteractionContext(ToolType.None);
            if (crop.CanInteract(interactionContext))
            {
                crop.Interact(interactionContext);
                return true;
            }
        }

        return TryPlantSeed(seed);
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

        if (isCurrentToolLocked)
        {
            if (currentToolType == ToolType.None || !HasTool(currentToolType))
            {
                return ToolType.None;
            }

            return preferredToolType == ToolType.None || preferredToolType == currentToolType
                ? currentToolType
                : ToolType.None;
        }

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

    private ToolType ResolveActiveToolType(IInteractable target)
    {
        if (target != null) return ResolveToolType(target);

        if (isCurrentToolLocked)
        {
            return currentToolType == ToolType.Shovel && HasTool(ToolType.Shovel)
                ? ToolType.Shovel
                : ToolType.None;
        }

        return currentToolType == ToolType.Shovel && HasTool(ToolType.Shovel)
            ? ToolType.Shovel
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

            // 강화 도구도 같은 종류로 인정 (21201 -> 21101)
            if (!string.IsNullOrEmpty(itemId) && ToolItemIds.IsSameTool(item.ItemID, itemId)) return true;
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

    [Header("상호작용 범위 표시")]
    [SerializeField] private bool showInteractionRangeGizmo = true;
    [SerializeField] private Color interactionGizmoColor = new Color(0.2f, 0.8f, 1f, 0.8f);
    private void OnDrawGizmos()
    {
        if (!showInteractionRangeGizmo) return;

        // FindNearestInteractable과 같은 기준 ( 중심 interactionOrigin, 반지름 AttackRange )
        Vector3 origin = interactionOrigin != null ? interactionOrigin.position : transform.position;
        float range = 1f;

        if (Application.isPlaying && stats != null)
        {
            range = stats.AttackRange;
        }
        else
        {
            PlayerContext playerContext = GetComponent<PlayerContext>();
            if (playerContext != null && playerContext.BaseData != null)
            {
                range = playerContext.BaseData.AttackRange;
            }
        }

        Color previousColor = Gizmos.color;
        Gizmos.color = interactionGizmoColor;

        const int segmentCount = 48;
        Vector3 previousPoint = origin + new Vector3(range, 0f, 0f);
        for (int i = 1; i <= segmentCount; i++)
        {
            float angle = i * Mathf.PI * 2f / segmentCount;
            Vector3 nextPoint = origin + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * range;
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }

        Gizmos.color = previousColor;
    }
}
