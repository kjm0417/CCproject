using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 용 보스 기믹
/// 1. 좌 / 우 구역에 일정 시간 머무름 -> 해당 구역의 탁수 지점에 씨앗 생성
/// 2. 씨앗 growDuration 후 다 자람
/// 3. 양쪽 모두 자라면 맵 중앙에 구체 드랍 -> 플레이어 습득 시 보스 스턴
/// 4. 스턴 종료 시 씨앗 / 구체 제거 후 처음부터
/// 씨앗 종류 : 하단층 씨앗 타일 ( DungeonSeedTile ) 에 마지막으로 심은 씨앗
/// </summary>
public class DragonSeedGimmick : BossStunGimmick
{
    [Serializable]
    private class SeedVisual
    {
        public InvenItemData seed;
        [Tooltip("성장 단계 순서 ( 0 : 씨앗 -> 마지막 : 다 자람 )")]
        public List<Sprite> stages = new List<Sprite>();
        [Tooltip("씨앗 -> 다 자람 총 시간 ( 단계는 균등 분할 )")]
        public float growDuration = 5f;
    }

    [Header("씨앗")]
    [SerializeField, Tooltip("zones 와 같은 순서 ( 0 : Zone_Left -> Taksu_Left )")]
    private List<Transform> plantPoints = new List<Transform>();
    [SerializeField, Tooltip("씨앗 종류별 스프라이트")]
    private List<SeedVisual> seedVisuals = new List<SeedVisual>();
    [SerializeField] private Vector3 plantScale = Vector3.one;
    [SerializeField] private string plantSortingLayer = "Gimik";
    [SerializeField] private int plantSortingOrder = 10;

    [Header("기본 씨앗 ( 심은 씨앗 없음 / 목록에 없음 - 보스층 바로 테스트 등 )")]
    [SerializeField, Tooltip("비우면 seedVisuals 첫 번째 스프라이트 사용")]
    private List<Sprite> defaultStages = new List<Sprite>();
    [SerializeField, FormerlySerializedAs("growDuration")]
    private float defaultGrowDuration = 5f;

    [Header("구체")]
    [SerializeField, Tooltip("BossStunOrb + Collider2D 포함 프리팹 ( 비우면 orbSprite 로 자동 생성 )")]
    private BossStunOrb orbPrefab;
    [SerializeField] private Sprite orbSprite;
    [SerializeField] private Transform orbSpawnPoint;

    private SpriteRenderer[] plants;
    private bool[] grown;
    private Coroutine[] growRoutines;
    private BossStunOrb orb;
    private InvenItemData lastPlantedSeed;
    private readonly List<DungeonSeedTile> seedTiles = new List<DungeonSeedTile>();

