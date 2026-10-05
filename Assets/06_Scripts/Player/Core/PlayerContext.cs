using UnityEngine;

// 플레이어를 구성하는 컴포넌트 참조를 한곳에서 관리하는 컨텍스트.
// 각 컴포넌트는 필요한 다른 컴포넌트를 직접 검색하지 않고 이 Context를 통해 참조한다.
public class PlayerContext : MonoBehaviour
{
    [Header("기본 플레이어 데이터 (SO)")]
    [SerializeField]
    private PlayerBaseData baseData;
    public PlayerBaseData BaseData => baseData;

    // 이동 속도, 공격력, 공격 범위 등의 최종 스탯
    public PlayerStats Stats { get; private set; }

    // 조이스틱 입력과 물리 이동
    public PlayerMovement Movement { get; private set; }

    // 이동 및 상호작용 애니메이션. PlayerAnim 자식에서 찾는다.
    public PlayerAnimation Animation { get; private set; }

    // 체력과 허기
    public PlayerVitals Vitals { get; private set; }

    // 경험치, 레벨, 스킬 포인트
    public PlayerProgression Progression { get; private set; }

    // 플레이어가 보유한 재화
    public PlayerWallet Wallet { get; private set; }

    // 필드 오브젝트 및 타일 상호작용
    public PlayerInteraction Interaction { get; private set; }

    // 플레이어 인벤토리
    public PlayerInventory Inventory { get; private set; }

    // 플레이어 외형을 그리는 SpriteRenderer
    public SpriteRenderer SpriteRenderer { get; private set; }

    // 적 탐색과 공격 판정
    public PlayerCombat Combat { get; private set; }

    private void Awake()
    {
        // 루트 오브젝트에 붙은 필수 컴포넌트는 TryGetComponent로 캐싱한다.
        Stats = GetRequiredComponent<PlayerStats>();
        Movement = GetRequiredComponent<PlayerMovement>();
        Vitals = GetRequiredComponent<PlayerVitals>();
        Progression = GetRequiredComponent<PlayerProgression>();
        Wallet = GetRequiredComponent<PlayerWallet>();
        Interaction = GetRequiredComponent<PlayerInteraction>();
        Inventory = GetRequiredComponent<PlayerInventory>();
        Combat = GetRequiredComponent<PlayerCombat>();

        // TryGetComponent는 현재 GameObject만 검색하므로 자식 컴포넌트는 별도로 찾는다.
        Animation = GetComponentInChildren<PlayerAnimation>(true);
        SpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // 자신과 자식에 있는 모든 플레이어 컴포넌트에 Context를 전달한다.
        InitializeComponents();
    }

    private T GetRequiredComponent<T>() where T : Component
    {
        if (TryGetComponent(out T component))
        {
            return component;
        }

        Debug.LogError($"PlayerContext: 필수 컴포넌트 {typeof(T).Name}이(가) 없습니다.", this);
        return null;
    }

    // 자신과 자식에 있는 모든 IPlayerComponent를 초기화한다.
    private void InitializeComponents()
    {
        foreach (IPlayerComponent component in GetComponentsInChildren<IPlayerComponent>(true))
        {
            component.Initialize(this);
        }
    }
}
