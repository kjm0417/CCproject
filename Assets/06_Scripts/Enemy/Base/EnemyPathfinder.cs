using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

/// <summary>
/// 적 공통 - 길찾기 ( 벽을 돌아가는 추격 방향 계산 / 진행 방향 막힘 체크 )
/// 대상까지 막힌 게 없으면 직선, 막혀 있으면 A* 경로의 다음 지점 방향
/// </summary>
[RequireComponent(typeof(Seeker))]
public class EnemyPathfinder : MonoBehaviour
{
    [SerializeField, Tooltip("경로 재계산 간격 (초)")]
    private float repathInterval = 0.3f;
    [SerializeField, Tooltip("다음 지점 도착 판정 거리")]
    private float nextWaypointDistance = 0.3f;
    private const float DefaultBodyRadius = 0.3f; //WallBody 없을 때

    private Seeker seeker;
    private Rigidbody2D rb;
    private int obstacleMask;

    //몸 반지름 ( 월드 기준 ) - 직선 이동 / 막힘 판정용, WallBody 콜라이더 실제 크기에서 계산
    //프리팹 크기를 바꿔도 자동으로 따라가도록 직접 입력하지 않음
    private float bodyRadius = DefaultBodyRadius;

    private readonly List<Vector3> path = new List<Vector3>();
    private int waypointIndex;
    private float lastRepathTime = float.NegativeInfinity;

    private void Awake()
    {
        seeker = GetComponent<Seeker>();
        rb = GetComponentInChildren<Rigidbody2D>();
        obstacleMask = LayerMask.GetMask("Obstacle");

        Transform wallBody = rb != null ? rb.transform.Find("WallBody") : null;
        CircleCollider2D body = wallBody != null ? wallBody.GetComponent<CircleCollider2D>() : null;
        if (body != null)
        {
            Vector3 scale = body.transform.lossyScale;
            bodyRadius = body.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        }
        else
        {
            Debug.LogWarning($"[EnemyPathfinder] {name} WallBody 없음 - 기본 반지름 {DefaultBodyRadius} 사용");
        }
    }

    private void OnDisable()
    {
        //풀 반납 시 이전 경로 정리
        seeker.CancelCurrentPathRequest();
        path.Clear();
        lastRepathTime = float.NegativeInfinity;
    }

    /// <summary>
    /// 해당 방향으로 distance 만큼 가는 길에 벽이 있는지
    /// </summary>
    public bool IsBlocked(Vector2 direction, float distance)
    {
        return Physics2D.CircleCast(rb.position, bodyRadius, direction, distance, obstacleMask).collider != null;
    }

    /// <summary>
    /// 대상 추격 이동 방향 ( 정규화 )
    /// </summary>
    public Vector2 GetChaseDirection(Vector2 target)
    {
        Vector2 position = rb.position;
        Vector2 toTarget = target - position;
        Vector2 straight = toTarget.normalized;

        //직선으로 갈 수 있거나 길찾기 그리드 없는 씬이면 직선
        if (AstarPath.active == null || !IsBlocked(straight, toTarget.magnitude))
        {
            path.Clear();
            return straight;
        }

        //일정 간격으로 경로 재계산 ( 매 프레임 X )
        if (Time.time >= lastRepathTime + repathInterval && seeker.IsDone())
        {
            lastRepathTime = Time.time;
            seeker.StartPath(position, target, OnPathComplete);
        }

        //첫 경로 계산 전 - 일단 직선 ( 벽 충돌 콜라이더가 통과는 막음 )
        if (path.Count == 0)
            return straight;

        //다음 지점에 도착했으면 그 다음 지점으로
        while (waypointIndex < path.Count - 1 &&
               Vector2.Distance(position, path[waypointIndex]) < nextWaypointDistance)
        {
            waypointIndex++;
        }

        Vector2 toWaypoint = (Vector2)path[waypointIndex] - position;
        return toWaypoint.sqrMagnitude > 0.0001f ? toWaypoint.normalized : straight;
    }

    private void OnPathComplete(Path p)
    {
        path.Clear();

        if (p.error)
            return;

        path.AddRange(p.vectorPath);
        waypointIndex = path.Count > 1 ? 1 : 0; //0번은 시작 위치
    }
}
