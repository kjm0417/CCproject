using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 던전 진행 상태
/// </summary>
public enum DungeonState
{
    Ready,      //시작 전
    InProgress, //진행 중
    Cleared,    //클리어 ( 보스 처치 )
    Failed,     //실패 ( 플레이어 사망 / 포기 )
}

/// <summary>
/// 던전 스테이지(구역) 1개 - 선형 진행이라 앞 스테이지 적 전멸 시 다음 스테이지 진행
/// 마지막 스테이지 = 보스 스테이지
/// </summary>
[Serializable]
public class DungeonStage
{
    public string StageName;

    [Tooltip("스테이지 시작 시 활성화할 루트 ( 적/구역 ). 비우면 처음부터 활성 상태")]
    public GameObject StageRoot;

    [Tooltip("처치해야 할 적. 비우면 StageRoot 하위 EnemyBase 자동 수집")]
    public List<EnemyBase> Enemies = new List<EnemyBase>();

    [Tooltip("스테이지 클리어 시 비활성화할 오브젝트 ( 다음 구역을 막는 벽/문 )")]
    public List<GameObject> Blockers = new List<GameObject>();
}

/// <summary>
/// 던전 결과 ( 추후 결과 UI에서 사용 )
/// </summary>
public class DungeonResult
{
    public DungeonData Dungeon;
    public DungeonState State;
    public bool IsFirstClear;
    public int ClearCount;

    public List<CurrencyAmount> RewardCurrencies = new List<CurrencyAmount>();
    public float RewardExp;
    public List<DungeonItemAmount> RewardItems = new List<DungeonItemAmount>();

    public List<DungeonItemAmount> RefundItems = new List<DungeonItemAmount>();
}

/// <summary>
/// 던전 진행 관리자
/// 선형 진행 ( 스테이지 순서대로 적 전멸 ) -> 보스 처치 시 클리어 -> 최초/반복 보상 지급
/// 플레이어 사망/포기 시 실패 -> 클리어 기록 없으면 입장 아이템 일부 환급
/// 시간 제한 없음 / 재입장 무제한
/// </summary>
public class DungeonProgressManager : MonoBehaviour
{
    #region 싱글톤 ( 던전 씬 한정 )
    private static DungeonProgressManager instance;
    public static DungeonProgressManager Instance => instance;
    #endregion

    [Header("입장 절차 없이 씬을 바로 실행했을 때 사용할 던전 ( 테스트용 )")]
    [SerializeField]
    private DungeonData fallbackDungeon;

    [Header("스테이지 ( 순서대로 진행, 마지막 = 보스 )")]
    [SerializeField]
    private List<DungeonStage> stages = new List<DungeonStage>();

    [Header("던전 종료 후 돌아갈 씬")]
    [SerializeField]
    private string returnSceneName = "KJ_Scene";

    [Header("결과 UI 기획 전 임시 - 결과 후 자동 퇴장")]
    [SerializeField]
    private bool autoExit = true;
    [SerializeField]
    private float autoExitDelay = 3f;

    #region 이벤트 ( 추후 UI 연결용 )
    public event Action<DungeonData> OnDungeonStarted;
    public event Action<int> OnStageStarted;            //스테이지 인덱스
    public event Action<int> OnStageCleared;            //스테이지 인덱스
    public event Action OnBossStageStarted;
    public event Action<int, int> OnEnemyCountChanged;  //남은 적 수, 스테이지 전체 적 수
    public event Action<DungeonResult> OnDungeonCleared;
    public event Action<DungeonResult> OnDungeonFailed;
    #endregion

    private DungeonData dungeon;
    private DungeonState state = DungeonState.Ready;
    private int currentStageIndex = -1;
    private readonly HashSet<EnemyBase> aliveEnemies = new HashSet<EnemyBase>();

    private PlayerContext player;
    private PlayerInventory inventory;

    private DungeonSaveData dungeonSaveData;
    private DungeonRecordData record;

