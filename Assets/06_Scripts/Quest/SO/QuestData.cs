using UnityEngine;

public enum QuestTargetType
{
    GatherResource, //자원 채집 (벌목 / 채광)
    KillEnemy,      //몬스터 처치
}

/// <summary>
/// 퀘스트 데이터 (완료 조건 + 보상 + 지급 횟수)
/// </summary>
[CreateAssetMenu(fileName = "QuestData", menuName = "Scriptable Objects/QuestData")]
public class QuestData : ScriptableObject
{
    public string QuestID; //퀘스트 ID (저장 키)
    public string QuestName; //퀘스트명
    public string ConditionText; //완료 조건 문구 (예: 참나무 벌목)
    public Sprite Icon; //완료 조건 아이콘

    public QuestTargetType TargetType; //완료 조건 종류

    [Tooltip("채집할 자원 prefab 또는 처치할 몬스터 prefab")]
    public GameObject TargetPrefab;

    [Min(1)]
    public int RequiredCount = 1; //완료 조건 수량

    public int RewardGold; //퀘스트 보상 골드

    [Min(1), Tooltip("보상 지급 횟수 (1 = 1회, 2 이상 = 반복)")]
    public int MaxPayCount = 1;

    public bool IsRepeatable => MaxPayCount > 1;
}
