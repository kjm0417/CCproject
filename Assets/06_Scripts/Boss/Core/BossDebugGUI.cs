using UnityEngine;

/// <summary>
/// 보스 디버그 버튼 ( OnGUI ) - 화면 맨 우측 버튼으로 보스 즉시 사망
/// 에디터 / Development Build 에서만 표시
/// </summary>
public class BossDebugGUI : MonoBehaviour
{
    [SerializeField, Tooltip("비우면 같은 오브젝트의 BossBase")]
    private BossBase boss;
    [SerializeField] private Vector2 buttonSize = new Vector2(140f, 50f);
    [SerializeField] private float margin = 10f;

    private void Awake()
    {
        if (boss == null) boss = GetComponent<BossBase>();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void OnGUI()
    {
        if (boss == null || boss.IsDead) return;

        Rect rect = new Rect(Screen.width - buttonSize.x - margin, (Screen.height - buttonSize.y) * 0.5f, buttonSize.x, buttonSize.y);
        if (GUI.Button(rect, "보스 사망"))
        {
            boss.Kill();
        }
    }
#endif
}
