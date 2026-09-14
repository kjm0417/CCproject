using System;
using UnityEngine;

/// <summary>
/// 구역별 카메라 변수 정의
/// </summary>
public class TerritoryCamera : MonoBehaviour
{
    [SerializeField]
    Transform[] territoryAnchor; //구역의 중심 
    public Transform TerritoryMiddleAnchor => territoryAnchor[0];
    public Transform TerritoryUpAnchor => territoryAnchor[1];
    public Transform TerritoryDownAnchor => territoryAnchor[2];
    public Transform TerritoryLeftAnchor => territoryAnchor[3];

    public Transform TerritoryRightAnchor => territoryAnchor[4];
}
