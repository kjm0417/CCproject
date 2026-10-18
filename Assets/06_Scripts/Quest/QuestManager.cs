using System;
using System.Collections.Generic;
using UnityEngine;

public enum QuestState
{
    Progress, //진행 중
    Ready,    //완료, 보상 받기 대기
    Done,     //지급 횟수 모두 받음
}

/// <summary>
/// 영지 퀘스트 진행 관리. 자원 채집 / 몬스터 처치를 세고, 조건을 채우면 받기 대기 상태로 둔다.
/// 보상은 Claim(받기)으로 지급. 진행 상황은 TerritorySaveData에 함께 저장한다.
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [SerializeField]
    private List<QuestData> quests = new List<QuestData>();

    // QuestID -> 진행 수 / 받은 횟수
    private readonly Dictionary<string, QuestProgressData> progressDic = new Dictionary<string, QuestProgressData>();

    private PlayerContext player;

    public IReadOnlyList<QuestData> Quests => quests;

    /// <summary>
    /// 퀘스트 진행 / 받기 상태가 바뀔 때 (바뀐 퀘스트, 전체 갱신이면 null)
    /// </summary>
    public event Action<QuestData> OnQuestChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        ResetProgress();
    }

    private void OnEnable()
    {
        ResourceSpawner.OnAnyResourceDepleted += OnResourceDepleted;
        MonsterSpawner.OnAnyEnemyKilled += OnEnemyKilled;
    }

    private void OnDisable()
    {
        ResourceSpawner.OnAnyResourceDepleted -= OnResourceDepleted;
        MonsterSpawner.OnAnyEnemyKilled -= OnEnemyKilled;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    #region 조회
    public int GetCount(QuestData quest)
    {
        return progressDic.TryGetValue(quest.QuestID, out QuestProgressData progress) ? progress.Count : 0;
    }

    public int GetPaidCount(QuestData quest)
    {
        return progressDic.TryGetValue(quest.QuestID, out QuestProgressData progress) ? progress.PaidCount : 0;
    }

    public QuestState GetState(QuestData quest)
    {
        if (GetPaidCount(quest) >= quest.MaxPayCount) return QuestState.Done;
        return GetCount(quest) >= quest.RequiredCount ? QuestState.Ready : QuestState.Progress;
    }

    /// <summary>
    /// 받기 대기 중인 퀘스트 수 (배지 표시용)
    /// </summary>
    public int GetReadyCount()
    {
        int count = 0;
        foreach (QuestData quest in quests)
        {
            if (quest != null && GetState(quest) == QuestState.Ready) count++;
        }
        return count;
    }
    #endregion

    #region 진행
    private void OnResourceDepleted(GameObject resourcePrefab)
    {
        AddProgress(QuestTargetType.GatherResource, resourcePrefab);
    }

    private void OnEnemyKilled(GameObject enemyPrefab)
    {
        AddProgress(QuestTargetType.KillEnemy, enemyPrefab);
    }

    private void AddProgress(QuestTargetType targetType, GameObject targetPrefab)
    {
        foreach (QuestData quest in quests)
        {
            if (quest == null || quest.TargetType != targetType || quest.TargetPrefab != targetPrefab) continue;

            // 받기 대기 / 완료 상태면 더 세지 않음
            if (GetState(quest) != QuestState.Progress) continue;

            progressDic[quest.QuestID].Count++;
            OnQuestChanged?.Invoke(quest);
        }
    }
    #endregion

    #region 받기
    /// <summary>
    /// 보상 받기 - 반복 퀘스트는 진행 수를 0으로 되돌려 다시 진행
    /// </summary>
    public bool Claim(QuestData quest)
    {
        if (quest == null || GetState(quest) != QuestState.Ready) return false;

        QuestProgressData progress = progressDic[quest.QuestID];
        progress.PaidCount++;
        if (progress.PaidCount < quest.MaxPayCount) progress.Count = 0;

        if (player == null) player = FindAnyObjectByType<PlayerContext>();
        if (player != null && player.Wallet != null) player.Wallet.Add(CurrencyType.Gold, quest.RewardGold);

        OnQuestChanged?.Invoke(quest);
        return true;
    }

    public void ClaimAll()
    {
        foreach (QuestData quest in quests)
        {
            if (quest != null && GetState(quest) == QuestState.Ready) Claim(quest);
        }
    }
    #endregion

    private void ResetProgress()
    {
        progressDic.Clear();
        foreach (QuestData quest in quests)
        {
            if (quest == null) continue;
            progressDic[quest.QuestID] = new QuestProgressData { QuestID = quest.QuestID };
        }
    }

    #region 저장 및 불러오기
    public List<QuestProgressData> Save()
    {
        List<QuestProgressData> data = new List<QuestProgressData>();
        foreach (QuestProgressData progress in progressDic.Values)
        {
            data.Add(new QuestProgressData
            {
                QuestID = progress.QuestID,
                Count = progress.Count,
                PaidCount = progress.PaidCount
            });
        }
        return data;
    }

    public void Load(List<QuestProgressData> data)
    {
        ResetProgress();

        if (data != null)
        {
            foreach (QuestProgressData saved in data)
            {
                if (!progressDic.TryGetValue(saved.QuestID, out QuestProgressData progress)) continue;

                progress.Count = saved.Count;
                progress.PaidCount = saved.PaidCount;
            }
        }

        OnQuestChanged?.Invoke(null);
    }
    #endregion
}
