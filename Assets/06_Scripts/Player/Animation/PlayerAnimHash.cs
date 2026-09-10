using UnityEngine;

// 애니메이터 파라미터 해시를 한 곳에 모아둔다.
// StringToHash를 시작 시 한 번만 계산해 보관 -> 문자열 대신 int로 접근(오타 방지 + 조회 비용 제거).
public static class PlayerAnimHash
{
    // 걷는 중 여부 (Bool)
    public static readonly int Moving = Animator.StringToHash("Moving");
}
