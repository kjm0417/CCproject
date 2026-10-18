using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 오른쪽 퀘스트 사이드 패널. 받기 가능 -> 진행 중 순으로 카드 정렬, 보상을 모두 받은 퀘스트는 숨김.
/// 열려 있는 동안 플레이어 이동 입력 차단. ×, 패널 바깥 터치로 닫힘
/// </summary>
public class QuestPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button outsideButton; //패널 바깥 터치 영역
    [SerializeField] private Button closeButton;
    [SerializeField] private Button claimAllButton;
    [SerializeField] private Image claimAllImage;
    [SerializeField] private TMP_Text txtReadyCount;
    [SerializeField] private Transform cardRoot;
    [SerializeField] private QuestCardUI cardTemplate;

    [SerializeField] private Color goldColor = new Color32(0xF2, 0xC7, 0x5C, 0xFF);
    [SerializeField] private Color disabledColor = new Color32(0xE4, 0xE7, 0xE9, 0xFF);

    private readonly List<QuestCardUI> cards = new List<QuestCardUI>();
    private PlayerContext player;
    private bool subscribed;

    public bool IsOpen => panelRoot.activeSelf;

    /// <summary>
    /// 열림 / 닫힘 (트래커 숨김용)
    /// </summary>
    public event Action<bool> OnOpenChanged;

    private void Awake()
    {
        cardTemplate.gameObject.SetActive(false);
        outsideButton.onClick.AddListener(Close);
        closeButton.onClick.AddListener(Close);
        claimAllButton.onClick.AddListener(OnClaimAll);
        panelRoot.SetActive(false);
        outsideButton.gameObject.SetActive(false);
    }

    // QuestManager Awake보다 OnEnable이 먼저 불릴 수 있어 Start에서도 구독
    private void Start()
    {
        Subscribe();
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

    public void Open()
    {
        panelRoot.SetActive(true);
        outsideButton.gameObject.SetActive(true);
        SetPlayerInputBlocked(true);
        Refresh();
        OnOpenChanged?.Invoke(true);
    }

    public void Close()
    {
        if (!IsOpen) return;

        panelRoot.SetActive(false);
        outsideButton.gameObject.SetActive(false);
        SetPlayerInputBlocked(false);
        OnOpenChanged?.Invoke(false);
    }

    private void SetPlayerInputBlocked(bool blocked)
    {
        if (player == null) player = FindAnyObjectByType<PlayerContext>();
        if (player != null && player.Movement != null) player.Movement.SetInputBlocked(blocked);
    }

    private void OnQuestChanged(QuestData quest)
    {
        if (IsOpen) Refresh();
    }

    private void OnClaimAll()
    {
        if (QuestManager.Instance == null) return;

        QuestManager.Instance.ClaimAll();
    }

    private List<QuestData> GetSortedQuests()
    {
        QuestManager manager = QuestManager.Instance;
        List<QuestData> sorted = new List<QuestData>();
        foreach (QuestData quest in manager.Quests)
        {
            // 보상을 모두 받은 퀘스트는 목록에서 제외
            if (quest != null && manager.GetState(quest) != QuestState.Done) sorted.Add(quest);
        }

        // 받기 가능 -> 진행 중 (같은 상태는 등록 순서 유지)
        List<QuestData> order = new List<QuestData>(sorted);
        sorted.Sort((a, b) =>
        {
            int stateCompare = StateOrder(manager.GetState(a)).CompareTo(StateOrder(manager.GetState(b)));
            return stateCompare != 0 ? stateCompare : order.IndexOf(a).CompareTo(order.IndexOf(b));
        });
        return sorted;
    }

    private static int StateOrder(QuestState state)
    {
        switch (state)
        {
            case QuestState.Ready: return 0;
            case QuestState.Progress: return 1;
            default: return 2;
        }
    }

    private void Refresh()
    {
        QuestManager manager = QuestManager.Instance;
        if (manager == null) return;

        List<QuestData> sorted = GetSortedQuests();

        while (cards.Count < sorted.Count)
        {
            QuestCardUI card = Instantiate(cardTemplate, cardRoot);
            cards.Add(card);
        }

        for (int i = 0; i < cards.Count; i++)
        {
            bool visible = i < sorted.Count;
            cards[i].gameObject.SetActive(visible);
            if (!visible) continue;

            QuestData quest = sorted[i];
            cards[i].Bind(quest, manager.GetCount(quest), manager.GetPaidCount(quest), manager.GetState(quest));
        }

        int readyCount = manager.GetReadyCount();
        if (readyCount > 0) txtReadyCount.text = $"받을 보상 {readyCount}";
        else txtReadyCount.text = sorted.Count > 0 ? "받을 보상 없음" : "모두 완료";
        claimAllButton.interactable = readyCount > 0;
        claimAllImage.color = readyCount > 0 ? goldColor : disabledColor;
    }
}
