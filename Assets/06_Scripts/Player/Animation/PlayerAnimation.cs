using UnityEngine;

// 이동 상태를 읽어 애니메이터/스프라이트에 반영하는 부품. "보이는 것"만 담당.
// 애니메이션은 좌우만 구분한다(확정). 상하로도 움직이지만 상하 전용 애니메이션은 없으므로,
// 세로 이동 중에는 마지막 좌우 방향을 유지한다. 좌우 구분은 스프라이트 X 뒤집기로 처리.
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimation : MonoBehaviour, IPlayerComponent
{
    [Header("스프라이트 원본이 오른쪽을 보고 있으면 true")]
    [SerializeField]
    private bool spriteDefaultFacesRight = true;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerMovement movement;

    private bool hasMoving; // 애니메이터에 Moving 파라미터가 있을 때만 세팅(경고 스팸 방지)
    private bool facingRight = true; // 마지막 좌우 방향 (세로 이동 시 유지)

    // 바깥(공격/상호작용 등)에서 좌우 방향을 재사용할 수 있게 노출
    public bool FacingRight => facingRight;

    public void Initialize(PlayerContext context)
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        movement = context.Movement;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.nameHash == PlayerAnimHash.Moving) hasMoving = true;
        }
    }

    // 이동(FixedUpdate) 이후인 LateUpdate에서 표현을 반영한다.
    private void LateUpdate()
    {
        if (movement == null || animator == null) return;

        bool moving = movement.IsMoving;

        // 좌우 입력이 있을 때만 방향 갱신. 순수 세로 이동(x=0)이면 마지막 좌우 방향 유지.
        float x = movement.MoveInput.x;
        if (moving && Mathf.Abs(x) > 0.0001f)
        {
            facingRight = x > 0f;
        }

        // 좌우는 스프라이트 뒤집기로 표현 (상하는 방향 표현 없음)
        spriteRenderer.flipX = (facingRight != spriteDefaultFacesRight);

        // 걷기/정지 전환
        if (hasMoving) animator.SetBool(PlayerAnimHash.Moving, moving);
    }
}
