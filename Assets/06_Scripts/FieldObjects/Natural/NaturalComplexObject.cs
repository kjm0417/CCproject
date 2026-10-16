using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 복잡한 필드 오브젝트 - 단계 별 모습, 열매 성장 + 기능
/// 성장 로직은 GrowthController에서 담당, 여기서는 단계별 모습 / 드랍 / 열매 표시를 처리
/// </summary>
public class NaturalComplexObject : DamageableFieldObject, IGrowable
{
    public virtual int GrowthStage
    {
        get => growth.CurrentStage;
        set
        {
            growth.CurrentStage = value;
            ApplyStageSprite(value);
            RefreshFruits(value);
        }
    }
    public virtual bool IsHarvestable => false;

    /// <summary>
    /// 최대 단계에서 수확 후 돌아갈 단계 - 저장/로드 시 최대 단계 반복 방지
    /// </summary>
    public bool IsGrowthComplete
    {
        get => growth.IsGrowthComplete;
        set => growth.IsGrowthComplete = value;
    }

    private FieldComplexObjData fieldComplexObjData;

    [SerializeField]
    private ComplexFieldObjectDropper dropper;

    [SerializeField]
    List<FarmObjData> farmObjDatas = new();

    [SerializeField]
    private int maxGrowthStage = 5;    //마지막 단계

    [Tooltip("최대 단계 수확 후 돌아갈 단계")]
    [SerializeField]
    private int resetStage = 3;        //리셋할 단계

    [Header("단계별 모습")]
    [Tooltip("index 0 = 1단계. 비어 있으면 모습 변경 안 함")]
    [SerializeField]
    private List<Sprite> stageSprites = new();

    private SpriteRenderer spriteRenderer;

    [System.Serializable]
    private class FruitData
    {
        [Tooltip("열매 이름 (매화, 오디 등)")]
        public string fruitName;

        [Tooltip("열매가 생성될 단계")]
        public int stage;

        [Tooltip("생성할 열매 프리팹")]
        public GameObject prefab;

        [Tooltip("열매 생성 위치 (나무 자식으로 빈 오브젝트 배치)")]
        public List<Transform> spawnPoints = new();

        [System.NonSerialized]
        public List<GameObject> spawnedObjects = new();

        public void Clear()
        {
            foreach (GameObject obj in spawnedObjects)
            {
                if (obj != null) ObjectPoolManager.Despawn(obj);
            }
            spawnedObjects.Clear();
        }
    }

    [Header("열매")]
    [SerializeField]
    private List<FruitData> fruits = new();

    private GrowthController growth;
    private bool hasStarted; //Start 실행 여부 ( 풀 재사용 판정 )

    protected override void Awake()
    {
        base.Awake();

        InitDropper(dropper);

        growth = new GrowthController(this, farmObjDatas, maxGrowthStage);
        #region [이전] 단계 완료 시 자동 드랍
        // growth.OnStageCompleted += HandleStageCompleted;
        #endregion
        growth.OnStageChanged += ApplyStageSprite;
        growth.OnStageChanged += RefreshFruits;

        // 스프라이트는 자식(Sprite_Ani)에 있음
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        fieldComplexObjData = Data as FieldComplexObjData;

        // 로드 시 GrowthStage/IsGrowthComplete가 Start 전에 채워지므로 그 단계부터 이어서 성장
        ApplyStageSprite(growth.CurrentStage);
        RefreshFruits(growth.CurrentStage);
        growth.StartGrowth();
        hasStarted = true;
    }

    /// <summary>
    /// 풀 재사용 - 1단계부터 다시 성장 ( 로드 시 단계 세팅 후 시작되게 Start 와 같이 다음 프레임 )
    /// </summary>
    public override void OnSpawned()
    {
        base.OnSpawned();

        if (!hasStarted) return; //첫 생성은 Start 에서 시작

        growth.Stop();
        growth.CurrentStage = 1;
        growth.IsGrowthComplete = false;
        StartCoroutine(BeginGrowthNextFrame());
    }

    public override void OnDespawned()
    {
        base.OnDespawned();

        growth.Stop();
        ClearFruits();
    }

    private IEnumerator BeginGrowthNextFrame()
    {
        yield return null;

        ApplyStageSprite(growth.CurrentStage);
        RefreshFruits(growth.CurrentStage);
        growth.StartGrowth();
    }

