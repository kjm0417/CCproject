using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimation : MonoBehaviour, IPlayerComponent
{
    [Header("스프라이트 방향")]
    [SerializeField]
    private bool spriteDefaultFacesRight = true;

    [Header("상호작용 애니메이션")]
    [SerializeField]
    private int farmingLayerIndex = 1;

    [SerializeField]
    private float interactionLayerHoldTime = 0.45f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerMovement movement;
    private Coroutine interactionRoutine;

    private bool hasMoving;
    private bool facingRight = true;

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

    public void PlayToolInteraction(ToolType toolType)
    {
        if (animator == null) return;

        int stateHash = GetToolStateHash(toolType);
        if (stateHash == 0) return;

        if (interactionRoutine != null)
        {
            StopCoroutine(interactionRoutine);
        }

        interactionRoutine = StartCoroutine(PlayToolInteractionRoutine(stateHash));
    }

    private void LateUpdate()
    {
        if (movement == null || animator == null) return;

        bool moving = movement.IsMoving;
        float x = movement.MoveInput.x;
        if (moving && Mathf.Abs(x) > 0.0001f)
        {
            facingRight = x > 0f;
        }

        spriteRenderer.flipX = facingRight != spriteDefaultFacesRight;

        if (hasMoving) animator.SetBool(PlayerAnimHash.Moving, moving);
    }

    private IEnumerator PlayToolInteractionRoutine(int stateHash)
    {
        if (farmingLayerIndex < 0 || farmingLayerIndex >= animator.layerCount)
        {
            animator.Play(stateHash, 0, 0f);
            yield break;
        }

        animator.SetLayerWeight(farmingLayerIndex, 1f);
        animator.Play(stateHash, farmingLayerIndex, 0f);

        yield return new WaitForSeconds(interactionLayerHoldTime);

        animator.SetLayerWeight(farmingLayerIndex, 0f);
        interactionRoutine = null;
    }

    private int GetToolStateHash(ToolType toolType)
    {
        switch (toolType)
        {
            case ToolType.Axe:
                return PlayerAnimHash.PlayerAxe;
            case ToolType.Pickaxe:
                return PlayerAnimHash.PlayerPick;
            default:
                return 0;
        }
    }
}