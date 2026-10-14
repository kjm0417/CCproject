using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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

    [Header("연출 - 애니 동기화 ( MoveAlongPath )")]
    [SerializeField, Tooltip("준비 동작 ( 위치 고정 프레임 ) 위치 - 지나가기는 여기서 Lane 아래 끝까지 이동. 비우면 Lane 윗변")]
    private Transform windupPoint;
    [SerializeField, FormerlySerializedAs("diveAnimLength"), Tooltip("지나가는 프레임 구간 길이 ( 초 ). 0 보다 크면 준비 동작은 원래 속도, 지나가기 동안 이 구간이 passDuration 에 맞게 늘어나도록 애니 속도 조절")]
    private float passAnimLength;
    [SerializeField, Tooltip("지나간 뒤 연출이 다시 나타날 위치 ( 보스 자리 ). 비우면 복귀 연출 없음")]
    private Transform returnPoint;
    [SerializeField, Range(0f, 1f), Tooltip("복귀 연출 시작 지점 ( 클립 정규화 시간, 0.5 = 클립 절반부터 )")]
    private float returnNormalizedTime = 0.5f;
    [SerializeField, Tooltip("복귀 연출 시간 - 끝나면 연출 끄고 숨긴 보스 다시 표시")]
    private float returnDuration = 0.42f;
    [SerializeField, Tooltip("숨겼던 보스가 다시 나타날 때 페이드 인 시간 ( 0 이면 바로 표시 )")]
    private float reappearFadeDuration = 0.4f;

    private enum PassVisualMode { PlayAtStart, MoveAlongPath }

    private void Awake()
    {
        // 씬에 켜진 채 저장돼 있어도 패턴 전에는 숨김
        if (dragonVisual != null) dragonVisual.gameObject.SetActive(false);
    }

    protected override IEnumerator Execute()
    {
        if (lanes.Count == 0) yield break;

        ZoneShape lane = lanes[Random.Range(0, lanes.Count)].Snapshot();

        // 위 -> 아래, 지나간 부분 ( top ~ head ) 만 피해
        Vector2 up = lane.Up;
        Vector2 top = lane.Center + up * lane.size.y * 0.5f;
        Vector2 bottom = lane.Center - up * lane.size.y * 0.5f;

        Vector2 visualStart = windupPoint != null ? (Vector2)windupPoint.position : top;
        float visualStartTime = float.NegativeInfinity;
        if (showVisualDuringWarning)
        {
            ShowVisual(visualStart, lane.Angle);
            visualStartTime = Time.time;
        }

        BossTelegraph telegraph = CreateTelegraph(lane, telegraphStyle);
        yield return telegraph.Blink(blinkCount, blinkOnTime, blinkOffTime);
        telegraph.Release();

        if (!showVisualDuringWarning)
        {
            ShowVisual(visualStart, lane.Angle);
            visualStartTime = Time.time;
        }

        Animator visualAnimator = dragonVisual != null ? dragonVisual.GetComponentInChildren<Animator>() : null;

        // 준비 동작이 남았으면 마저 대기
        float remainWindup = visualStartTime + windupDuration - Time.time;
        if (dragonVisual != null && remainWindup > 0f) yield return new WaitForSeconds(remainWindup);

        // 지나가는 프레임 구간이 passDuration 동안 재생되도록
        if (visualAnimator != null && passAnimLength > 0f && passDuration > 0f)
        {
            visualAnimator.speed = passAnimLength / passDuration;
        }

        float nextHitTime = 0f;
        float elapsed = 0f;
        while (elapsed < passDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / passDuration);
            Vector2 head = Vector2.Lerp(top, bottom, t);

            if (dragonVisual != null && visualMode == PassVisualMode.MoveAlongPath) dragonVisual.position = Vector2.Lerp(visualStart, bottom, t);

            float sweptLength = lane.size.y * t;
            ZoneShape swept = ZoneShape.Fixed((top + head) * 0.5f, new Vector2(lane.size.x, sweptLength), lane.Angle);
            TryDamagePlayer(swept, damage, tickInterval, ref nextHitTime);

            yield return null;
        }

        if (visualAnimator != null) visualAnimator.speed = 1f;
        if (dragonVisual != null && exitDuration > 0f) yield return new WaitForSeconds(exitDuration);

        // 보스 자리에서 다시 나타나는 연출 ( 클립 나머지 구간 )
        if (dragonVisual != null && returnPoint != null && returnDuration > 0f)
        {
            dragonVisual.SetPositionAndRotation(returnPoint.position, Quaternion.identity);
            if (visualAnimator != null)
            {
                visualAnimator.speed = 1f;
                visualAnimator.Play(visualAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, returnNormalizedTime);
            }
            yield return new WaitForSeconds(returnDuration);
        }

        // 연출 끄고 숨긴 보스 페이드 인
        if (dragonVisual != null && reappearFadeDuration > 0f)
        {
            HideVisual();
            float elapsed2 = 0f;
            while (elapsed2 < reappearFadeDuration)
            {
                elapsed2 += Time.deltaTime;
                SetHiddenAlpha(Mathf.Clamp01(elapsed2 / reappearFadeDuration));
                yield return null;
            }
        }
    }

    private void HideVisual()
    {
        if (dragonVisual == null) return;

        Animator visualAnimator = dragonVisual.GetComponentInChildren<Animator>();
        if (visualAnimator != null) visualAnimator.speed = 1f;
        dragonVisual.gameObject.SetActive(false);

        foreach (GameObject go in hideWhileVisual)
        {
            if (go != null) go.SetActive(true);
        }
    }

    /// <summary> 숨겼던 오브젝트 스프라이트 알파 ( 원래 알파 기준 비율 ) </summary>
    private void SetHiddenAlpha(float ratio)
    {
        for (int i = 0; i < hiddenRenderers.Count; i++)
        {
            if (hiddenRenderers[i] == null) continue;
            Color c = hiddenRenderers[i].color;
            c.a = hiddenAlphas[i] * ratio;
            hiddenRenderers[i].color = c;
        }
    }

    private readonly List<SpriteRenderer> hiddenRenderers = new List<SpriteRenderer>();
    private readonly List<float> hiddenAlphas = new List<float>();

    private void ShowVisual(Vector2 top, float angle)
    {
        if (dragonVisual == null) return;

        hiddenRenderers.Clear();
        hiddenAlphas.Clear();
        foreach (GameObject go in hideWhileVisual)
        {
            if (go == null) continue;
            foreach (SpriteRenderer sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                hiddenRenderers.Add(sr);
                hiddenAlphas.Add(sr.color.a);
            }
            go.SetActive(false);
        }
        SetHiddenAlpha(0f);

        dragonVisual.SetPositionAndRotation(top, Quaternion.Euler(0f, 0f, angle));
        dragonVisual.gameObject.SetActive(true);

        // 이미 켜져 있었거나 상태가 남아 있어도 기본 State 처음부터
        Animator visualAnimator = dragonVisual.GetComponentInChildren<Animator>();
        if (visualAnimator != null)
        {
            visualAnimator.speed = 1f;
            visualAnimator.Rebind();
            visualAnimator.Update(0f);
        }

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
        HideVisual();
        SetHiddenAlpha(1f);
    }

    private void OnDrawGizmosSelected()
    {
        foreach (ZoneShape lane in lanes)
        {
            lane.DrawGizmo(Color.red);
        }
    }
}
