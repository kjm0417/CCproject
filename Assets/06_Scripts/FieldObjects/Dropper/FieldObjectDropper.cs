using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.Image;

/// <summary>
/// 자원 드랍 공통
/// </summary>
public abstract class FieldObjectDropper : MonoBehaviour
{
    [Header("드랍 데이터 / HP 0일 때")]
    [SerializeField]
    protected DropTableData deathDropTable;
    [SerializeField]
    protected PickupItem deathPickupPrefab;

    [Header("드랍 테스트 값")]
    [SerializeField]
    private float scatterRadius = 0.35f;


    protected void CreatePickUpItem(PickupItem item, List<ItemDrop> drops, Vector3 origin)
    {

        for (int i = 0; i < drops.Count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * scatterRadius;
            PickupItem pickup = Instantiate(item, origin + (Vector3)offset, Quaternion.identity);
            pickup.Initialize(drops[i].Item, drops[i].Count);
        }
    }

    public void DropOnDeath(int dropGroupId, Vector3 origin)
    {
        if (dropGroupId <= 0 || deathDropTable == null || deathPickupPrefab == null) return;

        List<ItemDrop> drops = deathDropTable.Roll(dropGroupId);

        CreatePickUpItem(deathPickupPrefab, drops, origin);
    }

}
