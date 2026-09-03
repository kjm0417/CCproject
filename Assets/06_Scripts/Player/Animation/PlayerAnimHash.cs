using UnityEngine;

// 애니메이터 파라미터 해시를 한 곳에 모아둔다.
// StringToHash를 시작 시 한 번만 계산해 보관 -> 문자열 대신 int로 접근(오타 방지 + 조회 비용 제거).
// 파라미터 이름이 바뀌면 여기 한 줄만 고치면 된다.
public static class PlayerAnimHash
{
    // 걷는 중 여부 (Bool)
    public static readonly int Moving = Animator.StringToHash("Moving");
    // 바라보는 방향 X (Float, 블렌드트리용)
    public static readonly int MoveX = Animator.StringToHash("MoveX");
    // 바라보는 방향 Y (Float, 블렌드트리용)
    public static readonly int MoveY = Animator.StringToHash("MoveY");
}
