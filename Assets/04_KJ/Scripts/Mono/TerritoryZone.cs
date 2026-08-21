using System;
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

    [SerializeField]
    private Tilemap territoryTileMap; //구역에 대한 타일맵

    [SerializeField]
    private bool isLocked = true; //구역이 잠겨 있는지 여부 (초기엔 잠겨 있음)

    public static Action<TerritoryZone> OnZoneApproached;

    //외부 읽기 전용 프로퍼티
    public TerritoryZoneData TerritoryZoneData => territoryZoneData;

    public bool IsLocked => isLocked;


    #endregion

    private void Awake()
    {
        //구역별 잠금 여부에 따른 초기에 UI 설정
        if (isLocked)
        {
            territoryIcon.LockUI(territoryZoneData.RequireGold);
        }
           
        else
        {
            territoryIcon.UnLockUI();
        }
    }


    /// <summary>
    /// 구역 잠금 해제 성공 시 호출 되는 메서드
    /// </summary>
    public void UnLockZone()
    {
        string[] objParts = this.gameObject.name.Split('_');
        string number = objParts[1];

        this.gameObject.name = "Zone_" + number;

        isLocked = false;
    
        //해당 구역 활성화 처리
        Color c = territoryTileMap.color;
        c.a = 1f;
        territoryTileMap.color = c;

        //아이콘 UI 처리
        territoryIcon.UnLockUI();
        territoryIcon.ArrowUI(false);

        //TODO KJ - 구매 UI 처리

        //TODO KJ - 카메라 이동 가능 범위 확장
        //TODO KJ - 신규 구역에 자원 오브젝트 스폰
    }

    /// <summary>
    /// 잠겨 있는 구역에 접근 시 처리할 것
    /// </summary>
    public void ApproachZone() 
    {
        //현재 구역 상/하/좌/우 화살표 생성
        territoryIcon.ArrowUI(true);

        //접근한 구역으로 카메라 이동     
        TerritoryCamera approveterritoryCamera = GetComponent<TerritoryCamera>(); //접근한 구역의 카메라 컴포

        TerritoryManager.Instance?.ApproveCameraTerritoryUnlock(approveterritoryCamera);

        //선택 UI
        OnZoneApproached?.Invoke(this);

    }
}
