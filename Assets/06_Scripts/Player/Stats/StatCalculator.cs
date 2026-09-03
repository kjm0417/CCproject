using System.Collections.Generic;

// 스탯 하나에 대한 변경치. 스킬트리 노드 하나 = StatModifier 하나로 표현된다.
// (예: 이동속도 +0.5 스킬 -> new StatModifier(StatType.MoveSpeed, 0.5f))
// StatType 정의는 Core/PlayerEnums.cs 참고.
public struct StatModifier
{
    public StatType Type;
    public float Value;

    public StatModifier(StatType type, float value)
    {
        Type = type;
        Value = value;
    }
}

// 순수 C# 스탯 계산기. Unity를 전혀 모른다 -> 단위 테스트가 쉽다.
// 기본값 + 해당 종류의 모든 Modifier 합산으로 최종 스탯을 계산한다.
public class StatCalculator
{
    private readonly Dictionary<StatType, float> baseValues = new Dictionary<StatType, float>();
    private readonly List<StatModifier> modifiers = new List<StatModifier>();

    // 기본 스탯값 설정. 인스펙터의 기초 수치가 여기로 들어온다.
    public void SetBase(StatType type, float value)
    {
        baseValues[type] = value;
    }

    // Modifier 추가. 스킬트리/장비가 호출한다.
    public void AddModifier(StatModifier modifier)
    {
        modifiers.Add(modifier);
    }

    // Modifier 제거. 스킬 해제/장비 탈착 시 호출한다.
    public bool RemoveModifier(StatModifier modifier)
    {
        return modifiers.Remove(modifier);
    }

    // 모든 Modifier 초기화
    public void ClearModifiers()
    {
        modifiers.Clear();
    }

    // 기본값 + 해당 종류의 모든 Modifier 합산 = 최종값
    public float GetFinalValue(StatType type)
    {
        baseValues.TryGetValue(type, out float result);
        for (int i = 0; i < modifiers.Count; i++)
        {
            if (modifiers[i].Type == type)
            {
                result += modifiers[i].Value;
            }
        }
        return result;
    }
}