    #region [이전] 단계 완료 시 자동 드랍 (최대 단계 제외)
    // /// <summary>
    // /// 한 단계 성장이 끝날 때마다 해당 단계 드랍
    // /// </summary>
    // private void HandleStageCompleted(FarmObjData farmObj)
    // {
    //     // 최대 단계(열매)는 자동 드랍하지 않고 상호작용 시 드랍
    //
    //     if (growth.IsMaxStage) return;
    //
    //     if (dropper != null)
    //     {
    //         dropper.DropOnHit(farmObj.DropGroupID, transform.position);
    //     }
    // }
    #endregion

    /// <summary>
    /// 최대 단계(열매) 수확 - HP 공격 후 수확 기능 + resetStage로 시간 초기화
    /// </summary>
    private void HarvestFruit()
    {
        FarmObjData stageData = growth.GetStageData(growth.CurrentStage);
        if (dropper != null && stageData != null)
        {
            dropper.DropOnHit(stageData.DropGroupID, transform.position);
        }

        growth.ResetTo(resetStage);
    }

    #region [이전] 피격마다 현재 단계 드랍 (최대 단계 제외)
    // private void DropOnHit()
    // {
    //     FarmObjData stageData = growth.GetStageData(growth.CurrentStage);
    //     if (dropper != null && stageData != null)
    //     {
    //         dropper.DropOnHit(stageData.DropGroupID, transform.position);
    //     }
    //
    //     if (growth.IsMaxStage)
    //     {
    //         growth.ResetTo(resetStage);
    //     }
    // }
    #endregion

    /// <summary>
    /// HP 0(파괴) 시 현재 단계 드랍 (파괴 드랍)
    /// </summary>
    private void DropOnDepleted()
    {
        // dropMappings의 단계 그룹을 보고 pickOne 규칙으로 랜덤 드랍 또는 DropFinal 또는 DropOnHit 처리
        FarmObjData stageData = growth.GetStageData(growth.CurrentStage);
        if (dropper != null && stageData != null)
        {
            dropper.DropOnHit(stageData.DropGroupID, transform.position);
        }
    }

    public override void TakeDamage(InteractionContext context)
    {
        // 최대 단계: HP 공격 후 수확 + resetStage로 돌아옴
        if (growth.IsMaxStage)
        {
            HarvestFruit();
            return;
        }

        // 1 ~ (최대-1) 단계: HP 공격만, 드랍은 파괴(OnDepleted) 시에만
        base.TakeDamage(context);

        #region [이전] 피격마다 현재 단계 드랍
        // base.TakeDamage(context);
        //
        // // 이번 타격으로 HP 0이 되면 OnDepleted에서 파괴 드랍만 처리
        // if (IsDepleted) return;
        //
        // DropOnHit();
        #endregion

        #region [이전] 최대 단계에서만 열매 드랍 + 리셋
        // if (growth.IsMaxStage)
        // {
        //     FarmObjData maxStageData = growth.GetStageData(maxGrowthStage);
        //     if (dropper != null && maxStageData != null)
        //     {
        //         dropper.DropOnHit(maxStageData.DropGroupID, transform.position);
        //     }
        //     growth.ResetTo(resetStage);
        // }
        #endregion

        #region [이전] 피격 시 리셋만 (열매는 단계 완료 시 자동 드랍)
        // if (growth.IsMaxStage)
        // {
        //     IsTakeDamageSeedDrop = true;
        //     growth.ResetTo(resetStage); // 성장 재시작
        // }
        #endregion
    }

    private void ApplyStageSprite(int stage)
    {
        if (spriteRenderer == null) return;

        int index = stage - 1;
        if (index < 0 || index >= stageSprites.Count || stageSprites[index] == null) return;

        spriteRenderer.sprite = stageSprites[index];
    }

    /// <summary>
    /// 각 열매별로 stage일 때만 생성, 아니면 제거
    /// </summary>
    private void RefreshFruits(int stage)
    {
        foreach (FruitData fruit in fruits)
        {
            bool shouldSpawn = stage == fruit.stage && fruit.prefab != null;

            if (!shouldSpawn || fruit.spawnedObjects.Count > 0)
            {
                fruit.Clear();
                continue;
            }

            foreach (Transform point in fruit.spawnPoints)
            {
                if (point == null) continue;
                fruit.spawnedObjects.Add(ObjectPoolManager.Spawn(fruit.prefab, point.position, Quaternion.identity, point));
            }
        }
    }

    private void ClearFruits()
    {
        foreach (FruitData fruit in fruits)
        {
            fruit.Clear();
        }
    }

    protected override void OnDepleted()
    {
        DropOnDepleted();
        growth.Stop();
        ObjectPoolManager.Despawn(gameObject);
        #region [이전] Instantiate / Destroy
        // Destroy(gameObject);
        #endregion
    }
}
