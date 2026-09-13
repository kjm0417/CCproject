using UnityEngine;

// 애니메이터 파라미터와 상태 이름을 한 곳에서 관리한다.
public static class PlayerAnimHash
{
    public static readonly int Moving = Animator.StringToHash("Moving");
    public static readonly int PlayerAxe = Animator.StringToHash("Player_Axe");
    public static readonly int PlayerPick = Animator.StringToHash("Player_Pick");
}