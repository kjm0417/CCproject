using System;
using UnityEngine;

/// <summary>
/// 던전 2층 층 발판
/// 플레이어가 N초간 밟으면 발판 활성화 -> 상호작용 버튼 활성화 -> 버튼 클릭 시 조건 1개 충족
/// 발에서 떼면 비활성화 ( 밟은 시간은 유지, 다시 밟으면 이어서 누적 )
/// </summary>
public class DungeonFootThold : DungeonGimmickObject
{
    [SerializeField, Tooltip("활성화까지 밟고 있어야 하는 시간 ( 초 )")]
    private float requiredTime = 3f;
    [SerializeField, Tooltip("플레이어 밟음 감지 범위")]
    private Vector2 stepSize = Vector2.one;
    [SerializeField, Tooltip("밟음 감지 레이어 ( 비우면 Player 레이어 )")]
    private LayerMask playerLayer;

    private Collider2D interactCollider; //본체 콜라이더 ( 활성화 상태일 때만 켜짐 -> 상호작용 버튼 노출 )
    private float stepTime; //누적 밟은 시간

    public bool IsActivated { get; private set; } //N초 밟아서 활성화된 상태 ( 떼면 해제 )
    public bool IsCompleted { get; private set; } //버튼 클릭까지 완료 ( 조건 충족 )
    public float Progress => requiredTime > 0f ? Mathf.Clamp01(stepTime / requiredTime) : 1f;

    /// <summary>
    /// 조건 충족 시 ( 버튼 클릭 )
    /// </summary>
    public event Action<DungeonFootThold> OnCompleted;

    private void Awake()
    {
        if (playerLayer.value == 0)
        {
            playerLayer = LayerMask.GetMask("Player");
        }

        //밟음 감지는 OverlapBox 라 콜라이더는 상호작용 감지용 - 켜져 있으면 근처만 가도 버튼 노출됨
        interactCollider = GetComponent<Collider2D>();
        SetInteractable(false);
    }

    private void Update()
    {
        if (IsCompleted) return;

        bool isPlayerOn = Physics2D.OverlapBox(transform.position, stepSize, 0f, playerLayer) != null;

        //2. 플레이어 발판 밟고 있음
        if (isPlayerOn)
        {
            //밟는 시간 누적
            stepTime = Mathf.Min(stepTime + Time.deltaTime, requiredTime);
        }

        bool activated = isPlayerOn && stepTime >= requiredTime; //플레이어가 밟고 있고, 밟는 시간이 필요시간보다 크다면
        if (activated != IsActivated)
        {
            IsActivated = activated; //이 발판은 활성화
            SetInteractable(activated); //발판 콜라이더 활성화
            Debug.Log($"[DungeonFootThold] {name} {(activated ? "활성화" : "비활성화")}");
        }
    }

    /// <summary>
    /// 상호작용 가능 여부
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override bool CanInteract(InteractionContext context)
    {
        return IsActivated && !IsCompleted;
    }

    /// <summary>
    /// 상호작용 버튼 클릭 시 호출
    /// </summary>
    /// <param name="context"></param>
    public override void Interact(InteractionContext context)
    {
        if (!CanInteract(context)) return;

        IsCompleted = true;
        SetInteractable(false);
        Debug.Log($"[DungeonFootThold] {name} 조건 충족");
        OnCompleted?.Invoke(this);
    }

    /// <summary>
    /// 초기화 ( 층 진입 시 )
    /// </summary>
    public void ResetState()
    {
        stepTime = 0f;
        IsActivated = false;
        IsCompleted = false;
        SetInteractable(false);
    }

    /// <summary>
    /// 발판 콜라이더 활성화/비활성화 
    /// </summary>
    /// <param name="active"></param>
    private void SetInteractable(bool active)
    {
        if (interactCollider != null)
        {
            interactCollider.enabled = active;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsCompleted ? Color.green : IsActivated ? Color.yellow : Color.red;
        Gizmos.DrawWireCube(transform.position, stepSize);
    }
}
