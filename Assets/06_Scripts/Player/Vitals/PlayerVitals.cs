using System;
using UnityEngine;

// �÷��̾��� "��� ��ġ" �ý��� - ü�°� ��⸦ �Բ� �����Ѵ�.
// �� �� DepletableStat(���� C#)�̶� �� ������ �����ϰ�, ���⼱ ���� ��Ģ(���� ����/�ܰ�/���)�� ��´�.
// ü��+��⸦ �� ������Ʈ�� �� ����: ���� ���� ��ȣ�ۿ��ϱ� ����(��� 0 -> ü�� ����, ���� 2.1/2.2).
public class PlayerVitals : MonoBehaviour, IPlayerComponent
{
    [Header("��� (���� 2.2: 0~100)")]
    [SerializeField]
    private float maxHunger = 100f;
    [SerializeField]
    private float hungerDrainPerSecond = 0.5f; // ��� ���� �ð��� �⺻ �Ҹ�
    [SerializeField]
    private float lowThreshold = 20f;  // �̸��̸� ����(�޸��� ���� ��)
    [SerializeField]
    private float fullThreshold = 80f; // �̻��̸� ����(���ʽ�)

    [Header("����(��� 0) ���Ƽ (���� 2.2)")]
    [SerializeField]
    private float starveInterval = 1f;  // �� �ʸ��� ü���� ������
    [SerializeField]
    private float starveDamage = 1f;    // ƽ�� ü�� ���ҷ�

    // ü�� �ִ��� PlayerStats(StatType.MaxHp)���� �����´�. ���⼱ ���簪�� ����.

    // ��� (���� ����/����/��Ȱ�� �ٸ� �ý����� ������ ó�� - ���� 2.1)
    public event Action OnDied;
    public event Action OnHealthChanged;
    public event Action OnHungerChanged;
    // ��� �ܰ� ��ȭ (HUD ��/�޸��� ���� ���� ����)
    public event Action<HungerTier> OnHungerTierChanged;

    private readonly DepletableStat health = new DepletableStat(0f);
    private readonly DepletableStat hunger = new DepletableStat(0f);
    private PlayerStats stats;
    private HungerTier hungerTier = HungerTier.Full;
    private float starveTimer;
    private bool isDead;

    // HUD/�ܺ� ����
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

        // ��ų/���� �ִ� ü���� �ٲ�� ������ ���󰣴�.
        if (stats != null) stats.OnStatsChanged += HandleStatsChanged;

        OnHealthChanged?.Invoke();
        OnHungerChanged?.Invoke();
        OnHungerTierChanged?.Invoke(hungerTier);
    }

    /// <summary>
    /// ����� ������ �ε�
    /// </summary>
    /// <param name="HP"></param>
    /// <param name="HungerValue"></param>
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
    }

    private void HandleStatsChanged()
    {
        health.SetMax(stats.MaxHp, false); // ���Ѹ� ����, ���� ü�� ����
        OnHealthChanged?.Invoke();
    }

    private void Update()
    {
        if (isDead) return;

        // ���: �ð��� ���� �⺻ �Ҹ� (��� ����). deltaTime ���� ������ ����.
        float hungerBefore = hunger.Current;
        hunger.Reduce(hungerDrainPerSecond * Time.deltaTime);
        if (!Mathf.Approximately(hungerBefore, hunger.Current))
        {
            OnHungerChanged?.Invoke();
        }
        UpdateHungerTier();
        UpdateStarve();
    }

    // --- ��� API ---
    // �ൿ(�̵�/�޸���/����/����)�� ���� �߰� �Ҹ�. �ش� ��ǰ�� ȣ��. (���� 2.2)
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
    // ���� ������ ȸ��. (���� 2.3)
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

    // --- ü�� API (���� 2.1) ---
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

    // ��� 0�̸� �ֱ������� ü�� ���� (���� 2.2). ���� ������Ʈ�� ���ο��� �ٷ� ó��.
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
            starveTimer -= starveInterval; // ���� �ð� ����(ƽ ���� ����)
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

    // �� ���� -> �ܰ� (���� 2.2)
    private HungerTier CalculateHungerTier()
    {
        if (hunger.IsEmpty) return HungerTier.Empty;          // 0
        if (hunger.Current < lowThreshold) return HungerTier.Low;    // 1~19
        if (hunger.Current < fullThreshold) return HungerTier.Normal; // 20~79
        return HungerTier.Full;                               // 80~100
    }
}
