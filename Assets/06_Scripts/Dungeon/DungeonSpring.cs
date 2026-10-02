using System;
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

    protected override void Awake()
    {
        base.Awake();

        dungeonSpring1F = new DungeonSpring1F(this);
        dungeonSpring2F = new DungeonSpring2F(this);
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
            //TODO KJ - 3층 / 4층
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
/// 봄 던전 3층
/// </summary>
public class DungeonSpring3F
{

}

/// <summary>
/// 봄 던전 4층
/// </summary>
public class DungeonSpring4F
{

}
