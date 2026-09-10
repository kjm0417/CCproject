using System;
using UnityEngine;

// 경험치/레벨/스킬포인트 (문서 2.3). 플레이어 개인 육성 데이터라 플레이어가 소유한다.
// 레벨업 시 "어떤 스탯이 오를지"는 여기서 정하지 않는다. OnLevelUp만 발행하고,
// 실제 스탯 성장은 나중에 만들 데이터 시스템이 이 이벤트를 구독해 적용한다. (스탯과 분리)
public class PlayerProgression : MonoBehaviour, IPlayerComponent
{
    [Header("필요 경험치 공식: baseExp * (level ^ exponent)")]
    [SerializeField]
    private int baseExp = 100;      // 1 -> 2 레벨에 필요한 기준치
    [SerializeField]
    private float exponent = 1.5f;  // 레벨이 오를수록 필요치가 커지는 정도

    [Header("레벨업 보상")]
    [SerializeField]
    private int skillPointsPerLevel = 1;

    public event Action OnExpChanged;
    public event Action<int> OnLevelUp;
    public event Action OnSkillPointsChanged;

    private int level = 1;
    private int expInLevel;
    private int skillPoints;

    public int Level => level;
    public int ExpInLevel => expInLevel;
    public int ExpToNext => RequiredExp(level);
    public float ExpPercent01 => Mathf.Clamp01((float)expInLevel / RequiredExp(level));
    public int SkillPoints => skillPoints;

    public void Initialize(PlayerContext context)
    {
        // 시작 레벨은 기본 지급 데이터(SO)에서 가져온다. 세이브가 생기면 여기서 불러오기.
        if (context.BaseData != null)
        {
            level = Mathf.Max(1, context.BaseData.StartingLevel);
        }
        OnExpChanged?.Invoke();
    }

    // 경험치 획득 (몬스터 처치/수확/제작 등이 호출 - 문서 2.3). 한 번에 여러 레벨도 처리.
    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        expInLevel += amount;

        int need = RequiredExp(level);
        while (need > 0 && expInLevel >= need)
        {
            expInLevel -= need;
            level++;
            skillPoints += skillPointsPerLevel;
            OnLevelUp?.Invoke(level);
            OnSkillPointsChanged?.Invoke();
            need = RequiredExp(level);
        }

        OnExpChanged?.Invoke();
    }

    public bool TrySpendSkillPoints(int amount)
    {
        if (amount <= 0 || skillPoints < amount) return false;
        skillPoints -= amount;
        OnSkillPointsChanged?.Invoke();
        return true;
    }

    private int RequiredExp(int forLevel)
    {
        return Mathf.RoundToInt(baseExp * Mathf.Pow(forLevel, exponent));
    }
}
