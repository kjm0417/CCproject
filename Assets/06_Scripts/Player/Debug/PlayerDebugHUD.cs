using UnityEngine;

// 개발용 테스트 HUD. 플레이어 현재 상태를 화면에 표시하고, 버튼으로 재화/경험치를 넣어본다.
// 실제 빌드용이 아니라 테스트용. PlayerContext와 같은 오브젝트(Player)에 붙여서 Play.
// (Input System 설정과 무관하게 동작하도록 키 대신 OnGUI 버튼을 쓴다.)
[RequireComponent(typeof(PlayerContext))]
public class PlayerDebugHUD : MonoBehaviour
{
    private PlayerContext context;

    private void Awake()
    {
        context = GetComponent<PlayerContext>();
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

        GUILayout.EndArea();
    }
}