    protected override void Start()
    {
        base.Start();

        plants = new SpriteRenderer[zones.Count];
        grown = new bool[zones.Count];
        growRoutines = new Coroutine[zones.Count];

        // 하단층 씨앗 타일 심기 기록 ( 같은 씬에 층이 모두 있음 )
        foreach (DungeonSeedTile tile in FindObjectsByType<DungeonSeedTile>(FindObjectsInactive.Include))
        {
            tile.OnPlanted += HandleSeedPlanted;
            seedTiles.Add(tile);
            if (tile.IsPlanted) lastPlantedSeed = tile.RequiredSeed;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        foreach (DungeonSeedTile tile in seedTiles)
        {
            if (tile != null) tile.OnPlanted -= HandleSeedPlanted;
        }
    }

    private void HandleSeedPlanted(DungeonSeedTile tile)
    {
        lastPlantedSeed = tile.RequiredSeed;
    }

    protected override void OnZoneActivated(BossActivationZone zone, int index)
    {
        if (index >= plantPoints.Count || plantPoints[index] == null)
        {
            Debug.LogError($"[DragonSeedGimmick] plantPoints[{index}] 설정 필요");
            return;
        }

        // 심은 씨앗 있음 -> 해당 씨앗 단계 / 시간
        // 없음 -> 기본 씨앗 ( 단계 비었으면 seedVisuals 첫 번째 스프라이트 ) + defaultGrowDuration
        SeedVisual visual = FindVisual(lastPlantedSeed);
        List<Sprite> stages;
        float duration;

        if (visual != null)
        {
            stages = visual.stages;
            duration = visual.growDuration;
        }
        else
        {
            stages = defaultStages.Count == 0 && seedVisuals.Count > 0 ? seedVisuals[0].stages : defaultStages;
            duration = defaultGrowDuration;
            Debug.LogWarning($"[DragonSeedGimmick] 심은 씨앗 없음 - 기본 씨앗 사용 ( {duration}초 )");
        }

        plants[index] = CreatePlant(plantPoints[index], stages.Count > 0 ? stages[0] : null);
        growRoutines[index] = StartCoroutine(GrowRoutine(index, stages, duration));
    }

    #region [이전] 씨앗 / 다 자람 2단계
    // SeedVisual visual = FindVisual(lastPlantedSeed);
    // plants[index] = CreatePlant(plantPoints[index], visual != null ? visual.seedSprite : defaultSeedSprite);
    // growRoutines[index] = StartCoroutine(GrowRoutine(index, visual != null ? visual.grownSprite : defaultGrownSprite));
    #endregion

    // 모든 구역 활성화만으로는 스턴 X - 씨앗이 다 자란 뒤 구체 습득 시 스턴
    protected override void OnAllZonesActivated() { }

    private IEnumerator GrowRoutine(int index, List<Sprite> stages, float duration)
    {
        // 단계 0 은 생성 시 표시, 나머지 단계를 duration 동안 균등 간격으로 전환
        int steps = Mathf.Max(1, stages.Count - 1);
        float interval = duration / steps;

        for (int stage = 1; stage <= steps; stage++)
        {
            yield return new WaitForSeconds(interval);
            if (plants[index] != null && stage < stages.Count) plants[index].sprite = stages[stage];
        }

        grown[index] = true;
        growRoutines[index] = null;

        foreach (bool g in grown)
        {
            if (!g) yield break;
        }
        SpawnOrb();
    }

    private void SpawnOrb()
    {
        if (orb != null) return;

        Vector3 pos = orbSpawnPoint != null ? orbSpawnPoint.position : transform.position;
        if (orbPrefab != null)
        {
            orb = ObjectPoolManager.Spawn(orbPrefab, pos, Quaternion.identity);
            #region [이전] Instantiate / Destroy
            // orb = Instantiate(orbPrefab, pos, Quaternion.identity);
            #endregion
        }
        else
        {
            GameObject go = new GameObject("BossStunOrb");
            go.transform.position = pos;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = orbSprite;
            sr.sortingLayerName = plantSortingLayer;
            sr.sortingOrder = plantSortingOrder + 1;
            go.AddComponent<CircleCollider2D>().radius = 0.5f;
            orb = go.AddComponent<BossStunOrb>();
        }

        orb.OnPicked += ApplyStun;
    }

    protected override void ResetAll()
    {
        base.ResetAll();

        for (int i = 0; i < zones.Count; i++)
        {
            if (growRoutines[i] != null) StopCoroutine(growRoutines[i]);
            growRoutines[i] = null;
            grown[i] = false;
            if (plants[i] != null) Destroy(plants[i].gameObject);
            plants[i] = null;
        }

        if (orb != null) ObjectPoolManager.Despawn(orb.gameObject); //풀 오브젝트가 아니면 ( 런타임 생성 ) Destroy
        #region [이전] Instantiate / Destroy
        // if (orb != null) Destroy(orb.gameObject);
        #endregion
        orb = null;
    }

    private SpriteRenderer CreatePlant(Transform point, Sprite sprite)
    {
        GameObject go = new GameObject("BossSeed");
        go.transform.SetParent(transform, false);
        go.transform.position = point.position;
        go.transform.localScale = plantScale;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = plantSortingLayer;
        sr.sortingOrder = plantSortingOrder;
        return sr;
    }

    private SeedVisual FindVisual(InvenItemData seed)
    {
        if (seed == null) return null;

        // 같은 SO 가 아니어도 ID 가 같으면 같은 씨앗 ( DungeonSeedTile 과 동일 기준 )
        foreach (SeedVisual visual in seedVisuals)
        {
            if (visual.seed != null && visual.seed.ItemID == seed.ItemID) return visual;
        }
        return null;
    }

    #region [이전] 구역 활성화만 하던 구조
    // protected override bool CanZoneProgress(BossActivationZone zone)
    // {
    //     return true;
    // }
    #endregion
}
