using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class SkillTreeNodeUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
    IPointerExitHandler, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private SkillData skill;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private GameObject learnedMark;
    [SerializeField] private GameObject incomingConnection;
    [SerializeField] private Image holdGauge;
    [SerializeField] private TMP_Text statusText;
    [Min(0.1f)] [SerializeField] private float holdDuration = 1f;

    private Button button;
    private PlayerSkills playerSkills;
    private ScrollRect scrollRect;
    private float holdTime;
    private int? holdingPointer;
    private int? draggingPointer;
    private bool holding;

    public SkillData Skill => skill;
    public float HoldProgress => Mathf.Clamp01(holdTime / Mathf.Max(0.1f, holdDuration));

    public void Bind(PlayerSkills player, SkillData data = null)
    {
        CancelHold();
        playerSkills = player;
        if (data != null) skill = data;
        button = GetComponent<Button>();
        scrollRect = GetComponentInParent<ScrollRect>();
        // OnClick은 사용하지 않는다. 게이지 완료 시에만 습득한다.
        Refresh();
    }

    public void Refresh()
    {
        SkillState state = playerSkills != null ? playerSkills.GetState(skill) : SkillState.Hidden;
        bool visible = state != SkillState.Hidden;
        if (incomingConnection != null) incomingConnection.SetActive(visible);
        gameObject.SetActive(visible);
        if (!visible) return;
        bool learned = state == SkillState.Learned;
        if (button != null) button.interactable = !learned;
        if (nameText != null) nameText.text = skill.DisplayName;
        if (costText != null) costText.text = $"{skill.PointCost} SP";
        if (descriptionText != null) descriptionText.text = skill.Description;
        if (learnedMark != null) learnedMark.SetActive(learned);
        if (icon != null)
        {
            icon.sprite = skill.Icon;
            icon.enabled = skill.Icon != null;
        }
        if (learned) CancelHold();
        if (statusText != null) statusText.text = learned ? "습득 완료" : "길게 눌러 습득";
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || holdingPointer.HasValue ||
            draggingPointer.HasValue || playerSkills == null || button == null || !button.IsInteractable()) return;
        SkillState state = playerSkills.GetState(skill);
        if (state != SkillState.Available && state != SkillState.InsufficientPoints) return;
        holdingPointer = eventData.pointerId;
        holding = true;
        holdTime = 0f;
        if (scrollRect != null) scrollRect.StopMovement();
        if (statusText != null) statusText.text = "습득 중...";
        SetGauge(0f);
    }

    private void Update() => AdvanceHold(Time.unscaledDeltaTime);

    private void AdvanceHold(float deltaTime)
    {
        if (!holding) return;
        holdTime += Mathf.Max(0f, deltaTime);
        SetGauge(HoldProgress);
        if (HoldProgress < 1f) return;

        holding = false; // 실패해도 계속 누르고 있는 동안 재시도하지 않는다.
        bool learned = playerSkills != null && playerSkills.TryLearn(skill);
        holdTime = 0f;
        SetGauge(0f);
        if (statusText != null) statusText.text = learned ? "습득 완료" : "스킬 포인트 부족";
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (holdingPointer == eventData.pointerId) CancelHold();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (holdingPointer == eventData.pointerId) CancelHold();
    }

    private void CancelHold()
    {
        holding = false;
        holdingPointer = null;
        holdTime = 0f;
        SetGauge(0f);
    }

    private void SetGauge(float value)
    {
        if (holdGauge == null) return;

        // 단색 Image의 가로 앵커를 늘려 채운다. 내장 Sprite 참조가 필요 없다.
        holdGauge.type = Image.Type.Simple;
        RectTransform gaugeRect = holdGauge.rectTransform;
        gaugeRect.anchorMin = Vector2.zero;
        gaugeRect.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
        gaugeRect.offsetMin = Vector2.zero;
        gaugeRect.offsetMax = Vector2.zero;
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (scrollRect != null) scrollRect.OnInitializePotentialDrag(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (draggingPointer.HasValue || (holdingPointer.HasValue && holdingPointer != eventData.pointerId)) return;
        CancelHold();
        draggingPointer = eventData.pointerId;
        if (scrollRect != null) scrollRect.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggingPointer == eventData.pointerId && scrollRect != null) scrollRect.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggingPointer != eventData.pointerId) return;
        if (scrollRect != null) scrollRect.OnEndDrag(eventData);
        draggingPointer = null;
    }

    private void OnDisable()
    {
        CancelHold();
        draggingPointer = null;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) CancelHold();
    }
}
