using System.Collections.Generic;
using UnityEngine;

// 재화 한 종류의 시작 지급량 (SO에서 목록으로 설정)
[System.Serializable]
public struct CurrencyAmount
{
    public CurrencyType Type;
    public int Amount;
}

// 플레이어 기본 지급 데이터(SO). 시작 스탯/레벨/재화를 한 애셋에 담는다.
// 런타임 부품(Stats/Wallet/Progression)이 시작 시 이 값을 읽어가고,
// 이후 "현재값"은 각 부품이 각자 관리한다. (기본 지급 = 데이터, 현재값 = 런타임)
[CreateAssetMenu(fileName = "PlayerBaseData", menuName = "Scriptable Objects/PlayerBaseData")]
public class PlayerBaseData : ScriptableObject
{
    [Header("기본 스탯")]
    public float MoveSpeed = 5f;    // 속도
    public float AttackPower = 10f; // 공격
    public float AttackRange = 2f;  // 공격 범위
    public float MaxHp = 100f;      // 체력

    [Header("시작 지급")]
    public int StartingLevel = 1;                                 // 시작 레벨
    public List<CurrencyAmount> StartingCurrencies = new List<CurrencyAmount>(); // 시작 재화(돈 등)
}
