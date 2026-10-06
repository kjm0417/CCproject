using UnityEngine;

/// <summary>
/// 보스 공통 수치 데이터
/// 보스마다 에셋 하나씩 생성해서 사용
/// </summary>
[CreateAssetMenu(fileName = "BossData", menuName = "Boss/BossData")]
public class BossData : ScriptableObject
{
    [Header("기본")]
    public string bossName;
    [Min(1)] public int maxHp = 1000;

    [Header("피격")]
    [Tooltip("true 면 스턴 상태일 때만 피해를 받음 ( 기믹 -> 스턴 -> 공격타임 구조 )")]
    public bool invulnerableUnlessStunned = true;
    [Tooltip("스턴 중 받는 피해 배율")]
    [Min(0f)] public float stunDamageMultiplier = 1f;

    [Header("패턴")]
    [Tooltip("직전 패턴 연속 사용 방지")]
    public bool avoidRepeatPattern = true;
    [Tooltip("패턴 사이 대기 시간 ( 최소 / 최대 )")]
    public Vector2 idleIntervalRange = new Vector2(1.5f, 2.5f);
}
