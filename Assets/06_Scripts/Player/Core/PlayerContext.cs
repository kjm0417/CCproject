using UnityEngine;

// 플레이어 부품들을 한 곳에서 찾아 서로 연결해주는 지휘자
// 부품끼리 직접 GetComponent 하지 않고, 항상 이 Context를 통해 접근
public class PlayerContext : MonoBehaviour
{
    // 플레이어 스탯(이속, 공격력, 공격범위 등 최종 계산값)
    public PlayerStats Stats { get; private set; }

    // 이동 부품(조이스틱 입력 -> 이동) - 루트
    public PlayerMovement Movement { get; private set; }

    // 애니메이션 부품(이동 상태 -> 애니메이터/방향) - 보통 자식(PlayerAnim)
    public PlayerAnimation Animation { get; private set; }

    // 아직 안 만든 부품들 - 만들면 아래 주석과 Awake의 짝을 함께 푼다.
    // public PlayerTargeting Targeting { get; private set; }
    // public PlayerInteraction Interaction { get; private set; }
    // public PlayerCollector Collector { get; private set; }

    private void Awake()
    {
        //부품 참조를 먼저 모두 찾음
        Stats = GetComponent<PlayerStats>();
        Movement = GetComponent<PlayerMovement>();
        Animation = GetComponentInChildren<PlayerAnimation>(true); // 자식까지 탐색

        //self + 자식에 있는 모든 IPlayerComponent 부품을 초기화
        InitializeComponents();
    }

    // self와 자식에 붙은 모든 IPlayerComponent 부품에 자기 자신을 넘겨 초기화
    private void InitializeComponents()
    {
        foreach (IPlayerComponent component in GetComponentsInChildren<IPlayerComponent>(true))
        {
            component.Initialize(this);
        }
    }
}
