using System;
using UnityEngine;

// 플레이어 스탯의 MonoBehaviour 껍데기.
// 인스펙터에서 기초 수치를 받고, 내부 StatCalculator로 최종값을 계산한다.
// 스탯이 바뀌면 OnStatsChanged 이벤트만 방송한다.
// -> 이동/전투 등 다른 부품은 이 값을 읽기만 하면 되고, 스킬트리를 알 필요가 없다.
public class PlayerStats : MonoBehaviour, IPlayerComponent
{
    [Header("기초 스탯 (스킬트리 적용 전 값)")]
    [SerializeField]
    private float baseMoveSpeed = 5f;
    [SerializeField]
    private float baseAttackPower = 10f;
    [SerializeField]
    private float baseAttackRange = 2f;
    [SerializeField]
    private float baseMaxHp = 100f;

    // 스탯이 재계산될 때마다 방송. 구독자는 필요한 값을 다시 읽어가면 된다.
    public event Action OnStatsChanged;

    private readonly StatCalculator calculator = new StatCalculator();

    // 최종 이동 속도
    public float MoveSpeed => calculator.GetFinalValue(StatType.MoveSpeed);
    // 최종 공격력
    public float AttackPower => calculator.GetFinalValue(StatType.AttackPower);
    // 최종 공격 범위
    public float AttackRange => calculator.GetFinalValue(StatType.AttackRange);
    // 최종 최대 체력
    public float MaxHp => calculator.GetFinalValue(StatType.MaxHp);

    // PlayerContext가 호출. 인스펙터 기초 수치를 계산기에 주입하고 첫 방송을 보낸다.
    public void Initialize(PlayerContext context)
    {
        calculator.SetBase(StatType.MoveSpeed, baseMoveSpeed);
        calculator.SetBase(StatType.AttackPower, baseAttackPower);
        calculator.SetBase(StatType.AttackRange, baseAttackRange);
        calculator.SetBase(StatType.MaxHp, baseMaxHp);
        OnStatsChanged?.Invoke();
    }

    // 스킬트리/장비가 스탯 Modifier를 추가할 때 호출. 추가 후 재계산 방송.
    public void AddModifier(StatModifier modifier)
    {
        calculator.AddModifier(modifier);
        OnStatsChanged?.Invoke();
    }

    // 스탯 Modifier 제거 후 재계산 방송
    public void RemoveModifier(StatModifier modifier)
    {
        calculator.RemoveModifier(modifier);
        OnStatsChanged?.Invoke();
    }
}
