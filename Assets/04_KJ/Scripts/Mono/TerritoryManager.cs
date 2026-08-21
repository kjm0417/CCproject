using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 여러 TerritoryZone을 관리하고, 해금 요청을 처리하는 매니저
/// </summary>
public class TerritoryManager : MonoBehaviour
{
    private static TerritoryManager instance;
    public static TerritoryManager Instance
    {
        get
        {
            return instance;
        }
    }

    private ICurrencyProvider currencyProvider;

    private Dictionary<int, TerritoryZone> territoryZonesDic = new Dictionary<int, TerritoryZone>();
   
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        //구역 딕셔너리 초기화 - 구역 id : 구역 객체
        foreach (TerritoryZone zone in FindObjectsOfType<TerritoryZone>())
        {
                territoryZonesDic.Add(zone.TerritoryZoneData.ZoneId, zone);
        }

        currencyProvider = FindAnyObjectByType<CurrencyManager>();

    }

    /// <summary>
    /// 구역 해금 시도, true : 성공 / false : 실패
    /// </summary>
    /// <param name="territoryZone"></param>
    /// <returns></returns>
    public bool TryTerritoryUnlock(TerritoryZone territoryZone)
    {
        if(!territoryZone.IsLocked)
        {
            return true; //이미 열려 있다면 true 반환(성공처리)
        }

        if (currencyProvider.TrySpend(territoryZone.TerritoryZoneData.RequireGold))
        {
            territoryZone.UnLockZone();
            return true;
        }

        return false;
    }

    BoxCollider2D prevConfinerBound;

    CinemachineCamera cinemachineCamera;
    CinemachineConfiner2D cinemachineConfiner2D;
    /// <summary>
    /// 구역 접근 했을 때 카메라 설정
    /// </summary>
    /// <param name="prevterritoryCam"></param>
    /// <param name="approveterritoryCam"></param>
    public void ApproveCameraTerritoryUnlock(TerritoryCamera approveterritoryCam)
    {
        cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();
        cinemachineConfiner2D = FindAnyObjectByType<CinemachineConfiner2D>();

        prevConfinerBound = (BoxCollider2D)cinemachineConfiner2D.BoundingShape2D;  //이전 구역의 Bound 저장

        cinemachineCamera.Follow = approveterritoryCam.TerritoryAnchor;
        cinemachineConfiner2D.BoundingShape2D = approveterritoryCam.CameBoundCollider2D; //시네머신의 Bound를 다음 구역의 Bound 설정
    }

    /// <summary>
    /// 선택 UI에서 구역 구매 거절 했을 때 카메라 설정
    /// </summary>
    /// <param name="prevterritoryCam"></param>
    /// <param name="approveterritoryCam"></param>
    public void LeaveCameraTerritoryUnlock()
    {
        cinemachineCamera.Follow = GameObject.FindGameObjectWithTag("Player").transform;
        cinemachineConfiner2D.BoundingShape2D = prevConfinerBound; //시네머신의 Bound를 다음 구역의 Bound 설정
    }

}
