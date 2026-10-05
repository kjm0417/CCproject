using UnityEngine;

// 개발용 테스트 HUD. 플레이어 현재 상태를 화면에 표시하고, 버튼으로 재화/경험치를 넣어본다.
// 실제 빌드용이 아니라 테스트용. PlayerContext와 같은 오브젝트(Player)에 붙여서 Play.
// (Input System 설정과 무관하게 동작하도록 키 대신 OnGUI 버튼을 쓴다.)
[RequireComponent(typeof(PlayerContext))]
public class PlayerDebugHUD : MonoBehaviour
{
    private const string SwordItemId = "25101";
    private const string AxeItemId = "22101";
    private const string PickaxeItemId = "21101";
    private const string ShovelItemId = "23101";

    [Header("Test Tools")]
    [SerializeField]
    private bool grantTestToolsOnStart = true;

    [Header("Test Food")]
    [SerializeField]
    private bool grantTestFoodOnStart = true;
    [SerializeField]
    private InvenItemData testFoodItem;
    [SerializeField]
    private int testFoodCount = 5;

    private PlayerContext context;
    private InvenItemData runtimeSword;
    private InvenItemData runtimeAxe;
    private InvenItemData runtimePickaxe;
    private InvenItemData runtimeShovel;

    private void Awake()
    {
        context = GetComponent<PlayerContext>();
    }

    private void Start()
    {
        if (grantTestToolsOnStart)
        {
            GrantTestTools();
        }

        if (grantTestFoodOnStart
            && testFoodItem != null
            && !HasItem(testFoodItem.ItemID))
        {
            GrantTestFood(testFoodCount);
        }
    }

    private void OnGUI()
    {
        if (context == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 420, 460), GUI.skin.box);
        GUILayout.Label("== Player Debug ==");

        if (context.Progression != null)
        {
            var p = context.Progression;
            GUILayout.Label($"Lv {p.Level}   Exp {p.ExpInLevel} / {p.ExpToNext}   SP {p.SkillPoints}");
        }

        if (context.Vitals != null)
        {
            var v = context.Vitals;
            GUILayout.Label($"HP {v.Hp:F0} / {v.MaxHp:F0}      허기 {v.HungerValue:F0} / {v.MaxHunger:F0}  ({v.HungerTier})");
        }

        if (context.Wallet != null)
        {
            var w = context.Wallet;
            GUILayout.Label($"Gold {w.GetBalance(CurrencyType.Gold)}    Gem {w.GetBalance(CurrencyType.Gem)}");
        }

        if (context.Inventory != null)
        {
            GUILayout.Label(
                $"Test Tools  Sword: {HasItem(SwordItemId)}    "
                + $"Axe: {HasItem(AxeItemId)}    "
                + $"Pickaxe: {HasItem(PickaxeItemId)}    "
                + $"Shovel: {HasItem(ShovelItemId)}");
            string foodName = testFoodItem != null ? testFoodItem.ItemName : "Not Assigned";
            int foodCount = testFoodItem != null ? GetItemCount(testFoodItem.ItemID) : 0;
            GUILayout.Label($"Test Food  {foodName}: {foodCount}");
        }

        GUILayout.Space(6);

        if (context.Wallet != null && GUILayout.Button("+100 Gold"))
        {
            context.Wallet.Add(CurrencyType.Gold, 100);
        }
        if (context.Wallet != null && GUILayout.Button("+5 Gem"))
        {
            context.Wallet.Add(CurrencyType.Gem, 5);
        }
        if (context.Progression != null && GUILayout.Button("+50 Exp"))
        {
            context.Progression.AddExp(50);
        }
        if (context.Vitals != null && GUILayout.Button("-10 HP"))
        {
            context.Vitals.TakeDamage(10);
        }
        if (context.Vitals != null && GUILayout.Button("-30 Hunger"))
        {
            context.Vitals.ConsumeHunger(30);
        }
        if (context.Inventory != null && GUILayout.Button("Give Sword + Axe + Pickaxe + Shovel"))
        {
            GrantTestTools();
        }
        if (context.Inventory != null && GUILayout.Button("Give Sword"))
        {
            GrantTestSword();
        }
        if (context.Inventory != null && GUILayout.Button("Give Shovel"))
        {
            GrantTestShovel();
        }
        string testFoodName = testFoodItem != null ? testFoodItem.ItemName : "Food";
        if (context.Inventory != null && GUILayout.Button($"+{testFoodCount} {testFoodName}"))
        {
            GrantTestFood(testFoodCount);
        }

        GUILayout.EndArea();
    }

    private void GrantTestTools()
    {
        if (context == null || context.Inventory == null) return;

        GrantTestSword();

        if (!HasItem(AxeItemId))
        {
            runtimeAxe = CreateTestTool(AxeItemId, "도끼");
            context.Inventory.Add(runtimeAxe, 1);
        }

        if (!HasItem(PickaxeItemId))
        {
            runtimePickaxe = CreateTestTool(PickaxeItemId, "곡괭이");
            context.Inventory.Add(runtimePickaxe, 1);
        }

        GrantTestShovel();
    }

    private void GrantTestSword()
    {
        if (context == null || context.Inventory == null || HasItem(SwordItemId)) return;

        runtimeSword = CreateTestTool(SwordItemId, "칼");
        int remaining = context.Inventory.Add(runtimeSword, 1);
        if (remaining > 0)
        {
            Destroy(runtimeSword);
            runtimeSword = null;
        }
    }

    private void GrantTestShovel()
    {
        if (context == null || context.Inventory == null || HasItem(ShovelItemId)) return;

        runtimeShovel = CreateTestTool(ShovelItemId, "삽");
        int remaining = context.Inventory.Add(runtimeShovel, 1);
        if (remaining > 0)
        {
            Destroy(runtimeShovel);
            runtimeShovel = null;
        }
    }

    private bool HasItem(string itemId)
    {
        return GetItemCount(itemId) > 0;
    }

    private int GetItemCount(string itemId)
    {
        if (context == null || context.Inventory == null) return 0;

        int count = 0;

        for (int i = 0; i < context.Inventory.Slots.Count; i++)
        {
            InventorySlot slot = context.Inventory.Slots[i];
            if (slot.Item != null && slot.Item.ItemID == itemId)
            {
                count += slot.Count;
            }
        }

        return count;
    }

    private void GrantTestFood(int count)
    {
        if (context == null || context.Inventory == null || testFoodItem == null) return;

        context.Inventory.Add(testFoodItem, Mathf.Max(1, count));
    }

    private static InvenItemData CreateTestTool(string itemId, string itemName)
    {
        InvenItemData item = ScriptableObject.CreateInstance<InvenItemData>();
        item.name = $"RuntimeTest_{itemName}";
        item.hideFlags = HideFlags.DontSave;
        item.ItemID = itemId;
        item.ItemName = itemName;
        item.ItemType = "장비 아이템";
        item.SubType = "도구 아이템";
        item.Description = "테스트용 자동 장착 도구";
        item.MaxStack = 1;
        return item;
    }

    private void OnDestroy()
    {
        if (runtimeSword != null) Destroy(runtimeSword);
        if (runtimeAxe != null) Destroy(runtimeAxe);
        if (runtimePickaxe != null) Destroy(runtimePickaxe);
        if (runtimeShovel != null) Destroy(runtimeShovel);
    }
}
