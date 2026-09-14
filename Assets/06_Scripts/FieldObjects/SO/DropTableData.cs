using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DropTableData", menuName = "Game/DropTableData")]
public class DropTableData : ScriptableObject
{
    #region Json 데이터 매핑 용도 
    public string DropGroupID;
    public string ItemID;
    public int DropRate;
    public int MinCount;
    public int MaxCount;
    #endregion

    [Header("데이터 연결")]
    [Tooltip("ItemData 에셋들이 준비된 뒤 연결합니다.")]
    [SerializeField]
    private ItemDatabase itemDatabase;

    [SerializeField]
    private List<DropTableEntry> entries = new List<DropTableEntry>();

    public void AddEntry(DropTableEntry entry)
    {
        entries.Add(entry);
    }

    public List<ItemDrop> Roll(int dropGroupId)
    {
        List<ItemDrop> drops = new List<ItemDrop>();

        for (int i = 0; i < entries.Count; i++)
        {
            DropTableEntry entry = entries[i];
            if (entry.DropGroupID != dropGroupId) continue;

            InvenItemData item = entry.ResolveItem(itemDatabase);
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

    public InvenItemData ResolveItem(ItemDatabase itemDatabase)
    {
        //if (Item != null) return Item;

        return itemDatabase != null ? itemDatabase.FindById(ItemID) : null;
    }
}

public readonly struct ItemDrop
{
    public ItemDrop(InvenItemData item, int count)
    {
        Item = item;
        Count = count;
    }

    public InvenItemData Item { get; }
    public int Count { get; }
}
