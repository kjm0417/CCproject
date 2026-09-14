using System.Collections.Generic;
using UnityEngine;

public class FieldObjectDropper : MonoBehaviour
{
    [Header("데이터 연결")]
    [SerializeField]
    private DropTableData dropTable;

    [Tooltip("필드에 떨어질 아이템 픽업 프리팹이 준비되면 연결합니다.")]
    [SerializeField]
    private PickupItem pickupPrefab;

    [Header("드랍 테스트 값")]
    [SerializeField]
    private float scatterRadius = 0.35f;

    public void Drop(int dropGroupId, Vector3 origin)
    {
        if (dropGroupId <= 0 || dropTable == null || pickupPrefab == null) return;

        List<ItemDrop> drops = dropTable.Roll(dropGroupId);
        for (int i = 0; i < drops.Count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * scatterRadius;
            PickupItem pickup = Instantiate(pickupPrefab, origin + (Vector3)offset, Quaternion.identity);
            pickup.Initialize(drops[i].Item, drops[i].Count);
        }
    }
}
