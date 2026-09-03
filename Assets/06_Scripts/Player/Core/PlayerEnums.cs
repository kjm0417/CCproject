
// 스탯 종류. 스킬트리/장비 등이 이 종류를 지정해 수치를 더한다.
public enum StatType
{
    MoveSpeed,   // 이동 속도
    AttackPower, // 공격력
    AttackRange, // 공격 범위
    MaxHp,       // 최대 체력 (현재 체력은 depletable 이라 여기서 다루지 않는다)
}

// 플레이어(및 공격/상호작용)가 바라보는 4방향.
public enum FacingDirection
{
    Down,
    Up,
    Left,
    Right,
}
