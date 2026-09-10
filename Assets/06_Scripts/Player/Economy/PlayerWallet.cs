using System;
using System.Collections.Generic;
using UnityEngine;

// 플레이어 소유 재화(여러 종류). 시작 지급은 PlayerBaseData(SO)에서 읽고, 현재 잔액을 관리한다.
// KJ의 전역 CurrencyManager와 별개로 플레이어가 직접 관리한다.
public class PlayerWallet : MonoBehaviour, IPlayerComponent
{
    // 잔액 변동 시 방송 (인자: 바뀐 재화 종류). 골드 UI 등이 구독.
    public event Action<CurrencyType> OnChanged;

    private readonly Dictionary<CurrencyType, int> balances = new Dictionary<CurrencyType, int>();

    public void Initialize(PlayerContext context)
    {
        balances.Clear();
        PlayerBaseData data = context.BaseData;
        if (data != null && data.StartingCurrencies != null)
        {
            foreach (CurrencyAmount c in data.StartingCurrencies)
            {
                balances[c.Type] = c.Amount; // 시작 지급
            }
        }
    }

    // 잔액 조회 (없으면 0)
    public int GetBalance(CurrencyType type)
    {
        return balances.TryGetValue(type, out int v) ? v : 0;
    }

    // 획득 (판매/보상/드롭). 음수/0은 무시.
    public void Add(CurrencyType type, int amount)
    {
        if (amount <= 0) return;
        balances[type] = GetBalance(type) + amount;
        OnChanged?.Invoke(type);
    }

    // 소비 시도. 부족하면 false 반환(구매 취소).
    public bool TrySpend(CurrencyType type, int amount)
    {
        if (amount <= 0) return false;
        int cur = GetBalance(type);
        if (cur < amount) return false;
        balances[type] = cur - amount;
        OnChanged?.Invoke(type);
        return true;
    }
}
