using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 던전 입장 UI ( 좌측 정보 / 우측 입장 2단 패널 )
/// 상태 : 제출 없음 ( B 던전 / 클리어 후 ) / 첫 클리어 전 제출 / 재료 부족
/// 던전 입구에서 멀어지면 자동 닫힘
/// </summary>
public class DungeonEntryUI : MonoBehaviour
{
    [Header("패널")]
    [SerializeField]
    private GameObject panelRoot;
    [SerializeField]
    private Button closeButton;
    [SerializeField, Tooltip("입구에서 이 거리 이상 멀어지면 자동 닫힘")]
    private float autoCloseDistance = 3f;

    [Header("좌측 - 던전 정보")]
    [SerializeField]
    private TextMeshProUGUI dungeonNameText;
    [SerializeField]
    private TextMeshProUGUI recommendLevelText;
    [SerializeField]
    private TextMeshProUGUI monsterInfoText;
    [SerializeField, Tooltip("BossBox 이미지 - DungeonData.BossSprite 적용 ( 없으면 기본 배경 )")]
    private Image bossBoxImage;
    [SerializeField, Tooltip("보스 스프라이트 없을 때 표시할 기본 아이콘 ( 해골 )")]
    private GameObject bossDefaultIcon;
    [SerializeField]
    private DungeonEntryRewardRow firstRewardRow;
    [SerializeField]
    private DungeonEntryRewardRow repeatRewardRow;

    [Header("우측 - 제출 아이템")]
    [SerializeField, Tooltip("제출 아이템 있을 때 ( 제목 + 첫 클리어 전까지 + 칸 )")]
    private GameObject submitGroup;
    [SerializeField]
    private RectTransform submitContent;
    [SerializeField]
    private DungeonEntryItemSlot submitSlotTemplate;
    [SerializeField, Tooltip("✓ 제출 아이템 없음")]
    private GameObject noSubmitGroup;

    [Header("우측 - 환급 안내")]
    [SerializeField]
    private GameObject refundGroup;
    [SerializeField]
    private Button refundInfoButton;
    [SerializeField]
    private GameObject refundTooltip;
    [SerializeField]
    private TextMeshProUGUI refundTooltipText;

    [Header("우측 - 입장 버튼")]
    [SerializeField]
    private Button enterButton;
    [SerializeField]
    private Image enterButtonImage;
    [SerializeField]
    private TextMeshProUGUI enterButtonText;

    [Header("토스트")]
    [SerializeField]
    private CanvasGroup toastGroup;
    [SerializeField]
    private TextMeshProUGUI toastText;
    [SerializeField]
    private float toastDuration = 1.5f;

    [Header("색상")]
    [SerializeField]
    private Color enterColor = new Color(0.17f, 0.45f, 0.84f);
    [SerializeField]
    private Color enterTextColor = Color.white;
    [SerializeField]
    private Color disabledColor = new Color(0.93f, 0.93f, 0.93f);
    [SerializeField]
    private Color disabledTextColor = new Color(0.25f, 0.25f, 0.25f);
    [SerializeField]
    private Color enoughCountColor = new Color(0.2f, 0.2f, 0.2f);
    [SerializeField]
    private Color lackCountColor = new Color(0.85f, 0.2f, 0.2f);

    private const string SubmitEnterLabel = "제출하고 입장";
    private const string EnterLabel = "입장";
    private const string LackToast = "재료가 부족해요";

    private readonly List<DungeonEntryItemSlot> submitSlots = new List<DungeonEntryItemSlot>();

    private DungeonData dungeon;
    private PlayerContext player;
    private Transform entrance;
    private Action onEnter;
    private bool canEnter;
    private HUDBottomPanel hudPanel;
    private Coroutine toastRoutine;

    //BossBox 기본 배경 ( 보스 스프라이트 없을 때 복구용 )
    private Sprite bossBoxDefaultSprite;
    private Color bossBoxDefaultColor;
    private Image.Type bossBoxDefaultType;

    public bool IsOpen => panelRoot.activeSelf;

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
        if (submitSlotTemplate != null) submitSlotTemplate.gameObject.SetActive(false);

        if (bossBoxImage != null)
        {
            bossBoxDefaultSprite = bossBoxImage.sprite;
            bossBoxDefaultColor = bossBoxImage.color;
            bossBoxDefaultType = bossBoxImage.type;
        }

        closeButton.onClick.AddListener(Close);
        enterButton.onClick.AddListener(OnEnterClick);
        if (refundInfoButton != null) refundInfoButton.onClick.AddListener(ToggleRefundTooltip);

