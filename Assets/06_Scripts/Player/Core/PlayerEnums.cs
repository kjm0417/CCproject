// 플레이어 관련 enum을 한 곳에 모아둔다.
// 필요한 파일마다 흩어서 정의하지 말고, 새 enum도 여기에 추가한다.

// 스탯 종류. 스킬트리/장비 등이 이 종류를 지정해 수치를 더한다.
public enum StatType
{
    MoveSpeed,   // 이동 속도
    AttackPower, // 공격력
    AttackRange, // 공격 범위
    MaxHp,       // 최대 체력 (현재 체력은 depletable 이라 여기서 다루지 않는다)
}

// 허기 4단계 상태 (문서 2.2). 이동/전투 상태와는 독립된 축이라 별도 enum으로 둔다.
public enum HungerTier
{
    Empty,   // 0        : 주기적 체력 감소 + 화면효과
    Low,     // 1~19     : 달리기 제한, 회복효율 감소
    Normal,  // 20~79    : 일반
    Full,    // 80~100   : 이동/회복 보너스
}

// 재화 종류. 필요하면 여기에 추가/이름변경.
public enum CurrencyType
{
    Gold, // 기본 재화(돈)
    Gem,  // 보석 등 특수 재화
}
