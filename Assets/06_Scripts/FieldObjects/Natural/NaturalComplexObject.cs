using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 자연 생성물 - 복합 자원 오브젝트 ( 성장 단계가 존재 함)
/// 성장 자체는 GrowthController가 담당, 여기서는 단계마다 드랍 / 피격 시 단계 리셋만 처리
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
        }
    }
    public virtual bool IsHarvestable => false;

    /// <summary>
    /// 최대 단계 드랍까지 끝났는지 여부 - 저장/로드 시 최대 단계 중복 드랍 방지
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
   
    [Tooltip("최대 단계 열매 수확 후 돌아갈 단계")]
    [SerializeField] 
    private int resetStage = 3;        //리셋할 단계

    [Header("단계별 모습")]
    [Tooltip("index 0 = 1단계. 비어 있으면 모습 변경 안 함")]
    [SerializeField]
    private List<Sprite> stageSprites = new();

    private SpriteRenderer spriteRenderer;

    private GrowthController growth;

    protected override void Awake()
    {
        base.Awake();

        InitDropper(dropper);

        growth = new GrowthController(this, farmObjDatas, maxGrowthStage);
        #region [이전] 단계 완료 시 자동 드랍
        // growth.OnStageCompleted += HandleStageCompleted;
        #endregion
        growth.OnStageChanged += ApplyStageSprite;

        // 스프라이트는 자식(Sprite_Ani)에 있음
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        fieldComplexObjData = Data as FieldComplexObjData;

        // 로드 시 GrowthStage/IsGrowthComplete가 Start 전에 채워지므로 그 단계부터 이어서 성장
        ApplyStageSprite(growth.CurrentStage);
        growth.StartGrowth();
    }

    #region [이전] 단계 완료 시 자동 드랍 (최대 단계 제외)
    // /// <summary>
    // /// 한 단계 성장이 끝날 때마다 해당 단계 드랍
    // /// </summary>
    // private void HandleStageCompleted(FarmObjData farmObj)
    // {
    //     // 최대 단계(열매)는 자동 드랍하지 않고 상호작용 시 드랍
    //     if (growth.IsMaxStage) return;
    //
    //     if (dropper != null)
    //     {
    //         dropper.DropOnHit(farmObj.DropGroupID, transform.position);
    //     }
    // }
    #endregion

    /// <summary>
    /// 플레이어 상호작용(피격) 시 현재 단계 드랍.
    /// 최대 단계(열매)면 드랍 후 resetStage로 되돌려 재성장
    /// </summary>
    private void DropOnHit()
    {
        FarmObjData stageData = growth.GetStageData(growth.CurrentStage);
        if (dropper != null && stageData != null)
        {
            dropper.DropOnHit(stageData.DropGroupID, transform.position);
        }

        if (growth.IsMaxStage)
        {
            growth.ResetTo(resetStage);
        }
    }

    /// <summary>
    /// HP 0(파괴) 시 현재 단계 드랍 (리셋 없음)
    /// </summary>
    private void DropOnDepleted()
    {
        // dropMappings에 단계 그룹이 다 있고 pickOne 설정도 쓰기 위해 DropFinal 대신 DropOnHit 사용
        FarmObjData stageData = growth.GetStageData(growth.CurrentStage);
        if (dropper != null && stageData != null)
        {
            dropper.DropOnHit(stageData.DropGroupID, transform.position);
        }
    }

    public override void TakeDamage(InteractionContext context)
    {
        base.TakeDamage(context);

        // 이번 타격으로 HP 0이 되면 OnDepleted에서 파괴 드랍만 처리
        if (IsDepleted) return;

        DropOnHit();

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

    protected override void OnDepleted()
    {
        DropOnDepleted();
        growth.Stop();
        Destroy(gameObject);
    }
}
