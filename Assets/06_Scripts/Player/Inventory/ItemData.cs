using UnityEngine;

// 아이템 "정의"(데이터). 실제 보유는 PlayerInventory가 이 데이터 + 개수로 관리한다.
// 몬스터/구역 데이터처럼 아이템도 ScriptableObject 애셋으로 만든다.
[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    public string ItemId;       // 고유 id (저장/비교용)
    public string DisplayName;  // 표시 이름
    //public ItemType Type;       // 분류 (문서 3.2
    public int MaxStack = 1;    // 한 칸 최대 중첩 수. 1이면 중첩 불가(장비/도구)
    public Sprite Icon;         // 인벤토리 UI 아이콘
}
