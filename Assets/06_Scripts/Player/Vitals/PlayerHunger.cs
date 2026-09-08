using System;
using UnityEngine;

// 허기 시스템 (문서 2.2). 시간에 따라 감소하고, 값 구간에 따라 4단계로 나뉜다.
// 각 단계의 실제 효과(HP 감소, 달리기 제한, 보너스 등)는 다른 부품이
// Tier/이벤트를 구독해 처리한다. 허기 자체는 "값과 단계"만 책임진다.
public class PlayerHunger : MonoBehaviour, IPlayerComponent
{
    [Header("허기 값 (문서 2.2: 0~100)")]
    [SerializeField]
    private float maxHunger = 100f;
    [SerializeField]
    private float drainPerSecond = 0.5f; // 시간에 따른 기본 소모(대기 중에도 감소)

    [Header("단계 경계 (문서 2.2)")]
    [SerializeField]
    private float lowThreshold = 20f;  // 이 값 미만이면 저하(달리기 제한 등)
    [SerializeField]
    private float fullThreshold = 80f; // 이 값 이상이면 포만(보너스)

    [Header("고갈(0) 페널티")]
    [SerializeField]
    private float starveTickInterval = 1f;   // 몇 초마다 체력을 깎을지
    [SerializeField]
    private float starveDamagePerTick = 1f;  // 틱당 체력 감소량 (HP 부품이 구독)

    // 단계가 바뀔 때만 방송한다 (매 프레임 아님). HUD 색/아이콘, 달리기 제한 등이 구독.
    public event Action<HungerTier> OnTierChanged;

    // 고갈 상태에서 일정 주기로 방송. 나중에 PlayerHealth가 구독해 체력을 깎는다. (인자: 이번 틱 데미지)
    public event Action<float> OnStarveTick;

    private readonly DepletableStat hunger = new DepletableStat(0f);
    private HungerTier tier = HungerTier.Full;
    private float starveTimer;

    // HUD/외부에서 읽는 값
    public float Current => hunger.Current;
    public float Max => hunger.Max;
    public float Percent01 => hunger.Percent01;
    public HungerTier Tier => tier;

    // PlayerContext가 호출. 최대치 설정 + 가득 채우기(씬 재진입 시 리셋).
    public void Initialize(PlayerContext context)
    {
        hunger.SetMax(maxHunger, true);
        tier = CalculateTier();
        OnTierChanged?.Invoke(tier);
    }

    private void Update()
    {
        // 시간에 따른 기본 소모. deltaTime 곱해서 프레임레이트와 무관하게.
        hunger.Reduce(drainPerSecond * Time.deltaTime);

        UpdateTier();
        UpdateStarve();
    }

    // 행동(이동/달리기/도구/전투)에 따른 추가 소모. 해당 행동 부품이 호출한다. (문서 2.2)
    public void Consume(float amount)
    {
        hunger.Reduce(amount);
        UpdateTier();
    }

    // 음식 등으로 회복. (문서 2.3) 아직 음식 오브젝트가 없어 디버그로 테스트.
    public void Recover(float amount)
    {
        hunger.Add(amount);
        UpdateTier();
    }

    // 현재값으로 단계를 다시 계산해 바뀌었으면 방송.
    private void UpdateTier()
    {
        HungerTier now = CalculateTier();
        if (now != tier)
        {
            tier = now;
            OnTierChanged?.Invoke(tier);
        }
    }

    // 값 구간 -> 단계 (문서 2.2)
    private HungerTier CalculateTier()
    {
        if (hunger.IsEmpty) return HungerTier.Empty;          // 0
        if (hunger.Current < lowThreshold) return HungerTier.Low;    // 1~19
        if (hunger.Current < fullThreshold) return HungerTier.Normal; // 20~79
        return HungerTier.Full;                               // 80~100
    }

    // 고갈 상태에서만 일정 주기로 OnStarveTick 방송. (HP 부품이 구독해 체력 감소)
    private void UpdateStarve()
    {
        if (tier != HungerTier.Empty)
        {
            starveTimer = 0f;
            return;
        }

        starveTimer += Time.deltaTime;
        if (starveTimer >= starveTickInterval)
        {
            starveTimer -= starveTickInterval; // 남는 시간 보존(틱 누락 방지)
            OnStarveTick?.Invoke(starveDamagePerTick);
        }
    }
}
