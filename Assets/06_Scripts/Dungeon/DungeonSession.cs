using System.Collections.Generic;

/// <summary>
/// 씬 이동 사이에 유지되는 현재 던전 입장 정보
/// 영지 씬(입장) -> 던전 씬(진행/결과) 으로 데이터 전달용
/// </summary>
public static class DungeonSession
{
    /// <summary>
    /// 현재 입장한 던전 ( 입장 안 했으면 null )
    /// </summary>
    public static DungeonData CurrentDungeon { get; private set; }

    /// <summary>
    /// 입장 시 실제로 소모한 아이템 ( 실패 시 환급 계산용 )
    /// </summary>
    public static IReadOnlyList<DungeonItemAmount> ConsumedItems => consumedItems;
    private static readonly List<DungeonItemAmount> consumedItems = new List<DungeonItemAmount>();

    public static bool IsInDungeon => CurrentDungeon != null;

    /// <summary>
    /// 입장 가능 여부 ( 입장 아이템 보유 체크 )
    /// </summary>
    public static bool CanEnter(DungeonData dungeon, PlayerInventory inventory)
    {
        if (dungeon == null) return false;

        foreach (DungeonItemAmount cost in dungeon.EntryCosts)
        {
            if (cost.Item == null || cost.Count <= 0) continue;
            if (inventory == null || !DungeonInventoryBridge.HasEnough(inventory, cost.Item, cost.Count)) return false;
        }
        return true;
    }

    /// <summary>
    /// 던전 입장 - 입장 아이템 소모 후 세션 시작. 씬 로드는 호출하는 쪽에서 처리
    /// </summary>
    public static bool TryEnter(DungeonData dungeon, PlayerInventory inventory)
    {
        if (!CanEnter(dungeon, inventory)) return false;

        consumedItems.Clear();
        foreach (DungeonItemAmount cost in dungeon.EntryCosts)
        {
            if (cost.Item == null || cost.Count <= 0) continue;

            int removed = DungeonInventoryBridge.Remove(inventory, cost.Item, cost.Count);
            consumedItems.Add
            (
                new DungeonItemAmount 
                { 
                      Item = cost.Item, 
                      Count = removed 
                }
            );
        }

        CurrentDungeon = dungeon;
        return true;
    }

    /// <summary>
    /// 입장 절차 없이 던전 씬을 바로 실행한 경우(테스트) 세션만 설정
    /// </summary>
    public static void BeginWithoutEntry(DungeonData dungeon)
    {
        consumedItems.Clear();
        CurrentDungeon = dungeon;
    }

    /// <summary>
    /// 던전 종료 ( 클리어/실패 결과 처리 후 )
    /// </summary>
    public static void End()
    {
        consumedItems.Clear();
        CurrentDungeon = null;
    }
}
