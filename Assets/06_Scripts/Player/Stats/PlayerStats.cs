using System;
using UnityEngine;

// 플레이어 스탯의 MonoBehaviour 껍데기.
// 기본값은 PlayerContext가 물고 있는 PlayerBaseData(SO)에서 읽고, 스킬/장비/레벨업은 Modifier로 얹는다.
// 스탯이 바뀌면 OnStatsChanged 이벤트만 방송 -> 다른 부품은 값을 읽기만 한다.
public class PlayerStats : MonoBehaviour, IPlayerComponent
{
    // 스탯이 재계산될 때마다 방송. 구독자는 필요한 값을 다시 읽어가면 된다.
    public event Action OnStatsChanged;

    private readonly StatCalculator calculator = new StatCalculator();

    public float MoveSpeed => calculator.GetFinalValue(StatType.MoveSpeed);
    public float AttackPower => calculator.GetFinalValue(StatType.AttackPower);
    public float AttackRange => calculator.GetFinalValue(StatType.AttackRange);
    public float MaxHp => calculator.GetFinalValue(StatType.MaxHp);

    public void Initialize(PlayerContext context)
    {
        PlayerBaseData data = context.BaseData;
        if (data != null)
        {
            calculator.SetBase(StatType.MoveSpeed, data.MoveSpeed);
            calculator.SetBase(StatType.AttackPower, data.AttackPower);
            calculator.SetBase(StatType.AttackRange, data.AttackRange);
            calculator.SetBase(StatType.MaxHp, data.MaxHp);
        }
        else
        {
            Debug.LogWarning("PlayerStats: PlayerContext에 기본 데이터(SO)가 없습니다. PlayerContext 인스펙터에 PlayerBaseData를 연결하세요.");
        }
        OnStatsChanged?.Invoke();
    }

    public void AddModifier(StatModifier modifier)
    {
        calculator.AddModifier(modifier);
        OnStatsChanged?.Invoke();
    }

    public void RemoveModifier(StatModifier modifier)
    {
        calculator.RemoveModifier(modifier);
        OnStatsChanged?.Invoke();
    }
}
