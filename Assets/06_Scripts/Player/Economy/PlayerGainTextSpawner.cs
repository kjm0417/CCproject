using UnityEngine;

/// <summary>
/// 경험치 / 재화 획득 시 플레이어 머리 위에 획득 텍스트 생성
/// </summary>
public class PlayerGainTextSpawner : MonoBehaviour, IPlayerComponent
{
    [SerializeField] private FloatingText floatingTextPrefab;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private float stackSpacing = 0.4f;  // 동시에 여러 개 뜰 때 세로 간격
    [SerializeField] private float stackResetTime = 0.3f; // 이 시간 안에 다시 뜨면 위로 쌓음

    [Header("색상")]
    [SerializeField] private Color expColor = new Color(0.55f, 0.85f, 1f);
    [SerializeField] private Color goldColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color gemColor = new Color(0.8f, 0.5f, 1f);

    private PlayerContext context;
    private int stackIndex;
    private float lastSpawnTime;

    public void Initialize(PlayerContext context)
    {
        this.context = context;
    }

    private void OnEnable()
    {
        // Initialize가 OnEnable보다 늦을 수 있어 Start에서도 구독 보장
        Subscribe();
    }

    private void Start()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private bool subscribed;

    private void Subscribe()
    {
        if (subscribed || context == null) return;

        if (context.Progression != null) context.Progression.OnExpGained += HandleExpGained;
        if (context.Wallet != null) context.Wallet.OnGained += HandleCurrencyGained;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || context == null) return;

        if (context.Progression != null) context.Progression.OnExpGained -= HandleExpGained;
        if (context.Wallet != null) context.Wallet.OnGained -= HandleCurrencyGained;
        subscribed = false;
    }

    private void HandleExpGained(int amount)
    {
        Spawn($"+{amount} EXP", expColor);
    }

    private void HandleCurrencyGained(CurrencyType type, int amount)
    {
        switch (type)
        {
            case CurrencyType.Gold: Spawn($"+{amount} 골드", goldColor); break;
            case CurrencyType.Gem:  Spawn($"+{amount} 보석", gemColor); break;
        }
    }

    private void Spawn(string text, Color color)
    {
        if (floatingTextPrefab == null) return;

        // 짧은 시간 안에 연달아 획득하면 겹치지 않게 위로 쌓기
        stackIndex = Time.time - lastSpawnTime < stackResetTime ? stackIndex + 1 : 0;
        lastSpawnTime = Time.time;

        Vector3 pos = transform.position + spawnOffset + Vector3.up * (stackSpacing * stackIndex);
        // 플레이어 자식으로 생성 -> 이동 중에도 머리 위를 따라다님
        FloatingText ft = ObjectPoolManager.Spawn(floatingTextPrefab, pos, Quaternion.identity, transform);
        ft.Play(text, color);
    }
}
