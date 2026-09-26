using UnityEngine;

/// <summary>
/// 던전 시스템 -> 인벤토리 접근 창구
/// </summary>
public static class DungeonInventoryBridge
{
    /// <summary>
    /// 보유 개수 조회
    /// </summary>
    public static int GetCount(PlayerInventory inventory, InvenItemData item)
    {
        if (inventory == null || item == null) return 0;

        //TODO KJ - 인벤토리 병합 후 inventory.GetCount(item) 로 교체 ( 현재 GetCount는 ItemData 타입을 받음 )
        int total = 0;
        foreach (InventorySlot slot in inventory.Slots)
        {
            if (slot.Item == item) total += slot.Count;
        }
        return total;
    }

    public static bool HasEnough(PlayerInventory inventory, InvenItemData item, int count)
    {
        return GetCount(inventory, item) >= count;
    }

    /// <summary>
    /// 아이템 지급. 못 넣은 개수 반환
    /// </summary>
    public static int Add(PlayerInventory inventory, InvenItemData item, int count)
    {
        if (inventory == null || item == null) return count;
        return inventory.Add(item, count);
    }

    /// <summary>
    /// 아이템 소모. 실제 소모한 개수 반환
    /// </summary>
    public static int Remove(PlayerInventory inventory, InvenItemData item, int count)
    {
        if (inventory == null || item == null || count <= 0) return 0;

        //TODO KJ - 인벤토리 병합 후 inventory.Remove(item, count) 로 교체
        //현재 PlayerInventory.Remove는 ItemData 타입을 받아 InvenItemData 소모 불가 -> 임시로 소모하지 않음
        Debug.LogWarning($"[Dungeon] 인벤토리 소모 API 미연결 - {item.ItemName} {count}개 소모 생략");
        return 0;
    }
}
