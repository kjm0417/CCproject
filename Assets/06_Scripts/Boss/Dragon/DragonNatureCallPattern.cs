using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 4번 자연의 부름
/// 상하를 잇는 나무뿌리 생성 -> 좌우 이동 방해
/// 뿌리는 2층 DungeonTreeRoot 프리팹 재사용 - 위에 그려진 뿌리부터 곡괭이로 파괴 ( 기존 로직 그대로 )
/// </summary>
public class DragonNatureCallPattern : BossPattern
{
    [Header("뿌리")]
    [SerializeField, Tooltip("2층 나무 뿌리 프리팹 ( DungeonTreeRoot )")]
    private GameObject rootPrefab;
    [SerializeField, Tooltip("생성 위치 / 회전 - 리스트 뒤쪽일수록 위에 그려짐 ( 먼저 파괴 가능 )")]
    private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private int baseSortingOrder = 10;
    [SerializeField] private float spawnInterval = 0.2f;
    [SerializeField, Tooltip("0 이면 파괴될 때까지 유지")]
    private float rootLifetime;
    [SerializeField, Tooltip("다시 사용할 때 남아 있는 이전 뿌리 제거")]
    private bool clearPreviousRoots = true;

    [Header("종료")]
    [SerializeField, Tooltip("생성 완료 후 다음 패턴까지 대기")]
    private float afterSpawnDelay = 1f;

    private readonly List<GameObject> spawnedRoots = new List<GameObject>();
    private Transform staging; // 비활성 부모 - 정렬 순서 세팅 전 OnEnable 방지
    private Transform rootContainer;

    protected override IEnumerator Execute()
    {
        if (rootPrefab == null) yield break;

        EnsureContainers();
        if (clearPreviousRoots) ClearRoots();

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            Transform point = spawnPoints[i];
            if (point == null) continue;

            spawnedRoots.Add(SpawnRoot(point, baseSortingOrder + i));
            if (spawnInterval > 0f) yield return new WaitForSeconds(spawnInterval);
        }

        yield return new WaitForSeconds(afterSpawnDelay);
    }

    /// <summary>
    /// DungeonTreeRoot 는 OnEnable 에서 겹침 판정 -> 정렬 순서를 먼저 세팅한 뒤 활성화
    /// </summary>
    private GameObject SpawnRoot(Transform point, int sortingOrder)
    {
        GameObject root = Instantiate(rootPrefab, point.position, point.rotation, staging);

        foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            sr.sortingOrder = sortingOrder;
        }

        root.transform.SetParent(rootContainer, true);
        if (rootLifetime > 0f) Destroy(root, rootLifetime);
        return root;
    }

    private void EnsureContainers()
    {
        if (staging == null)
        {
            staging = new GameObject("RootStaging").transform;
            staging.SetParent(transform, false);
            staging.gameObject.SetActive(false);
        }
        if (rootContainer == null)
        {
            rootContainer = new GameObject("NatureCallRoots").transform;
        }
    }

    public void ClearRoots()
    {
        foreach (GameObject root in spawnedRoots)
        {
            if (root != null) Destroy(root);
        }
        spawnedRoots.Clear();
    }

    // 패턴 취소 ( 스턴 ) 시에도 이미 생성된 뿌리는 장애물로 유지 - Cleanup override 없음

    private void OnDestroy()
    {
        ClearRoots();
        if (rootContainer != null) Destroy(rootContainer.gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.25f, 0.1f);
        foreach (Transform point in spawnPoints)
        {
            if (point != null) Gizmos.DrawWireSphere(point.position, 0.3f);
        }
    }
}
