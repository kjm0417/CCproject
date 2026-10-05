using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonSpring : DungeonBase
{
    private DungeonSpring1F dungeonSpring1F;
    private DungeonSpring2F dungeonSpring2F;
    private DungeonSpring3F dungeonSpring3F;
    private DungeonSpring4F dungeonSpring4F;

    [Header("던전 1층")]
    [SerializeField, Tooltip("던전 1층 적 수 ( 출몰 수 = 처치 목표 수 )")]
    private int enemyCount1F;
    [SerializeField, Tooltip("슬라임 프리팹")]
    private EnemyBase slimePrefab;
    [SerializeField, Tooltip("슬라임 적 출몰 위치")]
    private Transform[] slimeSpawnPoints;
    [SerializeField, Tooltip("1층 클리어 시 이 중 하나를 랜덤으로 활성화 ( 포탈마다 다른 2층 맵 연결 )")]
    private DungeonPortal[] portals1F;

    public int EnemyCount1F => enemyCount1F;
    public EnemyBase SlimePrefab => slimePrefab;
    public Transform[] SlimeSpawnPoints => slimeSpawnPoints;
    public DungeonPortal[] Portals1F => portals1F;

    [Header("던전 2층")]
    [SerializeField, Tooltip("2층 맵별 설정 ( 맵 / 발판 / 3층 포탈 ). 2층_0 -> 포탈_0, 2층_1 -> 포탈_1")]
    private DungeonSpring2FMap[] maps2F;

    public DungeonSpring2FMap[] Maps2F => maps2F;

    [Header("던전 3층")]
    [SerializeField, Tooltip("3층 맵별 설정 ( 맵 / 적 / 적 출몰 위치 / 씨앗 타일 / 4층 포탈 ). 3층_0 -> 포탈_0, 3층_1 -> 포탈_1")]
    private DungeonSpring3FMap[] maps3F;

    public DungeonSpring3FMap[] Maps3F => maps3F;

    [Header("던전 4층")]
    [SerializeField, Tooltip("4층 맵별 설정 ( 맵 / 적 / 적 출몰 위치 / 샘 / 포탈 ). 4층_0 -> 포탈_0, 4층_1 -> 포탈_1")]
    private DungeonSpring4FMap[] maps4F;

    public DungeonSpring4FMap[] Maps4F => maps4F;

    protected override void Awake()
    {
        base.Awake();

        dungeonSpring1F = new DungeonSpring1F(this);
        dungeonSpring2F = new DungeonSpring2F(this);
        dungeonSpring3F = new DungeonSpring3F(this);
        dungeonSpring4F = new DungeonSpring4F(this);
    }

    /// <summary>
    /// 포탈로 맵 이동 시 층별 처리
    /// </summary>
    protected override void OnMapEntered(DungeonMap map)
    {
        switch (map.Floor)
        {
            case 1: dungeonSpring1F.Enter(); break;
            case 2: dungeonSpring2F.Enter(map); break;
            case 3: dungeonSpring3F.Enter(map); break;
            case 4: dungeonSpring4F.Enter(map); break;
        }
    }

}


/// <summary>
/// 봄 던전 1층 - 슬라임 출몰 -> 일정 마리 처치 시 다음 층 포탈 생성
/// </summary>
public class DungeonSpring1F
{
    private DungeonSpring dungeonSpring;

    private int killCount; //처치한 슬라임 수

    public DungeonSpring1F(DungeonSpring dungeonSpring)
    {
        this.dungeonSpring = dungeonSpring;
    }

    /// <summary>
    /// 1층 시작 - 포탈 숨기고 슬라임 출몰
    /// </summary>
    public void Enter()
    {
        killCount = 0;

        SetPortalsActive(false);

        SpawnSlimes();
    }

    /// <summary>
    /// 출몰 위치를 돌아가며 슬라임 생성
    /// </summary>
    private void SpawnSlimes()
    {
        Transform[] spawnPoints = dungeonSpring.SlimeSpawnPoints;
        if (dungeonSpring.SlimePrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[DungeonSpring1F] 슬라임 프리팹 / 출몰 위치 설정 필요");
            return;
        }

        for (int i = 0; i < dungeonSpring.EnemyCount1F; i++)
        {
            Transform spawnPoint = spawnPoints[i % spawnPoints.Length];
            EnemyBase slime = UnityEngine.Object.Instantiate(dungeonSpring.SlimePrefab, spawnPoint.position, Quaternion.identity);
            slime.OnEnemyDied += OnSlimeDied;
        }
    }

