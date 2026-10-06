using UnityEngine;

/// <summary>
/// 일정 시간 유지되는 피해 영역 - 영역 안 플레이어에게 tickInterval 마다 피해
/// hole 이 있으면 해당 영역은 제외 ( 도넛 모양 )
/// </summary>
public class BossDamageZone : MonoBehaviour
{
    private BossBase owner;
    private ZoneShape shape;
    private ZoneShape hole;
    private float damage;
    private float tickInterval;
    private float endTime;
    private float nextHitTime;
    private GameObject vfx;

    public static BossDamageZone Spawn(BossBase owner, ZoneShape shape, ZoneShape hole, float damage, float duration, float tickInterval, GameObject vfxPrefab)
    {
        GameObject go = new GameObject("BossDamageZone");
        BossDamageZone zone = go.AddComponent<BossDamageZone>();

        zone.owner = owner;
        zone.shape = shape.Snapshot();
        zone.hole = hole?.Snapshot();
        zone.damage = damage;
        zone.tickInterval = Mathf.Max(0.05f, tickInterval);
        zone.endTime = Time.time + duration;

        if (vfxPrefab != null)
        {
            zone.vfx = Instantiate(vfxPrefab, zone.shape.Center, Quaternion.Euler(0f, 0f, zone.shape.Angle), go.transform);
        }
        return zone;
    }

    public bool IsAlive => this != null && Time.time < endTime;

    private void Update()
    {
        if (Time.time >= endTime || owner == null || owner.IsDead)
        {
            Release();
            return;
        }

        if (Time.time < nextHitTime || !owner.HasPlayer) return;

        Vector2 playerPos = owner.PlayerPosition;
        if (!shape.Contains(playerPos)) return;
        if (hole != null && hole.Contains(playerPos)) return;

        owner.DamagePlayer(damage);
        nextHitTime = Time.time + tickInterval;
    }

    public void Release()
    {
        Destroy(gameObject);
    }
}
