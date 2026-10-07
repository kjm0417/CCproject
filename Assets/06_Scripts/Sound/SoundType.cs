/// <summary>
/// BGM 종류. 새 BGM 추가 시 여기에 항목 추가 후 SoundDatabase에 클립 연결
/// </summary>
public enum BGMType
{
    None,
    Title,
    Territory,
    Dungeon,
    Boss,
}

/// <summary>
/// 효과음 종류. 새 효과음 추가 시 여기에 항목 추가 후 SoundDatabase에 클립 연결
/// </summary>
public enum SFXType
{
    None,
    ButtonClick,

    // 도구로 오브젝트 타격
    AxeHit,
    PickaxeHit,
    ShovelHit,

    // 플레이어 이동
    Footstep,
}
