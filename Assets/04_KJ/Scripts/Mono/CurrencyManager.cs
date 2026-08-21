using UnityEngine;

/// <summary>
/// 재화 매니저
/// </summary>
public class CurrencyManager : MonoBehaviour , ICurrencyProvider
{
    [SerializeField]
    private float TestCurrentGold = 999999f;
  
    public float GetGold()
    {
        return TestCurrentGold;
    }

    public bool TrySpend(float amount)
    {
        if(TestCurrentGold >= amount)
        {
            TestCurrentGold -= amount;
            return true;
        }
        return false;
    }

}
