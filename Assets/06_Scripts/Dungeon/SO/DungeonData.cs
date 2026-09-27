using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 던전 1개의 정의 데이터 ( 난이도 구분 없음 / 선형 진행 / 보스 처치 시 클리어 )
/// </summary>
[CreateAssetMenu(fileName = "DungeonData", menuName = "Game/DungeonData")]
public class DungeonData : ScriptableObject
{
    public string DungeonID;
    public string DungeonName;
    public string SceneName; //던전 씬 이름

    [Header("입장 시 제출(소모) 아이템 - 없으면 비워둠")]
    public List<DungeonItemAmount> EntryCosts = new List<DungeonItemAmount>();

    [Header("클리어 실패 시 입장 아이템 환급 비율 ( 0 ~ 1 )")]
    [Tooltip("TODO KJ - 환급 비율 / 대상 아이템 범위 추후 확정")]
    [Range(0f, 1f)]
    public float FailRefundRatio = 0.5f;

    [Header("최초 클리어 보상 ( 1회만 지급 )")]
    public DungeonReward FirstClearReward = new DungeonReward();

    [Header("반복 클리어 보상")]
    public DungeonReward RepeatClearReward = new DungeonReward();
}

/// <summary>
/// 아이템 + 개수
/// </summary>
[System.Serializable]
public class DungeonItemAmount
{
    public InvenItemData Item;
    public int Count = 1;
}

/// <summary>
/// 던전 클리어 보상 ( 골드/경험치/아이템 구체 수치는 추후 추가 예정 )
/// </summary>
[System.Serializable]
public class DungeonReward
{
    public List<CurrencyAmount> Currencies = new List<CurrencyAmount>(); //재화 또는 다이아를 얼만큼 줄건지
    public float Exp; //경험치를 얼만큼?
    public List<DungeonItemAmount> Items = new List<DungeonItemAmount>(); //아이템과 해당 아이템의 개수는 얼만큼 줄건지?

    [Header("랜덤 보상 테이블 ( 선택 )")]
    public DropTableData DropTable;
    public int DropGroupID;
}
