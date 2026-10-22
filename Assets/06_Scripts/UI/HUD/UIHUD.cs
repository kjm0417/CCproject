using UnityEngine;

public class UIHUD : MonoBehaviour
{
    [Header("HUD Panels")]
    [SerializeField] private HUDTopPanel topPanel;
    [SerializeField] private HUDBottomPanel bottomPanel;
    [SerializeField, Tooltip("Off: Top panel hidden ( e.g. dungeon scene )")] private bool showTopPanel = true;

    public PlayerContext Context { get; private set; }

    // 키보드 단축키에서 사용 (상단 패널을 숨긴 씬에서는 null)
    public HUDTopPanel TopPanel => showTopPanel ? topPanel : null;
    public HUDBottomPanel BottomPanel => bottomPanel;
    public bool IsInitialized => Context != null;

    private IHUDPanel[] panels;

    private void Awake()
    {
        ResolvePanels();
        BuildPanelList();
    }

    private void Start()
    {
        if (IsInitialized) return;

        PlayerContext playerContext = FindFirstObjectByType<PlayerContext>();
        if (playerContext == null)
        {
            Debug.LogError("UIHUD: PlayerContext was not found in the scene.", this);
            return;
        }

        Initialize(playerContext);
        Show();
    }

    private void ResolvePanels()
    {
        if (topPanel == null)
        {
            topPanel = GetComponentInChildren<HUDTopPanel>(true);
        }

        if (bottomPanel == null)
        {
            bottomPanel = GetComponentInChildren<HUDBottomPanel>(true);
        }
    }

    private void BuildPanelList()
    {
        panels = new IHUDPanel[]
        {
            showTopPanel ? topPanel : null,
            bottomPanel
        };

        if (!showTopPanel && topPanel != null)
        {
            topPanel.gameObject.SetActive(false);
        }
    }

    public void Initialize(PlayerContext context)
    {
        if (context == null)
        {
            Debug.LogError("UIHUD: PlayerContext is null.", this);
            return;
        }

        ResolvePanels();
        BuildPanelList();
        Context = context;

        foreach (IHUDPanel panel in panels)
        {
            panel?.Initialize(context);
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);

        foreach (IHUDPanel panel in panels)
        {
            panel?.Show();
        }
    }

    public void Hide()
    {
        foreach (IHUDPanel panel in panels)
        {
            panel?.Hide();
        }

        gameObject.SetActive(false);
    }

    public void Release()
    {
        foreach (IHUDPanel panel in panels)
        {
            panel?.Release();
        }

        Context = null;
    }

    private void OnDestroy()
    {
        Release();
    }
}
