using UnityEngine;

/// <summary>
/// 1번 보스 - 용
/// 공통 로직은 BossBase, 패턴은 Dragon*Pattern 컴포넌트, 판단은 Behavior Graph
/// 애니메이션 / 사운드 연출은 아래 훅에 추가
/// </summary>
public class DragonBoss : BossBase
{
    protected override void OnStunStarted()
    {
        Debug.Log("[DragonBoss] 스턴 시작 - 공격 타임");
    }

    protected override void OnStunEnded()
    {
        Debug.Log("[DragonBoss] 스턴 종료");
    }

    protected override void OnDied()
    {
        Debug.Log("[DragonBoss] 사망");
    }
}
