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

    [SerializeField]
    private Tilemap territoryTileMap; //구역에 대한 타일맵

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

        //아이콘 UI 처리
        territoryIcon.UnLockUI();

        //구역이 밝아짐
        StartCoroutine(TerritoryAlpha());
    }

    IEnumerator TerritoryAlpha()
    {
        Color c = territoryTileMap.color;
        float startAlpha = c.a; 
        float elapsed = 0f;

        while (elapsed < 1.0f)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, 1f, elapsed / 1.0f);
            territoryTileMap.color = c;
            yield return null;
        }

        c.a = 1f;
        territoryTileMap.color = c; //마지막에 정확히 1로 고정 (Lerp 오차 방지)

        yield return new WaitForSeconds(1.0f);
        TerritoryDirectionManager.Instance.ZoomOutClearTerritory();
    }

    /// <summary>
    /// 해금되지 않는 구역에 접근 시 처리할 것
    /// </summary>
    public void ApproachZone(TerrirotyDirection terrirotyDirection) 
    {
        //줌 아웃 UI
        OnZoneApproached?.Invoke(this, terrirotyDirection);
    }

    /// <summary>
    /// 해금되지 않는 구역에 접근 후 이탈 시 처리할 것
    /// </summary>
    public void LeaveZone()
    {
        OnZoneLeaved?.Invoke();
    }
    /// <summary>
    /// 해금되지 않는 구역에 줌 인 했을 때 처리할 것
    /// </summary>
    public void ZoomInZone()
    {
        OnZoneBuyClick?.Invoke(this); //줌인 시, 구매 의사 UI 활성화 하기 위한 이벤트 발행
    }

}
