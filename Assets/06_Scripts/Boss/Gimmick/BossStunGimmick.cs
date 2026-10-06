using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 스턴 기믹 공용 - 모든 활성 구역이 활성화되면 보스 스턴 -> 공격 타임
/// 보스별 추가 조건은 상속 후 CanZoneProgress override
/// </summary>
public class BossStunGimmick : MonoBehaviour
{
    [SerializeField] protected BossBase boss;
    [SerializeField] protected List<BossActivationZone> zones = new List<BossActivationZone>();
    [SerializeField] protected float stunDuration = 8f;

    protected virtual void Start()
    {
        foreach (BossActivationZone zone in zones)
        {
            BossActivationZone captured = zone;
            zone.Initialize(boss, () => CanZoneProgress(captured));
            zone.OnActivated += HandleZoneActivated;
        }

        boss.OnStunEndedEvent += ResetAll;
    }

    protected virtual void OnDestroy()
    {
        foreach (BossActivationZone zone in zones)
        {
            if (zone != null) zone.OnActivated -= HandleZoneActivated;
        }
        if (boss != null) boss.OnStunEndedEvent -= ResetAll;
    }

    /// <summary> 구역 진행 추가 조건 - 기본은 항상 허용 </summary>
    protected virtual bool CanZoneProgress(BossActivationZone zone)
    {
        return true;
    }

    private void HandleZoneActivated()
    {
        foreach (BossActivationZone zone in zones)
        {
            if (!zone.IsActivated) return;
        }

        boss.ApplyStun(stunDuration);
    }

    protected void ResetAll()
    {
        foreach (BossActivationZone zone in zones)
        {
            zone.ResetZone();
        }
    }
}
