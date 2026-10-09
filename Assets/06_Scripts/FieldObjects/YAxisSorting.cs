using UnityEngine;
using UnityEngine.Rendering;

/// <summary>월드 Y 좌표가 낮은 오브젝트를 앞에 그린다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SortingGroup))]
[DefaultExecutionOrder(1000)]
public sealed class YAxisSorting : MonoBehaviour
{
    // 모든 정렬 대상이 같은 레이어와 배율을 사용해야 서로 비교할 수 있다.
    // TagManager에 등록된 이름에는 끝 공백이 포함되어 있다.
    private const string WorldSortingLayer = "Character ";
    private const float OrdersPerUnit = 100f;

    [SerializeField, Tooltip("비워 두면 이 오브젝트의 위치를 사용합니다. 필요하면 발 위치를 지정하세요.")]
    private Transform sortingPoint;

    [SerializeField, Tooltip("정렬 기준의 월드 Y 보정값입니다.")]
    private float yOffset;

    private SortingGroup sortingGroup;

    public static void EnsureAttached(GameObject target)
    {
        if (!target.TryGetComponent<YAxisSorting>(out _))
        {
            target.AddComponent<YAxisSorting>();
        }
    }

    private void Awake()
    {
        sortingGroup = GetComponent<SortingGroup>();
    }

    private void OnEnable()
    {
        UpdateSortingOrder();
    }

    private void LateUpdate()
    {
        // 이동, 텔레포트, 풀 재사용 후의 최종 위치를 반영한다.
        UpdateSortingOrder();
    }

    private void UpdateSortingOrder()
    {
        if (sortingGroup == null)
        {
            sortingGroup = GetComponent<SortingGroup>();
        }

        float worldY = (sortingPoint != null ? sortingPoint.position.y : transform.position.y) + yOffset;
        sortingGroup.sortingLayerName = WorldSortingLayer;
        sortingGroup.sortingOrder = Mathf.RoundToInt(Mathf.Clamp(-worldY * OrdersPerUnit, -32768f, 32767f));
    }
}
