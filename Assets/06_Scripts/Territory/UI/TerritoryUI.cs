using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 영토확장시스템 - 월드
/// </summary>
public class TerritoryUI : TerritoryBaseUI
{
    [SerializeField]
    GameObject  zoomrightUI,zoomleftUI , zoomupUI,zoomdownUI;

    [SerializeField]
    Button zoomRightBtn , zoomLeftBtn, zoomUpBtn, zoomDownBtn;

    GameObject prevZoomUI;
    private void OnEnable()
    {
        //TerritoryZone.OnZoneApproached += OnZoomOutViewed;
        TerritoryZone.OnZoneLeaved += OnLevaeZonUI;

        OnLevaeZonUI();
    }

    private void OnDisable()
    {
       // TerritoryZone.OnZoneApproached -= OnZoomOutViewed;
        TerritoryZone.OnZoneLeaved -= OnLevaeZonUI;
    }

    private void OnLevaeZonUI()
    {
        zoomleftUI.SetActive(false); 
        zoomrightUI.SetActive(false); 
        zoomupUI.SetActive(false); 
        zoomdownUI.SetActive(false);
    }

    /// <summary>
    /// 뒤로가기 버튼 눌렀을 때, 이전 줌 UI 활성화 유지
    /// </summary>
    public void PrevZoomUIActive() => prevZoomUI.SetActive(true);

    /// <summary>
    /// territoryZone : 현재 구역 , TerrirotyDirection : 접근하려는 구역의 방향
    /// </summary>
    /// <param name="playerZone"></param>
    /// <param name="adjacentZone"></param>
    /// <param name="terrirotyDirection"></param>
    public void OnZoomOutViewed(TerritoryZone playerZone, TerrirotyDirection terrirotyDirection)
    {
        zoomLeftBtn.onClick.RemoveAllListeners();
        zoomRightBtn.onClick.RemoveAllListeners();
        zoomUpBtn.onClick.RemoveAllListeners();
        zoomDownBtn.onClick.RemoveAllListeners();

        switch (terrirotyDirection)
        {
            case TerrirotyDirection.Left:
                prevZoomUI = zoomleftUI;
                zoomleftUI.SetActive(true);
                zoomLeftBtn.onClick.AddListener(() => ZoomOutViewed(playerZone, terrirotyDirection));
                break;
            case TerrirotyDirection.Right:
                prevZoomUI = zoomrightUI;
                zoomrightUI.SetActive(true);
                zoomRightBtn.onClick.AddListener(() => ZoomOutViewed(playerZone, terrirotyDirection));
                break;
            case TerrirotyDirection.Up:
                prevZoomUI = zoomupUI;
                zoomupUI.SetActive(true);
                zoomUpBtn.onClick.AddListener(() => ZoomOutViewed(playerZone, terrirotyDirection));
                break;
            case TerrirotyDirection.Down:
                prevZoomUI = zoomdownUI;
                zoomdownUI.SetActive(true);
                zoomDownBtn.onClick.AddListener(() => ZoomOutViewed(playerZone, terrirotyDirection));
                break;
        }

    }

    private void ZoomOutViewed(TerritoryZone playerZone, TerrirotyDirection terrirotyDirection)
    {
        //현재 구역(territoryZone) 기준으로 인접한 구역들 딕셔너리로 가져옴
        TerritoryManager.Instance.SaveRuntimeTerritoryData(playerZone, terrirotyDirection);

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

        BackBtn.gameObject.SetActive(true);

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
