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
/// 던전 결과 ( 추후 결과 UI에서 사용 )
/// </summary>
public class DungeonResult
{
    public DungeonData Dungeon;
    public DungeonState State;
    public bool IsFirstClear;
    public int ClearCount;
    public float ClearTime; //클리어까지 걸린 시간 ( 초 )
    public bool RewardClaimed; //보상 수령 여부 ( 결과 UI 영지로 돌아가기 시 수령 )

    public List<CurrencyAmount> RewardCurrencies = new List<CurrencyAmount>();
    public float RewardExp;
    public List<DungeonItemAmount> RewardItems = new List<DungeonItemAmount>();

    public List<DungeonItemAmount> RefundItems = new List<DungeonItemAmount>();
}

/// <summary>
/// 던전 진행 관리자
/// 클리어 시 최초/반복 보상 지급 ( 스테이지 진행은 별도 구현 후 ClearDungeon 호출 )
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
    public event Action<DungeonResult> OnDungeonCleared;
    public event Action<DungeonResult> OnDungeonFailed;
    #endregion

    private DungeonData dungeon;
    private DungeonState state = DungeonState.Ready;

    private PlayerContext player;
    private PlayerInventory inventory;

    private DungeonSaveData dungeonSaveData;
    private DungeonRecordData record;

    private float startTime;
    private float clearTime;

    public DungeonData Dungeon => dungeon;
    public DungeonState State => state;
    public DungeonResult LastResult { get; private set; }

    /// <summary>
    /// 진행 시간 ( 초 ) - 클리어 후에는 클리어 시간으로 고정
    /// </summary>
    public float ElapsedTime
    {
        get
        {
            switch (state)
            {
                case DungeonState.InProgress: return Time.time - startTime;
                case DungeonState.Cleared: return clearTime;
                default: return 0f;
            }
        }
    }

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
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;

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

        player = FindAnyObjectByType<PlayerContext>();
        if (player != null)
        {
            inventory = player.GetComponentInChildren<PlayerInventory>();
            if (player.Vitals != null)
            {
                player.Vitals.OnDied += OnPlayerDied;
            }
        }

        //던전 씬을 바로 실행하면 SaveManager ( 영지 씬에서 생성 ) 가 없어 클리어 기록이 저장/로드되지 않음 -> 생성
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[Dungeon] SaveManager 없음 ( 던전 씬 직접 실행 ) - 임시 생성");
            new GameObject("SaveManager").AddComponent<SaveManager>();
        }

        dungeonSaveData = SaveManager.Instance.LoadDungeon();
        record = dungeonSaveData.GetOrCreate(dungeon.DungeonID);

        state = DungeonState.InProgress;
        startTime = Time.time;
        Debug.Log($"[Dungeon] 시작 : {dungeon.DungeonName} ( 클리어 기록 {record.ClearCount}회 )");
        OnDungeonStarted?.Invoke(dungeon);

        //TODO KJ - 스테이지 진행 시작
    }
    #endregion

    #region 클리어
    /// <summary>
    /// 던전 클리어 - 최초 클리어면 최초 보상, 이후는 반복 보상
    /// 보상은 결과 UI 에 먼저 표시하고, 영지로 돌아가기 ( ClaimRewardAndExit ) 시 지급
    /// </summary>
    public void ClearDungeon()
    {
        if (state != DungeonState.InProgress) return;
        clearTime = Time.time - startTime;
        state = DungeonState.Cleared;

        bool isFirstClear = !record.FirstRewardClaimed;
        DungeonReward reward = isFirstClear ? dungeon.FirstClearReward : dungeon.RepeatClearReward;

        DungeonResult result = CreateResult();
        result.IsFirstClear = isFirstClear;
        result.ClearTime = clearTime;
        result.ClearCount = record.ClearCount + 1;
        PrepareReward(reward, result);
        LastResult = result;

        Debug.Log($"[Dungeon] 클리어 : {dungeon.DungeonName} ( {(isFirstClear ? "최초" : "반복")} / {clearTime:0.0}초 )");

        //결과 UI 없으면 바로 지급 후 자동 퇴장
        if (OnDungeonCleared == null)
        {
            ClaimReward();
            FinishDungeon(result);
            return;
        }

        OnDungeonCleared.Invoke(result);
    }

    /// <summary>
    /// 결과 UI - 영지로 돌아가기 : 보상 수령 후 영지 씬으로
    /// </summary>
    public void ClaimRewardAndExit()
    {
        if (state != DungeonState.Cleared) return;

        ClaimReward();
        DungeonSession.End();
        ExitDungeon();
    }

    /// <summary>
    /// 지급할 보상 목록 확정 ( 재화 / 경험치 / 아이템 / 랜덤 테이블 ) - 아직 지급 X
    /// </summary>
    private void PrepareReward(DungeonReward reward, DungeonResult result)
    {
        if (reward == null) return;

        result.RewardCurrencies.AddRange(reward.Currencies);
        result.RewardExp = reward.Exp;

        foreach (DungeonItemAmount item in reward.Items)
        {
            if (item.Item == null || item.Count <= 0) continue;
            result.RewardItems.Add(new DungeonItemAmount { Item = item.Item, Count = item.Count });
        }

        if (reward.DropTable != null)
        {
            foreach (ItemDrop drop in reward.DropTable.Roll(reward.DropGroupID))
            {
                if (drop.Item == null || drop.Count <= 0) continue;
                result.RewardItems.Add(new DungeonItemAmount { Item = drop.Item, Count = drop.Count });
            }
        }
    }

    /// <summary>
    /// 확정된 보상 지급 + 클리어 기록 저장 ( 1회만 )
    /// </summary>
    private void ClaimReward()
    {
        DungeonResult result = LastResult;
        if (result == null || result.RewardClaimed) return;
        result.RewardClaimed = true;

        if (player != null)
        {
            if (player.Wallet != null)
            {
                foreach (CurrencyAmount currency in result.RewardCurrencies)
                {
                    player.Wallet.Add(currency.Type, currency.Amount);
                }
            }

            if (result.RewardExp > 0f && player.Progression != null)
            {
                player.Progression.AddExp(result.RewardExp);
            }

            foreach (DungeonItemAmount item in result.RewardItems)
            {
                int left = DungeonInventoryBridge.Add(inventory, item.Item, item.Count);
                if (left > 0)
                {
                    //TODO KJ - 인벤토리 가득 찼을 때 처리 ( 우편함 / 바닥 드롭 등 ) 미정
                    Debug.LogWarning($"[Dungeon] 인벤토리 부족 - {item.Item.ItemName} {left}개 지급 못함");
                }
            }
        }

        record.ClearCount++;
        record.FirstRewardClaimed = true;
        SaveRecord();

        Debug.Log($"[Dungeon] 보상 수령 : {string.Join(", ", result.RewardCurrencies.ConvertAll(c => $"{c.Type} +{c.Amount}"))} / Exp +{result.RewardExp} / 아이템 {result.RewardItems.Count}종 ( 누적 {record.ClearCount}회 )");
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
        else if (state == DungeonState.Cleared && LastResult != null && !LastResult.RewardClaimed)
        {
            //결과 UI 거치지 않고 퇴장해도 클리어 보상은 지급
            ClaimReward();
            DungeonSession.End();
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

        SceneFader.LoadScene(returnSceneName);
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
