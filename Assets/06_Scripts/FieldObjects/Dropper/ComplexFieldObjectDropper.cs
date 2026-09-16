using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 복합 자원 오브젝트 - 피격 시 드랍되는 오브젝트가 존재
/// </summary>
public class ComplexFieldObjectDropper : FieldObjectDropper
{
    [Header("드랍 데이터 / 피격 시")]
    [SerializeField]
    private DropTableData hitDropTable;
    [SerializeField]
    private PickupItem hitPickupPrefab;
    public void DropOnHit(int dropGroupId, Vector3 origin)
    {
        if (dropGroupId <= 0 || hitDropTable == null || hitPickupPrefab == null) return;

        List<ItemDrop> drops = hitDropTable.Roll(dropGroupId);

        CreatePickUpItem(hitPickupPrefab, drops, origin);
    }
}
