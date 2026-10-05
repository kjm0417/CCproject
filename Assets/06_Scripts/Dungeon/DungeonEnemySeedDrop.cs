using UnityEngine;

/// <summary>
/// 던전용 적 - 죽으면 pickupPrefab 에 설정된 씨앗 드랍 ( 같은 적이라도 던전용 프리팹에만 부착 )
/// </summary>
[RequireComponent(typeof(EnemyBase))]
public class DungeonEnemySeedDrop : MonoBehaviour
{
    [SerializeField, Tooltip("드랍 아이템 프리팹 ( PickupItem 에 설정된 씨앗 그대로 드랍 )")]
    private PickupItem pickupPrefab;

    private EnemyBase enemy;
    private bool isDropped; //중복 드랍 방지

    private void Awake()
    {
        enemy = GetComponent<EnemyBase>();
    }

    private void OnEnable()
    {
        enemy.OnEnemyDied += OnEnemyDied;
    }

    private void OnDisable()
    {
        enemy.OnEnemyDied -= OnEnemyDied;
    }

    private void OnEnemyDied(EnemyBase enemy)
    {
        if (isDropped) return;

        if (pickupPrefab == null || pickupPrefab.Item == null)
        {
            Debug.LogError($"[DungeonEnemySeedDrop] {name} 드랍 아이템 프리팹 / 아이템 설정 필요");
            return;
        }

        isDropped = true;

        PickupItem pickup = Instantiate(pickupPrefab, transform.position, Quaternion.identity);
        pickup.PrefabName = pickupPrefab.name;

        Debug.Log($"[DungeonEnemySeedDrop] {name} 씨앗 드랍 : {pickupPrefab.Item.ItemName}");
    }
}
