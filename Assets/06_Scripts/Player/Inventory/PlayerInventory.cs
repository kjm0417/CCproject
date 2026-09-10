using System;
using System.Collections.Generic;
using UnityEngine;

// 플레이어 아이템 보유 (문서 3.1). "어떤 아이템 몇 개"를 슬롯 단위로 관리한다.
// 스택 가능 아이템은 ItemData.MaxStack 까지 한 칸에 모으고, 넘치면 새 칸.
// 인벤토리 UI는 Slots를 그리고 OnInventoryChanged로 갱신, 제작은 GetCount/HasEnough로 조회한다.
public class PlayerInventory : MonoBehaviour, IPlayerComponent
{
    [Header("슬롯 상한 (0 = 무제한)")]
    [SerializeField]
    private int maxSlots = 0;

    // 보유 변경 시 방송 (인벤토리 UI 갱신용)
    public event Action OnInventoryChanged;

    private readonly List<InventorySlot> slots = new List<InventorySlot>();

    // UI가 읽을 수 있게 슬롯 목록을 읽기 전용으로 노출
    public IReadOnlyList<InventorySlot> Slots => slots;

    public void Initialize(PlayerContext context)
    {
        // 시작 시 비어 있음. 세이브가 생기면 여기서 불러오면 된다.
    }

    // 아이템 획득. 넣지 못하고 남은 개수를 반환한다(0이면 전부 들어감 - 문서 3.1 꽉 참 처리).
    public int Add(ItemData item, int count)
    {
        if (item == null || count <= 0) return count;
        int remaining = count;

        // 1) 스택 가능하면 같은 아이템의 기존 칸부터 채운다.
        if (item.MaxStack > 1)
        {
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                if (slots[i].Item == item && slots[i].SpaceLeft > 0)
                {
                    int put = Mathf.Min(slots[i].SpaceLeft, remaining);
                    slots[i].Count += put;
                    remaining -= put;
                }
            }
        }

        // 2) 남은 건 새 칸에 (슬롯 상한이 있으면 그만큼만).
        while (remaining > 0 && (maxSlots <= 0 || slots.Count < maxSlots))
        {
            int put = Mathf.Min(item.MaxStack, remaining);
            slots.Add(new InventorySlot(item, put));
            remaining -= put;
        }

        if (remaining != count) OnInventoryChanged?.Invoke();
        return remaining; // 못 넣은 수량 (필드에 유지할 몫)
    }

    // 아이템 제거(소비/버리기). 실제 제거한 개수 반환.
    public int Remove(ItemData item, int count)
    {
        if (item == null || count <= 0) return 0;
        int removed = 0;

        for (int i = slots.Count - 1; i >= 0 && removed < count; i--)
        {
            if (slots[i].Item != item) continue;
            int take = Mathf.Min(slots[i].Count, count - removed);
            slots[i].Count -= take;
            removed += take;
            if (slots[i].Count <= 0) slots.RemoveAt(i); // 빈 칸 정리
        }

        if (removed > 0) OnInventoryChanged?.Invoke();
        return removed;
    }

    // 이 아이템 총 몇 개? (여러 칸 합산) - 제작/UI용
    public int GetCount(ItemData item)
    {
        if (item == null) return 0;
        int total = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].Item == item) total += slots[i].Count;
        }
        return total;
    }

    // 제작 재료가 충분한지 (문서 3.3)
    public bool HasEnough(ItemData item, int count)
    {
        return GetCount(item) >= count;
    }
}
