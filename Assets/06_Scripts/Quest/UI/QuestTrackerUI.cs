using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 기본 화면 퀘스트 트래커. 제목 줄(받을 보상 배지) + 2줄 + '+N개 더'.
/// 줄 순서: 받기 가능 -> 완료에 가까운 순. 방금 진행된 퀘스트는 잠시 끼워 보여줌.
/// 받기 줄을 누르면 바로 보상, 나머지는 패널 열림
/// </summary>
public class QuestTrackerUI : MonoBehaviour
{
    [SerializeField] private GameObject trackerRoot;
    [SerializeField] private Button headButton;
    [SerializeField] private GameObject badgeRoot;
    [SerializeField] private TMP_Text txtBadge;
    [SerializeField] private QuestTrackerRowUI[] rows;
    [SerializeField] private TMP_Text txtMore;
    [SerializeField] private QuestPanelUI panel;

    [SerializeField, Tooltip("방금 진행된 퀘스트를 끼워 보여줄 시간 (초)")]
    private float recentDuration = 2.5f;

    private QuestData recentQuest;
    private float recentTime = float.NegativeInfinity;
    private bool subscribed;

    private void Start()
    {
        headButton.onClick.AddListener(panel.Open);
        foreach (QuestTrackerRowUI row in rows)
        {
            QuestTrackerRowUI target = row;
            row.Button.onClick.AddListener(() => OnRowClicked(target));
        }
        panel.OnOpenChanged += open => Refresh(null);

        Subscribe();
        Refresh(null);
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        if (subscribed && QuestManager.Instance != null) QuestManager.Instance.OnQuestChanged -= OnQuestChanged;
        subscribed = false;
    }

    private void Subscribe()
    {
        if (subscribed || QuestManager.Instance == null) return;
        QuestManager.Instance.OnQuestChanged += OnQuestChanged;
        subscribed = true;
    }

    private void Update()
    {
        // 끼워 보여준 시간이 지나면 원래 순서로
        if (recentQuest != null && Time.unscaledTime - recentTime >= recentDuration)
        {
            recentQuest = null;
            Refresh(null);
        }
    }

    /// <summary>
    /// 받기 대기 줄은 바로 보상 받기, 진행 중 줄은 패널 열기
    /// </summary>
    private void OnRowClicked(QuestTrackerRowUI row)
    {
        if (row.IsReady && QuestManager.Instance != null && QuestManager.Instance.Claim(row.Quest)) return;

        panel.Open();
    }

    private void OnQuestChanged(QuestData quest)
    {
        if (quest != null && QuestManager.Instance.GetState(quest) == QuestState.Progress)
        {
            recentQuest = quest;
            recentTime = Time.unscaledTime;
        }
        Refresh(quest);
    }

    private void Refresh(QuestData bumpQuest)
    {
        QuestManager manager = QuestManager.Instance;
        if (manager == null) return;

        // 지급 완료 제외, 받기 가능 -> 진행률 높은 순
        List<QuestData> active = new List<QuestData>();
        foreach (QuestData quest in manager.Quests)
        {
            if (quest != null && manager.GetState(quest) != QuestState.Done) active.Add(quest);
        }
        active.Sort((a, b) =>
        {
            int readyCompare = (manager.GetState(b) == QuestState.Ready).CompareTo(manager.GetState(a) == QuestState.Ready);
            return readyCompare != 0 ? readyCompare : Progress(manager, b).CompareTo(Progress(manager, a));
        });

        // 방금 진행된 퀘스트를 앞쪽에 (받기 가능 줄이 맨 앞이면 그 다음)
        if (recentQuest != null && active.Remove(recentQuest))
        {
            int insertIndex = active.Count > 0 && manager.GetState(active[0]) == QuestState.Ready ? 1 : 0;
            active.Insert(Mathf.Min(insertIndex, active.Count), recentQuest);
        }

        // 패널이 열려 있거나 남은 퀘스트가 없으면 트래커 숨김
        trackerRoot.SetActive(active.Count > 0 && !panel.IsOpen);

        for (int i = 0; i < rows.Length; i++)
        {
            bool visible = i < active.Count;
            rows[i].gameObject.SetActive(visible);
            if (!visible) continue;

            QuestData quest = active[i];
            rows[i].Bind(quest, manager.GetCount(quest), manager.GetState(quest));
            if (quest == bumpQuest) rows[i].Bump();
        }

        int more = Mathf.Max(0, active.Count - rows.Length);
        txtMore.gameObject.SetActive(more > 0);
        txtMore.text = $"+{more}개 더";

        int readyCount = manager.GetReadyCount();
        badgeRoot.SetActive(readyCount > 0);
        txtBadge.text = readyCount.ToString();
    }

    private static float Progress(QuestManager manager, QuestData quest)
    {
        return (float)manager.GetCount(quest) / quest.RequiredCount;
    }
}
