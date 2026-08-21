using UnityEngine;

/// <summary>
/// 외부 시스템이 플레이어의 골드 조회 및 소비 할 수 있게 해주는 인터페이스.( 재화 매니저와 통신을 이룸 )
/// </summary>
public interface ICurrencyProvider
{
    /// <summary>
    /// //현재 보유 골드 조회
    /// </summary>
    /// <returns></returns>
    public float GetGold();

    /// <summary>
    /// amount만큼 골드 소비 시도, 부족하면 false 반환해서 호출한쪽이 처리 
    /// </summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public bool TrySpend(float amount); 

}
