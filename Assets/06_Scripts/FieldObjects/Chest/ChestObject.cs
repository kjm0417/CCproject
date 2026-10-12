using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 상자 오브젝트. 상호작용 시 열림 Sprite + 보관 UI 열기 + 플레이어 이동 정지.
/// 닫기는 ChestUI의 닫기 버튼으로만 하며, 닫으면 기본 Sprite 복구 + 이동 정지 해제.
/// 보관 공간은 '아이템 종류 수'로 정해진다. 한 종류는 한 칸을 차지하고, 같은 종류는 개수 제한 없이 그 칸에 쌓인다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ChestObject : MonoBehaviour, IInteractable
{
    [Header("Sprite")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;
    [SerializeField]
    private Sprite closedSprite;
    [SerializeField]
    private Sprite openedSprite;

    [Header("보관")]
    [Tooltip("넣을 수 있는 아이템 종류 수 (= 칸 수). 같은 종류는 개수 제한 없이 한 칸에 쌓임")]
    [FormerlySerializedAs("maxSlots")]
    [SerializeField, Min(1)]
    private int maxItemTypes = 20;

    [Header("저장")]
    [Tooltip("저장/불러오기용 고유 ID. 비우면 오브젝트 이름 + 위치로 구분 (상자가 여러 개면 지정 권장)")]
    [SerializeField]
    private string chestId;

    [Header("UI (상자 1 : ChestUI 1)")]
    [Tooltip("이 상자 전용 ChestUI. 상자마다 다른 ChestUI를 연결")]
    [SerializeField]
    private ChestUI chestUI;

    /// <summary>
    /// 보관 아이템 변경 시 방송 (상자 UI 갱신용)
    /// </summary>
    public event Action OnStorageChanged;

    private readonly List<InventorySlot> slots = new List<InventorySlot>();
    private PlayerContext openedBy;

    /// <summary>
    /// 상자에 보관 중인 슬롯 목록 (읽기 전용)
    /// </summary>
    public IReadOnlyList<InventorySlot> Slots => slots;

    /// <summary>
    /// 현재 열려 있는지 여부
    /// </summary>
    public bool IsOpen { get; private set; }

    /// <summary>
    /// 넣을 수 있는 아이템 종류 수 (= 칸 수)
    /// </summary>
    public int Capacity => maxItemTypes;

    /// <summary>
    /// 저장 데이터에서 이 상자를 찾는 키
    /// </summary>
    public string SaveKey => !string.IsNullOrEmpty(chestId)
        ? chestId
        : $"{name}_{transform.position.x:F1}_{transform.position.y:F1}";

    /// <summary>
    /// SpriteRenderer 자동 할당, 닫힘 Sprite 기본값 설정
    /// </summary>
    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (closedSprite == null && spriteRenderer != null) closedSprite = spriteRenderer.sprite;
        ApplySprite();
    }

    /// <summary>
    /// 닫혀 있고 플레이어 정보가 있으면 상호작용 가능 (열린 상태에선 닫기 버튼으로만 닫음)
    /// </summary>
    public bool CanInteract(InteractionContext context)
    {
        return !IsOpen && context.Player != null;
    }

    /// <summary>
    /// 상호작용 시 상자 열기
    /// </summary>
    public void Interact(InteractionContext context)
    {
        Open(context.Player);
    }

    /// <summary>
    /// 상자를 열고 열림 Sprite로 변경, 보관 UI를 열고 플레이어 이동을 정지한다.
    /// </summary>
    public void Open(PlayerContext player)
    {
        if (IsOpen || player == null) return;

        #region 이전 코드 - 씬의 ChestUI 하나를 모든 상자가 공유
        //if (chestUI == null) chestUI = FindAnyObjectByType<ChestUI>(FindObjectsInactive.Include);
        //if (chestUI == null)
        //{
        //    Debug.LogWarning($"[{name}] ChestUI를 찾을 수 없습니다.");
        //    return;
        //}
        #endregion

        if (chestUI == null)
        {
            Debug.LogWarning($"[{name}] 전용 ChestUI가 없습니다. chestUI를 연결하세요.");
            return;
        }

        IsOpen = true;
        openedBy = player;
        ApplySprite();
        if (player.Movement != null) player.Movement.SetInputBlocked(true);
        chestUI.Open(this, player.Inventory);
    }

    /// <summary>
    /// 상자를 닫고 기본 Sprite로 복구, 보관 UI를 닫고 플레이어 이동 정지를 해제한다.
    /// </summary>
    public void Close()
    {
        if (!IsOpen) return;

        IsOpen = false;
        if (openedBy != null && openedBy.Movement != null) openedBy.Movement.SetInputBlocked(false);
        openedBy = null;
        ApplySprite();
        if (chestUI != null) chestUI.Close(this);
    }

    /// <summary>
    /// IsOpen 상태에 맞는 Sprite 적용
    /// </summary>
    private void ApplySprite()
    {
        if (spriteRenderer == null) return;

        Sprite target = IsOpen ? openedSprite : closedSprite;
        if (target != null) spriteRenderer.sprite = target;
    }

    /// <summary>
    /// 상자에 아이템 넣기. 넣지 못하고 남은 개수 반환 (0이면 전부 들어감).
    /// </summary>
    public int Add(InvenItemData item, int count)
    {
        if (item == null || count <= 0) return count;

        // 1) 이미 있는 종류면 그 칸에 전부 쌓는다 (개수 제한 없음).
        InventorySlot slot = FindSlot(item);
        if (slot != null)
        {
            slot.Count += count;
            OnStorageChanged?.Invoke();
            return 0;
        }

        // 2) 새 종류는 빈 칸이 있을 때만 들어간다.
        if (slots.Count >= maxItemTypes) return count;

        slots.Add(new InventorySlot(item, count));
        OnStorageChanged?.Invoke();
        return 0;
    }

    /// <summary>
    /// 상자에서 아이템 빼기. 실제 제거한 개수 반환.
    /// </summary>
    public int Remove(InvenItemData item, int count)
    {
        if (item == null || count <= 0) return 0;
        int removed = 0;

        for (int i = slots.Count - 1; i >= 0 && removed < count; i--)
        {
            if (slots[i].Item != item) continue;

            int take = Mathf.Min(slots[i].Count, count - removed);
            slots[i].Count -= take;
            removed += take;
            if (slots[i].Count <= 0) slots.RemoveAt(i);
        }

        if (removed > 0) OnStorageChanged?.Invoke();
        return removed;
    }

    /// <summary>
    /// 이 아이템을 넣을 수 있는지 (이미 있는 종류거나 빈 칸이 남아 있으면 true)
    /// </summary>
    public bool CanAccept(InvenItemData item)
    {
        return item != null && (FindSlot(item) != null || slots.Count < maxItemTypes);
    }

    /// <summary>
    /// 해당 아이템 종류의 칸 반환 (없으면 null)
    /// </summary>
    private InventorySlot FindSlot(InvenItemData item)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].Item == item) return slots[i];
        }
        return null;
    }

    /// <summary>
    /// 상자에 보관된 해당 아이템 총 개수
    /// </summary>
    public int GetCount(InvenItemData item)
    {
        if (item == null) return 0;
        int total = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].Item == item) total += slots[i].Count;
        }
        return total;
    }

    #region 저장 및 불러오기
    /// <summary>
    /// 보관 아이템 저장 데이터 생성
    /// </summary>
    public List<InventoryItemSaveData> CreateSaveData()
    {
        List<InventoryItemSaveData> data = new List<InventoryItemSaveData>();
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].Item == null || slots[i].Count <= 0) continue;
            data.Add(InventoryItemSaveData.FromSlot(slots[i]));
        }
        return data;
    }

    /// <summary>
    /// 저장된 보관 아이템 복원. resolveItem으로 저장 데이터 -> 아이템 SO 변환
    /// </summary>
    public void Load(List<InventoryItemSaveData> savedItems, Func<InventoryItemSaveData, InvenItemData> resolveItem)
    {
        slots.Clear();

        if (savedItems != null && resolveItem != null)
        {
            foreach (InventoryItemSaveData savedItem in savedItems)
            {
                if (savedItem == null || savedItem.Count <= 0) continue;

                InvenItemData item = resolveItem(savedItem);
                if (item == null) continue;

                InventorySlot slot = FindSlot(item);
                if (slot != null) slot.Count += savedItem.Count;
                else slots.Add(new InventorySlot(item, savedItem.Count));
            }
        }

        OnStorageChanged?.Invoke();
    }
    #endregion

    /// <summary>
    /// 비활성화 시 열려 있으면 닫아서 UI가 남지 않게 한다.
    /// </summary>
    private void OnDisable()
    {
        Close();
    }
}
