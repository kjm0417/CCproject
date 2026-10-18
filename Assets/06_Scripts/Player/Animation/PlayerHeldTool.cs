using System;
using UnityEngine;

/// <summary>
/// 플레이어 손에 든 도구 표시.
/// 상호작용 버튼에 도구가 표시되면 HUD가 Show로 같은 도구 스프라이트를 넘기고, 도구가 없으면 Hide.
/// 현재 플레이어 스프라이트 ( 대기 / 걷기 프레임 ) 별 손 위치 / 각도를 따라가고, 바라보는 방향에 맞춰 좌우 반전.
/// 도끼 / 곡괭이 모션처럼 도구가 그려진 애니메이션 중에는 숨긴다.
/// </summary>
public class PlayerHeldTool : MonoBehaviour, IPlayerComponent
{
    /// <summary>
    /// 플레이어 스프라이트 1장에 대한 손 자세 ( 오른쪽을 볼 때 기준 )
    /// </summary>
    [Serializable]
    public class HandPose
    {
        [Tooltip("플레이어 스프라이트 이름 ( 예: Walking_1_0 )")]
        public string spriteName;
        [Tooltip("손 위치 ( 플레이어 스프라이트 로컬 좌표, 중심 기준 )")]
        public Vector2 handPosition;
        [Tooltip("도구 회전 ( 도 단위, + 반시계 )")]
        public float angle;
        [Tooltip("팔이 몸 뒤로 갔을 때 도구를 몸 뒤에 그림")]
        public bool behindBody;

        public HandPose(string spriteName, Vector2 handPosition, float angle, bool behindBody = false)
        {
            this.spriteName = spriteName;
            this.handPosition = handPosition;
            this.angle = angle;
            this.behindBody = behindBody;
        }
    }

    [Header("표시")]
    [SerializeField, Tooltip("도구를 그릴 SpriteRenderer. 비우면 PlayerAnim 자식에 HeldTool 오브젝트를 자동 생성")]
    private SpriteRenderer toolRenderer;
    [SerializeField, Tooltip("도구 긴 변 길이 ( 플레이어 스프라이트 로컬 단위 ) - 아이콘 원본 크기가 달라도 같은 크기로 맞춤")]
    private float toolSize = 1.5f;
    [SerializeField, Tooltip("손으로 잡는 지점 ( 도구 아이콘 크기 대비 비율, 0 = 중심 / -0.5 = 왼쪽 아래 끝 )")]
    private Vector2 gripPointRatio = new Vector2(-0.33f, -0.33f);
    [SerializeField, Tooltip("플레이어 스프라이트보다 앞에 그릴 정렬 순서 차이")]
    private int sortingOrderOffset = 1;
    [SerializeField, Tooltip("도구 아이콘 원본이 오른쪽을 향하면 true")]
    private bool toolSpriteFacesRight = true;

    [Header("손 자세 ( 스프라이트별 )")]
    [SerializeField, Tooltip("목록에 없는 스프라이트일 때 사용할 자세")]
    private HandPose defaultPose = new HandPose("", new Vector2(-0.25f, -1.0f), 0f);
    [SerializeField, Tooltip("바라보는 방향 쪽 손 ( 오른쪽을 볼 때 오른손 = 화면 왼쪽 팔 ) 기준")]
    private HandPose[] handPoses =
    {
        new HandPose("Idle_1_0", new Vector2(-0.21f, -1.02f), 0f),
        new HandPose("Idle_2_0", new Vector2(-0.185f, -0.985f), 0f),
        new HandPose("Walking_1_0", new Vector2(-0.775f, -0.82f), 20f),
        new HandPose("Walking_2_0", new Vector2(-0.325f, -1.02f), 0f),
        new HandPose("Walking_3_0", new Vector2(0.055f, -0.74f), -20f),
        new HandPose("Walking_4_0", new Vector2(-0.325f, -1.02f), 0f),
    };

