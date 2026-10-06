using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 1번 지나가용
/// 세로 구역 빨간 테두리 blinkCount 회 점멸 -> 용이 위에서 아래로 지나가며 지나간 구역에 피해
/// 연출 : 시작점 ( Lane 윗변 ) 에서 내리꽂는 애니메이션 재생 or 경로따라 이동
/// </summary>
public class DragonPassPattern : BossPattern
{
    [Header("구역")]
    [SerializeField, Tooltip("용이 지나갈 구역 후보 ( 랜덤 1개 선택 ), 영역의 위쪽 -> 아래쪽으로 이동")]
    private List<ZoneShape> lanes = new List<ZoneShape>();

    [Header("경고")]
    [SerializeField] private TelegraphStyle telegraphStyle = new TelegraphStyle();
    [SerializeField] private int blinkCount = 3;
    [SerializeField] private float blinkOnTime = 0.25f;
    [SerializeField] private float blinkOffTime = 0.2f;

    [Header("공격")]
    [SerializeField] private float passDuration = 1.2f;
    [SerializeField] private float damage = 20f;
    [SerializeField] private float tickInterval = 0.5f;

    [Header("연출")]
    [SerializeField, Tooltip("용 연출 오브젝트 ( 선택 ) - 패턴 동안만 활성화")]
    private Transform dragonVisual;
    [SerializeField, Tooltip("PlayAtStart : 시작점 고정 + 애니메이션이 내리꽂음 ( 피벗 Top )\nMoveAlongPath : 오브젝트를 경로따라 이동 ( 피벗 = 머리 )")]
    private PassVisualMode visualMode = PassVisualMode.PlayAtStart;
    [SerializeField, Tooltip("비어 있으면 활성화 시 기본 State 처음부터 재생")]
    private string visualAnimTrigger;
    [SerializeField, Tooltip("true : 경고 점멸과 동시에 연출 시작 ( 준비 동작 겹침 )")]
    private bool showVisualDuringWarning = true;
    [SerializeField, Tooltip("연출 시작 ~ 내리꽂기 시작 시간 ( 준비 프레임 길이 )")]
    private float windupDuration = 0.8f;
    [SerializeField, Tooltip("내리꽂기 끝 ~ 연출 끄기 시간 ( 꼬리 / 잔상 프레임 길이 )")]
    private float exitDuration = 0.4f;
    [SerializeField, Tooltip("연출 동안 숨길 오브젝트 ( 대기 중인 보스 스프라이트 등 )")]
    private List<GameObject> hideWhileVisual = new List<GameObject>();

    private enum PassVisualMode { PlayAtStart, MoveAlongPath }

    protected override IEnumerator Execute()
    {
        if (lanes.Count == 0) yield break;

        ZoneShape lane = lanes[Random.Range(0, lanes.Count)].Snapshot();

        // 위 -> 아래, 지나간 부분 ( top ~ head ) 만 피해
        Vector2 up = lane.Up;
        Vector2 top = lane.Center + up * lane.size.y * 0.5f;
        Vector2 bottom = lane.Center - up * lane.size.y * 0.5f;

        float visualStartTime = float.NegativeInfinity;
        if (showVisualDuringWarning)
        {
            ShowVisual(top, lane.Angle);
            visualStartTime = Time.time;
        }

        BossTelegraph telegraph = CreateTelegraph(lane, telegraphStyle);
        yield return telegraph.Blink(blinkCount, blinkOnTime, blinkOffTime);
        telegraph.Release();

        if (!showVisualDuringWarning)
        {
            ShowVisual(top, lane.Angle);
            visualStartTime = Time.time;
        }

        // 준비 동작이 남았으면 마저 대기
        float remainWindup = visualStartTime + windupDuration - Time.time;
        if (dragonVisual != null && remainWindup > 0f) yield return new WaitForSeconds(remainWindup);

        float nextHitTime = 0f;
        float elapsed = 0f;
        while (elapsed < passDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / passDuration);
            Vector2 head = Vector2.Lerp(top, bottom, t);

            if (dragonVisual != null && visualMode == PassVisualMode.MoveAlongPath) dragonVisual.position = head;

            float sweptLength = lane.size.y * t;
            ZoneShape swept = ZoneShape.Fixed((top + head) * 0.5f, new Vector2(lane.size.x, sweptLength), lane.Angle);
            TryDamagePlayer(swept, damage, tickInterval, ref nextHitTime);

            yield return null;
        }

        if (dragonVisual != null && exitDuration > 0f) yield return new WaitForSeconds(exitDuration);
    }

    private void ShowVisual(Vector2 top, float angle)
    {
        if (dragonVisual == null) return;

        foreach (GameObject go in hideWhileVisual)
        {
            if (go != null) go.SetActive(false);
        }

        dragonVisual.SetPositionAndRotation(top, Quaternion.Euler(0f, 0f, angle));
        dragonVisual.gameObject.SetActive(true); // 활성화 시 Animator 기본 State 처음부터 재생

        if (!string.IsNullOrEmpty(visualAnimTrigger))
        {
            Animator animator = dragonVisual.GetComponentInChildren<Animator>();
            if (animator != null) animator.SetTrigger(visualAnimTrigger);
        }
    }

    #region [이전] 연출을 경로따라 이동만 하던 Execute
    // protected override IEnumerator Execute()
    // {
    //     if (lanes.Count == 0) yield break;
    //
    //     ZoneShape lane = lanes[Random.Range(0, lanes.Count)].Snapshot();
    //
    //     BossTelegraph telegraph = CreateTelegraph(lane, telegraphStyle);
    //     yield return telegraph.Blink(blinkCount, blinkOnTime, blinkOffTime);
    //     telegraph.Release();
    //
    //     Vector2 up = lane.Up;
    //     Vector2 top = lane.Center + up * lane.size.y * 0.5f;
    //     Vector2 bottom = lane.Center - up * lane.size.y * 0.5f;
    //
    //     if (dragonVisual != null)
    //     {
    //         dragonVisual.position = top;
    //         dragonVisual.gameObject.SetActive(true);
    //     }
    //
    //     float nextHitTime = 0f;
    //     float elapsed = 0f;
    //     while (elapsed < passDuration)
    //     {
    //         elapsed += Time.deltaTime;
    //         float t = Mathf.Clamp01(elapsed / passDuration);
    //         Vector2 head = Vector2.Lerp(top, bottom, t);
    //
    //         if (dragonVisual != null) dragonVisual.position = head;
    //
    //         float sweptLength = lane.size.y * t;
    //         ZoneShape swept = ZoneShape.Fixed((top + head) * 0.5f, new Vector2(lane.size.x, sweptLength), lane.Angle);
    //         TryDamagePlayer(swept, damage, tickInterval, ref nextHitTime);
    //
    //         yield return null;
    //     }
    // }
    #endregion

    public override void Cleanup()
    {
        base.Cleanup();
        if (dragonVisual != null) dragonVisual.gameObject.SetActive(false);

        foreach (GameObject go in hideWhileVisual)
        {
            if (go != null) go.SetActive(true);
        }
    }

    private void OnDrawGizmosSelected()
    {
        foreach (ZoneShape lane in lanes)
        {
            lane.DrawGizmo(Color.red);
        }
    }
}
