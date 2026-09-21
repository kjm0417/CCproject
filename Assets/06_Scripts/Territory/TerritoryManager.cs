using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 런타임에 따라 데이터가 달라지는 런타임용 아일랜드 데이터
/// </summary>
public class TerritoryRunTimeData
{
    public TerritoryZone PlayerZone { get; private set; }
    public TerrirotyDirection TerrirotyDirection { get; private set; }

    public TerritoryRunTimeData()
    {
    }

    public void SaveRunTimeData(TerritoryZone PlayerZone, TerrirotyDirection TerrirotyDirection)
    {
        this.PlayerZone = PlayerZone;
        this.TerrirotyDirection = TerrirotyDirection;
    }

    public void InitRunTimeData()
    {
        PlayerZone = null;
        TerrirotyDirection = TerrirotyDirection.None;
    }
}

/// <summary>
/// 게임 내의 구역 관리 시스템
/// </summary>
public class TerritoryManager : MonoBehaviour
{
    private static TerritoryManager instance;
    public static TerritoryManager Instance
    {
        get
        {
            return instance;
        }
    }

    private ICurrencyProvider currencyProvider;

    private Dictionary<int, TerritoryZone> territoryZonesDic = new Dictionary<int, TerritoryZone>();

    private TerritoryRunTimeData territoryRunTimeData;
    public TerritoryRunTimeData TerritoryRunTimeData => territoryRunTimeData;

    private Dictionary<int, Dictionary<ResourceSpawnEntry, float>> savedRespawningResources = new Dictionary<int, Dictionary<ResourceSpawnEntry, float>>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        foreach (TerritoryZone zone in FindObjectsOfType<TerritoryZone>())
        {
            territoryZonesDic.Add(zone.TerritoryZoneData.ZoneId, zone);
        }

        currencyProvider = FindAnyObjectByType<CurrencyManager>();

