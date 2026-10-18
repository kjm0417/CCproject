using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 구역이 열려 있는 동안 몬스터 스폰 테이블(TerritoryZoneData.MonsterSpawnTable)의 MaxCount만큼 몬스터를 유지하는 컴포넌트.
/// 처치되면 RespawnDelay 뒤 빈 칸에 다시 생성한다. 몬스터는 저장하지 않고 새 게임 / 로드 때 다시 채운다.
/// </summary>
[RequireComponent(typeof(TerritoryZone))]
public class MonsterSpawner : MonoBehaviour
{
    private TerritoryZone territoryZone;
    private ResourceSpawner resourceSpawner;
    private bool hasSpawned; //해금 / Start 중복 생성 방지

    // 살아있는 몬스터 -> 생성한 테이블 항목 ( 처치 시 같은 항목으로 재생성 )
    private readonly Dictionary<EnemyBase, MonsterSpawnEntry> aliveEnemies = new Dictionary<EnemyBase, MonsterSpawnEntry>();

    private PlayerContext player;

    /// <summary>
    /// 영지 몬스터가 처치될 때 (처치된 몬스터 prefab) - 퀘스트 진행용
    /// </summary>
    public static event Action<GameObject> OnAnyEnemyKilled;

    private void Awake()
    {
        territoryZone = GetComponent<TerritoryZone>();
        resourceSpawner = GetComponent<ResourceSpawner>();
    }

    private void Start()
    {
        // 처음부터 열린 구역 / 로드로 열린 구역 (자원 생성 이후라 자원 칸을 피할 수 있음)
        if (!territoryZone.IsLocked) SpawnAll();
    }

    /// <summary>
    /// 테이블 수만큼 몬스터 생성 (구역 해금 시 호출, 중복 호출은 무시)
    /// </summary>
    public void SpawnAll()
    {
        if (hasSpawned) return;
        hasSpawned = true;

        List<MonsterSpawnEntry> table = territoryZone.TerritoryZoneData.MonsterSpawnTable;
        if (table == null) return;

        foreach (MonsterSpawnEntry entry in table)
        {
            if (entry.EnemyPrefab == null) continue;

            for (int i = 0; i < entry.MaxCount; i++)
            {
                Spawn(entry);
            }
        }
    }

    private void Spawn(MonsterSpawnEntry entry)
    {
        if (!TryPickSpawnPosition(out Vector3 position))
        {
            // 빈 칸이 없으면 대기 후 다시 시도
            StartCoroutine(RespawnAfterDelay(entry));
            return;
        }

        EnemyBase enemy = ObjectPoolManager.Spawn(entry.EnemyPrefab, position, Quaternion.identity);
        aliveEnemies[enemy] = entry;
        enemy.OnEnemyDied += OnEnemyDied;
    }

    private void OnEnemyDied(EnemyBase enemy)
    {
        enemy.OnEnemyDied -= OnEnemyDied;

        if (!aliveEnemies.TryGetValue(enemy, out MonsterSpawnEntry entry)) return;
        aliveEnemies.Remove(enemy);

        GiveKillGold(entry);
        OnAnyEnemyKilled?.Invoke(entry.EnemyPrefab.gameObject);

        StartCoroutine(RespawnAfterDelay(entry));
    }

    /// <summary>
    /// 처치 골드 지급 (반복)
    /// </summary>
    private void GiveKillGold(MonsterSpawnEntry entry)
    {
        if (entry.KillGold <= 0) return;

        if (player == null) player = FindAnyObjectByType<PlayerContext>();
        if (player != null && player.Wallet != null) player.Wallet.Add(CurrencyType.Gold, entry.KillGold);
    }

    private IEnumerator RespawnAfterDelay(MonsterSpawnEntry entry)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, entry.RespawnDelay));

        Spawn(entry);
    }

    /// <summary>
    /// 자원 / 작물 / 구조물 주변을 피해 Ground 빈 칸 중 랜덤 위치
    /// </summary>
    private bool TryPickSpawnPosition(out Vector3 position)
    {
        position = default;

        var ground = territoryZone.TerritoryTileMapGround;
        List<Vector3Int> cells = new List<Vector3Int>();
        foreach (Vector3Int cell in ground.cellBounds.allPositionsWithin)
        {
            if (!ground.HasTile(cell)) continue;
            if (resourceSpawner != null && !resourceSpawner.IsSpawnableCell(cell)) continue;
            if (HasEnemyAt(ground, cell)) continue;

            cells.Add(cell);
        }

        if (cells.Count == 0) return false;

        position = ground.GetCellCenterWorld(cells[Random.Range(0, cells.Count)]);
        return true;
    }

    /// <summary>
    /// 살아있는 몬스터가 서 있는 칸인지 ( 같은 칸에 겹쳐 생성 방지 )
    /// </summary>
    private bool HasEnemyAt(UnityEngine.Tilemaps.Tilemap ground, Vector3Int cell)
    {
        foreach (EnemyBase enemy in aliveEnemies.Keys)
        {
            // 실제 이동은 자식 Rigidbody2D가 하므로 그 Transform 기준 (같은 프레임 생성분은 body.position 미갱신)
            Rigidbody2D body = enemy.GetComponentInChildren<Rigidbody2D>();
            Vector3 enemyPosition = body != null ? body.transform.position : enemy.transform.position;

            if (ground.WorldToCell(enemyPosition) == cell) return true;
        }
        return false;
    }
}
