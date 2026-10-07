/// <summary>
/// 풀 재사용 시 상태 초기화가 필요한 컴포넌트
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// 풀에서 꺼내 활성화된 직후 ( 첫 생성은 Awake / OnEnable 다음, Start 전 )
    /// </summary>
    void OnSpawned();

    /// <summary>
    /// 풀로 반납되기 직전 ( 비활성화 전 )
    /// </summary>
    void OnDespawned();
}
