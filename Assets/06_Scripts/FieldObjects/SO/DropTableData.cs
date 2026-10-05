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

    /// <summary>
    /// 같은 그룹 행 중 하나만 고르는 배타 드랍. DropRate를 가중치로 사용.
    /// 합이 100 미만이면 나머지 확률은 드랍 없음, 100 이상이면 반드시 하나 드랍.
    /// (Roll은 행마다 따로 굴리므로 둘 다/둘 다 안 나올 수 있음)
    /// </summary>
    public List<ItemDrop> RollOne(int dropGroupId)
    {
        List<ItemDrop> drops = new List<ItemDrop>();
        List<DropTableEntry> candidates = new List<DropTableEntry>();
        float totalRate = 0f;

        for (int i = 0; i < entries.Count; i++)
        {
            DropTableEntry entry = entries[i];
            if (entry.DropGroupID != dropGroupId || entry.DropRate <= 0f) continue;
            if (entry.ResolveItem(itemDatabase) == null) continue;

            candidates.Add(entry);
            totalRate += entry.DropRate;
        }

        if (candidates.Count == 0) return drops;

        float roll = Random.Range(0f, Mathf.Max(100f, totalRate));
        float cumulative = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            cumulative += candidates[i].DropRate;
            if (roll >= cumulative) continue;

            int count = Random.Range(candidates[i].MinCount, candidates[i].MaxCount + 1);
            if (count > 0)
            {
                drops.Add(new ItemDrop(candidates[i].ResolveItem(itemDatabase), count));
            }
            break;
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
