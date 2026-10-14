using UnityEngine;

/// <summary>
/// 제작대 오브젝트 (필드 오브젝트 42003 작업대).
/// 상호작용 시 제작 UI를 열고 플레이어 이동을 정지한다. 닫기는 CraftingUI의 닫기 버튼 또는 제작 완료 시 자동.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CraftingTable : MonoBehaviour, IInteractable
{
    [Header("제작 조건")]
    [Tooltip("이 제작대의 시설 ID. 레시피의 CraftFacilityID와 같은 것만 표시")]
    [SerializeField]
    private string facilityId = "42003";
    [Tooltip("현재 지역 계절. 레시피 Season이 이 값이거나 All인 것만 표시")]
    [SerializeField]
    private string season = "Spring";

    [Header("UI")]
    [SerializeField]
    private CraftingUI craftingUI;

    private PlayerContext openedBy;

    public string FacilityId => facilityId;
    public string Season => season;

    /// <summary>
    /// 현재 제작 UI가 열려 있는지
    /// </summary>
    public bool IsOpen { get; private set; }

    /// <summary>
    /// 닫혀 있고 플레이어 정보가 있으면 상호작용 가능
    /// </summary>
    public bool CanInteract(InteractionContext context)
    {
        return !IsOpen && context.Player != null;
    }

    /// <summary>
    /// 상호작용 시 제작 UI 열기
    /// </summary>
    public void Interact(InteractionContext context)
    {
        Open(context.Player);
    }

    /// <summary>
    /// 제작 UI 열기 + 플레이어 이동 정지
    /// </summary>
    public void Open(PlayerContext player)
    {
        if (IsOpen || player == null) return;

        if (craftingUI == null) craftingUI = FindAnyObjectByType<CraftingUI>(FindObjectsInactive.Include);
        if (craftingUI == null)
        {
            Debug.LogWarning($"[{name}] CraftingUI를 찾을 수 없습니다.");
            return;
        }

        IsOpen = true;
        openedBy = player;
        if (player.Movement != null) player.Movement.SetInputBlocked(true);
        craftingUI.Open(this, player);
    }

    /// <summary>
    /// 제작 UI 닫기 + 플레이어 이동 정지 해제
    /// </summary>
    public void Close()
    {
        if (!IsOpen) return;

        IsOpen = false;
        if (openedBy != null && openedBy.Movement != null) openedBy.Movement.SetInputBlocked(false);
        openedBy = null;
        if (craftingUI != null) craftingUI.Close(this);
    }

    /// <summary>
    /// 비활성화 시 열려 있으면 닫는다.
    /// </summary>
    private void OnDisable()
    {
        Close();
    }
}
