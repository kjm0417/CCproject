using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DropTableData", menuName = "Game/DropTableData")]
public class DropTableData : ScriptableObject
{
    [Header("데이터 연결 - 나중에 연결")]
    [Tooltip("ItemData 에셋들이 준비된 뒤 연결합니다. 그 전까지는 드랍 항목에 ItemID만 넣어둬도 됩니다.")]
    [SerializeField]
    private ItemDatabase itemDatabase;

    [Tooltip("나중에 DropTable.json의 값을 여기에 옮깁니다. DropGroupID, ItemID, DropRate, MinCount, MaxCount를 사용합니다.")]
    [SerializeField]
    private List<DropTableEntry> entries = new List<DropTableEntry>();

    public List<ItemDrop> Roll(int dropGroupId)
    {
        List<ItemDrop> drops = new List<ItemDrop>();

        for (int i = 0; i < entries.Count; i++)
        {
            DropTableEntry entry = entries[i];
            if (entry.DropGroupID != dropGroupId) continue;

            ItemData item = entry.ResolveItem(itemDatabase);
            if (item == null) continue;

            if (Random.Range(0f, 100f) <= entry.DropRate)
            {
                int count = Random.Range(entry.MinCount, entry.MaxCount + 1);
                if (count > 0)
                {
                    drops.Add(new ItemDrop(item, count));
                }
            }
        }

        return drops;
    }
}

[System.Serializable]
public class DropTableEntry
{
    public int DropGroupID;
    public int ItemID;
    [Tooltip("선택 직접 연결입니다. ItemDatabase가 ItemID로 찾게 할 거라면 비워둬도 됩니다.")]
    public ItemData Item;
    [Range(0f, 100f)]
    public float DropRate = 100f;
    public int MinCount = 1;
    public int MaxCount = 1;

    public ItemData ResolveItem(ItemDatabase itemDatabase)
    {
        if (Item != null) return Item;
        return itemDatabase != null ? itemDatabase.FindById(ItemID) : null;
    }
}

public readonly struct ItemDrop
{
    public ItemDrop(ItemData item, int count)
    {
        Item = item;
        Count = count;
    }

    public ItemData Item { get; }
    public int Count { get; }
}
