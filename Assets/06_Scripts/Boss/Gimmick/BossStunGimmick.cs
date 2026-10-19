using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 스턴 기믹 공용 - 활성 구역들을 관리하고 조건 달성 시 보스 스턴
/// 기본 : 모든 구역 활성화 -> 바로 스턴
/// 보스별 흐름은 OnZoneActivated / OnAllZonesActivated / ResetAll override
/// </summary>
public class BossStunGimmick : MonoBehaviour
{
    [SerializeField] protected BossBase boss;
    [SerializeField] protected List<BossActivationZone> zones = new List<BossActivationZone>();
    [SerializeField] protected float stunDuration = 8f;
    [SerializeField, Min(0f), Tooltip("두 번째 기절부터 줄어드는 시간 ( 한 번만 적용, 누적 X )")]
    private float repeatStunReduction = 1f;

    private int stunCount;

    private readonly List<Action> zoneHandlers = new List<Action>();

    protected virtual void Start()
    {
        for (int i = 0; i < zones.Count; i++)
        {
            BossActivationZone zone = zones[i];
            int index = i;

            zone.Initialize(boss, () => CanZoneProgress(zone));

            Action handler = () => HandleZoneActivated(zone, index);
            zone.OnActivated += handler;
            zoneHandlers.Add(handler);
        }

        boss.OnStunEndedEvent += ResetAll;
    }

    protected virtual void OnDestroy()
    {
        for (int i = 0; i < zones.Count && i < zoneHandlers.Count; i++)
        {
            if (zones[i] != null) zones[i].OnActivated -= zoneHandlers[i];
        }
        if (boss != null) boss.OnStunEndedEvent -= ResetAll;
    }

    /// <summary> 기믹 시도 중 - 플레이어가 구역 안에 있거나 진행도가 남아 있음 ( 스턴 종료 시 리셋 ) </summary>
    public bool IsAttempting
    {
        get
        {
            foreach (BossActivationZone zone in zones)
            {
                if (zone != null && (zone.IsPlayerInside || zone.Progress > 0f)) return true;
            }
            return false;
        }
    }

    /// <summary> 구역 진행 추가 조건 - 기본은 항상 허용 </summary>
    protected virtual bool CanZoneProgress(BossActivationZone zone)
    {
        return true;
    }

    private void HandleZoneActivated(BossActivationZone zone, int index)
    {
        OnZoneActivated(zone, index);

        foreach (BossActivationZone z in zones)
        {
            if (!z.IsActivated) return;
        }
        OnAllZonesActivated();
    }

    /// <summary> 구역 하나가 활성화됐을 때 </summary>
    protected virtual void OnZoneActivated(BossActivationZone zone, int index) { }

    /// <summary> 모든 구역 활성화 - 기본은 바로 스턴 </summary>
    protected virtual void OnAllZonesActivated()
    {
        ApplyStun();
    }

    /// <summary> 첫 기절은 stunDuration, 이후는 repeatStunReduction 만큼 짧게 ( 1회 감소 고정 ) </summary>
    protected void ApplyStun()
    {
        float duration = stunCount > 0 ? Mathf.Max(0f, stunDuration - repeatStunReduction) : stunDuration;
        stunCount++;
        boss.ApplyStun(duration);
    }

    /// <summary> 스턴 종료 시 처음부터 </summary>
    protected virtual void ResetAll()
    {
        foreach (BossActivationZone zone in zones)
        {
            zone.ResetZone();
        }
    }

    #region [이전] 모든 구역 활성화 시 바로 스턴만 하던 구조
    // protected virtual void Start()
    // {
    //     foreach (BossActivationZone zone in zones)
    //     {
    //         BossActivationZone captured = zone;
    //         zone.Initialize(boss, () => CanZoneProgress(captured));
    //         zone.OnActivated += HandleZoneActivated;
    //     }
    //
    //     boss.OnStunEndedEvent += ResetAll;
    // }
    //
    // private void HandleZoneActivated()
    // {
    //     foreach (BossActivationZone zone in zones)
    //     {
    //         if (!zone.IsActivated) return;
    //     }
    //
    //     boss.ApplyStun(stunDuration);
    // }
    #endregion
}
