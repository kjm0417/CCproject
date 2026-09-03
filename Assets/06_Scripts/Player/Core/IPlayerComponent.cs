using UnityEngine;

// 플레이어를 구성하는 모든 부품이 구현하는 공통 계약.
// PlayerContext가 Awake에서 모든 부품을 찾아 Initialize로 서로 연결해준다.
public interface IPlayerComponent
{
    // PlayerContext가 자기 자신을 넘겨주며 호출. 부품은 여기서 필요한 참조를 캐싱한다.
    // (GetComponent를 부품마다 흩뿌리지 않기 위한 단일 진입점)
    void Initialize(PlayerContext context);
}
