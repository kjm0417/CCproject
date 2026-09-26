using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 자연 생성물 - 복합 자원 오브젝트 ( 성장 단계가 존재 함)
/// </summary>
public class NaturalComplexObject : DamageableFieldObject, IGrowable
{
    public virtual int GrowthStage { get; set; } = 1;
    public virtual bool IsHarvestable => false;

    /// <summary>
    /// 최대 단계 드랍까지 끝났는지 여부 - 저장/로드 시 최대 단계 중복 드랍 방지
    /// </summary>
    public bool IsGrowthComplete { get; set; }

    private FieldComplexObjData fieldComplexObjData;

    [SerializeField]
    private ComplexFieldObjectDropper dropper;

    [SerializeField]
    List<FarmObjData> farmObjDatas = new();

    [SerializeField] 
    private int maxGrowthStage = 5;    //마지막 단계
   
    [SerializeField] 
    private int resetStage = 1;        //리셋할 단계
    protected override void Awake()
    {
        base.Awake();

        InitDropper(dropper);

    }

    private void Start()
    {
        fieldComplexObjData = Data as FieldComplexObjData;

        StartGrowthForStage(GrowthStage);
    }

    /// <summary>
    /// 현재 농작물의 성장 단계 찾기
    /// </summary>
    /// <param name="growIndex"></param>
    private void StartGrowthForStage(int growIndex)
    {
        if (growIndex > maxGrowthStage || IsGrowthComplete)
            return;


        foreach (FarmObjData farmObj in farmObjDatas)
        {
            string[] cropID = farmObj.CropID.Split('_');

            if (cropID.Length > 1 && cropID[1] == growIndex.ToString())
            {
                StartCoroutine(GrowCorutine(farmObj));
                break;
            }

        }
    }

    /// <summary>
    /// 실제 농작물이 성장하는 코루틴
    /// </summary>
    /// <param name="farmObj"></param>
    /// <returns></returns>
    private IEnumerator GrowCorutine(FarmObjData farmObj)
    {
        yield return new WaitForSeconds(farmObj.TimePerStageSec);
       
        dropper.DropOnHit(farmObj.DropGroupID, transform.position);

        if (GrowthStage >= maxGrowthStage)
        {
            IsGrowthComplete = true;
            yield break;
        }

        GrowthStage++;

        yield return new WaitForSeconds(0.1f);

        StartGrowthForStage(GrowthStage);
    }
    private bool IsTakeDamageSeedDrop;

    public override void TakeDamage(InteractionContext context)
    {
        base.TakeDamage(context);

        if (IsDepleted) return;

        if(GrowthStage == maxGrowthStage)
        {
            IsTakeDamageSeedDrop = true;
            GrowthStage = resetStage;
            IsGrowthComplete = false;
            StartGrowthForStage(GrowthStage); // 성장 재시작
        }
        
    }
    protected override void OnDepleted()
    {
        Destroy(gameObject);
    }
}