    public DungeonData Dungeon => dungeon;
    public DungeonState State => state;
    public int CurrentStageIndex => currentStageIndex;
    public int StageCount => stages.Count;
    public bool IsBossStage => currentStageIndex == stages.Count - 1;
    public int RemainingEnemyCount => aliveEnemies.Count;
    public DungeonResult LastResult { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        //스테이지 적 자동 수집 + 선형 진행이라 아직 도달하지 않은 스테이지는 비활성화
        for (int i = 0; i < stages.Count; i++)
        {
            DungeonStage stage = stages[i];
            if (stage.Enemies.Count == 0 && stage.StageRoot != null)
            {
                stage.Enemies.AddRange(stage.StageRoot.GetComponentsInChildren<EnemyBase>(true));
            }

            if (i > 0 && stage.StageRoot != null)
            {
                stage.StageRoot.SetActive(false);
            }
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;

        UnsubscribeAllEnemies();

        if (player != null && player.Vitals != null)
        {
            player.Vitals.OnDied -= OnPlayerDied;
        }
    }

    private void Start()
    {
        //세이브 데이터 로드(SaveManager.OnSceneLoaded)가 Awake 이후에 끝나므로 Start에서 시작
        StartDungeon();
    }

    #region 진행
    /// <summary>
    /// 던전 시작
    /// </summary>
    private void StartDungeon()
    {
        if (!DungeonSession.IsInDungeon)
        {
            if (fallbackDungeon == null)
            {
                Debug.LogError("[Dungeon] 입장한 던전 정보가 없음 - fallbackDungeon 설정 필요");
                return;
            }
            DungeonSession.BeginWithoutEntry(fallbackDungeon);
        }
        dungeon = DungeonSession.CurrentDungeon;

        if (stages.Count == 0)
        {
            Debug.LogError("[Dungeon] 스테이지가 없음");
            return;
        }

        player = FindAnyObjectByType<PlayerContext>();
        if (player != null)
        {
            inventory = player.GetComponentInChildren<PlayerInventory>();
            if (player.Vitals != null)
            {
                player.Vitals.OnDied += OnPlayerDied;
            }
        }

        dungeonSaveData = SaveManager.Instance != null ? SaveManager.Instance.LoadDungeon() : new DungeonSaveData();
        record = dungeonSaveData.GetOrCreate(dungeon.DungeonID);

        state = DungeonState.InProgress;
        Debug.Log($"[Dungeon] 시작 : {dungeon.DungeonName} ( 클리어 기록 {record.ClearCount}회 )");
        OnDungeonStarted?.Invoke(dungeon);

        EnterStage(0);
    }

    /// <summary>
    /// 스테이지 진입 - 적 등록 후 전멸 대기
    /// </summary>
    private void EnterStage(int index)
    {
        //적이 없는 스테이지는 바로 통과
        while (state == DungeonState.InProgress && index < stages.Count)
        {
            currentStageIndex = index;
            DungeonStage stage = stages[index];

            if (stage.StageRoot != null && !stage.StageRoot.activeSelf)
            {
                stage.StageRoot.SetActive(true);
            }

            UnsubscribeAllEnemies();
            foreach (EnemyBase enemy in stage.Enemies)
            {
                if (enemy == null || enemy.IsDie) continue;

                aliveEnemies.Add(enemy);
                enemy.OnEnemyDied += OnEnemyDied;
            }

            Debug.Log($"[Dungeon] 스테이지 {index + 1}/{stages.Count} 시작 : {stage.StageName} ( 적 {aliveEnemies.Count} )");
            OnStageStarted?.Invoke(index);
            if (IsBossStage) OnBossStageStarted?.Invoke();
            OnEnemyCountChanged?.Invoke(aliveEnemies.Count, aliveEnemies.Count);

            if (aliveEnemies.Count > 0) return;

            //적 없음 - 바로 클리어 처리 후 다음 스테이지
            if (!CompleteStage()) return;
            index++;
        }
    }

    private void OnEnemyDied(EnemyBase enemy)
    {
        enemy.OnEnemyDied -= OnEnemyDied;

        if (state != DungeonState.InProgress) return;
        if (!aliveEnemies.Remove(enemy)) return;

        int total = stages[currentStageIndex].Enemies.Count;
        OnEnemyCountChanged?.Invoke(aliveEnemies.Count, total);

        if (aliveEnemies.Count > 0) return;

        if (CompleteStage())
        {
            EnterStage(currentStageIndex + 1);
        }
    }

    /// <summary>
    /// 현재 스테이지 클리어 처리. 다음 스테이지로 진행해야 하면 true
    /// </summary>
    private bool CompleteStage()
    {
        DungeonStage stage = stages[currentStageIndex];
        foreach (GameObject blocker in stage.Blockers)
        {
            if (blocker != null) blocker.SetActive(false);
        }

        Debug.Log($"[Dungeon] 스테이지 {currentStageIndex + 1} 클리어");
        OnStageCleared?.Invoke(currentStageIndex);

        //클리어 조건 : 보스(마지막 스테이지) 처치
        if (IsBossStage)
        {
            ClearDungeon();
            return false;
        }
        return true;
    }

    private void UnsubscribeAllEnemies()
    {
        foreach (EnemyBase enemy in aliveEnemies)
        {
            if (enemy != null) enemy.OnEnemyDied -= OnEnemyDied;
        }
        aliveEnemies.Clear();
    }
    #endregion

    #region 클리어
    /// <summary>
    /// 던전 클리어 - 최초 클리어면 최초 보상, 이후는 반복 보상
    /// </summary>
    private void ClearDungeon()
    {
        if (state != DungeonState.InProgress) return;
        state = DungeonState.Cleared;

        bool isFirstClear = !record.FirstRewardClaimed;
        DungeonReward reward = isFirstClear ? dungeon.FirstClearReward : dungeon.RepeatClearReward;

        DungeonResult result = CreateResult();
        result.IsFirstClear = isFirstClear;
        GiveReward(reward, result);
        Debug.Log($"[Dungeon] 보상 : {string.Join(", ", result.RewardCurrencies.ConvertAll(c => $"{c.Type} +{c.Amount}"))} / Exp +{result.RewardExp} / 아이템 {result.RewardItems.Count}종");

        record.ClearCount++;
        record.FirstRewardClaimed = true;
        result.ClearCount = record.ClearCount;
        SaveRecord();

        Debug.Log($"[Dungeon] 클리어 : {dungeon.DungeonName} ( {(isFirstClear ? "최초" : "반복")} 보상 / 누적 {record.ClearCount}회 )");
        FinishDungeon(result);
        OnDungeonCleared?.Invoke(result);
    }

    /// <summary>
    /// 보상 지급 ( 재화 / 경험치 / 아이템 / 랜덤 테이블 )
    /// </summary>
    private void GiveReward(DungeonReward reward, DungeonResult result)
    {
        if (reward == null || player == null) return;

        if (player.Wallet != null)
        {
            foreach (CurrencyAmount currency in reward.Currencies)
            {
                player.Wallet.Add(currency.Type, currency.Amount);
                result.RewardCurrencies.Add(currency);
            }
        }

        if (reward.Exp > 0f && player.Progression != null)
        {
            player.Progression.AddExp(reward.Exp);
            result.RewardExp = reward.Exp;
        }

        foreach (DungeonItemAmount item in reward.Items)
        {
            AddItem(item.Item, item.Count, result.RewardItems);
        }

        if (reward.DropTable != null)
        {
            foreach (ItemDrop drop in reward.DropTable.Roll(reward.DropGroupID))
            {
                AddItem(drop.Item, drop.Count, result.RewardItems);
            }
        }
    }
    #endregion

    #region 실패
    private void OnPlayerDied()
    {
        FailDungeon();
    }

    /// <summary>
    /// 던전 포기 ( 추후 UI 버튼 연결 ) - 실패 처리
    /// </summary>
    public void GiveUp()
    {
        FailDungeon();
    }

    /// <summary>
    /// 던전 실패 - 클리어 기록이 없을 때만 입장 아이템 일부 환급
    /// </summary>
    private void FailDungeon()
    {
        if (state != DungeonState.InProgress) return;
        state = DungeonState.Failed;

        DungeonResult result = CreateResult();
        result.ClearCount = record.ClearCount;

        if (!record.HasCleared)
        {
            foreach (DungeonItemAmount consumed in DungeonSession.ConsumedItems)
            {
                int refund = Mathf.FloorToInt(consumed.Count * dungeon.FailRefundRatio);
                AddItem(consumed.Item, refund, result.RefundItems);
            }
        }

        Debug.Log($"[Dungeon] 실패 : {dungeon.DungeonName} ( 환급 {result.RefundItems.Count}종 )");
        FinishDungeon(result);
        OnDungeonFailed?.Invoke(result);
    }
    #endregion

    #region 종료 / 퇴장
    private DungeonResult CreateResult()
    {
        return new DungeonResult { Dungeon = dungeon, State = state };
    }

    private void FinishDungeon(DungeonResult result)
    {
        LastResult = result;
        UnsubscribeAllEnemies();
        DungeonSession.End();

        if (autoExit)
        {
            StartCoroutine(AutoExitRoutine());
        }
    }

    private IEnumerator AutoExitRoutine()
    {
        yield return new WaitForSeconds(autoExitDelay);
        ExitDungeon();
    }

    /// <summary>
    /// 던전 퇴장 - 플레이어 상태(보상 포함) 저장 후 영지 씬으로
    /// </summary>
    public void ExitDungeon()
    {
        if (state == DungeonState.InProgress)
        {
            //진행 중 퇴장 = 포기
            FailDungeon();
        }

        if (player != null && SaveManager.Instance != null)
        {
            PlayerSaveData saveData = PlayerSaveData.Save(player);

            //TODO KJ - 던전 사망 시 부활 규칙 기획 미정. 임시로 최대 체력으로 복귀
            if (player.Vitals != null && player.Vitals.IsDead)
            {
                saveData.HP = saveData.MaxHp;
            }

            SaveManager.Instance.Save(saveData);
        }

        SceneManager.LoadSceneAsync(returnSceneName);
    }

    private void SaveRecord()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveDungeon(dungeonSaveData);
        }
    }

    private void AddItem(InvenItemData item, int count, List<DungeonItemAmount> resultList)
    {
        if (item == null || count <= 0) return;

        int left = DungeonInventoryBridge.Add(inventory, item, count);
        if (left > 0)
        {
            //TODO KJ - 인벤토리 가득 찼을 때 처리 ( 우편함 / 바닥 드롭 등 ) 미정
            Debug.LogWarning($"[Dungeon] 인벤토리 부족 - {item.ItemName} {left}개 지급 못함");
        }

        if (count - left > 0)
        {
            resultList.Add(new DungeonItemAmount { Item = item, Count = count - left });
        }
    }
    #endregion
}
