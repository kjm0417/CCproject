// 인벤토리 한 칸. 어떤 아이템(ItemData)을 몇 개 들고 있는지.
// MonoBehaviour가 아니라 순수 데이터라 PlayerInventory 안에서 리스트로 다룬다.
public class InventorySlot
{
    public ItemData Item;
    public int Count;

    public InventorySlot(ItemData item, int count)
    {
        Item = item;
        Count = count;
    }

    // 이 칸에 더 넣을 수 있는 여유 (스택 상한 - 현재 개수)
    public int SpaceLeft => Item != null ? Item.MaxStack - Count : 0;
}
