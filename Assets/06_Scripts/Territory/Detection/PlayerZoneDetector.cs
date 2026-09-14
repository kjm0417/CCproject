using UnityEngine;

/// <summary>
/// 플레이어가 현재 어떤 Territory Zone에 있는지 추적
/// </summary>
public class PlayerZoneDetector : MonoBehaviour
{
    private static TerritoryZone currentZone;
    private TerritoryZone myZone;

    public static TerritoryZone GetCurrentZone() => currentZone;

    private void Start()
    {
        myZone = GetComponent<TerritoryZone>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            currentZone = myZone;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && currentZone == myZone)
        {
            currentZone = null;
        }
    }
}
