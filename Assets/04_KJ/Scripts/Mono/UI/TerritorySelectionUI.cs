using System;
using TMPro;
using Unity.UI;
using UnityEngine;
using UnityEngine.UI;

public class TerritorySelectionUI : MonoBehaviour
{
    [SerializeField]
    GameObject selectionUI;

    [SerializeField]
    TextMeshProUGUI selectionTMP;

    [SerializeField]
    Button yesBtn, noBtn;

    private void OnEnable()
    {
        TerritoryZone.OnZoneApproached += OnSelectionViewed;

        selectionUI.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        TerritoryZone.OnZoneApproached -= OnSelectionViewed;
    }

    private void OnSelectionViewed(TerritoryZone territoryZone)
    {
        selectionUI.gameObject.SetActive(true);
        selectionTMP.text = $"해당 구역을 구매하시겠습니까? (가격 : {territoryZone.TerritoryZoneData.RequireGold})";

        yesBtn.onClick.RemoveAllListeners();
        noBtn.onClick.RemoveAllListeners();

        yesBtn.onClick.AddListener(() =>
        {
            TerritoryManager.Instance?.TryTerritoryUnlock(territoryZone);
            TerritoryManager.Instance?.LeaveCameraTerritoryUnlock();
            selectionUI.gameObject.SetActive(false);
        });

        noBtn.onClick.AddListener(() =>
        {
            TerritoryManager.Instance?.LeaveCameraTerritoryUnlock();
            selectionUI.gameObject.SetActive(false);
        });
    }
}