    private void OnSlimeDied(EnemyBase slime)
    {
        slime.OnEnemyDied -= OnSlimeDied;

        killCount++;
        Debug.Log($"[DungeonSpring1F] 슬라임 처치 {killCount}/{dungeonSpring.EnemyCount1F}");

        if (killCount >= dungeonSpring.EnemyCount1F)
        {
            OpenPortal();
        }
    }

    /// <summary>
    /// 다음 층 이동 포탈 생성 - 등록된 포탈 중 하나 랜덤
    /// </summary>
    private void OpenPortal()
    {
        DungeonPortal[] portals = dungeonSpring.Portals1F;
        if (portals == null || portals.Length == 0)
        {
            Debug.LogError("[DungeonSpring1F] 1층 포탈 설정 필요");
            return;
        }

        DungeonPortal portal = portals[UnityEngine.Random.Range(0, portals.Length)];
        if (portal == null) return;

        portal.gameObject.SetActive(true);
        Debug.Log($"[DungeonSpring1F] 1층 클리어 - 포탈 생성 : {portal.name} -> {(portal.TargetMap != null ? portal.TargetMap.MapID : "없음")}");
    }

    private void SetPortalsActive(bool active)
    {
        if (dungeonSpring.Portals1F == null) return;

        foreach (DungeonPortal portal in dungeonSpring.Portals1F)
        {
            if (portal != null)
            {
                portal.gameObject.SetActive(active);
            }
        }
    }
}

/// <summary>
/// 봄 던전 2층 맵 1개 설정 ( 맵 / 발판 / 3층 포탈 묶음 )
/// </summary>
[Serializable]
public class DungeonSpring2FMap
{
    [Tooltip("2층 맵")]
    public DungeonMap Map;
    [Tooltip("이 맵의 발판 ( 모두 충족 시 포탈 활성화 )")]
    public DungeonFootThold[] FootTholds;
    [Tooltip("이 맵에서 활성화할 3층 포탈")]
    public DungeonPortal Portal;
}

/// <summary>
/// 봄 던전 2층 - 맵 여러 개 ( 들어온 포탈에 따라 맵 결정 )
/// 맵 안 발판 모두 충족 ( N초 밟기 -> 버튼 클릭 ) 시 그 맵의 3층 포탈 활성화
/// </summary>
public class DungeonSpring2F
{
    private DungeonSpring dungeonSpring;

    private DungeonSpring2FMap current; //현재 진행 중인 2층 맵
    private int completedCount;

    public DungeonSpring2F(DungeonSpring dungeonSpring)
    {
        this.dungeonSpring = dungeonSpring;
    }

    /// <summary>
    /// 2층 진입 - 들어온 맵에 해당하는 설정으로 진행
    /// </summary>
    public void Enter(DungeonMap map)
    {
        Debug.Log($"[DungeonSpring2F] 2층 진입 : {map.MapID}");

        Exit();

        current = FindMapSetting(map);
        if (current == null)
        {
            Debug.LogError($"[DungeonSpring2F] {map.MapID} 설정 없음 - DungeonSpring.maps2F 등록 필요");
            return;
        }

        if (current.Portal != null)
        {
            current.Portal.gameObject.SetActive(false);
        }

        if (current.FootTholds == null || current.FootTholds.Length == 0)
        {
            Debug.LogError($"[DungeonSpring2F] {map.MapID} 발판 설정 필요");
            return;
        }

        foreach (DungeonFootThold footThold in current.FootTholds)
        {
            if (footThold == null) continue;

            footThold.ResetState();
            footThold.OnCompleted += OnFootTholdCompleted;
        }
    }

    /// <summary>
    /// 이전 진입 구독 해제 / 초기화
    /// </summary>
    private void Exit()
    {
        if (current != null && current.FootTholds != null)
        {
            foreach (DungeonFootThold footThold in current.FootTholds)
            {
                if (footThold != null)
                {
                    footThold.OnCompleted -= OnFootTholdCompleted;
                }
            }
        }

        current = null;
        completedCount = 0;
    }

