using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 던전 4층 샘 - 트리거 안에 플레이어가 있으면 정화율 증가 ( 시계 방향 )
/// 플레이어가 없거나 적이 트리거에 닿아 있으면 ( 플레이어가 있어도 ) 정화율 감소 ( 반시계 방향 )
/// 정화율 100% 달성 시 OnPurified
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DungeonFountain : FieldObjectBase
{
    [SerializeField, Tooltip("초당 정화율 증가량 ( 0.05 = 20초에 100% )")]
    private float increaseSpeed = 0.05f;
    [SerializeField, Tooltip("초당 정화율 감소량 ( 플레이어가 없거나 적이 샘에 닿아 있을 때 )")]
    private float decreaseSpeed = 0.05f;
    [SerializeField, Tooltip("정화율 원형 UI ( 자식 Image, Filled - Radial 360 )")]
    private Image purifyGauge;

    private Collider2D triggerCollider; //샘 범위 ( Is Trigger )
    private readonly List<Collider2D> overlapResults = new List<Collider2D>();
    private bool isRunning; //층 진행 중일 때만 정화

    public float PurifyRate { get; private set; } //정화율 0 ~ 1
    public bool IsPurified { get; private set; }

    /// <summary>
    /// 정화율 100% 달성 시
    /// </summary>
    public event Action<DungeonFountain> OnPurified;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;

        if (purifyGauge != null)
        {
            //시계 방향으로 차고, 줄어들 땐 반시계 방향으로 빠짐
            purifyGauge.type = Image.Type.Filled;
            purifyGauge.fillMethod = Image.FillMethod.Radial360;
            purifyGauge.fillOrigin = (int)Image.Origin360.Top;
            purifyGauge.fillClockwise = true;
        }

        RefreshGauge();
    }

    /// <summary>
    /// 층 진입 시 초기화 후 정화 시작
    /// </summary>
    public void Begin()
    {
        PurifyRate = 0f;
        IsPurified = false;
        isRunning = true;
        RefreshGauge();
    }

    private void Update()
    {
        if (!isRunning || IsPurified) return;

        CheckInside(out bool isPlayerInside, out bool isEnemyInside);

        //플레이어만 있을 때 증가 - 플레이어가 없거나 적이 닿아 있으면 감소
        if (isPlayerInside && !isEnemyInside)
        {
            PurifyRate = Mathf.Min(1f, PurifyRate + increaseSpeed * Time.deltaTime);
        }
        else
        {
            PurifyRate = Mathf.Max(0f, PurifyRate - decreaseSpeed * Time.deltaTime);
        }

        RefreshGauge();

        if (PurifyRate >= 1f)
        {
            IsPurified = true;
            isRunning = false;
            Debug.Log($"[DungeonFountain] {name} 정화 완료");
            OnPurified?.Invoke(this);
        }
    }

    /// <summary>
    /// 샘 범위 안 플레이어 / 살아있는 적 확인
    /// </summary>
    private void CheckInside(out bool isPlayerInside, out bool isEnemyInside)
    {
        isPlayerInside = false;
        isEnemyInside = false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        triggerCollider.Overlap(filter, overlapResults);

        foreach (Collider2D hit in overlapResults)
        {
            if (hit == null) continue;

            if (!isPlayerInside && hit.GetComponentInParent<PlayerContext>() != null)
            {
                isPlayerInside = true;
            }

            EnemyBase enemy = hit.GetComponentInParent<EnemyBase>();
            if (enemy != null && !enemy.IsDie)
            {
                isEnemyInside = true;
            }
        }
    }

    private void RefreshGauge()
    {
        if (purifyGauge != null)
        {
            purifyGauge.fillAmount = PurifyRate;
        }
    }
}
