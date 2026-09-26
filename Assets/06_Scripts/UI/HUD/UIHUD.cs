using UnityEngine;

public class UIHUD : MonoBehaviour
{
    [Header("HUD Panels")]
    [SerializeField] private HUDTopPanel topPanel;
    [SerializeField] private HUDBottomPanel bottomPanel;

    public PlayerContext Context { get; private set; }
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
            topPanel,
            bottomPanel
        };
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
