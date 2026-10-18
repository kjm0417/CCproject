using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 퀘스트 패널 카드 한 장 (퀘스트명 + 태그 / 지급 횟수 / 완료 조건 / 보상 + 받기)
/// </summary>
public class QuestCardUI : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Outline outline;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text txtName;
    [SerializeField] private TMP_Text txtTag;
    [SerializeField] private TMP_Text txtPaid;
    [SerializeField] private Transform pipRoot;
    [SerializeField] private Image pipTemplate;
    [SerializeField] private Image icon;
    [SerializeField] private Image barFill;
    [SerializeField] private TMP_Text txtCondition;
    [SerializeField] private TMP_Text txtReward;
    [SerializeField] private Button claimButton;
    [SerializeField] private Image claimButtonImage;
    [SerializeField] private TMP_Text txtClaim;
    [SerializeField] private Image flash;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color readyColor = new Color32(0xFF, 0xF8, 0xE2, 0xFF);
    [SerializeField] private Color inkColor = new Color32(0x2E, 0x33, 0x38, 0xFF);
    [SerializeField] private Color goldColor = new Color32(0xF2, 0xC7, 0x5C, 0xFF);
    [SerializeField] private Color goldDarkColor = new Color32(0xB8, 0x86, 0x2A, 0xFF);
    [SerializeField] private Color skyColor = new Color32(0x6F, 0xC7, 0xF2, 0xFF);
    [SerializeField] private Color skyDarkColor = new Color32(0x2F, 0x8F, 0xC4, 0xFF);
    [SerializeField] private Color disabledColor = new Color32(0xE4, 0xE7, 0xE9, 0xFF);
    [SerializeField] private Color muteColor = new Color32(0x9A, 0xA3, 0xAA, 0xFF);
    [SerializeField] private Color oneTimeTagColor = new Color32(0xA3, 0x6A, 0x1A, 0xFF);

    private readonly List<Image> pips = new List<Image>();
    private QuestData quest;
    private Coroutine flashRoutine;

    private void Awake()
    {
        pipTemplate.gameObject.SetActive(false);
        flash.gameObject.SetActive(false);
        claimButton.onClick.AddListener(OnClaim);
    }

    public void Bind(QuestData quest, int count, int paidCount, QuestState state)
    {
        this.quest = quest;

        txtName.text = quest.QuestName;
        txtTag.text = quest.IsRepeatable ? "반복" : "1회";
        txtTag.color = quest.IsRepeatable ? skyDarkColor : oneTimeTagColor;

        txtPaid.text = $"지급 {paidCount}/{quest.MaxPayCount}";
        BindPips(paidCount, quest.MaxPayCount);

        icon.sprite = quest.Icon;
        icon.enabled = quest.Icon != null;
        barFill.fillAmount = Mathf.Clamp01((float)count / quest.RequiredCount);
        barFill.color = state == QuestState.Ready ? goldColor : skyColor;
        txtCondition.text = $"{quest.ConditionText}  <b>{Mathf.Min(count, quest.RequiredCount)}/{quest.RequiredCount}</b>";

        txtReward.text = $"{quest.RewardGold} G";

        background.color = state == QuestState.Ready ? readyColor : normalColor;
        outline.effectColor = state == QuestState.Ready ? goldDarkColor : inkColor;
        canvasGroup.alpha = state == QuestState.Done ? 0.55f : 1f;

        claimButton.interactable = state == QuestState.Ready;
        claimButtonImage.color = state == QuestState.Ready ? goldColor : disabledColor;
        txtClaim.text = state == QuestState.Done ? "완료" : "받기";
        txtClaim.color = state == QuestState.Ready ? inkColor : muteColor;
    }

    private void BindPips(int paidCount, int maxCount)
    {
        while (pips.Count < maxCount)
        {
            Image pip = Instantiate(pipTemplate, pipRoot);
            pips.Add(pip);
        }

        for (int i = 0; i < pips.Count; i++)
        {
            pips[i].gameObject.SetActive(i < maxCount);
            pips[i].color = i < paidCount ? goldColor : Color.white;
        }
    }

    private void OnClaim()
    {
        if (QuestManager.Instance == null || quest == null) return;

        // 반복 퀘스트는 카드가 남으므로 반짝임, 1회 퀘스트는 목록에서 사라짐
        if (QuestManager.Instance.Claim(quest) && QuestManager.Instance.GetState(quest) != QuestState.Done) PlayFlash();
    }

    public void PlayFlash()
    {
        if (!isActiveAndEnabled) return;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        flash.gameObject.SetActive(true);
        Color color = flash.color;
        const float duration = 0.5f;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            color.a = 0.5f * (1f - t / duration);
            flash.color = color;
            yield return null;
        }
        flash.gameObject.SetActive(false);
        flashRoutine = null;
    }
}
