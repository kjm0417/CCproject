using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PickupItem : MonoBehaviour
{
    [Header("런타임 아이템")]
    [Tooltip("보통 FieldObjectDropper가 실행 중에 넣어줍니다. 테스트할 때는 직접 넣어도 됩니다.")]
    [SerializeField]
    private InvenItemData item;

    [Tooltip("보통 FieldObjectDropper가 실행 중에 넣어줍니다. 테스트할 때는 직접 넣어도 됩니다.")]
    [SerializeField]
    private int count = 1;

    /// <summary>
    /// 드랍 시 사용된 프리팹 이름 (Resources/DropItem 기준) - 비어있으면 씬 배치 아이템이라 저장 안 함
    /// </summary>
    public string PrefabName { get; set; }
    public int Count => count;
    public InvenItemData Item => item;

    public void Initialize(InvenItemData itemData, int itemCount)
    {
        item = itemData;
        count = Mathf.Max(1, itemCount);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
        if (inventory == null || item == null) return;

        int remaining = inventory.Add(item, count);
        if (remaining <= 0)
        {
            Destroy(gameObject);
            return;
        }

        count = remaining;
    }
}
