using UnityEngine;

[CreateAssetMenu(fileName = "MonsterSkeletonData", menuName = "Scriptable Objects/MonsterSkeletonData")]
public class MonsterSkeletonData : ScriptableObject
{
    [Tooltip("Idle 상태에서 해당 변수의 시간이 지나고 Walk 상태 전환")]
    public float IdleDuration = 3f;
    [Tooltip("Walk 상태에서 해당 변수의 시간이 지나고 Idle 상태 전환")]
    public float WalkDruation = 3f;

    [Tooltip("슬라임 돌진 시간")]
    public float ChargeDuration = 1.0f;
    [Tooltip("슬라임 돌진 속도")]
    public float ChargeSpeed = 3f;
    [Tooltip("슬라임 걷는 속도")]
    public float WalkSpeed = 2f;

    [Tooltip("슬라임 기본 공격 쿨타임")]
    public float BaseAttackCooldown = 3f;
    [Tooltip("슬라임 스킬 공격 쿨타임")]
    public float SkillAttackCooldown = 6f;

    [Tooltip("좌/우 왔다갔다 하는 거리 ")]
    public float PatrolDistance = 3f;

    [Tooltip("슬라임 스킬 공격 후 기절 시간")]
    public float StunDuration = 2f;
}
