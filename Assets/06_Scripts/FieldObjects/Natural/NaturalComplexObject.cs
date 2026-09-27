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
        set => growth.CurrentStage = value;
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
   
    [SerializeField] 
    private int resetStage = 1;        //리셋할 단계

    private GrowthController growth;

    protected override void Awake()
    {
        base.Awake();

        InitDropper(dropper);

        growth = new GrowthController(this, farmObjDatas, maxGrowthStage);
        growth.OnStageCompleted += HandleStageCompleted;
    }

    private void Start()
    {
        fieldComplexObjData = Data as FieldComplexObjData;

        // 로드 시 GrowthStage/IsGrowthComplete가 Start 전에 채워지므로 그 단계부터 이어서 성장
        growth.StartGrowth();
    }

    /// <summary>
    /// 한 단계 성장이 끝날 때마다 해당 단계 드랍
    /// </summary>
    private void HandleStageCompleted(FarmObjData farmObj)
    {
        if (dropper != null)
        {
            dropper.DropOnHit(farmObj.DropGroupID, transform.position);
        }
    }

    private bool IsTakeDamageSeedDrop;

    public override void TakeDamage(InteractionContext context)
    {
        base.TakeDamage(context);

        if (IsDepleted) return;

        if (growth.IsMaxStage)
        {
            IsTakeDamageSeedDrop = true;
            growth.ResetTo(resetStage); // 성장 재시작
        }
        
    }
    protected override void OnDepleted()
    {
        growth.Stop();
        Destroy(gameObject);
    }
}
