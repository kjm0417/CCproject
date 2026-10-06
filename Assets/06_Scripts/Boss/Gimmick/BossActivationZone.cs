using System;
using UnityEngine;

/// <summary>
/// 플레이어가 requiredStayTime 동안 머무르면 활성화되는 구역 ( 보스 기믹 공용 )
/// </summary>
public class BossActivationZone : MonoBehaviour
{
    [SerializeField] private ZoneShape shape = new ZoneShape();
    [SerializeField] private float requiredStayTime = 6f;
    [SerializeField, Tooltip("구역 밖에 있을 때 초당 감소량 ( 0 이면 유지 )")]
    private float decayPerSecond;
    [SerializeField] private TelegraphStyle outlineStyle = new TelegraphStyle();

    private BossBase boss;
    private BossTelegraph outline;
    private float stayTime;
    private Func<bool> canProgress;

    public event Action<float> OnProgressChanged; // 0 ~ 1
    public event Action OnActivated;

    public bool IsActivated { get; private set; }
    public float Progress => requiredStayTime > 0f ? Mathf.Clamp01(stayTime / requiredStayTime) : 1f;

    /// <param name="canProgress">추가 활성 조건 ( null 이면 항상 허용 )</param>
    public void Initialize(BossBase owner, Func<bool> canProgress = null)
    {
        boss = owner;
        this.canProgress = canProgress;

        if (outline == null) outline = BossTelegraph.Create(shape, outlineStyle, transform);
        outline.SetVisible(true);
    }

    public void ResetZone()
    {
        IsActivated = false;
        stayTime = 0f;
        OnProgressChanged?.Invoke(0f);
        if (outline != null) outline.SetVisible(true);
    }

    private void Update()
    {
        if (boss == null || IsActivated || boss.IsDead || boss.IsStunned) return;

        bool inside = boss.HasPlayer && shape.Contains(boss.PlayerPosition) && (canProgress == null || canProgress());
        float before = stayTime;

        if (inside) stayTime += Time.deltaTime;
        else stayTime = Mathf.Max(0f, stayTime - decayPerSecond * Time.deltaTime);

        if (!Mathf.Approximately(before, stayTime)) OnProgressChanged?.Invoke(Progress);

        if (stayTime >= requiredStayTime)
        {
            IsActivated = true;
            if (outline != null) outline.SetVisible(false);
            OnActivated?.Invoke();
        }
    }

    private void OnDrawGizmosSelected()
    {
        shape.DrawGizmo(Color.yellow);
    }
}