    private void OnFootTholdCompleted(DungeonFootThold footThold)
    {
        footThold.OnCompleted -= OnFootTholdCompleted;

        completedCount++;
        Debug.Log($"[DungeonSpring2F] 발판 조건 충족 {completedCount}/{current.FootTholds.Length}");

        if (completedCount >= current.FootTholds.Length)
        {
            OpenPortal();
        }
    }

    /// <summary>
    /// 현재 맵의 3층 이동 포탈 생성
    /// </summary>
    private void OpenPortal()
    {
        if (current.Portal == null)
        {
            Debug.LogError($"[DungeonSpring2F] {current.Map.MapID} 3층 포탈 설정 필요");
            return;
        }

        current.Portal.gameObject.SetActive(true);
        Debug.Log($"[DungeonSpring2F] 2층 클리어 - 포탈 생성 : {current.Portal.name}");
    }

    private DungeonSpring2FMap FindMapSetting(DungeonMap map)
    {
        if (dungeonSpring.Maps2F == null) return null;

        foreach (DungeonSpring2FMap setting in dungeonSpring.Maps2F)
        {
            if (setting != null && setting.Map == map)
            {
                return setting;
            }
        }
        return null;
    }
}

/// <summary>
/// 봄 던전 3층 맵 1개 설정 ( 맵 / 적 / 적 출몰 위치 / 씨앗 타일 / 4층 포탈 묶음 )
/// </summary>
[Serializable]
public class DungeonSpring3FMap
{
    [Tooltip("3층 맵")]
    public DungeonMap Map;
    [Tooltip("이 맵의 적 프리팹 ( 출몰 위치와 같은 순서, DungeonEnemySeedDrop 부착 필요 )")]
    public EnemyBase[] EnemyPrefabs;
    [Tooltip("이 맵의 적 출몰 위치 ( EnemyPrefabs 와 같은 순서 )")]
    public Transform[] EnemySpawnPoints;
    [Tooltip("이 맵의 씨앗 타일 ( 모두 맞는 씨앗 심으면 포탈 활성화 )")]
    public DungeonSeedTile[] SeedTiles;
    [Tooltip("이 맵에서 활성화할 4층 포탈")]
    public DungeonPortal Portal;
}

/// <summary>
/// 봄 던전 3층 - 맵 여러 개 ( 들어온 포탈에 따라 맵 결정 )
/// 스폰 위치마다 적 1마리, 적마다 정해진 씨앗 드랍 ( DungeonEnemySeedDrop )
/// 맵 안 씨앗 타일에 모두 맞는 씨앗 심으면 그 맵의 4층 포탈 생성
/// </summary>
public class DungeonSpring3F
{
    private DungeonSpring dungeonSpring;

    private DungeonSpring3FMap current; //현재 진행 중인 3층 맵
    private int plantedCount; //심은 타일 수

    public DungeonSpring3F(DungeonSpring dungeonSpring)
    {
        this.dungeonSpring = dungeonSpring;
    }

    /// <summary>
    /// 3층 진입 - 들어온 맵 설정으로 포탈 숨기고 타일 초기화 -> 적 출몰
    /// </summary>
    public void Enter(DungeonMap map)
    {
        Debug.Log($"[DungeonSpring3F] 3층 진입 : {map.MapID}");

        Exit();

        current = FindMapSetting(map);
        if (current == null)
        {
            Debug.LogError($"[DungeonSpring3F] {map.MapID} 설정 없음 - DungeonSpring.maps3F 등록 필요");
            return;
        }

        if (current.Portal != null)
        {
            current.Portal.gameObject.SetActive(false);
        }

        InitSeedTiles();
        SpawnEnemies();
    }

    /// <summary>
    /// 이전 진입 구독 해제 / 초기화
    /// </summary>
    private void Exit()
    {
        if (current != null && current.SeedTiles != null)
        {
            foreach (DungeonSeedTile tile in current.SeedTiles)
            {
                if (tile != null)
                {
                    tile.OnPlanted -= OnTilePlanted;
                }
            }
        }

        current = null;
        plantedCount = 0;
    }

