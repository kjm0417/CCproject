using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TerritorySelectionUI : MonoBehaviour
{
    [SerializeField]
    GameObject  zoomrightUI,zoomleftUI , zoomupUI,zoomdownUI;

    [SerializeField]
    Button zoomRightBtn , zoomLeftBtn, zoomUpBtn, zoomDownBtn;

    [SerializeField]
    Button backBtn;
    
    private GameObject prevZoomUI; //줌 아웃하기 전 활성화 였던 UI 저장

    private void OnEnable()
    {
        TerritoryZone.OnZoneApproached += OnZoomOutViewed;
        TerritoryZone.OnZoneLeaved += OnLevaeZonUI;

        backBtn.onClick.AddListener(() =>
        {
            Dictionary<TerrirotyDirection, TerritoryZone> dic = TerritoryManager.Instance.GetAllAdjacentZones(playerZone);

            TerritoryZone approachZone = TerritoryManager.Instance.GetAdjacentZone(playerZone, terrirotyDirection);

            TerritoryDirectionManager.Instance.ResetBackClicked(approachZone, false);

            foreach (var pair in dic)
            {
                TerritoryZone zone = pair.Value;

                if (!zone.IsLocked) continue; //인접 구역이 잠겨있지 않으면, 즉, 해금 완료된 구역이라면 return

                zone.TryUnLockedZoneSet();

                if (zone == approachZone)
                {
                    zone.TerritoryIcon.FocusingIconUI(false);
                }

            }

            backBtn.gameObject.SetActive(false);

            playerZone = null;
            terrirotyDirection = TerrirotyDirection.None;

        });
    }

    private void OnDisable()
    {
        TerritoryZone.OnZoneApproached -= OnZoomOutViewed;
        TerritoryZone.OnZoneLeaved -= OnLevaeZonUI;
    }

    private void OnLevaeZonUI()
    {
        zoomleftUI.SetActive(false); 
        zoomrightUI.SetActive(false); 
        zoomupUI.SetActive(false); 
        zoomdownUI.SetActive(false);

        prevZoomUI.SetActive(false);
    }
    private void OnBackCliked()
    {
        prevZoomUI.SetActive(true);
    }

    /// <summary>
    /// territoryZone : 현재 구역 , TerrirotyDirection : 접근하려는 구역의 방향
    /// </summary>
    /// <param name="playerZone"></param>
    /// <param name="adjacentZone"></param>
    /// <param name="terrirotyDirection"></param>
    private void OnZoomOutViewed(TerritoryZone playerZone, TerrirotyDirection terrirotyDirection)
    {
        zoomLeftBtn.onClick.RemoveAllListeners();
        zoomRightBtn.onClick.RemoveAllListeners();
        zoomUpBtn.onClick.RemoveAllListeners();
        zoomDownBtn.onClick.RemoveAllListeners();

        switch (terrirotyDirection)
        {
            case TerrirotyDirection.Left:
                zoomleftUI.SetActive(true);
                prevZoomUI = zoomleftUI;
                zoomLeftBtn.onClick.AddListener(() => ZoomOutViewed(playerZone, terrirotyDirection));
                break;
            case TerrirotyDirection.Right:
                zoomrightUI.SetActive(true);
                prevZoomUI = zoomrightUI;
                zoomRightBtn.onClick.AddListener(() => ZoomOutViewed(playerZone, terrirotyDirection));
                break;
            case TerrirotyDirection.Up:
                zoomupUI.SetActive(true);
                prevZoomUI = zoomupUI;
                zoomUpBtn.onClick.AddListener(() => ZoomOutViewed(playerZone, terrirotyDirection));
                break;
            case TerrirotyDirection.Down:
                zoomdownUI.SetActive(true);
                prevZoomUI = zoomdownUI;
                zoomDownBtn.onClick.AddListener(() => ZoomOutViewed(playerZone, terrirotyDirection));
                break;
        }

    }

    TerritoryZone playerZone;
    TerrirotyDirection terrirotyDirection;

    private void ZoomOutViewed(TerritoryZone playerZone, TerrirotyDirection terrirotyDirection)
    {
        //현재 구역(territoryZone) 기준으로 인접한 구역들 딕셔너리로 가져옴
        this.playerZone = playerZone;
        this.terrirotyDirection = terrirotyDirection;

        Dictionary<TerrirotyDirection, TerritoryZone> dic = TerritoryManager.Instance.GetAllAdjacentZones(playerZone);

        TerritoryZone approachZone = TerritoryManager.Instance.GetAdjacentZone(playerZone, terrirotyDirection);

        foreach (var pair in dic) 
        {
            TerritoryZone zone = pair.Value;

            if (!zone.IsLocked) continue; //인접 구역이 잠겨있지 않으면, 즉, 해금 완료된 구역이라면 return

            zone.TryLockedZoneSet();

            if (zone == approachZone)
            {
                zone.TerritoryIcon.FocusingIconUI(true);
            }
            
        }

        SetCameraAnchorByApproachZone(terrirotyDirection, approachZone);

        backBtn.gameObject.SetActive(true);
        zoomleftUI.SetActive(false); zoomrightUI.SetActive(false); zoomupUI.SetActive(false); zoomdownUI.SetActive(false);

    }

    private void SetCameraAnchorByApproachZone(TerrirotyDirection dir, TerritoryZone approachZone)
    {
        switch(dir)
        {
            case TerrirotyDirection.Up:
                TerritoryDirectionManager.Instance.ZoomModeActive(approachZone.GetComponent<TerritoryCamera>().TerritoryDownAnchor);
                break;
            case TerrirotyDirection.Down:
                TerritoryDirectionManager.Instance.ZoomModeActive(approachZone.GetComponent<TerritoryCamera>().TerritoryUpAnchor);
                break;
            case TerrirotyDirection.Left:
                TerritoryDirectionManager.Instance.ZoomModeActive(approachZone.GetComponent<TerritoryCamera>().TerritoryRightAnchor);
                break;
            case TerrirotyDirection.Right:
                TerritoryDirectionManager.Instance.ZoomModeActive(approachZone.GetComponent<TerritoryCamera>().TerritoryLeftAnchor);
                break;

        }
    }


}
