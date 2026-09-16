using UnityEngine;

/// <summary>
/// 영토확장시스템 - 모든 UI 업데이트 조율 및 UI 관리
/// </summary>
public class TerritoryZoneUIManager : MonoBehaviour
{
    [SerializeField]
    private TerritoryZoneLockUI territoryIcon;

    [SerializeField]
    private TerritoryAdjacentZoneUI territoryUI;

    private TerritoryZone zone;

    private void Start()
    {
        zone = GetComponent<TerritoryZone>();
    }

    /// <summary>
    /// Locked 상태 UI 적용
    /// </summary>
    public void ApplyLockedState()
    {
        territoryIcon.UnLockUI();
        territoryIcon.FocusingIconUI(false);
        territoryIcon.FocusingImageUI(false);
    }

    /// <summary>
    /// Unlocking 상태 UI 적용
    /// </summary>
    public void ApplyUnlockingState()
    {
        territoryIcon.FocusingImageUI(true);
        territoryIcon.LockUI(zone.TerritoryZoneData.RequireGold);
    }

    /// <summary>
    /// Unlocked 상태 UI 적용
    /// </summary>
    public void ApplyUnlockedState()
    {
        territoryIcon.UnLockUI();
        territoryIcon.FocusingIconUI(false);
        territoryIcon.FocusingImageUI(false);
    }

    /// <summary>
    /// 해금 중 진행 UI 업데이트
    /// </summary>
    public void UpdateUnlockProgress()
    {
        territoryIcon.FocusingIconUI(false);
        territoryIcon.FocusingImageUI(false);
      
    }

    /// <summary>
    /// 줌 아웃 뷰 UI 처리
    /// </summary>
    public void OnZoomOutViewed(TerritoryZone playerZone, TerrirotyDirection direction)
    {
        territoryUI.OnZoomOutViewed(playerZone, direction);
    }


    public void ShowFocusingIcon() => territoryIcon.FocusingIconUI(true);
    public void HideFocusingIcon() => territoryIcon.FocusingIconUI(false);

    public void ShowFocusingImage() => territoryIcon.FocusingImageUI(true);
    public void HideFocusingImage() => territoryIcon.FocusingImageUI(false);

    public void BuyBtnAcitve(bool value) => territoryIcon.BuyBtnAcitve(value);

    public void SetZoomInClicked(bool value) => territoryIcon.SetZoomInClicked(value);

    public void PrevZoomUIActive() => territoryUI.PrevZoomUIActive();
}
