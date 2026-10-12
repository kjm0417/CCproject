using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 던전 클리어 결과 UI
/// 클리어 시간 + 획득 보상 ( 최초 / 반복 ) 표시 -> 영지로 돌아가기 시 보상 수령 후 영지 씬으로
/// </summary>
public class DungeonResultUI : MonoBehaviour
{
    [SerializeField]
    private GameObject panelRoot;
    [SerializeField, Tooltip("최초 클리어일 때만 표시")]
    private GameObject firstClearTag;
    [SerializeField]
    private TextMeshProUGUI clearTimeText;
    [SerializeField]
    private TextMeshProUGUI rewardTitleText;
    [SerializeField]
    private DungeonEntryRewardRow rewardRow;
    [SerializeField]
    private Button returnButton;

    private const string FirstClearTitle = "획득 보상 · 최초 클리어";
    private const string RepeatClearTitle = "획득 보상 · 반복 클리어";

    private DungeonProgressManager manager;
    private HUDBottomPanel hudPanel;

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
        returnButton.onClick.AddListener(OnReturnClick);
        panelRoot.SetActive(false);
    }

    private void Start()
    {
        manager = DungeonProgressManager.Instance;
        if (manager == null)
        {
            Debug.LogError("[DungeonResultUI] DungeonProgressManager 없음");
            return;
        }

        manager.OnDungeonCleared += Show;
    }

    private void OnDestroy()
    {
        if (manager != null) manager.OnDungeonCleared -= Show;
    }

    private void Show(DungeonResult result)
    {
        panelRoot.SetActive(true);
        returnButton.interactable = true;

        firstClearTag.SetActive(result.IsFirstClear);
        clearTimeText.text = DungeonTimerUI.Format(result.ClearTime);
        rewardTitleText.text = result.IsFirstClear ? FirstClearTitle : RepeatClearTitle;
        rewardRow.Set(ToReward(result), false, ResolveIcon);
    }

    /// <summary>
    /// 결과에 확정된 보상 ( 랜덤 테이블 포함 ) 을 보상 줄 표시용으로 변환
    /// </summary>
    private static DungeonReward ToReward(DungeonResult result)
    {
        DungeonReward reward = new DungeonReward { Exp = result.RewardExp };
        reward.Currencies.AddRange(result.RewardCurrencies);
        reward.Items.AddRange(result.RewardItems);
        return reward;
    }

    private void OnReturnClick()
    {
        returnButton.interactable = false; //중복 수령 방지
        manager.ClaimRewardAndExit();
    }

    private Sprite ResolveIcon(InvenItemData item)
    {
        if (hudPanel == null) hudPanel = FindAnyObjectByType<HUDBottomPanel>(FindObjectsInactive.Include);
        return hudPanel != null ? hudPanel.GetItemIcon(item) : null;
    }
}
