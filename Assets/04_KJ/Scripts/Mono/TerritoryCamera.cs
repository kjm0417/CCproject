using System;
using UnityEngine;

/// <summary>
/// 구역별 카메라 변수 정의
/// </summary>
public class TerritoryCamera : MonoBehaviour
{
    [SerializeField]
    private BoxCollider2D camBoundCollider2D; //구역에 맞는 카메라 영역

    public BoxCollider2D CameBoundCollider2D => camBoundCollider2D;

    [SerializeField]
    private BoxCollider2D boundSwitchTrigger; //안쪽 트리거. 해금 시, 카메라 영역을 바꾸기 위함

    [SerializeField]
    Transform territoryAnchor; //구역의 중심 
    public Transform TerritoryAnchor => territoryAnchor;

    private void OnEnable()
    {
        GetComponentInChildren<TerritoryIcon>().OnZoomInClick += OnZoomInClicked;
    }

    private void OnDisable()
    {
        GetComponentInChildren<TerritoryIcon>().OnZoomInClick -= OnZoomInClicked;
    }

    /// <summary>
    /// 줌인 클릭했을 때, 카메라 연출하기 위한 인자들 세팅 후 호출
    /// </summary>
    private void OnZoomInClicked()
    {
        TerritoryDirectionManager.Instance.ZoomInTerritory(camBoundCollider2D, territoryAnchor);

        GetComponent<TerritoryZone>().ZoomInZone();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (GetComponent<TerritoryZone>().IsLocked) return; // 잠긴 구역이면 무시(안전장치)

        TerritoryDirectionManager.Instance.SwitchActiveBound(camBoundCollider2D);
    }
}