    private PlayerAnimation playerAnimation;
    private SpriteRenderer bodyRenderer;
    private Sprite heldSprite;
    private Sprite lastBodySprite;
    private HandPose currentPose;

    public bool IsShowing => heldSprite != null;

    public void Initialize(PlayerContext context)
    {
        playerAnimation = context.Animation;
        bodyRenderer = playerAnimation != null
            ? playerAnimation.GetComponent<SpriteRenderer>()
            : context.SpriteRenderer;

        if (toolRenderer == null)
        {
            Transform parent = bodyRenderer != null ? bodyRenderer.transform : transform;
            GameObject toolObject = new GameObject("HeldTool");
            toolObject.transform.SetParent(parent, false);
            toolObject.layer = parent.gameObject.layer;
            toolRenderer = toolObject.AddComponent<SpriteRenderer>();
        }

        Hide();
    }

    public void Show(Sprite toolSprite)
    {
        if (toolRenderer == null) return;
        if (toolSprite == null)
        {
            Hide();
            return;
        }

        heldSprite = toolSprite;
        toolRenderer.sprite = toolSprite;
        ApplySize();
        ApplyPose();
    }

    public void Hide()
    {
        heldSprite = null;
        if (toolRenderer == null) return;

        toolRenderer.sprite = null;
        toolRenderer.enabled = false;
    }

    private void LateUpdate()
    {
        if (!IsShowing) return;
        ApplyPose();
    }

    /// <summary>
    /// 아이콘 원본 크기와 상관없이 긴 변이 toolSize 가 되도록 배율 조정
    /// </summary>
    private void ApplySize()
    {
        Vector2 spriteSize = toolRenderer.sprite.bounds.size;
        float longSide = Mathf.Max(spriteSize.x, spriteSize.y);
        if (longSide <= 0f) return;

        toolRenderer.transform.localScale = Vector3.one * (toolSize / longSide);
    }

    private void ApplyPose()
    {
        // 도끼 / 곡괭이 모션은 스프라이트에 도구가 그려져 있으므로 숨김
        bool hideForMotion = playerAnimation != null && playerAnimation.IsPlayingToolInteraction;
        toolRenderer.enabled = !hideForMotion;
        if (hideForMotion) return;

        HandPose pose = GetPose();
        bool facingRight = playerAnimation == null || playerAnimation.FacingRight;
        float side = facingRight ? 1f : -1f;

        toolRenderer.flipX = facingRight != toolSpriteFacesRight;

        // 잡는 지점이 손 위치에 오도록 도구 중심을 옮김 ( 회전 / 반전 반영 )
        float angle = pose.angle * side;
        Vector2 grip = Vector2.Scale((Vector2)toolRenderer.sprite.bounds.size, gripPointRatio) * toolRenderer.transform.localScale.x;
        grip.x *= side;
        Vector2 rotatedGrip = Quaternion.Euler(0f, 0f, angle) * grip;
        Vector2 hand = new Vector2(pose.handPosition.x * side, pose.handPosition.y);

        toolRenderer.transform.localPosition = hand - rotatedGrip;
        toolRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        if (bodyRenderer != null)
        {
            toolRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
            toolRenderer.sortingOrder = bodyRenderer.sortingOrder + (pose.behindBody ? -sortingOrderOffset : sortingOrderOffset);
        }
    }

    private HandPose GetPose()
    {
        Sprite bodySprite = bodyRenderer != null ? bodyRenderer.sprite : null;
        if (bodySprite == lastBodySprite && currentPose != null) return currentPose;

        lastBodySprite = bodySprite;
        currentPose = defaultPose;
        if (bodySprite == null) return currentPose;

        for (int i = 0; i < handPoses.Length; i++)
        {
            if (handPoses[i].spriteName == bodySprite.name)
            {
                currentPose = handPoses[i];
                break;
            }
        }

        return currentPose;
    }
}
