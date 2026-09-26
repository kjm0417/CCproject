using UnityEngine;

// 개발용 테스트 HUD. 플레이어 현재 상태를 화면에 표시하고, 버튼으로 재화/경험치를 넣어본다.
// 실제 빌드용이 아니라 테스트용. PlayerContext와 같은 오브젝트(Player)에 붙여서 Play.
// (Input System 설정과 무관하게 동작하도록 키 대신 OnGUI 버튼을 쓴다.)
[RequireComponent(typeof(PlayerContext))]
public class PlayerDebugHUD : MonoBehaviour
{
    private const string AxeItemId = "22101";
    private const string PickaxeItemId = "21101";

    [Header("Test Tools")]
    [SerializeField]
    private bool grantTestToolsOnStart = true;

    private PlayerContext context;
    private InvenItemData runtimeAxe;
    private InvenItemData runtimePickaxe;

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
    }

    private void OnGUI()
    {
        if (context == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 420, 340), GUI.skin.box);
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
            GUILayout.Label($"Test Tools  Axe: {HasItem(AxeItemId)}    Pickaxe: {HasItem(PickaxeItemId)}");
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
        if (context.Inventory != null && GUILayout.Button("Give Axe + Pickaxe"))
        {
            GrantTestTools();
        }

        GUILayout.EndArea();
    }

    private void GrantTestTools()
    {
        if (context == null || context.Inventory == null) return;

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
    }

    private bool HasItem(string itemId)
    {
        if (context == null || context.Inventory == null) return false;

        for (int i = 0; i < context.Inventory.Slots.Count; i++)
        {
            InvenItemData item = context.Inventory.Slots[i].Item;
            if (item != null && item.ItemID == itemId) return true;
        }

        return false;
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
        if (runtimeAxe != null) Destroy(runtimeAxe);
        if (runtimePickaxe != null) Destroy(runtimePickaxe);
    }
}
