using System;
using UnityEngine;

/// <summary>
/// 던전 3층 씨앗 타일 - 정해진 씨앗만 심을 수 있음
/// 근처에서 퀵슬롯 씨앗 선택 -> 버튼 클릭 ( TryPlant ) / 일반 상호작용 ( Interact ) 둘 다 가능
/// </summary>
public class DungeonSeedTile : DungeonGimmickObject
{
    [SerializeField, Tooltip("이 타일에 심을 수 있는 씨앗")]
    private InvenItemData requiredSeed;
    [SerializeField, Tooltip("심었을 때 켜질 오브젝트 ( 작물 스프라이트 등 )")]
    private GameObject plantedObject;

    private Collider2D interactCollider; //본체 콜라이더 ( 심으면 OFF -> 상호작용 버튼 숨김 )

    public InvenItemData RequiredSeed => requiredSeed;
    public bool IsPlanted { get; private set; }

    /// <summary>
    /// 씨앗 심었을 때
    /// </summary>
    public event Action<DungeonSeedTile> OnPlanted;

    private void Awake()
    {
        interactCollider = GetComponent<Collider2D>();
    }

    public override bool CanInteract(InteractionContext context)
    {
        return !IsPlanted && requiredSeed != null;
    }

    public override void Interact(InteractionContext context)
    {
        if (!CanInteract(context)) return;

        TryPlant(requiredSeed, context.Player != null ? context.Player.Inventory : null);
    }

    /// <summary>
    /// 씨앗 1개 소모하고 심기 - 이 타일에 맞는 씨앗만 가능
    /// </summary>
    public bool TryPlant(InvenItemData seed, PlayerInventory inventory)
    {
        if (IsPlanted || requiredSeed == null || seed == null || inventory == null) return false;

        // 같은 SO가 아니어도 ID가 같으면 같은 씨앗
        if (seed.ItemID != requiredSeed.ItemID)
        {
            Debug.Log($"[DungeonSeedTile] {name} - {seed.ItemName} 심을 수 없음 ( 필요 : {requiredSeed.ItemName} )");
            return false;
        }

        if (inventory.Remove(seed, 1) <= 0)
        {
            Debug.Log($"[DungeonSeedTile] {name} - {seed.ItemName} 없음");
            return false;
        }

        IsPlanted = true;
        SetPlanted(true);
        Debug.Log($"[DungeonSeedTile] {name} 심기 완료 : {seed.ItemName}");

        OnPlanted?.Invoke(this);
        return true;
    }

    /// <summary>
    /// 층 진입 시 초기화
    /// </summary>
    public void ResetState()
    {
        IsPlanted = false;
        SetPlanted(false);
    }

    private void SetPlanted(bool planted)
    {
        if (plantedObject != null)
        {
            plantedObject.SetActive(planted);
        }

        if (interactCollider != null)
        {
            interactCollider.enabled = !planted;
        }
    }
}
