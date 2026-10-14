using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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
    TextMeshProUGUI insufficientGoldText; // 골드 부족 안내

    [SerializeField]
    private Button zoomInBtn;
    [SerializeField]
    private Button buyBtn;

    [SerializeField]
    GameObject focusingIcons;
    [SerializeField]
    GameObject focusingImages;

    private bool IsZoomInClicked = false; // 줌인 버튼 클릭 여부
    private Color originalPriceColor; // 가격 텍스트 원래 색상

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
               () => ShowBuyButton());


            buyBtn.onClick.AddListener(() =>
            {
                OnBuyBtnClicked();
            });

        });
    }

    /// <summary>
    /// 구매 버튼 표시
    /// </summary>
    private void ShowBuyButton()
    {
        buyBtn.gameObject.SetActive(true);

        // 가격 텍스트 원래 색상 저장
        if (priceTMP != null)
        {
            originalPriceColor = priceTMP.color;
        }

        // 부족 텍스트 초기화
        if (insufficientGoldText != null)
        {
            insufficientGoldText.gameObject.SetActive(false);
        }
    }
    private void OnDisable()
    {
        zoomInBtn.onClick.RemoveAllListeners();
        buyBtn.onClick.RemoveAllListeners();
    }

    private void OnBuyBtnClicked()
    {
        TerritoryZone zone = this.GetComponentInParent<TerritoryZone>();
        bool result = TerritoryManager.Instance.TryTerritoryUnlock(zone);

        if (result) // 해금 성공 시
        {
            buyBtn.gameObject.SetActive(false);

            FocusingIconUI(false);
            UnLockUI();

            // 해금 성공 시, 현재 플레이어 주변 구역들의 인접 구역들을 활성화 상태로 변경
            var dic = TerritoryManager.Instance.GetAllAdjacentZones(PlayerZoneDetector.GetCurrentZone());
            foreach (var pair in dic)
            {
                TerritoryZone adjZone = pair.Value;

                if (adjZone.IsLocked)
                {
                    adjZone.TerritoryZoneUIManager.ApplyLockedState();
                    adjZone.UpdateAdjacentZoneLayer();
                }
            }
        }
        else // 해금 실패 (골드 부족)
        {
            if (zone != null)
            {
                PlayerWallet wallet = FindAnyObjectByType<PlayerWallet>();
                if (wallet != null)
                {
                    int required = (int)zone.TerritoryZoneData.RequireGold;
                    int current = wallet.GetBalance(CurrencyType.Gold);
                    int insufficient = required - current;

                    StartCoroutine(ShowInsufficientGoldUI(insufficient));
                }
            }
        }

        IsZoomInClicked = false;
    }

    /// <summary>
    /// 골드 부족 연출 (색상 변경, 부족액 표시, 흔들기, 자동 종료)
    /// </summary>
    private IEnumerator ShowInsufficientGoldUI(int insufficientAmount)
    {
        // 1. 가격 텍스트 빨간색으로
        if (priceTMP != null)
        {
            priceTMP.color = Color.red;
        }

        // 2. 부족 텍스트 표시
        if (insufficientGoldText != null)
        {
            insufficientGoldText.text = $"{insufficientAmount}골드가 부족합니다";
            insufficientGoldText.gameObject.SetActive(true);
        }

        // 3. 버튼 흔들기 (DoTween)
        if (buyBtn != null)
        {
            buyBtn.transform.DOShakePosition(0.5f, new Vector3(10f, 0, 0), 10, 90f);
        }

        // 4. 1초 대기
        yield return new WaitForSeconds(1f);

        // 5. UI 꺼기
        if (insufficientGoldText != null)
        {
            insufficientGoldText.gameObject.SetActive(false);
        }

        // 6. 가격 텍스트 원래 색상으로
        if (priceTMP != null)
        {
            priceTMP.color = originalPriceColor;
        }

        // 7. 상태 초기화 및 모든 UI 꺼기 (뒤로가기 버튼 않 누르면 자동 종료)
        HideAllUI();
    }

    /// <summary>
    /// 모든 UI 제거 및 카메라 원상태로
    /// </summary>
    private void HideAllUI()
    {
        FocusingIconUI(false);
        FocusingImageUI(false);
        buyBtn.gameObject.SetActive(false);
        UnLockUI();
        IsZoomInClicked = false;

        // 카메라 및 줌 상태 초기화
        TerritoryDirectionManager.Instance?.ResetBackClicked(
            this.GetComponentInParent<TerritoryZone>(), false);
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
