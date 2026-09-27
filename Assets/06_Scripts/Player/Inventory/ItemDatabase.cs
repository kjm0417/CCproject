using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    [Header("데이터 연결 - 나중에 연결")]
    [Tooltip("인벤토리 아이템 데이터가 준비되면 여기에 ItemData 에셋들을 넣습니다.")]
    [SerializeField]
    private List<InvenItemData> items = new List<InvenItemData>();

    public InvenItemData FindById(int itemId)
    {
        return FindById(itemId.ToString());
    }

    public InvenItemData FindById(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;

        for (int i = 0; i < items.Count; i++)
        {
            InvenItemData item = items[i];
            if (item != null && item.ItemID == itemId)
            {
                return item;
            }
        }

        return null;
    }
}
