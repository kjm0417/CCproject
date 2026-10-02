using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 던전 2층 나무 뿌리 - 곡괭이로 requiredHits 횟수만큼 상호작용 시 비활성
/// 겹쳐진 뿌리는 가장 위에 그려진 뿌리부터 제거 가능 ( 아래 깔린 뿌리는 콜라이더 OFF -> 감지 X )
/// 곡괭이 자동 변경은 PreferredToolType ( IToolInteractionTarget ) 으로 기존 PlayerInteraction 에서 처리
/// </summary>
public class DungeonTreeRoot : DungeonGimmickObject, IToolInteractionTarget
{
    [SerializeField, Tooltip("곡괭이 상호작용 필요 횟수")]
    private int requiredHits = 3;

    private int hitCount;

    public ToolType PreferredToolType => ToolType.Pickaxe; //감지 시 곡괭이 자동 변경

    private static readonly List<DungeonTreeRoot> activeRoots = new List<DungeonTreeRoot>(); //현재 활성화된 뿌리

    private Collider2D interactCollider; //본체 콜라이더 ( 위에 다른 뿌리가 덮고 있으면 OFF )
    private SpriteRenderer spriteRenderer; //겹침 / 위아래 판정용

    private void Awake()
    {
        interactCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        activeRoots.Add(this);
        RefreshAll();
    }

    private void OnDisable()
    {
        activeRoots.Remove(this);
        RefreshAll();
    }

    public override bool CanInteract(InteractionContext context)
    {
        return hitCount < requiredHits;
    }

    /// <summary>
    /// 곡괭이일 때만 1회 카운트 - 필요 횟수 도달 시 비활성
    /// </summary>
    public override void Interact(InteractionContext context)
    {
        if (!CanInteract(context) || context.ToolType != ToolType.Pickaxe) return;

        hitCount++;
        if (hitCount >= requiredHits)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 뿌리 활성 / 비활성 시 전체 갱신 - 덮인 뿌리는 감지 X
    /// </summary>
    private static void RefreshAll()
    {
        foreach (DungeonTreeRoot root in activeRoots)
        {
            if (root.interactCollider != null)
            {
                root.interactCollider.enabled = !root.IsCovered();
            }
        }
    }

    private bool IsCovered()
    {
        foreach (DungeonTreeRoot other in activeRoots)
        {
            if (other == this) continue;

            if (other.IsDrawnAbove(this) && other.Overlaps(this))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 정렬 기준 ( Sorting Layer -> Order in Layer ) 으로 위에 그려지는지
    /// </summary>
    private bool IsDrawnAbove(DungeonTreeRoot other)
    {
        if (spriteRenderer == null || other.spriteRenderer == null) return false;

        int layer = SortingLayer.GetLayerValueFromID(spriteRenderer.sortingLayerID);
        int otherLayer = SortingLayer.GetLayerValueFromID(other.spriteRenderer.sortingLayerID);
        if (layer != otherLayer) return layer > otherLayer;

        return spriteRenderer.sortingOrder > other.spriteRenderer.sortingOrder;
    }

    private bool Overlaps(DungeonTreeRoot other)
    {
        if (spriteRenderer == null || other.spriteRenderer == null) return false;

        return spriteRenderer.bounds.Intersects(other.spriteRenderer.bounds);
    }
}
