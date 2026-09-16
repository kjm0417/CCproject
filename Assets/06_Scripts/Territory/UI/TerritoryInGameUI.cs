using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 영토확장시스템 - 전역
/// </summary>
public class TerritoryInGameUI : MonoBehaviour
{
    [SerializeField]
    private Button backBtn;

    void ShowBackButton(bool value) => backBtn.gameObject.SetActive(value);

    private void OnEnable()
    {
        TerritoryAdjacentZoneUI.OnZoomStarted += ShowBackButton;

        TerritoryZone.OnTerritoryUnlocked += ShowBackButton;

        backBtn.onClick.AddListener(() =>
        {

            TerritoryZone playerZone = TerritoryManager.Instance.TerritoryRunTimeData.PlayerZone;
            TerrirotyDirection terrirotyDirection = TerritoryManager.Instance.TerritoryRunTimeData.TerrirotyDirection;

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
                    zone.TerritoryZoneUIManager.HideFocusingIcon();
                    zone.TerritoryZoneUIManager.SetZoomInClicked(false);
                    zone.TerritoryZoneUIManager.BuyBtnAcitve(false);
                }
                
            }

            playerZone.TerritoryZoneUIManager.PrevZoomUIActive();
            backBtn.gameObject.SetActive(false);

            TerritoryManager.Instance.TerritoryRunTimeData.InitRunTimeData();

        });
    }
    private void OnDisable()
    {
        TerritoryAdjacentZoneUI.OnZoomStarted -= ShowBackButton;
        TerritoryZone.OnTerritoryUnlocked -= ShowBackButton;

        backBtn.onClick.RemoveAllListeners();
    }
}
