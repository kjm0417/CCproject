using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 던전용 적 - 죽으면 finalPickupPrefab 에 설정된 씨앗 드랍 ( 같은 적이라도 던전용 프리팹에만 부착 )
/// 드랍 연출은 FieldObjectDropper 공통 사용 ( 드랍 테이블 X )
/// </summary>
[RequireComponent(typeof(EnemyBase))]
public class DungeonEnemySeedDrop : FieldObjectDropper
{
    private EnemyBase enemy;
    private Rigidbody2D body; //실제 이동하는 몸체 ( 슬라임은 자식에 있음 - 루트는 시작 위치에 남음 )
    private bool isDropped; //중복 드랍 방지

    private void Awake()
    {
        enemy = GetComponent<EnemyBase>();
        body = GetComponentInChildren<Rigidbody2D>();
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

        if (finalPickupPrefab == null || finalPickupPrefab.Item == null)
        {
            Debug.LogError($"[DungeonEnemySeedDrop] {name} 드랍 아이템 프리팹 / 아이템 설정 필요");
            return;
        }

        isDropped = true;

        List<ItemDrop> drops = new List<ItemDrop> { new ItemDrop(finalPickupPrefab.Item, finalPickupPrefab.Count) };
        Vector3 dropPosition = body != null ? (Vector3)body.position : transform.position; //죽은 위치
        CreatePickUpItem(finalPickupPrefab, drops, dropPosition);

        Debug.Log($"[DungeonEnemySeedDrop] {name} 씨앗 드랍 : {finalPickupPrefab.Item.ItemName}");
    }
}
