using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PickupItem : MonoBehaviour
{
    [Header("런타임 아이템")]
    [Tooltip("보통 FieldObjectDropper가 실행 중에 넣어줍니다. 테스트할 때는 직접 넣어도 됩니다.")]
    [SerializeField]
    private ItemData item;

    [Tooltip("보통 FieldObjectDropper가 실행 중에 넣어줍니다. 테스트할 때는 직접 넣어도 됩니다.")]
    [SerializeField]
    private int count = 1;

    public void Initialize(ItemData itemData, int itemCount)
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
