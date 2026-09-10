using System;
using UnityEngine;

// 플레이어의 "닳는 수치" 시스템 - 체력과 허기를 함께 관리한다.
// 둘 다 DepletableStat(순수 C#)이라 값 로직은 공유하고, 여기선 게임 규칙(감소 조건/단계/사망)만 얹는다.
// 체력+허기를 한 컴포넌트에 둔 이유: 서로 직접 상호작용하기 때문(허기 0 -> 체력 감소, 문서 2.1/2.2).
public class PlayerVitals : MonoBehaviour, IPlayerComponent
{
    [Header("허기 (문서 2.2: 0~100)")]
    [SerializeField]
    private float maxHunger = 100f;
    [SerializeField]
    private float hungerDrainPerSecond = 0.5f; // 대기 포함 시간당 기본 소모
    [SerializeField]
    private float lowThreshold = 20f;  // 미만이면 저하(달리기 제한 등)
    [SerializeField]
    private float fullThreshold = 80f; // 이상이면 포만(보너스)

    [Header("고갈(허기 0) 페널티 (문서 2.2)")]
    [SerializeField]
    private float starveInterval = 1f;  // 몇 초마다 체력을 깎을지
    [SerializeField]
    private float starveDamage = 1f;    // 틱당 체력 감소량

    // 체력 최댓값은 PlayerStats(StatType.MaxHp)에서 가져온다. 여기선 현재값만 관리.

    // 사망 (조작 중지/연출/부활은 다른 시스템이 구독해 처리 - 문서 2.1)
    public event Action OnDied;
    // 허기 단계 변화 (HUD 색/달리기 제한 등이 구독)
    public event Action<HungerTier> OnHungerTierChanged;

    private readonly DepletableStat health = new DepletableStat(0f);
    private readonly DepletableStat hunger = new DepletableStat(0f);
    private PlayerStats stats;
    private HungerTier hungerTier = HungerTier.Full;
    private float starveTimer;
    private bool isDead;

    // HUD/외부 노출
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

        hunger.SetMax(maxHunger, true);
        health.SetMax(stats != null ? stats.MaxHp : 100f, true);
        hungerTier = CalculateHungerTier();

        // 스킬/장비로 최대 체력이 바뀌면 상한을 따라간다.
        if (stats != null) stats.OnStatsChanged += HandleStatsChanged;
    }

    private void OnDestroy()
    {
        if (stats != null) stats.OnStatsChanged -= HandleStatsChanged;
    }

    private void HandleStatsChanged()
    {
        health.SetMax(stats.MaxHp, false); // 상한만 갱신, 현재 체력 유지
    }

    private void Update()
    {
        if (isDead) return;

        // 허기: 시간에 따른 기본 소모 (대기 포함). deltaTime 곱해 프레임 독립.
        hunger.Reduce(hungerDrainPerSecond * Time.deltaTime);
        UpdateHungerTier();
        UpdateStarve();
    }

    // --- 허기 API ---
    // 행동(이동/달리기/도구/전투)에 따른 추가 소모. 해당 부품이 호출. (문서 2.2)
    public void ConsumeHunger(float amount) { hunger.Reduce(amount); UpdateHungerTier(); }
    // 음식 등으로 회복. (문서 2.3)
    public void RecoverHunger(float amount) { hunger.Add(amount); UpdateHungerTier(); }

    // --- 체력 API (문서 2.1) ---
    public void TakeDamage(float amount)
    {
        if (isDead) return;
        health.Reduce(amount);
        if (health.IsEmpty) Die();
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        health.Add(amount);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        OnDied?.Invoke();
    }

    // 허기 0이면 주기적으로 체력 감소 (문서 2.2). 같은 컴포넌트라 내부에서 바로 처리.
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
            starveTimer -= starveInterval; // 남는 시간 보존(틱 누락 방지)
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

    // 값 구간 -> 단계 (문서 2.2)
    private HungerTier CalculateHungerTier()
    {
        if (hunger.IsEmpty) return HungerTier.Empty;          // 0
        if (hunger.Current < lowThreshold) return HungerTier.Low;    // 1~19
        if (hunger.Current < fullThreshold) return HungerTier.Normal; // 20~79
        return HungerTier.Full;                               // 80~100
    }
}
