using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스 체력바 ( 화면 상단 ) - 보스 맵 진입 ( 보스 활성화 ) 시 표시, 사망 / 맵 이탈 시 숨김
/// 등장 시 0 -> 현재 체력까지 차오르는 연출, 피해 시 잔상 바가 늦게 따라감
/// </summary>
public class BossHpUI : MonoBehaviour
{
    [SerializeField, Tooltip("비우면 씬의 BossBase 자동 탐색")]
    private BossBase boss;
    [SerializeField, Tooltip("비우면 BossData.bossName")]
    private string bossNameOverride;

    [Header("UI")]
    [SerializeField, Tooltip("표시 / 숨김 대상 ( CanvasGroup )")]
    private CanvasGroup root;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField, Tooltip("Image Type : Filled / Horizontal")]
    private Image fill;
    [SerializeField, Tooltip("피해 잔상 ( Filled / Horizontal ) - 선택")]
    private Image trailFill;
    [SerializeField, Tooltip("체력 수치 ( 선택 )")]
    private TextMeshProUGUI hpText;

    [Header("연출")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField, Tooltip("0 -> 최대 체력 차오르는 시간")]
    private float fillUpDuration = 1.2f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField, Tooltip("피해 후 잔상 바가 줄어들기 시작하기까지")]
    private float trailDelay = 0.4f;
    [SerializeField, Tooltip("잔상 바 초당 감소량 ( 비율 )")]
    private float trailSpeed = 0.6f;

    private bool shown;
    private bool introPlaying;
    private float trailHoldUntil;
    private Coroutine showRoutine;

    private void Awake()
    {
        if (boss == null) boss = FindAnyObjectByType<BossBase>(FindObjectsInactive.Include);
        SetAlpha(0f);
    }

    private void OnEnable()
    {
        if (boss == null) return;
        boss.OnHpChanged += HandleHpChanged;
        boss.OnDiedEvent += Hide;
    }

    private void OnDisable()
    {
        if (boss == null) return;
        boss.OnHpChanged -= HandleHpChanged;
        boss.OnDiedEvent -= Hide;
    }

    private void Update()
    {
        if (boss == null) return;

        // 보스 맵 활성화 = 보스 조우
        bool active = boss.gameObject.activeInHierarchy && !boss.IsDead;
        if (active && !shown) Show();
        else if (!active && shown && !boss.IsDead) Hide(); //보스 처치 전 맵 이탈

        UpdateTrail();
    }

    private void Show()
    {
        shown = true;
        if (nameText != null) nameText.text = string.IsNullOrEmpty(bossNameOverride) ? boss.Data.bossName : bossNameOverride;

        if (showRoutine != null) StopCoroutine(showRoutine);
        showRoutine = StartCoroutine(ShowRoutine());
    }

    private void Hide()
    {
        if (!shown) return;
        shown = false;
        introPlaying = false;

        if (showRoutine != null) StopCoroutine(showRoutine);
        showRoutine = StartCoroutine(FadeRoutine(fadeOutDuration, 0f));
    }

    private IEnumerator ShowRoutine()
    {
        introPlaying = true;
        SetRatio(0f, 0);
        if (trailFill != null) trailFill.fillAmount = 0f;

        yield return FadeRoutine(fadeInDuration, 1f);

        // 0 -> 현재 체력 ( 처음엔 최대 체력 )
        float elapsed = 0f;
        while (elapsed < fillUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fillUpDuration));
            SetRatio(boss.HpRatio * t, Mathf.RoundToInt(boss.CurrentHp * t));
            if (trailFill != null) trailFill.fillAmount = fill != null ? fill.fillAmount : 0f;
            yield return null;
        }

        introPlaying = false;
        SetRatio(boss.HpRatio, boss.CurrentHp);
        if (trailFill != null) trailFill.fillAmount = boss.HpRatio;
        showRoutine = null;
    }

    private IEnumerator FadeRoutine(float duration, float target)
    {
        if (root == null) yield break;

        float start = root.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }
        SetAlpha(target);
    }

    private void HandleHpChanged(int current, int max)
    {
        if (!shown || introPlaying) return;

        SetRatio(max > 0 ? (float)current / max : 0f, current);
        trailHoldUntil = Time.time + trailDelay;
    }

    private void UpdateTrail()
    {
        if (trailFill == null || fill == null || introPlaying) return;

        // 회복 시 바로 맞춤, 피해 시 잠시 뒤 천천히 감소
        if (trailFill.fillAmount < fill.fillAmount) trailFill.fillAmount = fill.fillAmount;
        else if (Time.time >= trailHoldUntil) trailFill.fillAmount = Mathf.MoveTowards(trailFill.fillAmount, fill.fillAmount, trailSpeed * Time.deltaTime);
    }

    private void SetRatio(float ratio, int hp)
    {
        if (fill != null) fill.fillAmount = Mathf.Clamp01(ratio);
        if (hpText != null) hpText.text = $"{hp} / {boss.Data.maxHp}";
    }

    private void SetAlpha(float alpha)
    {
        if (root == null) return;
        root.alpha = alpha;
        root.blocksRaycasts = false;
        root.interactable = false;
    }
}
