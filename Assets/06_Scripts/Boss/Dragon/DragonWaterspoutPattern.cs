using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 3번 용오름
/// 마름모 ( 바깥 -> 안쪽 ) 순서로 점멸, 바깥이 nextStartAfterBlinks 회 점멸하면 다음 마름모 점멸 시작
/// 각 마름모 blinkCount 회 점멸 후 해당 구역에 용오름 ( 피해 )
/// </summary>
public class DragonWaterspoutPattern : BossPattern
{
    [Header("구역")]
    [SerializeField, Tooltip("바깥 -> 안쪽 순서, 마름모는 angle 45")]
    private List<ZoneShape> rings = new List<ZoneShape>();
    [SerializeField, Tooltip("true: 안쪽 마름모 영역은 피해 제외 ( 테두리 띠만 피해 )")]
    private bool ringOnly = true;

    [Header("경고")]
    [SerializeField] private TelegraphStyle telegraphStyle = new TelegraphStyle();
    [SerializeField] private int blinkCount = 3;
    [SerializeField, Tooltip("이전 마름모가 몇 번 점멸한 뒤 다음 마름모 시작")]
    private int nextStartAfterBlinks = 2;
    [SerializeField] private float blinkOnTime = 0.25f;
    [SerializeField] private float blinkOffTime = 0.2f;

    [Header("용오름")]
    [SerializeField] private float eruptDuration = 1.5f;
    [SerializeField] private float damage = 15f;
    [SerializeField] private float tickInterval = 0.5f;
    [SerializeField] private GameObject waterspoutVfx;

    protected override IEnumerator Execute()
    {
        if (rings.Count == 0) yield break;

        float blinkPeriod = blinkOnTime + blinkOffTime;
        float stagger = nextStartAfterBlinks * blinkPeriod;

        for (int i = 0; i < rings.Count; i++)
        {
            ZoneShape hole = ringOnly && i + 1 < rings.Count ? rings[i + 1] : null;
            StartSubRoutine(RunRing(rings[i], hole));

            if (i < rings.Count - 1) yield return new WaitForSeconds(stagger);
        }

        // 마지막 마름모 점멸 + 용오름 종료까지 대기
        yield return new WaitForSeconds(blinkCount * blinkPeriod + eruptDuration);
    }

    private IEnumerator RunRing(ZoneShape ring, ZoneShape hole)
    {
        BossTelegraph telegraph = CreateTelegraph(ring.Snapshot(), telegraphStyle);
        yield return telegraph.Blink(blinkCount, blinkOnTime, blinkOffTime);
        telegraph.Release();

        SpawnDamageZone(ring, hole, damage, eruptDuration, tickInterval, waterspoutVfx);
    }

    private void OnDrawGizmosSelected()
    {
        foreach (ZoneShape ring in rings)
        {
            ring.DrawGizmo(Color.red);
        }
    }
}
