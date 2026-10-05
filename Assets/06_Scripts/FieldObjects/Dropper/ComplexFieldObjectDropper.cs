using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DropGroupMapping
{
   // public int dropGroupId;
    public DropTableData dropTable;
    public PickupItem prefab;

    [Tooltip("켜면 그룹 행 중 하나만 드랍 (DropRate = 가중치). 예: 씨앗 75 / 통나무 25")]
    public bool pickOne;
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

        List<ItemDrop> drops = mapping.pickOne
            ? mapping.dropTable.RollOne(dropGroupId)
            : mapping.dropTable.Roll(dropGroupId);

        #region [이전] 행마다 따로 확률 판정
        // List<ItemDrop> drops = mapping.dropTable.Roll(dropGroupId);
        #endregion

        CreatePickUpItem(mapping.prefab, drops, origin);
    }
}
