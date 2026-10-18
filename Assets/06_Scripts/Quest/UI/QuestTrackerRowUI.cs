using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 기본 화면 트래커 한 줄 (아이콘 · 조건 · 남은 수 · 얇은 바). 받기 대기면 노란 줄 + 빨간 '받기'
/// </summary>
public class QuestTrackerRowUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private Outline outline;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text txtCondition;
    [SerializeField] private TMP_Text txtCount;
    [SerializeField] private Image countBackground; //'받기' 빨간 배경
    [SerializeField] private Image barFill;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color32(0xFF, 0xFA, 0xF0, 0xF0);
    [SerializeField] private Color readyColor = new Color32(0xFF, 0xF3, 0xCF, 0xFF);
    [SerializeField] private Color barColor = new Color32(0x6F, 0xC7, 0xF2, 0xFF);
    [SerializeField] private Color readyBarColor = new Color32(0xF2, 0xC7, 0x5C, 0xFF);
    [SerializeField] private Color inkColor = new Color32(0x2E, 0x33, 0x38, 0xFF);
    [SerializeField] private Color readyOutlineColor = new Color32(0xB8, 0x86, 0x2A, 0xFF);

    private bool isReady;
    private Coroutine bumpRoutine;

    public Button Button => button;
    public QuestData Quest { get; private set; }
    public bool IsReady => isReady;

    public void Bind(QuestData quest, int count, QuestState state)
    {
        Quest = quest;
        isReady = state == QuestState.Ready;

        icon.sprite = quest.Icon;
        icon.enabled = quest.Icon != null;
        txtCondition.text = quest.ConditionText;
        barFill.fillAmount = Mathf.Clamp01((float)count / quest.RequiredCount);

        background.color = isReady ? readyColor : normalColor;
        barFill.color = isReady ? readyBarColor : barColor;

        txtCount.text = isReady ? "받기" : $"{Mathf.Min(count, quest.RequiredCount)}/{quest.RequiredCount}";
        txtCount.color = isReady ? Color.white : inkColor;
        countBackground.enabled = isReady;

        if (!isReady) outline.effectColor = inkColor;
    }

    /// <summary>
    /// 방금 진행된 줄 튀어 오르기
    /// </summary>
    public void Bump()
    {
        if (bumpRoutine != null) StopCoroutine(bumpRoutine);
        bumpRoutine = StartCoroutine(BumpRoutine());
    }

    private void Update()
    {
        // 받기 대기 줄 테두리 천천히 깜빡임
        if (!isReady) return;
        float t = Mathf.PingPong(Time.unscaledTime / 0.8f, 1f);
        outline.effectColor = Color.Lerp(readyOutlineColor, readyBarColor, t);
    }

    private void OnDisable()
    {
        transform.localScale = Vector3.one;
        bumpRoutine = null;
    }

    private IEnumerator BumpRoutine()
    {
        const float duration = 0.45f;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float scale = 1f + 0.07f * Mathf.Sin(t / duration * Mathf.PI);
            transform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        transform.localScale = Vector3.one;
        bumpRoutine = null;
    }
}
