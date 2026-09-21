using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 영토확장시스템 - 해금 상태 표시 버튼 생성/해제 버튼 ( 잠금 )
/// </summary>
public class TerritoryZoneLockUI : MonoBehaviour
{
    [SerializeField]
    GameObject icon;
    [SerializeField]
    TextMeshProUGUI priceTMP;

    [SerializeField]
    private Button zoomInBtn;
    [SerializeField]
    private Button buyBtn;

    [SerializeField]
    GameObject focusingIcons;
    [SerializeField]
    GameObject focusingImages;

    private bool IsZoomInClicked = false; // 줌인 버튼 클릭 여부

    public void SetZoomInClicked(bool value)
    {
        IsZoomInClicked = value;
    }

    private void OnEnable()
    {
        zoomInBtn.onClick.AddListener(() =>
        {
            if (IsZoomInClicked) return;

            IsZoomInClicked = true;

            TerritoryDirectionManager.Instance.ZoneIconClicked(this.GetComponentInParent<TerritoryZone>(),
               () => buyBtn.gameObject.SetActive(true));


            buyBtn.onClick.AddListener(() =>
            {
                OnBuyBtnClicked();
            });

        });
    }
    private void OnDisable()
    {
        zoomInBtn.onClick.RemoveAllListeners();
        buyBtn.onClick.RemoveAllListeners();
    }

    private void OnBuyBtnClicked()
    {
        bool result = TerritoryManager.Instance.TryTerritoryUnlock(this.GetComponentInParent<TerritoryZone>());

        if (result) // 해금 성공 시
        {
            buyBtn.gameObject.SetActive(false);

            FocusingIconUI(false);
            UnLockUI();

            // 해금 성공 시, 현재 플레이어 주변 구역들의 인접 구역들을 활성화 상태로 변경
            var dic = TerritoryManager.Instance.GetAllAdjacentZones(PlayerZoneDetector.GetCurrentZone());
            foreach (var pair in dic)
            {
                TerritoryZone zone = pair.Value;

                if (zone.IsLocked)
                {
                    zone.TerritoryZoneUIManager.ApplyLockedState();
                    zone.UpdateAdjacentZoneLayer();
                }
            }

        }

        IsZoomInClicked = false;
    }

    public void BuyBtnAcitve(bool value)
    {
        buyBtn.gameObject.SetActive(value);
    }

    public void FocusingIconUI(bool isAcitve)
    {
        if(isAcitve)
        {
            focusingIcons.SetActive(true);
        }
        else
        {
            focusingIcons.SetActive(false);
        }
    }

    public void FocusingImageUI(bool isAcitve)
    {
        if (isAcitve)
        {
            focusingImages.SetActive(true);
        }
        else
        {
            focusingImages.SetActive(false);
        }
    }

    /// <summary>
    /// 잠금 상태 UI 처리 ( 아이콘 활성화 및 TMP 활성화 처리 )
    /// </summary>
    public void LockUI(float price)
    {
        if (icon != null)
            icon.gameObject.SetActive(true);
        if (priceTMP != null)
        {
            priceTMP.gameObject.SetActive(true);
            priceTMP.text = $"필요 골드: {price}";
        }
    }

    /// <summary>
    /// 잠금 해제 UI 처리 ( 아이콘 비활성화 및 TMP 비활성화 처리 )
    /// </summary>
    public void UnLockUI()
    {
        if (icon != null)
            icon.gameObject.SetActive(false);
        if (priceTMP != null)
        {
            priceTMP.gameObject.SetActive(false);
            priceTMP.text = "";
        }
    }
}
