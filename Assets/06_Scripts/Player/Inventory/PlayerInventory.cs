using System;
using System.Collections.Generic;
using UnityEngine;

// 플레이어 아이템 보유 (문서 3.1). "어떤 아이템 몇 개"를 슬롯 단위로 관리한다.
// 스택 가능 아이템은 ItemData.MaxStack 까지 한 칸에 모으고, 넘치면 새 칸.
// 인벤토리 UI는 Slots를 그리고 OnInventoryChanged로 갱신, 제작은 GetCount/HasEnough로 조회한다.
public class PlayerInventory : MonoBehaviour, IPlayerComponent
{
    public const int SlotLimit = 100;

    [Header("슬롯 상한 (최대 100칸)")]
    [SerializeField, Range(1, SlotLimit)]
    private int maxSlots = SlotLimit;
    [SerializeField]
    private ItemDatabase itemDatabase;

    // 보유 변경 시 방송 (인벤토리 UI 갱신용)
    public event Action OnInventoryChanged;

    private readonly List<InventorySlot> slots = new List<InventorySlot>();
    private readonly List<InvenItemData> runtimeLoadedItems = new List<InvenItemData>();

    // UI가 읽을 수 있게 슬롯 목록을 읽기 전용으로 노출
    public IReadOnlyList<InventorySlot> Slots => slots;
    // 기존 프리팹/씬의 0(무제한) 설정도 현재 최대 용량으로 처리한다.
    public int MaxSlots => maxSlots <= 0 ? SlotLimit : Mathf.Min(maxSlots, SlotLimit);

    public void Initialize(PlayerContext context)
    {
        // 시작 시 비어 있음. 세이브가 생기면 여기서 불러오면 된다.
    }

    // 아이템 획득. 넣지 못하고 남은 개수를 반환한다(0이면 전부 들어감 - 문서 3.1 꽉 참 처리).
    public int Add(InvenItemData item, int count)
    {
        return AddInternal(item, count, true);
    }

    private int AddInternal(InvenItemData item, int count, bool notify)
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
        while (remaining > 0 && slots.Count < MaxSlots)
        {
            int put = Mathf.Min(item.MaxStack, remaining);
            slots.Add(new InventorySlot(item, put));
            remaining -= put;
        }

