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

        TeleportPlayer(player, map.ArrivalPoint.position);

        Debug.Log($"[Dungeon] 맵 이동 : {map.Floor}층 {map.MapID}");
        OnMapEntered(map);
    }

    /// <summary>
    /// 층/맵 진입 시 처리 ( 던전별로 구현 )
    /// </summary>
    protected abstract void OnMapEntered(DungeonMap map);

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
