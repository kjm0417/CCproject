using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 구역 하나에 대표하는 컴포넌트(구역별로 하나씩 존재)
/// </summary>
public class TerritoryZone : MonoBehaviour
{
    #region 필요 변수 정의
    [SerializeField]
    private TerritoryZoneData territoryZoneData; //구역에 대한 데이터

    [SerializeField]
    private TerritoryIcon territoryIcon;
    public TerritoryIcon TerritoryIcon => territoryIcon;

    [SerializeField]
    private TerritoryUI territoryUI;
    public TerritoryUI TerritoryUI => territoryUI;

    [SerializeField]
    private Tilemap territoryTileMapGround,territoryTileMapWall; //구역에 대한 타일맵

    public Tilemap TerritoryTileMapGround => territoryTileMapGround;
    public Tilemap TerritoryTileMapWall=> territoryTileMapWall;

    [SerializeField]
    private bool isLocked = true; //구역이 잠겨 있는지 여부 (초기엔 잠겨 있음)

    public static Action<TerritoryZone, TerrirotyDirection> OnZoneApproached;
    public static Action OnZoneLeaved;
    public static Action<TerritoryZone> OnZoneBuyClick; //구역 구매 버튼 클릭 이벤트

    //외부 읽기 전용 프로퍼티
    public TerritoryZoneData TerritoryZoneData => territoryZoneData;

    public bool IsLocked => isLocked;


    #endregion

    private void Awake()
    {
        if(isLocked) //잠겨 있다면
        {
            LockedZoneSet();
        }
        else
        {
            UnLockedZoneSet();
        }
       
    }

    /// <summary>
    /// 인접 구역에 해당하는 타일맵의 레이어를 변경
    /// </summary>
    public void UpdateAdjacentZoneLayer()
    {
        territoryTileMapGround.gameObject.layer = 6; //LockZone 레이어로 설정
        territoryTileMapWall.gameObject.layer = 6;
    }

    /// <summary>
    /// 구역이 잠겨있을 때 세팅
    /// </summary>
    private void LockedZoneSet()
    {
        territoryTileMapGround.gameObject.layer = 6; //LockZone 레이어로 설정
        territoryTileMapWall.gameObject.layer = 6;

        territoryTileMapWall.GetComponent<TilemapCollider2D>().enabled = true;

    }
    /// <summary>
    /// 구역이 잠겨있지 않을 때 세팅
    /// </summary>
    private void UnLockedZoneSet()
    {
        territoryTileMapGround.gameObject.layer = 7; //UnlockZone 레이어로 설정
        territoryTileMapWall.gameObject.layer = 7;

        territoryTileMapWall.GetComponent<TilemapCollider2D>().enabled = false;
    }

    /// <summary>
    /// 잠겨있는 구역에 해금 시도 과정에서 구역 아이콘 클릭 했을 때 
    /// </summary>
    public void TryLockedZoneSet()
    {
        territoryTileMapGround.gameObject.layer = 8; //TrylockZone 레이어로 설정
        territoryTileMapWall.gameObject.layer = 8; //TrylockZone 레이어로 설정

        //구매 아이콘 및 토지 구입 비용 관련 캔버스 활성화
        if(IsLocked)
        {
            territoryIcon.FocusingImageUI(true);
            territoryIcon.LockUI(territoryZoneData.RequireGold);
        }

        //현재 구역 기준 상/하/좌/우에 해당하는 구역들을 찾아서 , 구역 비용 UI 보여주고 , 포커싱 설정

    }

    /// <summary>
    /// 잠겨있는 구역에 해금 시도 과정에서 뒤로가기 아이콘 클릭 했을 때
    /// </summary>
    public void TryUnLockedZoneSet()
    {
        territoryTileMapGround.gameObject.layer = 6; 
        territoryTileMapWall.gameObject.layer = 6; 

        //구매 아이콘 및 토지 구입 비용 관련 캔버스 활성화
        if (IsLocked)
        {
            territoryIcon.FocusingImageUI(false);
            territoryIcon.ButtonUI(false);
            territoryIcon.UnLockUI();
        }

        //현재 구역 기준 상/하/좌/우에 해당하는 구역들을 찾아서 , 구역 비용 UI 보여주고 , 포커싱 설정

    }


    /// <summary>
    /// 구역 잠금 해제 성공 시 호출 되는 메서드
    /// </summary>
    public void UnLockZone()
    {
        territoryIcon.FocusingIconUI(false);
        territoryIcon.FocusingImageUI(false);

        this.gameObject.layer = 7;

        string[] objParts = this.gameObject.name.Split('_');
        string number = objParts[1];

        this.gameObject.name = "Zone_" + number;

        isLocked = false;

        //구역이 밝아짐
        StartCoroutine(TerritoryAlpha());
    }

    IEnumerator TerritoryAlpha()
    {
        Color c1 = territoryTileMapGround.color;
        Color c2 = territoryTileMapWall.color;
        float startAlpha1 = c1.a;
        float startAlpha2 = c2.a;
        float elapsed = 0f;

        while (elapsed < 1.0f)
        {
            elapsed += Time.deltaTime;
            c1.a = Mathf.Lerp(startAlpha1, 1f, elapsed / 1.0f);
            c2.a = Mathf.Lerp(startAlpha2, 1f, elapsed / 1.0f);
            territoryTileMapGround.color = c1;
            territoryTileMapWall.color = c2;
            yield return null;
        }

        c1.a = 1f;
        territoryTileMapGround.color = c1; //마지막에 정확히 1로 고정 (Lerp 오차 방지)
        c2.a = 1f;
        territoryTileMapWall.color = c2; //마지막에 정확히 1로 고정 (Lerp 오차 방지)

        territoryIcon.FocusingIconUI(false);
        territoryIcon.FocusingImageUI(false);

        UnLockedZoneSet();

        GetComponent<ResourceSpawner>().SpawnInitial();

        yield return new WaitForSeconds(1.5f);

        TerritoryDirectionManager.Instance.ResetBackClicked(this,true);
    }

    TerrirotyDirection currentZoneDir; //현재 플레이어가 접근한 구역 기준으로 상/하/좌/우 어느 방향에서 접근했는지 저장
    /// <summary>
    /// 해금되지 않는 구역에 접근 시 처리할 것
    /// </summary>
    public void ApproachZone(TerritoryZone playerZone, TerrirotyDirection terrirotyDirection) 
    {
        currentZoneDir = terrirotyDirection;

        //줌 아웃 UI 처리       
        playerZone.territoryUI.OnZoomOutViewed(playerZone, terrirotyDirection);
    }

    /// <summary>
    /// 해금되지 않는 구역에 접근 후 이탈 시 처리할 것
    /// </summary>
    public void LeaveZone()
    {
        OnZoneLeaved?.Invoke();
    }

}
