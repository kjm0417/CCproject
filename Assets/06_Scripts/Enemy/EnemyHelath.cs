using System;
using UnityEngine;

/// <summary>
/// 적 공통 - 체력 
/// </summary>
public class EnemyHelath 
{
    public float CurrentHealth { get; private set; }
    public float MaxHealth { get; private set; }
    public bool IsDie { get; private set; }

    public Action OnDie;

    public EnemyHelath(float maxHealth)
    {
        this.MaxHealth = maxHealth;
        this.CurrentHealth = maxHealth;
    }

    /// <summary>
    /// 데미지를 체력에 적용
    /// </summary>
    /// <param name="damage"></param>
    public void ApplyDamaged(int damage)
    {
        CurrentHealth -= damage;

        if (CurrentHealth <= 0f)
        {
            IsDie = true;
            OnDie?.Invoke();
        }
    }

    /// <summary>
    /// 체력 리셋 필요 시 사용
    /// </summary>
    public void ResetHelath()
    {
        this.CurrentHealth = MaxHealth;
        this.IsDie = false;
    }

}