    private void InitSeedTiles()
    {
        if (current.SeedTiles == null || current.SeedTiles.Length == 0)
        {
            Debug.LogError($"[DungeonSpring3F] {current.Map.MapID} 씨앗 타일 설정 필요");
            return;
        }

        foreach (DungeonSeedTile tile in current.SeedTiles)
        {
            if (tile == null) continue;

            tile.ResetState();
            tile.OnPlanted += OnTilePlanted;
        }
    }

    /// <summary>
    /// 스폰 위치마다 적 1마리 생성 ( 씨앗은 적 프리팹의 DungeonEnemySeedDrop 이 드랍 )
    /// </summary>
    private void SpawnEnemies()
    {
        EnemyBase[] prefabs = current.EnemyPrefabs;
        Transform[] spawnPoints = current.EnemySpawnPoints;

        if (prefabs == null || spawnPoints == null || prefabs.Length != spawnPoints.Length)
        {
            Debug.LogError($"[DungeonSpring3F] {current.Map.MapID} 적 프리팹 / 출몰 위치 수가 같아야 함");
            return;
        }

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (prefabs[i] == null || spawnPoints[i] == null) continue;

            if (prefabs[i].GetComponent<DungeonEnemySeedDrop>() == null)
            {
                Debug.LogError($"[DungeonSpring3F] {prefabs[i].name} DungeonEnemySeedDrop 부착 필요");
            }

            UnityEngine.Object.Instantiate(prefabs[i], spawnPoints[i].position, Quaternion.identity);
        }
    }

    private void OnTilePlanted(DungeonSeedTile tile)
    {
        tile.OnPlanted -= OnTilePlanted;

        plantedCount++;
        Debug.Log($"[DungeonSpring3F] 씨앗 심기 {plantedCount}/{current.SeedTiles.Length}");

        if (plantedCount >= current.SeedTiles.Length)
        {
            OpenPortal();
        }
    }

    /// <summary>
    /// 현재 맵의 4층 이동 포탈 생성
    /// </summary>
    private void OpenPortal()
    {
        if (current.Portal == null)
        {
            Debug.LogError($"[DungeonSpring3F] {current.Map.MapID} 4층 포탈 설정 필요");
            return;
        }

        current.Portal.gameObject.SetActive(true);
        Debug.Log($"[DungeonSpring3F] 3층 클리어 - 포탈 생성 : {current.Portal.name}");
    }

    private DungeonSpring3FMap FindMapSetting(DungeonMap map)
    {
        if (dungeonSpring.Maps3F == null) return null;

        foreach (DungeonSpring3FMap setting in dungeonSpring.Maps3F)
        {
            if (setting != null && setting.Map == map)
            {
                return setting;
            }
        }
        return null;
    }
}

/// <summary>
/// 봄 던전 4층 맵 1개 설정 ( 맵 / 적 / 적 출몰 위치 / 샘 / 포탈 묶음 )
/// </summary>
[Serializable]
public class DungeonSpring4FMap
{
    [Tooltip("4층 맵")]
    public DungeonMap Map;
    [Tooltip("이 맵에 출몰할 적 프리팹 ( 출몰 시 랜덤 )")]
    public EnemyBase[] EnemyPrefabs;
    [Tooltip("적 출몰 위치 ( 사방 ) - 진입 시 위치마다 1마리")]
    public Transform[] EnemySpawnPoints;
    [Tooltip("적이 죽은 뒤 새 적이 출몰하기까지 대기 ( 초 )")]
    public float RespawnDelay = 3f;
    [Tooltip("이 맵의 샘 ( 정화율 100% 시 포탈 활성화 )")]
    public DungeonFountain Fountain;
    [Tooltip("정화 완료 시 활성화할 포탈")]
    public DungeonPortal Portal;
}

/// <summary>
/// 봄 던전 4층 - 진입 시 출몰 위치마다 적 1마리, 적이 죽으면 새 적 출몰 ( 정화 완료 전까지 )
/// 샘 안에 있으면 정화율 증가 / 적이 샘에 닿으면 감소. 정화율 100% 시 그 맵의 포탈 생성 ( 5층 )
/// </summary>
public class DungeonSpring4F
{
    private DungeonSpring dungeonSpring;

    private DungeonSpring4FMap current; //현재 진행 중인 4층 맵
    private bool isSpawning; //정화 완료 / 맵 이탈 시 false -> 새 적 출몰 X

