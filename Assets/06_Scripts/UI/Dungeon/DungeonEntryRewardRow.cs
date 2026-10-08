using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 던전 입장 UI 보상 한 줄 ( 최초 / 반복 )
/// 순서 : EXP -> 골드 -> 아이템 슬롯. 수치는 아이콘 + 텍스트, 아이템은 슬롯
/// </summary>
public class DungeonEntryRewardRow : MonoBehaviour
{
    [SerializeField, Tooltip("흐리게 처리할 보상 내용 ( 획득 완료 표시는 제외 )")]
    private CanvasGroup contentGroup;
    [SerializeField]
    private GameObject expGroup;
    [SerializeField]
    private TextMeshProUGUI expText;
    [SerializeField]
    private GameObject goldGroup;
    [SerializeField]
    private TextMeshProUGUI goldText;
    [SerializeField]
    private RectTransform itemContent;
    [SerializeField]
    private DungeonEntryItemSlot itemSlotTemplate;
    [SerializeField, Tooltip("최초 보상 획득 후 표시")]
    private GameObject claimedTag;
    [SerializeField, Range(0f, 1f)]
    private float claimedAlpha = 0.4f;

    private readonly List<DungeonEntryItemSlot> slots = new List<DungeonEntryItemSlot>();

    private void Awake()
    {
        if (itemSlotTemplate != null) itemSlotTemplate.gameObject.SetActive(false);
    }

    public void Set(DungeonReward reward, bool claimed, Func<InvenItemData, Sprite> iconResolver)
    {
        int exp = reward != null ? Mathf.RoundToInt(reward.Exp) : 0;
        int gold = reward != null ? reward.GetCurrency(CurrencyType.Gold) : 0;

        expGroup.SetActive(exp > 0);
        expText.text = exp.ToString("N0");
        goldGroup.SetActive(gold > 0);
        goldText.text = gold.ToString("N0");

        int index = 0;
        if (reward != null)
        {
            foreach (DungeonItemAmount item in reward.Items)
            {
                if (item.Item == null || item.Count <= 0) continue;

                DungeonEntryItemSlot slot = GetSlot(index++);
                slot.Set(iconResolver?.Invoke(item.Item), $"x{item.Count}", null, Color.white);
            }
        }

        for (int i = index; i < slots.Count; i++)
        {
            slots[i].gameObject.SetActive(false);
        }

        if (contentGroup != null) contentGroup.alpha = claimed ? claimedAlpha : 1f;
        if (claimedTag != null) claimedTag.SetActive(claimed);
    }

    private DungeonEntryItemSlot GetSlot(int index)
    {
        while (slots.Count <= index)
        {
            DungeonEntryItemSlot slot = Instantiate(itemSlotTemplate, itemContent);
            slot.name = $"ItemSlot_{slots.Count + 1}";
            slots.Add(slot);
        }

        slots[index].gameObject.SetActive(true);
        return slots[index];
    }
}
