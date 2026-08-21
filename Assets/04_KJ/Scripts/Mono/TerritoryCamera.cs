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
    Transform territoryAnchor; //구역의 중심 
    public Transform TerritoryAnchor => territoryAnchor;
}