        if (notify && remaining != count) OnInventoryChanged?.Invoke();
        return remaining; // 못 넣은 수량 (필드에 유지할 몫)
    }

    public List<InventoryItemSaveData> CreateSaveData()
    {
        List<InventoryItemSaveData> data = new List<InventoryItemSaveData>();
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];
            if (slot.Item == null || slot.Count <= 0) continue;
            data.Add(InventoryItemSaveData.FromSlot(slot));
        }
        return data;
    }

    public void Load(List<InventoryItemSaveData> savedItems)
    {
        ClearRuntimeLoadedItems();
        slots.Clear();

        if (savedItems != null)
        {
            for (int i = 0; i < savedItems.Count; i++)
            {
                InventoryItemSaveData savedItem = savedItems[i];
                if (savedItem == null || savedItem.Count <= 0) continue;

                InvenItemData item = ResolveSavedItem(savedItem);
                if (item == null) continue;

                AddInternal(item, savedItem.Count, false);
            }
        }

        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// 저장 데이터 -> 아이템 SO. ItemDatabase 우선, 없으면 런타임 아이템 생성 (같은 ID는 같은 인스턴스 재사용).
    /// 상자 등 다른 보관함도 이걸로 찾아야 인벤토리와 같은 아이템으로 취급된다.
    /// </summary>
    public InvenItemData ResolveSavedItem(InventoryItemSaveData savedItem)
    {
        if (savedItem == null || string.IsNullOrEmpty(savedItem.ItemID)) return null;

        InvenItemData item = itemDatabase != null ? itemDatabase.FindById(savedItem.ItemID) : null;
        if (item != null) return item;

        for (int i = 0; i < runtimeLoadedItems.Count; i++)
        {
            if (runtimeLoadedItems[i] != null && runtimeLoadedItems[i].ItemID == savedItem.ItemID)
                return runtimeLoadedItems[i];
        }

        item = CreateRuntimeItem(savedItem);
        runtimeLoadedItems.Add(item);
        return item;
    }

    private static InvenItemData CreateRuntimeItem(InventoryItemSaveData data)
    {
        InvenItemData item = ScriptableObject.CreateInstance<InvenItemData>();
        item.name = $"Loaded_{data.ItemID}_{data.ItemName}";
        item.hideFlags = HideFlags.DontSave;
        item.ItemID = data.ItemID;
        item.ItemName = data.ItemName;
        item.ItemType = data.ItemType;
        item.SubType = data.SubType;
        item.Description = data.Description;
        item.MaxStack = Mathf.Max(1, data.MaxStack);
        item.SellPrice = data.SellPrice;
        item.MinBuyGold = data.MinBuyGold;
        item.MaxBuyGold = data.MaxBuyGold;
        item.BuyDiamond = data.BuyDiamond;
        item.HungerRecovery = data.HungerRecovery;
        return item;
    }

    private void ClearRuntimeLoadedItems()
    {
        for (int i = 0; i < runtimeLoadedItems.Count; i++)
        {
            if (runtimeLoadedItems[i] != null) Destroy(runtimeLoadedItems[i]);
        }
        runtimeLoadedItems.Clear();
    }

    private void OnDestroy()
    {
        ClearRuntimeLoadedItems();
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

    public int Remove(InvenItemData item, int count)
    {
        if (item == null || count <= 0) return 0;
        int removed = 0;

        for (int i = slots.Count - 1; i >= 0 && removed < count; i--)
        {
            if (slots[i].Item != item) continue;
            int take = Mathf.Min(slots[i].Count, count - removed);
            slots[i].Count -= take;
            removed += take;
            if (slots[i].Count <= 0) slots.RemoveAt(i);
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

    // 저장 복원으로 생성된 아이템도 같은 ID면 같은 재료로 취급한다.
    public int GetCount(InvenItemData item)
    {
        if (item == null) return 0;
        int total = 0;
        foreach (InventorySlot slot in slots)
            if (SameItem(slot.Item, item)) total += slot.Count;
        return total;
    }

    public bool CanCook(RecipeData recipe, out string reason) => BuildCookingResult(recipe, out _, out reason);

    // 복사본에서 재료 차감과 결과 공간을 모두 확인한 뒤 한 번에 반영한다.
    public bool TryCook(RecipeData recipe, out string reason)
    {
        if (!BuildCookingResult(recipe, out List<InventorySlot> result, out reason)) return false;
        slots.Clear();
        slots.AddRange(result);
        OnInventoryChanged?.Invoke();
        return true;
    }

    private bool BuildCookingResult(RecipeData recipe, out List<InventorySlot> result, out string reason)
    {
        result = new List<InventorySlot>();
        reason = string.Empty;
        if (recipe == null || recipe.ResultItem == null || recipe.ResultCount <= 0
            || recipe.ResultItem.MaxStack <= 0 || recipe.Ingredients == null || recipe.Ingredients.Count == 0)
        {
            reason = "레시피 데이터를 확인해 주세요.";
            return false;
        }
        foreach (InventorySlot slot in slots) result.Add(new InventorySlot(slot.Item, slot.Count));
        foreach (RecipeIngredient ingredient in recipe.Ingredients)
        {
            if (ingredient == null || ingredient.Item == null || ingredient.Count <= 0)
            {
                reason = "재료 데이터를 확인해 주세요.";
                return false;
            }
            int remaining = ingredient.Count;
            for (int i = result.Count - 1; i >= 0 && remaining > 0; i--)
            {
                if (!SameItem(result[i].Item, ingredient.Item)) continue;
                int take = Mathf.Min(result[i].Count, remaining);
                result[i].Count -= take;
                remaining -= take;
                if (result[i].Count == 0) result.RemoveAt(i);
            }
            if (remaining > 0)
            {
                reason = $"{ingredient.Item.ItemName} 재료가 부족합니다.";
                return false;
            }
        }
        int toAdd = recipe.ResultCount;
        foreach (InventorySlot slot in result)
        {
            if (!SameItem(slot.Item, recipe.ResultItem)) continue;
            int add = Mathf.Min(Mathf.Max(0, slot.SpaceLeft), toAdd);
            slot.Count += add;
            toAdd -= add;
        }
        while (toAdd > 0 && result.Count < MaxSlots)
        {
            int add = Mathf.Min(recipe.ResultItem.MaxStack, toAdd);
            result.Add(new InventorySlot(recipe.ResultItem, add));
            toAdd -= add;
        }
        if (toAdd > 0)
        {
            reason = "완성된 음식을 담을 공간이 부족합니다.";
            return false;
        }
        return true;
    }

    private static bool SameItem(InvenItemData left, InvenItemData right)
    {
        return left != null && right != null && (left == right
            || (!string.IsNullOrEmpty(left.ItemID) && left.ItemID == right.ItemID));
    }
}
