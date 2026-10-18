using UnityEngine;

/// <summary>
/// 던전 입구 - 플레이어 상호작용 버튼으로 던전 입장 UI 열기
/// 입장 UI 의 입장 버튼 -> 입장 아이템 소모 + 저장 -> 던전 씬 이동
/// </summary>
public class DungeonEntrance : InteractiveFixedStructureObject
{
    [SerializeField]
    private DungeonData dungeonData;
    [SerializeField, Tooltip("비우면 씬에서 자동 탐색")]
    private DungeonEntryUI entryUI;
    [SerializeField, Tooltip("던전 클리어 후 입구 스프라이트 ( 비우면 변경 안 함 )")]
    private Sprite clearSprite;

    private const string DefaultSceneName = "KJ_DungeonScene";

    private void Start()
    {
        RefreshSprite();
    }

    /// <summary>
    /// 클리어 기록 ( DungeonSave ) 있으면 클리어 스프라이트로 교체 - 영지 씬 로드마다 저장 기준으로 판정
    /// </summary>
    public void RefreshSprite()
    {
        if (dungeonData == null || clearSprite == null) return;
        if (!DungeonSession.GetRecord(dungeonData).HasCleared) return;

        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.sprite = clearSprite;
    }

    public override bool CanInteract(InteractionContext context)
    {
        if (dungeonData == null || context.Player == null || SceneFader.IsTransitioning) return false;

        DungeonEntryUI ui = GetEntryUI();
        return ui != null && !ui.IsOpen;
    }

    public override void Interact(InteractionContext context)
    {
        if (!CanInteract(context)) return;

        PlayerContext player = context.Player;
        GetEntryUI().Open(dungeonData, player, transform, () => EnterDungeon(player));
    }

    private DungeonEntryUI GetEntryUI()
    {
        if (entryUI == null) entryUI = FindAnyObjectByType<DungeonEntryUI>(FindObjectsInactive.Include);
        return entryUI;
    }

    /// <summary>
    /// 입장 아이템 소모 + 플레이어 / 영지 저장 후 던전 씬 이동
    /// </summary>
    private void EnterDungeon(PlayerContext player)
    {
        if (SceneFader.IsTransitioning || player == null) return;

        if (!DungeonSession.TryEnter(dungeonData, player.Inventory))
        {
            Debug.Log($"[Dungeon] 입장 불가 - 입장 아이템 부족 : {dungeonData.DungeonName}");
            return;
        }

        //플레이어 데이터 저장
        SaveManager.Instance.Save(PlayerSaveData.Save(player));

        //해금된 영토 및 생성된 자원 저장
        SaveManager.Instance.SaveTerritory(TerritoryManager.Instance.Save());

        string sceneName = !string.IsNullOrEmpty(dungeonData.SceneName) ? dungeonData.SceneName : DefaultSceneName;
        SceneFader.LoadScene(sceneName);
    }
}
