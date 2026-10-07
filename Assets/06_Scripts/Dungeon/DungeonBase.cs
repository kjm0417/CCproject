using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 던전 공통 - 봄/여름/가을/겨울 던전 공통 스크립트
/// 
/// </summary>
public abstract class DungeonBase : MonoBehaviour
{
    [SerializeField, Tooltip("시작 맵 ( 0층 ). 플레이어는 씬에 배치된 위치에서 시작")]
    private DungeonMap startMap;
    [SerializeField, Tooltip("맵 이동 시 바운드 영역을 바꿀 시네머신 Confiner2D. 비우면 씬에서 자동 탐색")]
    private CinemachineConfiner2D confiner;

    private CinemachineCamera cinemachineCamera;
    private Transform cameraTarget; //보스 맵 외 카메라 타겟 ( 플레이어 )

    public DungeonMap CurrentMap { get; private set; }
    public int CurrentDungeonFloor => CurrentMap != null ? CurrentMap.Floor : 0; //현재 던전 몇층에 있는지 ( 시작 맵 없으면 0층 )

    protected virtual void Awake()
    {
        CurrentMap = startMap;

        //시작 맵 외 나머지 맵은 포탈로 들어갈 때까지 비활성
        foreach (DungeonMap map in FindObjectsByType<DungeonMap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (startMap != null && map != startMap)
            {
                map.gameObject.SetActive(false);
            }
        }
    }

    protected virtual void Start()
    {
        //시작 맵도 층별 처리 ( 하위 클래스 Awake 에서 층 객체 생성 후라 Start 에서 호출 )
        if (CurrentMap != null)
        {
            //테스트용 - 시작 맵에 도착 위치가 있으면 플레이어를 그 위치로 ( 없으면 씬에 배치된 위치 )
            PlayerContext player = FindAnyObjectByType<PlayerContext>();
            if (CurrentMap.HasArrivalPoint)
            {
                TeleportPlayer(player, CurrentMap.ArrivalPoint.position);
            }

            ApplyCamBound(CurrentMap);
            ApplyCameraTarget(CurrentMap, player);
            OnMapEntered(CurrentMap);
        }
    }

    /// <summary>
    /// 맵 이동 - 이전 맵 비활성 -> 새 맵 활성 -> 플레이어 이동 -> 층별 처리
    /// </summary>
    public void MoveToMap(DungeonMap map, PlayerContext player)
    {
        if (map == null) return;

        DungeonMap prevMap = CurrentMap;
        if (prevMap != null && prevMap != map)
        {
            prevMap.gameObject.SetActive(false);
        }

        map.gameObject.SetActive(true);
        CurrentMap = map;

        ApplyCamBound(map);
        TeleportPlayer(player, map.ArrivalPoint.position);
        ApplyCameraTarget(map, player);

        Debug.Log($"[Dungeon] 맵 이동 : {map.Floor}층 {map.MapID}");
        OnMapEntered(map);
    }

    /// <summary>
    /// 층/맵 진입 시 처리 ( 던전별로 구현 )
    /// </summary>
    protected abstract void OnMapEntered(DungeonMap map);

    /// <summary>
    /// 맵의 CamBound 로 시네머신 바운드 영역 변경
    /// </summary>
    private void ApplyCamBound(DungeonMap map)
    {
        if (confiner == null)
        {
            confiner = FindAnyObjectByType<CinemachineConfiner2D>();
        }

        if (confiner == null)
        {
            Debug.LogError("[Dungeon] CinemachineConfiner2D 없음");
            return;
        }

        if (map.CamBound == null)
        {
            Debug.LogError($"[Dungeon] {map.MapID} CamBound 설정 필요");
            return;
        }

        confiner.BoundingShape2D = map.CamBound;
        confiner.InvalidateBoundingShapeCache();
    }

    /// <summary>
    /// 보스 맵 : 카메라 타겟 null + CamBound 중앙 고정 / 그 외 : 플레이어 추적
    /// </summary>
    private void ApplyCameraTarget(DungeonMap map, PlayerContext player)
    {
        if (cinemachineCamera == null)
        {
            cinemachineCamera = confiner != null ? confiner.GetComponent<CinemachineCamera>() : null;
            if (cinemachineCamera == null) cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();
        }

        if (cinemachineCamera == null)
        {
            Debug.LogError("[Dungeon] CinemachineCamera 없음");
            return;
        }

        //씬에 할당된 타겟 ( 플레이어 ) 기억 - 보스 맵에서 나올 때 복구용
        if (cameraTarget == null)
        {
            cameraTarget = cinemachineCamera.Follow != null ? cinemachineCamera.Follow : (player != null ? player.transform : null);
        }

        if (!map.IsBossMap)
        {
            cinemachineCamera.Follow = cameraTarget;
            return;
        }

        cinemachineCamera.Follow = null;

        if (map.CamBound != null)
        {
            Vector3 center = map.CamBound.bounds.center;
            center.z = cinemachineCamera.transform.position.z;
            cinemachineCamera.ForceCameraPosition(center, cinemachineCamera.transform.rotation);
        }
    }

    private void TeleportPlayer(PlayerContext player, Vector3 position)
    {
        if (player == null) return;

        Transform playerTransform = player.transform;
        Vector3 delta = position - playerTransform.position;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = position;
        }
        playerTransform.position = position;

        //카메라가 따라오면서 미끄러지지 않게 순간이동 알림
        CinemachineCore.OnTargetObjectWarped(playerTransform, delta);
    }
}
