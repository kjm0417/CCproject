using UnityEngine;

/// <summary>
/// 적 공통 - 애님 Hash 최적화
/// </summary>
public class EnemyAnimHash
{
    /// <summary>
    /// Idle 전환 
    /// </summary>
    public static int IdleTrigger = Animator.StringToHash("IdleTrigger");

    /// <summary>
    /// Walk 전환 ( true : 걷기, false : Idle )
    /// </summary>
    public static int IsWalk = Animator.StringToHash("IsWalk");

    /// <summary>
    /// Charge 전환 ( true : 추격 , false : Idle )
    /// </summary>
    public static int IsCharge = Animator.StringToHash("IsCharge");

    /// <summary>
    /// 기본 Attack 전환 ( 전환 시, Exit 애님 노드에 의해 Spawn -> Idle 전환이 될 것 임)
    /// </summary>
    public static int BaseAttackTrigger = Animator.StringToHash("BaseAttackTrigger");

    /// <summary>
    /// Spawn 초기 애니메이션  (초기에 true 시키고, Idle 상태 진행 시 false )
    /// </summary>
    public static int Spawn = Animator.StringToHash("Spawn");


    /// <summary>
    /// Hit 클립 이름 자체
    /// </summary>
    public static int Hit = Animator.StringToHash("Hit");


    /// <summary>
    /// Die
    /// </summary>
    public static int DieTrigger = Animator.StringToHash("DieTrigger");

    /// <summary>
    /// Skill -> Exit
    /// </summary>
    public static int SkillTrigger = Animator.StringToHash("SkillTrigger");

    /// <summary>
    /// Skill 
    /// </summary>
    public static int Skill = Animator.StringToHash("Skill");


    /// <summary>
    /// Stun 클립 이름 자체
    /// </summary>
    public static int Stun = Animator.StringToHash("Stun");

    /// <summary>
    /// 스턴 유지 ( true : 스턴 반복, false : Idle )
    /// </summary>
    public static int IsStun = Animator.StringToHash("IsStun");
}
