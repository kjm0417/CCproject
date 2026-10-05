using System;
using UnityEngine;

// 플레이어의 체력과 허기를 함께 관리한다.
// 각 수치는 DepletableStat이 보관하고, 이 컴포넌트는 소모·회복·단계 변화 규칙을 담당한다.
// 허기가 0이 되면 주기적으로 체력이 감소하므로 두 수치를 한 컴포넌트에서 관리한다.
public class PlayerVitals : MonoBehaviour, IPlayerComponent
{
    [Header("허기 (0~100)")]
    [SerializeField]
    private float maxHunger = 100f;
    [SerializeField]
    private float hungerDrainPerSecond = 0.5f; // 초당 기본 허기 소모량
    [SerializeField]
    private float lowThreshold = 20f;  // 이 값 미만이면 Low 단계
    [SerializeField]
    private float fullThreshold = 80f; // 이 값 이상이면 Full 단계

    [Header("굶주림 페널티 (허기 0)")]
    [SerializeField]
    private float starveInterval = 1f;  // 체력 감소 주기
    [SerializeField]
    private float starveDamage = 1f;    // 주기마다 감소하는 체력

    // 최대 체력은 PlayerStats의 MaxHp를 사용하고 허기의 최대치는 여기에서 관리한다.

    // 사망 후 부활과 씬 전환은 이 이벤트를 구독하는 외부 시스템에서 처리한다.
    public event Action OnDied;
    public event Action OnHealthChanged;
    public event Action OnHungerChanged;
    // HUD와 이동 제한 등이 허기 단계 변화에 대응할 때 사용한다.
    public event Action<HungerTier> OnHungerTierChanged;

    private readonly DepletableStat health = new DepletableStat(0f);
    private readonly DepletableStat hunger = new DepletableStat(0f);
    private PlayerStats stats;
    private PlayerProgression progression;
    private HungerTier hungerTier = HungerTier.Full;
    private float starveTimer;
    private bool isDead;

    // HUD 및 외부 시스템에 공개하는 읽기 전용 값
    public float Hp => health.Current;
    public float MaxHp => health.Max;
    public float HpPercent01 => health.Percent01;
    public float HungerValue => hunger.Current;
    public float MaxHunger => hunger.Max;
    public float HungerPercent01 => hunger.Percent01;
    public HungerTier HungerTier => hungerTier;
    public bool IsDead => isDead;

    public void Initialize(PlayerContext context)
    {
        stats = context.Stats;
        progression = context.Progression;

        hunger.SetMax(maxHunger, true);
        health.SetMax(stats != null ? stats.MaxHp : 100f, true);
        hungerTier = CalculateHungerTier();

        // 장비나 스킬로 최대 체력이 변경되면 즉시 반영한다.
        if (stats != null) stats.OnStatsChanged += HandleStatsChanged;
        if (progression != null) progression.OnLevelUp += HandleLevelUp;

        OnHealthChanged?.Invoke();
        OnHungerChanged?.Invoke();
        OnHungerTierChanged?.Invoke(hungerTier);
    }

    /// <summary>
    /// 저장된 체력과 허기 데이터를 불러온다.
    /// </summary>
    /// <param name="Hp">저장된 현재 체력</param>
    /// <param name="HungerValue">저장된 현재 허기</param>
    public void Load(float Hp , float HungerValue)
    {
        health.LoadSet(Hp);
        hunger.LoadSet(HungerValue);
        hungerTier = CalculateHungerTier();
        OnHealthChanged?.Invoke();
        OnHungerChanged?.Invoke();
        OnHungerTierChanged?.Invoke(hungerTier);
    }
    private void OnDestroy()
    {
        if (stats != null) stats.OnStatsChanged -= HandleStatsChanged;
        if (progression != null) progression.OnLevelUp -= HandleLevelUp;
    }

    private void HandleStatsChanged()
    {
        health.SetMax(stats.MaxHp, false); // 최대치만 변경하고 현재 체력은 유지한다.
        OnHealthChanged?.Invoke();
    }

    private void HandleLevelUp(int newLevel)
    {
        RestoreToFull();
    }

    public void RestoreToFull()
    {
        health.Reset();
        hunger.Reset();
        OnHealthChanged?.Invoke();
        OnHungerChanged?.Invoke();
        UpdateHungerTier();
    }

    private void Update()
    {
        if (isDead) return;

        // 허기는 시간에 따라 지속해서 감소하며 deltaTime으로 프레임 차이를 보정한다.
        float hungerBefore = hunger.Current;
        hunger.Reduce(hungerDrainPerSecond * Time.deltaTime);
        if (!Mathf.Approximately(hungerBefore, hunger.Current))
        {
            OnHungerChanged?.Invoke();
        }
        UpdateHungerTier();
        UpdateStarve();
    }

    // --- 허기 API ---
    // 이동, 달리기, 전투 등의 행동에 따른 추가 허기 소모에 사용한다.
    public void ConsumeHunger(float amount)
    {
        float before = hunger.Current;
        hunger.Reduce(amount);
        if (!Mathf.Approximately(before, hunger.Current))
        {
            OnHungerChanged?.Invoke();
        }
        UpdateHungerTier();
    }
    // 음식 아이템 등을 사용했을 때 허기를 회복한다.
    public void RecoverHunger(float amount)
    {
        float before = hunger.Current;
        hunger.Add(amount);
        if (!Mathf.Approximately(before, hunger.Current))
        {
            OnHungerChanged?.Invoke();
        }
        UpdateHungerTier();
    }

    // --- 체력 API ---
    public void TakeDamage(float amount)
    {
        if (isDead) return;
        float before = health.Current;
        health.Reduce(amount);
        if (!Mathf.Approximately(before, health.Current))
        {
            OnHealthChanged?.Invoke();
        }
        if (health.IsEmpty) Die();
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        float before = health.Current;
        health.Add(amount);
        if (!Mathf.Approximately(before, health.Current))
        {
            OnHealthChanged?.Invoke();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        OnDied?.Invoke();
    }

    // 허기가 0이면 일정한 주기로 체력을 감소시킨다.
    private void UpdateStarve()
    {
        if (hungerTier != HungerTier.Empty)
        {
            starveTimer = 0f;
            return;
        }

        starveTimer += Time.deltaTime;
        if (starveTimer >= starveInterval)
        {
            starveTimer -= starveInterval; // 남은 시간을 보존해 틱 간격이 흔들리지 않게 한다.
            TakeDamage(starveDamage);
        }
    }

    private void UpdateHungerTier()
    {
        HungerTier now = CalculateHungerTier();
        if (now != hungerTier)
        {
            hungerTier = now;
            OnHungerTierChanged?.Invoke(hungerTier);
        }
    }

    // 현재 허기 수치를 단계로 변환한다.
    private HungerTier CalculateHungerTier()
    {
        if (hunger.IsEmpty) return HungerTier.Empty;          // 0
        if (hunger.Current < lowThreshold) return HungerTier.Low;    // 1~19
        if (hunger.Current < fullThreshold) return HungerTier.Normal; // 20~79
        return HungerTier.Full;                               // 80~100
    }
}
