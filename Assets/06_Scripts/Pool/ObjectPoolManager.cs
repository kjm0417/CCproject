using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 공용 오브젝트 풀 ( Unity ObjectPool 기반 )
/// - 씬마다 "@ObjectPool" 루트 자동 생성, 프리팹 이름별 자식 그룹에 생성 / 반납 ( 사용 중 + 대기 모두 그룹 아래 )
/// - Spawn : 위치 / 부모 세팅 후 활성화 -> IPoolable.OnSpawned
/// - Despawn : IPoolable.OnDespawned -> 비활성화 -> 그룹으로 복귀 ( 풀 오브젝트가 아니면 Destroy )
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    private const string RootName = "@ObjectPool";
    private const int DefaultCapacity = 8;
    private const int MaxSize = 256;

    private static ObjectPoolManager instance;

    private readonly Dictionary<GameObject, ObjectPool<PooledObject>> pools = new Dictionary<GameObject, ObjectPool<PooledObject>>();
    private readonly Dictionary<GameObject, Transform> groups = new Dictionary<GameObject, Transform>();
    private Transform staging; //생성 직후 Awake 지연용 ( 비활성 부모 )

    public static ObjectPoolManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GameObject(RootName).AddComponent<ObjectPoolManager>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        staging = new GameObject("_Staging").transform;
        staging.SetParent(transform, false);
        staging.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    #region 생성
    public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
    {
        if (prefab == null) return null;

        GameObject go = Spawn(prefab.gameObject, position, rotation, parent);
        return go.GetComponent<T>();
    }

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null) return null;

        return Instance.SpawnInternal(prefab, position, rotation, parent);
    }

    private GameObject SpawnInternal(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        PooledObject pooled = GetPool(prefab).Get();
        Transform t = pooled.transform;

        //활성화 전에 위치 / 부모 세팅 -> OnEnable 시점에 올바른 위치
        t.SetParent(parent != null ? parent : groups[prefab], false);
        t.SetPositionAndRotation(position, rotation);

        pooled.gameObject.SetActive(true);
        pooled.NotifySpawned();
        return pooled.gameObject;
    }
    #endregion

    #region 반납
    /// <summary>
    /// 풀로 반납 ( 풀 오브젝트가 아니면 Destroy )
    /// </summary>
    public static void Despawn(GameObject obj)
    {
        if (obj == null) return;

        PooledObject pooled = obj.GetComponent<PooledObject>();
        if (pooled == null || instance == null)
        {
            Destroy(obj);
            return;
        }

        instance.DespawnInternal(pooled);
    }

    /// <summary>
    /// delay 초 뒤 반납 ( 그 사이 반납 -> 재사용됐으면 취소 )
    /// </summary>
    public static void Despawn(GameObject obj, float delay)
    {
        if (obj == null) return;

        if (delay <= 0f)
        {
            Despawn(obj);
            return;
        }

        PooledObject pooled = obj.GetComponent<PooledObject>();
        if (pooled == null || instance == null)
        {
            Destroy(obj, delay);
            return;
        }

        instance.StartCoroutine(instance.DespawnAfter(pooled, pooled.SpawnVersion, delay));
    }

    private IEnumerator DespawnAfter(PooledObject pooled, int version, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (pooled != null && pooled.IsSpawned && pooled.SpawnVersion == version)
        {
            DespawnInternal(pooled);
        }
    }

    private void DespawnInternal(PooledObject pooled)
    {
        if (!pooled.IsSpawned) return; //중복 반납 방지

        pooled.NotifyDespawned();
        pools[pooled.Prefab].Release(pooled);
    }
    #endregion

    #region 풀
    private ObjectPool<PooledObject> GetPool(GameObject prefab)
    {
        if (pools.TryGetValue(prefab, out ObjectPool<PooledObject> pool)) return pool;

        Transform group = new GameObject(prefab.name).transform;
        group.SetParent(transform, false);
        groups.Add(prefab, group);

        pool = new ObjectPool<PooledObject>(
            createFunc: () => Create(prefab),
            actionOnGet: null, //활성화는 위치 세팅 후 SpawnInternal 에서
            actionOnRelease: pooled => OnRelease(pooled, group),
            actionOnDestroy: pooled => { if (pooled != null) Destroy(pooled.gameObject); },
            collectionCheck: false,
            defaultCapacity: DefaultCapacity,
            maxSize: MaxSize);

        pools.Add(prefab, pool);
        return pool;
    }

    private PooledObject Create(GameObject prefab)
    {
        //비활성 부모 아래 생성 -> 실제 Spawn 위치에서 활성화될 때 Awake 실행
        GameObject go = Instantiate(prefab, staging);
        go.name = prefab.name;

        PooledObject pooled = go.GetComponent<PooledObject>();
        if (pooled == null) pooled = go.AddComponent<PooledObject>();
        pooled.Init(prefab);
        return pooled;
    }

    private void OnRelease(PooledObject pooled, Transform group)
    {
        pooled.gameObject.SetActive(false);
        pooled.transform.SetParent(group, false);
        pooled.RestoreChildPose();
    }
    #endregion
}