        panelRoot.SetActive(false);
    }

    private void Update()
    {
        if (!IsOpen || player == null || entrance == null) return;

        if (Vector2.Distance(player.transform.position, entrance.position) > autoCloseDistance)
        {
            Close();
        }
    }

    /// <summary>
    /// 패널 열기 - onEnter : 입장 버튼 ( 조건 충족 ) 시 호출 ( 아이템 소모 / 씬 이동은 호출하는 쪽 )
    /// </summary>
    public void Open(DungeonData dungeon, PlayerContext player, Transform entrance, Action onEnter)
    {
        if (dungeon == null) return;

        this.dungeon = dungeon;
        this.player = player;
        this.entrance = entrance;
        this.onEnter = onEnter;

        panelRoot.SetActive(true);
        if (refundTooltip != null) refundTooltip.SetActive(false);
        if (toastGroup != null) toastGroup.alpha = 0f;

        Refresh();
    }

    public void Close()
    {
        if (!IsOpen) return;

        panelRoot.SetActive(false);
        onEnter = null;
        player = null;
        entrance = null;
    }

    private void Refresh()
    {
        DungeonRecordData record = DungeonSession.GetRecord(dungeon);
        bool requiresSubmit = DungeonSession.RequiresSubmit(dungeon);

        //좌측 정보
        dungeonNameText.text = dungeon.DungeonName;
        recommendLevelText.text = $"권장 Lv{dungeon.RecommendLevel}";
        monsterInfoText.text = string.IsNullOrEmpty(dungeon.MonsterInfo) ? "" : $"출현: {dungeon.MonsterInfo}";
        RefreshBoss();

        firstRewardRow.Set(dungeon.FirstClearReward, record.FirstRewardClaimed, ResolveIcon);
        repeatRewardRow.Set(dungeon.RepeatClearReward, false, ResolveIcon);

        //우측 제출 아이템
        submitGroup.SetActive(requiresSubmit);
        noSubmitGroup.SetActive(!requiresSubmit);
        canEnter = !requiresSubmit || RefreshSubmitSlots();

        //환급 안내 ( 제출할 때만 )
        if (refundGroup != null) refundGroup.SetActive(requiresSubmit);
        if (refundTooltipText != null) refundTooltipText.text = $"실패 시 {Mathf.RoundToInt(dungeon.FailRefundRatio * 100f)}% 환급";

        //입장 버튼
        enterButtonText.text = requiresSubmit ? SubmitEnterLabel : EnterLabel;
        enterButtonImage.color = canEnter ? enterColor : disabledColor;
        enterButtonText.color = canEnter ? enterTextColor : disabledTextColor;
    }

    /// <summary>
    /// BossBox - 보스 스프라이트 있으면 적용, 없으면 기본 아이콘
    /// </summary>
    private void RefreshBoss()
    {
        bool hasSprite = dungeon.BossSprite != null;

        if (bossBoxImage != null)
        {
            bossBoxImage.sprite = hasSprite ? dungeon.BossSprite : bossBoxDefaultSprite;
            bossBoxImage.color = hasSprite ? Color.white : bossBoxDefaultColor;
            bossBoxImage.type = hasSprite ? Image.Type.Simple : bossBoxDefaultType;
            bossBoxImage.preserveAspect = hasSprite;
        }
        if (bossDefaultIcon != null) bossDefaultIcon.SetActive(!hasSprite);
    }

    /// <summary>
    /// 제출 칸 갱신 ( 보유/필요 ) - 모두 충분하면 true
    /// </summary>
    private bool RefreshSubmitSlots()
    {
        PlayerInventory inventory = player != null ? player.Inventory : null;
        bool enough = true;
        int index = 0;

        foreach (DungeonItemAmount cost in dungeon.EntryCosts)
        {
            if (cost.Item == null || cost.Count <= 0) continue;

            int owned = DungeonInventoryBridge.GetCount(inventory, cost.Item);
            bool lack = owned < cost.Count;
            if (lack) enough = false;

            DungeonEntryItemSlot slot = GetSubmitSlot(index++);
            slot.Set(ResolveIcon(cost.Item), null, $"{owned}/{cost.Count}", lack ? lackCountColor : enoughCountColor);
        }

        for (int i = index; i < submitSlots.Count; i++)
        {
            submitSlots[i].gameObject.SetActive(false);
        }
        return enough;
    }

    private DungeonEntryItemSlot GetSubmitSlot(int index)
    {
        while (submitSlots.Count <= index)
        {
            DungeonEntryItemSlot slot = Instantiate(submitSlotTemplate, submitContent);
            slot.name = $"SubmitSlot_{submitSlots.Count + 1}";
            submitSlots.Add(slot);
        }

        submitSlots[index].gameObject.SetActive(true);
        return submitSlots[index];
    }

    private void OnEnterClick()
    {
        if (!canEnter)
        {
            ShowToast(LackToast);
            return;
        }

        Action enter = onEnter;
        Close();
        enter?.Invoke();
    }

    private void ToggleRefundTooltip()
    {
        if (refundTooltip != null) refundTooltip.SetActive(!refundTooltip.activeSelf);
    }

    private void ShowToast(string message)
    {
        if (toastGroup == null) return;

        toastText.text = message;
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(ToastRoutine());
    }

    private IEnumerator ToastRoutine()
    {
        toastGroup.alpha = 1f;
        yield return new WaitForSeconds(toastDuration);

        for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
        {
            toastGroup.alpha = 1f - t / 0.3f;
            yield return null;
        }
        toastGroup.alpha = 0f;
        toastRoutine = null;
    }

    /// <summary>
    /// HUD 와 같은 아이콘 규칙으로 아이템 아이콘 조회
    /// </summary>
    private Sprite ResolveIcon(InvenItemData item)
    {
        if (hudPanel == null) hudPanel = FindAnyObjectByType<HUDBottomPanel>(FindObjectsInactive.Include);
        return hudPanel != null ? hudPanel.GetItemIcon(item) : null;
    }
}