    public DungeonSpring4F(DungeonSpring dungeonSpring)
    {
        this.dungeonSpring = dungeonSpring;
    }

    /// <summary>
    /// 4층 진입 - 포탈 숨기고 샘 정화 시작 -> 적 출몰 시작
    /// </summary>
    public void Enter(DungeonMap map)
    {
        Debug.Log($"[DungeonSpring4F] 4층 진입 : {map.MapID}");

        Exit();

        current = FindMapSetting(map);
        if (current == null)
        {
            Debug.LogError($"[DungeonSpring4F] {map.MapID} 설정 없음 - DungeonSpring.maps4F 등록 필요");
            return;
        }

        if (current.Portal != null)
        {
            current.Portal.gameObject.SetActive(false);
        }

        if (current.Fountain == null)
        {
            Debug.LogError($"[DungeonSpring4F] {map.MapID} 샘 설정 필요");
            return;
        }

        current.Fountain.OnPurified += OnPurified;
        current.Fountain.Begin();

        SpawnInitialEnemies();
    }

    /// <summary>
    /// 이전 진입 정리 ( 구독 해제 / 출몰 중지 )
    /// </summary>
    private void Exit()
    {
        isSpawning = false;

        if (current != null && current.Fountain != null)
        {
            current.Fountain.OnPurified -= OnPurified;
        }

        current = null;
    }

    /// <summary>
    /// 진입 시 출몰 위치마다 적 1마리
    /// </summary>
    private void SpawnInitialEnemies()
    {
        if (current.EnemyPrefabs == null || current.EnemyPrefabs.Length == 0
            || current.EnemySpawnPoints == null || current.EnemySpawnPoints.Length == 0)
        {
            Debug.LogError($"[DungeonSpring4F] {current.Map.MapID} 적 프리팹 / 출몰 위치 설정 필요");
            return;
        }

        isSpawning = true;

        foreach (Transform spawnPoint in current.EnemySpawnPoints)
        {
            SpawnEnemy(spawnPoint);
        }
    }

    /// <summary>
    /// 랜덤 적 생성 - 죽으면 새 적 출몰 예약
    /// </summary>
    private void SpawnEnemy(Transform spawnPoint)
    {
        EnemyBase prefab = current.EnemyPrefabs[UnityEngine.Random.Range(0, current.EnemyPrefabs.Length)];
        if (prefab == null || spawnPoint == null) return;

        EnemyBase enemy = UnityEngine.Object.Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        enemy.OnEnemyDied += OnEnemyDied;
    }

    private void OnEnemyDied(EnemyBase enemy)
    {
        enemy.OnEnemyDied -= OnEnemyDied;

        if (!isSpawning) return;

        dungeonSpring.StartCoroutine(RespawnRoutine(current));
    }

    /// <summary>
    /// 대기 후 랜덤 위치에 새 적 출몰 ( 그 사이 정화 완료 / 맵 이탈이면 취소 )
    /// </summary>
    private IEnumerator RespawnRoutine(DungeonSpring4FMap setting)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, setting.RespawnDelay));

        if (!isSpawning || current != setting) yield break;

        Transform[] spawnPoints = setting.EnemySpawnPoints;
        SpawnEnemy(spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)]);
    }

    private void OnPurified(DungeonFountain fountain)
    {
        fountain.OnPurified -= OnPurified;

        isSpawning = false;
        OpenPortal();
    }

    /// <summary>
    /// 정화 완료 - 현재 맵의 포탈 생성
    /// </summary>
    private void OpenPortal()
    {
        if (current.Portal == null)
        {
            Debug.LogError($"[DungeonSpring4F] {current.Map.MapID} 포탈 설정 필요");
            return;
        }

        current.Portal.gameObject.SetActive(true);
        Debug.Log($"[DungeonSpring4F] 4층 클리어 - 포탈 생성 : {current.Portal.name}");
    }

    private DungeonSpring4FMap FindMapSetting(DungeonMap map)
    {
        if (dungeonSpring.Maps4F == null) return null;

        foreach (DungeonSpring4FMap setting in dungeonSpring.Maps4F)
        {
            if (setting != null && setting.Map == map)
            {
                return setting;
            }
        }
        return null;
    }
}
