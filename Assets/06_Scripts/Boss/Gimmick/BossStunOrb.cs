using System;
using UnityEngine;

/// <summary>
/// 기믹 보상 구체 - 플레이어가 닿으면 습득 ( OnPicked ) 후 제거
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BossStunOrb : MonoBehaviour, IPoolable
{
    public event Action OnPicked;

    private bool picked;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    public void OnSpawned()
    {
        picked = false;
    }

    /// <summary>
    /// 풀 반납 - 습득 구독 정리 ( 재사용 시 중복 스턴 방지 )
    /// </summary>
    public void OnDespawned()
    {
        OnPicked = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (picked || other.GetComponentInParent<PlayerVitals>() == null) return;

        picked = true;
        OnPicked?.Invoke();
        ObjectPoolManager.Despawn(gameObject);
        #region [이전] Instantiate / Destroy
        // Destroy(gameObject);
        #endregion
    }
}
