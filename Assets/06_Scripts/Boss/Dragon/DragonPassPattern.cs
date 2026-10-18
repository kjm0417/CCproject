using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 1번 지나가용
/// 세로 구역 빨간 테두리 blinkCount 회 점멸 -> 보스 본체가 제자리 준비 동작 후 구역 위 -> 아래로 지나가며 피해
/// 연출 : 보스 본체 Animator 의 passTrigger ( DragonPass ) 재생 + 본체 이동, 끝나면 제자리로 복귀 후 페이드 인
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
    [SerializeField, Tooltip("보스가 제자리에서 구역 아래 끝까지 지나가는 시간 - 지나가는 프레임도 이 시간에 맞게 늘어남")]
    private float passDuration = 1.1f;
    [SerializeField] private float damage = 20f;
    [SerializeField] private float tickInterval = 0.5f;

    [Header("연출 ( 보스 본체 )")]
    [SerializeField, Tooltip("이동시킬 보스 스프라이트 ( Animator 포함 ). 비우면 보스 하위의 활성 Animator")]
    private Transform bossVisual;
    [SerializeField, Tooltip("보스 Animator 의 지나가용 트리거 ( Idle -> DragonPass )")]
    private string passTrigger = "Pass";
    [SerializeField, Tooltip("복귀 시 재생할 State 이름")]
    private string idleStateName = "Idle";
    [SerializeField, Tooltip("제자리 고정 프레임 구간 길이 ( 고정 프레임 수 / 샘플레이트 )")]
    private float windupDuration = 0.4167f;
    [SerializeField, FormerlySerializedAs("diveAnimLength"), Tooltip("지나가는 프레임 구간 길이 ( 클립 길이 - windupDuration ). 0 보다 크면 passDuration 에 맞게 애니 속도 조절")]
    private float passAnimLength = 0.4167f;
    [SerializeField, Tooltip("제자리 복귀 후 페이드 인 시간 ( 0 이면 바로 표시 )")]
    private float reappearFadeDuration = 0.4f;

    private Animator animator;
    private SpriteRenderer[] renderers;
    private float[] originAlphas;
    private Vector3 homeLocalPos;
    private bool moved;

    private void Start()
    {
        if (bossVisual == null)
        {
            Animator found = Boss != null ? Boss.GetComponentInChildren<Animator>() : null;
            if (found != null) bossVisual = found.transform;
        }
        if (bossVisual == null) return;

        animator = bossVisual.GetComponent<Animator>();
        renderers = bossVisual.GetComponentsInChildren<SpriteRenderer>(true);
        originAlphas = new float[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) originAlphas[i] = renderers[i].color.a;
        homeLocalPos = bossVisual.localPosition;
    }

    protected override IEnumerator Execute()
    {
        if (lanes.Count == 0) yield break;

        ZoneShape lane = lanes[Random.Range(0, lanes.Count)].Snapshot();

        // 1. 경고
        BossTelegraph telegraph = CreateTelegraph(lane, telegraphStyle);
        yield return telegraph.Blink(blinkCount, blinkOnTime, blinkOffTime);
        telegraph.Release();

        // 2. 제자리 준비 동작 + 3. 지나가기
        yield return PassMotion(lane, true);

        // 4. 제자리 복귀 + 페이드 인
        ReturnHome();
        if (reappearFadeDuration > 0f)
        {
            SetAlpha(0f);
            float fade = 0f;
            while (fade < reappearFadeDuration)
            {
                fade += Time.deltaTime;
                SetAlpha(Mathf.Clamp01(fade / reappearFadeDuration));
                yield return null;
            }
        }
        SetAlpha(1f);
    }

    /// <summary> 제자리 준비 동작 후 구역 위 -> 아래로 이동 ( dealDamage : 지나간 부분 피해 ) </summary>
    private IEnumerator PassMotion(ZoneShape lane, bool dealDamage)
    {
        Vector2 up = lane.Up;
        Vector2 top = lane.Center + up * lane.size.y * 0.5f;
        Vector2 bottom = lane.Center - up * lane.size.y * 0.5f;

        // 제자리 준비 동작
        if (animator != null && !string.IsNullOrEmpty(passTrigger)) animator.SetTrigger(passTrigger);
        if (bossVisual != null && windupDuration > 0f) yield return new WaitForSeconds(windupDuration);

        // 지나가기 - 지나가는 프레임이 passDuration 동안 재생되도록
        if (animator != null && passAnimLength > 0f && passDuration > 0f) animator.speed = passAnimLength / passDuration;

        Vector3 home = bossVisual != null ? bossVisual.position : (Vector3)top;
        moved = bossVisual != null;

        float nextHitTime = 0f;
        float elapsed = 0f;
        while (elapsed < passDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / passDuration);

            if (bossVisual != null)
            {
                Vector3 pos = Vector2.Lerp(home, bottom, t);
                pos.z = home.z;
                bossVisual.position = pos;
            }

            // 위 -> 아래, 지나간 부분 ( top ~ head ) 만 피해
            if (dealDamage)
            {
                Vector2 head = Vector2.Lerp(top, bottom, t);
                ZoneShape swept = ZoneShape.Fixed((top + head) * 0.5f, new Vector2(lane.size.x, lane.size.y * t), lane.Angle);
                TryDamagePlayer(swept, damage, tickInterval, ref nextHitTime);
            }

            yield return null;
        }
    }

    /// <summary> 등장 연출용 - 첫 번째 구역으로 지나가기만 ( 경고 / 피해 / 복귀 X, 본체는 구역 아래 끝에 남음 ) </summary>
    public IEnumerator PlayIntroPass()
    {
        if (lanes.Count == 0 || bossVisual == null) yield break;

        yield return PassMotion(lanes[0].Snapshot(), false);
        if (animator != null) animator.speed = 1f;
    }

    /// <summary> 등장 연출 종료 - Idle 로 전환 ( 위치는 연출에서 처리 ) </summary>
    public void EndIntroPass()
    {
        if (animator != null)
        {
            animator.speed = 1f;
            animator.ResetTrigger(passTrigger);
            if (!string.IsNullOrEmpty(idleStateName)) animator.Play(idleStateName, 0, 0f);
        }
        moved = false;
    }

    private void ReturnHome()
    {
        if (animator != null)
        {
            animator.speed = 1f;
            if (moved && !Boss.IsStunned && !Boss.IsDead && !string.IsNullOrEmpty(idleStateName))
            {
                animator.ResetTrigger(passTrigger);
                animator.Play(idleStateName, 0, 0f);
            }
        }

        if (bossVisual != null) bossVisual.localPosition = homeLocalPos;
        moved = false;
    }

    /// <param name="ratio">원래 알파 기준 비율</param>
    private void SetAlpha(float ratio)
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Color c = renderers[i].color;
            c.a = originAlphas[i] * ratio;
            renderers[i].color = c;
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        // 스턴 / 사망으로 중단돼도 본체는 제자리 + 원래 알파 ( 상태 애니는 Stun / Die 트리거가 처리 )
        ReturnHome();
        SetAlpha(1f);
    }

    private void OnDrawGizmosSelected()
    {
        foreach (ZoneShape lane in lanes)
        {
            lane.DrawGizmo(Color.red);
        }
    }
}
