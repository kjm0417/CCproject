using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 런타임에 따라 데이터가 달라지는 런타임용 아일랜드 데이터
/// </summary>
public class TerritoryRunTimeData
{
    public TerritoryZone PlayerZone { get; private set; }
    public TerrirotyDirection TerrirotyDirection { get; private set; }

    public TerritoryRunTimeData()
    {
    }

    /// <summary>
    /// 런타임 데이터 저장
    /// </summary>
    /// <param name="PlayerZone"></param>
    /// <param name="TerrirotyDirection"></param>
    public void SaveRunTimeData(TerritoryZone PlayerZone, TerrirotyDirection TerrirotyDirection)
    {
        this.PlayerZone = PlayerZone;
        this.TerrirotyDirection = TerrirotyDirection;
    }

    /// <summary>
    /// 런타임 데이터 초기화 
    /// </summary>
    public void InitRunTimeData()
    {
        PlayerZone = null;
        TerrirotyDirection = TerrirotyDirection.None;
    }
}
/// <summary>
/// 게임 내의 구역 관리 시스템
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

    private TerritoryRunTimeData territoryRunTimeData;
    public TerritoryRunTimeData TerritoryRunTimeData => territoryRunTimeData;

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

        foreach (TerritoryZone zone in FindObjectsOfType<TerritoryZone>())
        {
            territoryZonesDic.Add(zone.TerritoryZoneData.ZoneId, zone);
        }

        //테스트용
        currencyProvider = FindAnyObjectByType<CurrencyManager>();

        territoryRunTimeData = new TerritoryRunTimeData();
    }

    /// <summary>
    /// 구역 해금 시도 : 플레이어 골드와 구역 해금 비용 비교 후 해금 가능 시 해금 처리
    /// </summary>
    /// <param name="territoryZone"></param>
    /// <returns></returns>
    public bool TryTerritoryUnlock(TerritoryZone territoryZone)
    {
        if(!territoryZone.IsLocked)
        {
            return true; 
        }

        if (currencyProvider.TrySpend(territoryZone.TerritoryZoneData.RequireGold))
        {
            territoryZone.UnLockZone();
            //territoryZone.GetComponent<TerritoryApproachTrigger>().isco
            return true;
        }

        return false;
    }

    public TerritoryZone GetZone(int id)
    {
        if(territoryZonesDic.TryGetValue(id, out TerritoryZone zone))
        {
            return zone;
        }
        Debug.LogWarning("구역을 찾을 수 없음!");
        return null;
    }

    public void SaveRuntimeTerritoryData(TerritoryZone currentZone, TerrirotyDirection direction)
        => territoryRunTimeData.SaveRunTimeData(currentZone, direction);


    /// <summary>
    /// 인접한 Zone 가져오기
    /// </summary>
    public TerritoryZone GetAdjacentZone(TerritoryZone currentZone, TerrirotyDirection direction)
    {
        int adjacentZoneId = -1;

        switch (direction)
        {
            case TerrirotyDirection.Up:
                adjacentZoneId = currentZone.TerritoryZoneData.AdjacentZoneId_Up;
                break;
            case TerrirotyDirection.Down:
                adjacentZoneId = currentZone.TerritoryZoneData.AdjacentZoneId_Down;
                break;
            case TerrirotyDirection.Left:
                adjacentZoneId = currentZone.TerritoryZoneData.AdjacentZoneId_Left;
                break;
            case TerrirotyDirection.Right:
                adjacentZoneId = currentZone.TerritoryZoneData.AdjacentZoneId_Right;
                break;
        }

        if (adjacentZoneId == -1)
        {
            return null;
        }

        return GetZone(adjacentZoneId);
    }

    /// <summary>
    /// 모든 인접한 Zone 가져오기 (상/하/좌/우)
    /// </summary>
    public Dictionary<TerrirotyDirection, TerritoryZone> GetAllAdjacentZones(TerritoryZone currentZone)
    {
        Dictionary<TerrirotyDirection, TerritoryZone> adjacentZones = new Dictionary<TerrirotyDirection, TerritoryZone>();

        TerritoryZone upZone = GetAdjacentZone(currentZone, TerrirotyDirection.Up);
        TerritoryZone downZone = GetAdjacentZone(currentZone, TerrirotyDirection.Down);
        TerritoryZone leftZone = GetAdjacentZone(currentZone, TerrirotyDirection.Left);
        TerritoryZone rightZone = GetAdjacentZone(currentZone, TerrirotyDirection.Right);

        if (upZone != null)
            adjacentZones.Add(TerrirotyDirection.Up, upZone);
        if (downZone != null)
            adjacentZones.Add(TerrirotyDirection.Down, downZone);
        if (leftZone != null)
            adjacentZones.Add(TerrirotyDirection.Left, leftZone);
        if (rightZone != null)
            adjacentZones.Add(TerrirotyDirection.Right, rightZone);

        return adjacentZones;
    }

    public TerrirotyDirection GetDirection(TerritoryZone currentZone, TerritoryZone adjacentZone)
    {
        if (currentZone.TerritoryZoneData.AdjacentZoneId_Up == adjacentZone.TerritoryZoneData.ZoneId)
            return TerrirotyDirection.Up;

        if (currentZone.TerritoryZoneData.AdjacentZoneId_Down == adjacentZone.TerritoryZoneData.ZoneId)
            return TerrirotyDirection.Down;

        if (currentZone.TerritoryZoneData.AdjacentZoneId_Left == adjacentZone.TerritoryZoneData.ZoneId)
            return TerrirotyDirection.Left;

        if (currentZone.TerritoryZoneData.AdjacentZoneId_Right == adjacentZone.TerritoryZoneData.ZoneId)
            return TerrirotyDirection.Right;

        return TerrirotyDirection.Up; // 기본값
    }
}