        territoryRunTimeData = new TerritoryRunTimeData();
    }

    #region 영토 저장 및 불러오기
    public TerritorySaveData Save()
    {
        // 재생성 예정 자원 정보 저장
        savedRespawningResources.Clear();
        foreach (var zone in territoryZonesDic.Values)
        {
            ResourceSpawner spawner = zone.GetComponent<ResourceSpawner>();
            if (spawner != null)
            {
                var allRespawning = spawner.GetAllRespawningResources();
                if (allRespawning.Count > 0)
                {
                    var respawningDict = new Dictionary<ResourceSpawnEntry, float>();
                    foreach (var (entry, remainingTime) in allRespawning)
                    {
                        respawningDict[entry] = remainingTime;
                    }
                    savedRespawningResources[zone.TerritoryZoneData.ZoneId] = respawningDict;
                }
            }
        }

        // 재생성 대기 중인 코루틴 모두 중단
        foreach (var zone in territoryZonesDic.Values)
        {
            ResourceSpawner spawner = zone.GetComponent<ResourceSpawner>();
            if (spawner != null)
            {
                spawner.StopAllCoroutines();
            }
        }

        TerritorySaveData territorySaveData = new();
        territorySaveData.UnlockedZoneIds = new List<int>();
        territorySaveData.fieldObjects = new List<FieldObjectStateData>();
        territorySaveData.respawningResources = new List<RespawningResourceData>();

        foreach(var zone in territoryZonesDic.Values)
        {
           

            if (!zone.IsLocked)
            {
                Debug.Log("해금된 zone" + zone.gameObject.name);
                territorySaveData.UnlockedZoneIds.Add(zone.TerritoryZoneData.ZoneId);
            }

            ResourceSpawner spawner = zone.GetComponent<ResourceSpawner>();
            if (spawner != null)
            {
                var spawnedObjects = spawner.GetSpawnedObjects();
                int index = 0;
                foreach(var obj in spawnedObjects)
                {
                    DamageableFieldObject damageable = obj.GetComponent<DamageableFieldObject>();
                    if (damageable == null) continue;

                    FieldObjectStateData objState = new FieldObjectStateData();
                    objState.ZoneId = zone.TerritoryZoneData.ZoneId;
                    objState.ResourceEntryIndex = index;
                    objState.CurrentHp = damageable.CurrentHp;
                    objState.IsDepleted = damageable.IsDepleted;
                    objState.Position = obj.transform.position;
                    objState.ResourcePrefabName = damageable.ResourceEntry.ResourcePrefab.name;

                    NaturalComplexObject complexObj = obj.GetComponent<NaturalComplexObject>();
                    if (complexObj != null)
                    {
                        objState.GrowthStage = complexObj.GrowthStage;
                    }

                    Debug.Log("자원 생성물 저장" + obj.gameObject.name);

                    territorySaveData.fieldObjects.Add(objState);
                    index++;
                }
            }
        }

        // 재생성 자원 정보 추가 (모든 자원)
        foreach (var zone in territoryZonesDic.Values)
        {
            ResourceSpawner spawner = zone.GetComponent<ResourceSpawner>();
            if (spawner != null)
            {
                var allRespawning = spawner.GetAllRespawningResources();
                foreach (var (entry, remainingTime) in allRespawning)
                {
                    territorySaveData.respawningResources.Add(new RespawningResourceData
                    {
                        ZoneId = zone.TerritoryZoneData.ZoneId,
                        ResourcePrefabName = entry.ResourcePrefab.name,
                        RemainingTime = remainingTime
                    });
                }
            }
        }

        return territorySaveData;
    }

    public void Load(TerritorySaveData data)
    {
        if (data == null) return;

        Debug.Log($"Territory Load 시작. Zone 개수: {territoryZonesDic.Count}");
        Debug.Log($"UnlockedZoneIds 개수: {data.UnlockedZoneIds.Count}");

        // 기존 자원 제거
        foreach (var zone in territoryZonesDic.Values)
        {
            ResourceSpawner spawner = zone.GetComponent<ResourceSpawner>();
            if (spawner != null)
            {
                var spawnedObjects = spawner.GetSpawnedObjects();
                foreach (var obj in spawnedObjects)
                {
                    Destroy(obj);
                }
                // occupiedCells 초기화
                spawner.ClearSpawnedResources();
            }
        }

        // 구조물 생성
        foreach (var zone in territoryZonesDic.Values)
        {
            if (zone.TerritoryZoneData.StructSlots.Count > 0)
            {
                foreach (var structSlot in zone.TerritoryZoneData.StructSlots)
                {
                    if (structSlot.StructPrefab != null)
                        Instantiate(structSlot.StructPrefab, structSlot.StructSpawnPos, Quaternion.identity);
                }
            }
        }

        // NPC 생성
        foreach (var zone in territoryZonesDic.Values)
        {
            if (zone.TerritoryZoneData.NpcSlots.Count > 0)
            {
                foreach (var npcSlot in zone.TerritoryZoneData.NpcSlots)
                {
                    if (npcSlot.NpcPrefab != null)
                        Instantiate(npcSlot.NpcPrefab, npcSlot.NpcSpawnPos, Quaternion.identity);
                }
            }
        }

        foreach(var zone in territoryZonesDic.Values)
        {
            if (zone.TerritoryZoneData.ZoneId == 1) continue;

            Debug.Log($"Zone {zone.TerritoryZoneData.ZoneId} 처리. IsLocked: {zone.IsLocked}");
            bool shouldBeUnlocked = data.UnlockedZoneIds.Contains(zone.TerritoryZoneData.ZoneId);
            Debug.Log($"  shouldBeUnlocked: {shouldBeUnlocked}");

            if (shouldBeUnlocked && zone.IsLocked)
            {
                zone.UnLockZone(false);  // SpawnInitial() 스킵
            }
        }

        foreach(var objState in data.fieldObjects)
        {
            TerritoryZone zone = GetZone(objState.ZoneId);
            if (zone == null) continue;

            ResourceSpawner spawner = zone.GetComponent<ResourceSpawner>();
            if (spawner == null) continue;

            // 저장된 자원 정보로 ResourceSpawnEntry 찾기
            ResourceSpawnEntry targetEntry = null;
            foreach (var entry in zone.TerritoryZoneData.ResourceSpawnTable)
            {
                if (entry.ResourcePrefab.name == objState.ResourcePrefabName)
                {
                    targetEntry = entry;
                    break;
                }
            }

            if (targetEntry == null) continue;

            // 특정 위치에 자원 생성
            GameObject resourceObj = spawner.SpawnResourceAtPosition(targetEntry, objState.Position);
            DamageableFieldObject damageable = resourceObj.GetComponent<DamageableFieldObject>();

            if (damageable != null)
            {
                damageable.Load(objState.CurrentHp, objState.IsDepleted);

                NaturalComplexObject complexObj = resourceObj.GetComponent<NaturalComplexObject>();
                if (complexObj != null && objState.GrowthStage > 0)
                {
                    complexObj.GrowthStage = objState.GrowthStage;
                }
            }
        }

        // 재생성 코루틴 재시작 (저장된 데이터에서)
        foreach (var zone in territoryZonesDic.Values)
        {
            ResourceSpawner spawner = zone.GetComponent<ResourceSpawner>();
            if (spawner == null) continue;

            var zoneRespawningList = data.respawningResources.FindAll(r => r.ZoneId == zone.TerritoryZoneData.ZoneId);
            if (zoneRespawningList.Count == 0) continue;

            var respawningData = new List<(ResourceSpawnEntry, float)>();

            foreach (var respawningInfo in zoneRespawningList)
            {
                ResourceSpawnEntry targetEntry = null;
                foreach (var entry in zone.TerritoryZoneData.ResourceSpawnTable)
                {
                    if (entry.ResourcePrefab.name == respawningInfo.ResourcePrefabName)
                    {
                        targetEntry = entry;
                        break;
                    }
                }

                if (targetEntry != null)
                {
                    respawningData.Add((targetEntry, respawningInfo.RemainingTime));
                }
            }

            if (respawningData.Count > 0)
            {
                spawner.RestartRespawnCoroutines(respawningData);
            }
        }
    }
    #endregion
    public bool TryTerritoryUnlock(TerritoryZone territoryZone)
    {
        if(!territoryZone.IsLocked)
        {
            return true;
        }

        if (currencyProvider.TrySpend(territoryZone.TerritoryZoneData.RequireGold))
        {
            territoryZone.UnLockZone();
            return true;
        }

        return false;
    }

    public TerritoryZone GetZone(int id)
    {
        if(territoryZonesDic.TryGetValue(id, out TerritoryZone zone))
        {
            return zone;
        }
        Debug.LogWarning("구역을 찾을 수 없음!");
        return null;
    }

    public void SaveRuntimeTerritoryData(TerritoryZone currentZone, TerrirotyDirection direction)
        => territoryRunTimeData.SaveRunTimeData(currentZone, direction);

    public TerritoryZone GetAdjacentZone(TerritoryZone currentZone, TerrirotyDirection direction)
    {
        int adjacentZoneId = -1;

        switch (direction)
        {
            case TerrirotyDirection.Up:
                adjacentZoneId = currentZone.TerritoryZoneData.AdjacentZoneId_Up;
                break;
            case TerrirotyDirection.Down:
                adjacentZoneId = currentZone.TerritoryZoneData.AdjacentZoneId_Down;
                break;
            case TerrirotyDirection.Left:
                adjacentZoneId = currentZone.TerritoryZoneData.AdjacentZoneId_Left;
                break;
            case TerrirotyDirection.Right:
                adjacentZoneId = currentZone.TerritoryZoneData.AdjacentZoneId_Right;
                break;
        }

        if (adjacentZoneId == -1)
        {
            return null;
        }

        return GetZone(adjacentZoneId);
    }

    public Dictionary<TerrirotyDirection, TerritoryZone> GetAllAdjacentZones(TerritoryZone currentZone)
    {
        Dictionary<TerrirotyDirection, TerritoryZone> adjacentZones = new Dictionary<TerrirotyDirection, TerritoryZone>();

        TerritoryZone upZone = GetAdjacentZone(currentZone, TerrirotyDirection.Up);
        TerritoryZone downZone = GetAdjacentZone(currentZone, TerrirotyDirection.Down);
        TerritoryZone leftZone = GetAdjacentZone(currentZone, TerrirotyDirection.Left);
        TerritoryZone rightZone = GetAdjacentZone(currentZone, TerrirotyDirection.Right);

        if (upZone != null)
            adjacentZones.Add(TerrirotyDirection.Up, upZone);
        if (downZone != null)
            adjacentZones.Add(TerrirotyDirection.Down, downZone);
        if (leftZone != null)
            adjacentZones.Add(TerrirotyDirection.Left, leftZone);
        if (rightZone != null)
            adjacentZones.Add(TerrirotyDirection.Right, rightZone);

        return adjacentZones;
    }

    public TerrirotyDirection GetDirection(TerritoryZone currentZone, TerritoryZone adjacentZone)
    {
        if (currentZone.TerritoryZoneData.AdjacentZoneId_Up == adjacentZone.TerritoryZoneData.ZoneId)
            return TerrirotyDirection.Up;

        if (currentZone.TerritoryZoneData.AdjacentZoneId_Down == adjacentZone.TerritoryZoneData.ZoneId)
            return TerrirotyDirection.Down;

        if (currentZone.TerritoryZoneData.AdjacentZoneId_Left == adjacentZone.TerritoryZoneData.ZoneId)
            return TerrirotyDirection.Left;

        if (currentZone.TerritoryZoneData.AdjacentZoneId_Right == adjacentZone.TerritoryZoneData.ZoneId)
            return TerrirotyDirection.Right;

        return TerrirotyDirection.Up;
    }
}
