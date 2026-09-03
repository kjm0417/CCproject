using UnityEngine;

// 이동 상태를 읽어 애니메이터에 방향/이동 여부를 넘기는 부품. "보이는 것"만 담당
[RequireComponent(typeof(Animator))]
public class PlayerAnimation : MonoBehaviour, IPlayerComponent
{
    private Animator animator;
    private PlayerMovement movement;

    // 마지막으로 바라본 방향. 멈춰 있을 때 이 방향의 idle을 유지한다.
    private FacingDirection facing = FacingDirection.Down;

    // 애니메이터에 실제로 있는 파라미터만 세팅하기 위한 플래그
    private bool hasMoving;
    private bool hasMoveX;
    private bool hasMoveY;

    // 바깥(공격/상호작용 등)에서 현재 바라보는 방향을 재사용할 수 있게 노출
    public FacingDirection Facing => facing;

    // PlayerContext가 호출. 참조를 캐싱하고 애니메이터에 어떤 파라미터가 있는지 확인
    public void Initialize(PlayerContext context)
    {
        animator = GetComponent<Animator>();
        movement = context.Movement;

        // 현재 애니메이터 컨트롤러에 존재하는 파라미터만 캐싱
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.nameHash == PlayerAnimHash.Moving) hasMoving = true;
            else if (p.nameHash == PlayerAnimHash.MoveX) hasMoveX = true;
            else if (p.nameHash == PlayerAnimHash.MoveY) hasMoveY = true;
        }
    }

    // 이동 결과를 읽어 반영하는 것이라, 이동(FixedUpdate) 이후인 LateUpdate에서 처리한다.
    private void LateUpdate()
    {
        if (movement == null || animator == null) return;

        bool moving = movement.IsMoving;

        // 움직일 때만 방향을 갱신한다. 멈추면 마지막 방향을 유지(그 방향 idle).
        if (moving)
        {
            UpdateFacing(movement.MoveInput);
        }

        if (hasMoving) animator.SetBool(PlayerAnimHash.Moving, moving);

        // 스타듀밸리식: x/y 중 더 빠른 축이 방향을 결정 -> 한 축만 값이 들어간 단위 벡터.
        Vector2 dir = FacingToVector(facing);
        if (hasMoveX) animator.SetFloat(PlayerAnimHash.MoveX, dir.x);
        if (hasMoveY) animator.SetFloat(PlayerAnimHash.MoveY, dir.y);
    }

    // 입력에서 바라보는 방향 결정. |y| > |x| 면 상/하, 아니면(정확히 같을 때 포함) 좌/우.
    private void UpdateFacing(Vector2 input)
    {
        if (Mathf.Abs(input.y) > Mathf.Abs(input.x))
        {
            facing = input.y > 0f ? FacingDirection.Up : FacingDirection.Down;
        }
        else
        {
            facing = input.x > 0f ? FacingDirection.Right : FacingDirection.Left;
        }
    }

    // 방향 enum -> 애니메이터 블렌드용 단위 벡터 (한 축만 +-1)
    private Vector2 FacingToVector(FacingDirection d)
    {
        switch (d)
        {
            case FacingDirection.Up: return new Vector2(0f, 1f);
            case FacingDirection.Left: return new Vector2(-1f, 0f);
            case FacingDirection.Right: return new Vector2(1f, 0f);
            default: return new Vector2(0f, -1f); // Down
        }
    }
}
