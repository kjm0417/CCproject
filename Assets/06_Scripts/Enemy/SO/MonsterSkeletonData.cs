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

    [Header("스킬 이펙트")]
    [Tooltip("스킬 공격 시 생성되는 이펙트 (닿은 플레이어 피격)")]
    public SkeletonSkillFX SkillFXPrefab;
    [Tooltip("Skill 애니메이션 진행도(0~1) 중 이펙트 생성 시점 - 뼈다귀 내려치는 프레임(6프레임 중 5번째)")]
    [Range(0f, 1f)]
    public float SkillFXSpawnTime = 0.66f;
    [Tooltip("스켈레톤 기준 이펙트 생성 위치 - 왼쪽 바라볼 때 기준, 오른쪽이면 x 반전")]
    public Vector2 SkillFXOffset = new Vector2(-0.6f, 0f);
}
