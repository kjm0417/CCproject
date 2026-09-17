using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DropGroupMapping
{
   // public int dropGroupId;
    public DropTableData dropTable;
    public PickupItem prefab;
}

/// <summary>
/// 복합 자원 오브젝트 - 피격 시 드랍되는 오브젝트가 존재
/// </summary>
public class ComplexFieldObjectDropper : FieldObjectDropper
{
    [Header("드랍 데이터 / 피격 시")]

    [SerializeField]
    private List<DropGroupMapping> dropMappings;
    private Dictionary<int, DropGroupMapping> dropDict;

    void Awake()
    {
        dropDict = new();
        foreach (DropGroupMapping dropMapping in dropMappings)
            dropDict[int.Parse(dropMapping.dropTable.DropGroupID)] = dropMapping;
    }
    public void DropOnHit(int dropGroupId, Vector3 origin)
    {
        if (!dropDict.TryGetValue(dropGroupId, out var mapping)) return;

        List<ItemDrop> drops = mapping.dropTable.Roll(dropGroupId);
        CreatePickUpItem(mapping.prefab, drops, origin);
    }
}
