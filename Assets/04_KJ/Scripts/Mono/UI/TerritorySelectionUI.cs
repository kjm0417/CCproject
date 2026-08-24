using System;
using System.Collections;
using TMPro;
using Unity.UI;
using UnityEngine;
using UnityEngine.UI;

public class TerritorySelectionUI : MonoBehaviour
{
    [SerializeField]
    GameObject  zoomrightUI,zoomleftUI , zoomupUI,zoomdownUI;

    [SerializeField]
    Button buyBtn, zoomRightBtn , zoomLeftBtn, zoomUpBtn, zoomDownBtn;

    private GameObject prevZoomUI; //줌 아웃하기 전 활성화 였던 UI 저장

    private void OnEnable()
    {
        if(TerritoryDirectionManager.Instance != null)
        {
            TerritoryDirectionManager.Instance.OnBackClick += OnBackCliked;
        }

        TerritoryZone.OnZoneApproached += OnZoomOutViewed;
        TerritoryZone.OnZoneLeaved += OnLevaeZonUI;
        TerritoryZone.OnZoneBuyClick += OnBuyUIViewd;
    }

    private void OnDisable()
    {
        if (TerritoryDirectionManager.Instance != null)
        {
            TerritoryDirectionManager.Instance.OnBackClick -= OnBackCliked;
        }

        TerritoryZone.OnZoneApproached -= OnZoomOutViewed;
        TerritoryZone.OnZoneLeaved -= OnLevaeZonUI;
        TerritoryZone.OnZoneBuyClick -= OnBuyUIViewd;
    }

    private void OnLevaeZonUI()
    {
        zoomleftUI.SetActive(false); 
        zoomrightUI.SetActive(false); 
        zoomupUI.SetActive(false); 
        zoomdownUI.SetActive(false);

        prevZoomUI.SetActive(false);
        buyBtn.gameObject.SetActive(false);
    }
    private void OnBackCliked()
    {
        prevZoomUI.SetActive(true);
        buyBtn.gameObject.SetActive(false);
    }

    private void OnZoomOutViewed(TerritoryZone territoryZone, TerrirotyDirection terrirotyDirection)
    {
        OnZoomOutUIView(terrirotyDirection);
    }

    /// <summary>
    /// 줌 아웃 UI 활성화
    /// </summary>
    /// <param name="terrirotyDirection"></param>
    private void OnZoomOutUIView(TerrirotyDirection terrirotyDirection)
    {
        zoomLeftBtn.onClick.RemoveAllListeners();
        zoomRightBtn.onClick.RemoveAllListeners();
        zoomUpBtn.onClick.RemoveAllListeners();
        zoomDownBtn.onClick.RemoveAllListeners();

        switch(terrirotyDirection)
        {
            case TerrirotyDirection.Left:
                zoomleftUI.SetActive(true);
                prevZoomUI = zoomleftUI;
                zoomLeftBtn.onClick.AddListener(() => ZoomOutViewed());
                break;
            case TerrirotyDirection.Right:
                zoomrightUI.SetActive(true);
                prevZoomUI = zoomrightUI;
                zoomRightBtn.onClick.AddListener(() => ZoomOutViewed());
                break;
            case TerrirotyDirection.Up:
                zoomupUI.SetActive(true);
                prevZoomUI = zoomupUI;
                zoomUpBtn.onClick.AddListener(() => ZoomOutViewed());
                break;
            case TerrirotyDirection.Down:
                zoomdownUI.SetActive(true);
                prevZoomUI = zoomdownUI;
                zoomDownBtn.onClick.AddListener(() => ZoomOutViewed());
                break;
        }

    }

    private void ZoomOutViewed()
    {
        TerritoryDirectionManager.Instance.ZoomOutTerritory();

        zoomleftUI.SetActive(false); zoomrightUI.SetActive(false); zoomupUI.SetActive(false); zoomdownUI.SetActive(false);
    }


    /// <summary>
    /// 구매 의사 결정 UI 활성화
    /// </summary>
    /// <param name="territoryZone"></param>
    private void OnBuyUIViewd(TerritoryZone territoryZone)
    {
        buyBtn.gameObject.SetActive(true);

        buyBtn.onClick.RemoveAllListeners();

        buyBtn.onClick.AddListener(() =>
        {
            TerritoryManager.Instance?.TryTerritoryUnlock(territoryZone);
            buyBtn.gameObject.SetActive(false);
        });
    }
}
