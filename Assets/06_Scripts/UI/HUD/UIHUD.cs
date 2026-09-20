using UnityEngine;

public class UIHUD : MonoBehaviour
{
    [Header("HUD Panels")]
    [SerializeField] private HUDTopPanel topPanel;
    [SerializeField] private HUDLeftPanel leftPanel;
    [SerializeField] private HUDRightPanel rightPanel;
    [SerializeField] private HUDBottomPanel bottomPanel;

    public PlayerContext Context { get; private set; }
    public bool IsInitialized => Context != null;

    private IHUDPanel[] panels;

    private void Awake()
    {
        panels = new IHUDPanel[]
        {
            topPanel,
            leftPanel,
            rightPanel,
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
