using UnityEngine;

/// <summary>
/// 풀에서 생성된 오브젝트 표시 ( ObjectPoolManager 가 자동 부착 )
/// 재사용 시 자식 위치 복원 / IPoolable 콜백 전달
/// </summary>
[DisallowMultipleComponent]
public class PooledObject : MonoBehaviour
{
    public GameObject Prefab { get; private set; }
    public bool IsSpawned { get; private set; }
    public int SpawnVersion { get; private set; } //지연 반납 취소 판정용

    private IPoolable[] poolables;
    private Transform[] children;
    private Vector3[] childPositions;
    private Quaternion[] childRotations;
    private Vector3[] childScales;
    private Rigidbody2D[] bodies;

    public void Init(GameObject prefab)
    {
        Prefab = prefab;
        poolables = GetComponentsInChildren<IPoolable>(true);
        bodies = GetComponentsInChildren<Rigidbody2D>(true);

        //자식 초기 자세 저장 ( 자식만 이동하는 적 등 재사용 시 원위치 )
        children = GetComponentsInChildren<Transform>(true);
        childPositions = new Vector3[children.Length];
        childRotations = new Quaternion[children.Length];
        childScales = new Vector3[children.Length];
        for (int i = 0; i < children.Length; i++)
        {
            childPositions[i] = children[i].localPosition;
            childRotations[i] = children[i].localRotation;
            childScales[i] = children[i].localScale;
        }
    }

    public void NotifySpawned()
    {
        IsSpawned = true;
        SpawnVersion++;

        foreach (Rigidbody2D body in bodies)
        {
            if (body == null) continue;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        foreach (IPoolable poolable in poolables)
        {
            poolable.OnSpawned();
        }
    }

    public void NotifyDespawned()
    {
        IsSpawned = false;

        foreach (IPoolable poolable in poolables)
        {
            poolable.OnDespawned();
        }
    }

    public void RestoreChildPose()
    {
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] == null || children[i] == transform) continue;

            children[i].localPosition = childPositions[i];
            children[i].localRotation = childRotations[i];
            children[i].localScale = childScales[i];
        }
    }
}
