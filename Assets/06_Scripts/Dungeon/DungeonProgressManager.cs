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

    public DungeonData Dungeon => dungeon;
    public DungeonState State => state;
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

        dungeonSaveData = SaveManager.Instance != null ? SaveManager.Instance.LoadDungeon() : new DungeonSaveData();
        record = dungeonSaveData.GetOrCreate(dungeon.DungeonID);

        state = DungeonState.InProgress;
        Debug.Log($"[Dungeon] 시작 : {dungeon.DungeonName} ( 클리어 기록 {record.ClearCount}회 )");
        OnDungeonStarted?.Invoke(dungeon);

        //TODO KJ - 스테이지 진행 시작
    }
    #endregion

    #region 클리어
    /// <summary>
    /// 던전 클리어 - 최초 클리어면 최초 보상, 이후는 반복 보상
    /// 스테이지 진행 로직에서 클리어 조건 달성 시 호출
    /// </summary>
    public void ClearDungeon()
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
