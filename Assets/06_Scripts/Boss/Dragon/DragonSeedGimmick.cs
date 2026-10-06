using UnityEngine;

/// <summary>
/// 용 보스 기믹 - 좌우 수질 구간 붉은 영역에 6초 머무르면 활성화, 두 구간 모두 활성화 시 스턴
///
/// TODO ( 기획 확정 대기 ) : "하단층에서 심어둔 색과 같은 씨앗이 자람" 연동
///  - 구간별 씨앗 색 조건 -> CanZoneProgress 에서 판정
///  - 씨앗 성장 연출 -> 구역 OnProgressChanged 구독
/// 확정 전까지는 머무르기 조건만 동작
/// </summary>
public class DragonSeedGimmick : BossStunGimmick
{
    protected override bool CanZoneProgress(BossActivationZone zone)
    {
        return true;
    }
}
