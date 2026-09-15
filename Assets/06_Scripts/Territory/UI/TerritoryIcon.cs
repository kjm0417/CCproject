using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TerritoryIcon : MonoBehaviour
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

    private bool IsZoomInClicked = false; //줌인 버튼 클릭 여부

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

        if (result) //해금 성공 시
        {
            buyBtn.gameObject.SetActive(false);

            FocusingIconUI(false);
            UnLockUI();

            //해금 성공 시, 현재 플레이어 구역 기준으로 인접한 구역들의 캔버스를 비활성화 시킨다.
            var dic = TerritoryManager.Instance.GetAllAdjacentZones(PlayerZoneDetector.GetCurrentZone());
            foreach (var pair in dic)
            {
                TerritoryZone zone = pair.Value;

                if (zone.IsLocked)
                {
                    zone.TerritoryIcon.FocusingIconUI(false);
                    zone.TerritoryIcon.FocusingImageUI(false);
                    zone.TerritoryIcon.UnLockUI();

                    zone.UpdateAdjacentZoneLayer();

                }
            }

        }

        IsZoomInClicked = false;
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

    public void ButtonUI(bool isAcitve)
    {
        if (isAcitve)
        {
            buyBtn.gameObject.SetActive(true);
        }
        else
        {
            buyBtn.gameObject.SetActive(false);
        }
    }
    /// <summary>
    /// 구역 잠금 UI 처리 ( 아이콘 활성화 및 TMP 활성화 처리 등등 )
    /// </summary>
    public void LockUI(float price)
    {
        icon.gameObject.SetActive(true);
        priceTMP.gameObject.SetActive(true);
        priceTMP.text = $"필요 골드: {price}";
    }

    /// <summary>
    /// 구역 해금 UI 처리 ( 아이콘 비활성화 및 TMP 비활성화 처리 등등 )
    /// </summary>
    public void UnLockUI()
    {
        icon.gameObject.SetActive(false);
        priceTMP.gameObject.SetActive(false);
        priceTMP.text = "";
    }
}
