using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Shop_Squirrel prefab 루트에 붙이는 스크립트.
/// 상점 섬 해금으로 생성될 때 씬에 미리 배치된 가장 가까운 Shop_Object에 청설모(ShopkeeperAI)와 ShopArea를 연결하고,
/// ShopArea / ShopStandPoint를 Shop_Object 앞으로 옮긴다.
/// </summary>
public class ShopkeeperLinker : MonoBehaviour
{
    [Header("연결 대상")]
    [SerializeField] private ShopkeeperAI shopkeeper;
    [SerializeField] private ShopArea shopArea;
    [SerializeField] private Transform shopStandPoint;

    [Header("배치")]
    [Tooltip("Shop_Object 콜라이더 바깥으로 ShopArea를 넓힐 거리 (한 쪽 기준)")]
    [SerializeField, Min(0f)] private float areaPadding = 1f;
    [Tooltip("Shop_Object 근처에서 청설모가 설 NavMesh 지점을 찾을 거리")]
    [SerializeField, Min(0.1f)] private float standSampleDistance = 2f;

    private bool hasStarted;

    private void Start()
    {
        // 처음 생성은 켜진 뒤에 위치가 정해지므로 Start에서 연결
        hasStarted = true;
        Link();
    }

    private void OnEnable()
    {
        // 풀에서 다시 꺼낼 때는 위치가 먼저 정해진 뒤 켜진다
        if (hasStarted) Link();
    }

    private void Link()
    {
        ShopObject shop = FindNearestShop();
        if (shop == null)
        {
            Debug.LogWarning("ShopkeeperLinker: 연결할 Shop_Object가 씬에 없습니다.", this);
            return;
        }

        PlaceNearShop(shop);
        shop.Connect(shopkeeper, shopArea);
    }

    private ShopObject FindNearestShop()
    {
        ShopObject nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (ShopObject shop in FindObjectsByType<ShopObject>(FindObjectsSortMode.None))
        {
            float distance = (shop.transform.position - transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = shop;
            }
        }
        return nearest;
    }

    /// <summary>
    /// 플레이어가 Shop_Object와 상호작용하는 자리를 ShopArea가 덮도록 옮기고, 청설모가 설 지점을 상점 앞 NavMesh 위로 옮긴다
    /// </summary>
    private void PlaceNearShop(ShopObject shop)
    {
        Bounds shopBounds = shop.GetComponent<Collider2D>().bounds;

        if (shopArea != null)
        {
            BoxCollider2D areaCollider = shopArea.GetComponent<BoxCollider2D>();
            shopArea.transform.position = new Vector3(shopBounds.center.x, shopBounds.center.y, shopArea.transform.position.z);
            areaCollider.offset = Vector2.zero;
            areaCollider.size = (Vector2)shopBounds.size + Vector2.one * (areaPadding * 2f);
        }

        if (shopStandPoint != null
            && NavMesh.SamplePosition(shopBounds.center, out NavMeshHit hit, standSampleDistance, NavMesh.AllAreas))
        {
            shopStandPoint.position = new Vector3(hit.position.x, hit.position.y, shopStandPoint.position.z);
        }
    }
}
