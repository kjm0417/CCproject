using UnityEngine;

/// <summary>
/// 1번 보스 - 용
/// 공통 로직은 BossBase, 패턴은 Dragon*Pattern 컴포넌트, 판단은 Behavior Graph
/// 애니메이션 / 사운드 연출은 아래 훅에 추가
/// </summary>
public class DragonBoss : BossBase
{
    [Header("스턴 연출")]
    [SerializeField, Tooltip("스턴 동안 머리 위에 표시할 이펙트 ( StunSFX )")]
    private GameObject stunVfx;
    [SerializeField, Tooltip("이펙트 위치 ( 머리 위 ) - 보스를 따라다님")]
    private Transform stunVfxPoint;

    private GameObject stunVfxInstance;

    protected override void OnStunStarted()
    {
        Debug.Log("[DragonBoss] 스턴 시작 - 공격 타임");
        ShowStunVfx();
    }

    protected override void OnStunEnded()
    {
        Debug.Log("[DragonBoss] 스턴 종료");
        HideStunVfx();
    }

    protected override void OnDied()
    {
        Debug.Log("[DragonBoss] 사망");
        HideStunVfx();
    }

    private void ShowStunVfx()
    {
        if (stunVfx == null || stunVfxInstance != null) return;

        Transform point = stunVfxPoint != null ? stunVfxPoint : transform;
        stunVfxInstance = ObjectPoolManager.Spawn(stunVfx, point.position, Quaternion.identity, point);
    }

    private void HideStunVfx()
    {
        if (stunVfxInstance == null) return;

        ObjectPoolManager.Despawn(stunVfxInstance);
        stunVfxInstance = null;
    }
}
