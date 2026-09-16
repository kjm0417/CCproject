using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 자연 생성물 - 복합 자원 오브젝트 ( 성장 단계가 존재 함)
/// </summary>
public class NaturalComplexObject : DamageableFieldObject, IGrowable
{
    public virtual int GrowthStage { get; set; } = 1;
    public virtual bool IsHarvestable => false;

    private FieldComplexObjData fieldComplexObjData;

    [SerializeField]
    private ComplexFieldObjectDropper dropper;

    [SerializeField]
    List<FarmObjData> farmObjDatas = new();

    protected override void Awake()
    {
        base.Awake();

        InitDropper(dropper);

    }

    private void Start()
    {
        fieldComplexObjData = Data as FieldComplexObjData;

        Init(GrowthStage);
    }

    /// <summary>
    /// 현재 농작물의 성장 단계 찾기
    /// </summary>
    /// <param name="growIndex"></param>
    private void Init(int growIndex)
    {
        foreach(FarmObjData farmObj in farmObjDatas)
        {
            string cropID = farmObj.CropID;

            if(cropID.Contains(growIndex.ToString())) //growIndex에 해당하는 농작물 찾음
            { 
                StartCoroutine(GrowCorutine(farmObj)); //해당 농작물 성장 시작

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
        Debug.Log("성장 중..");
        yield return new WaitForSeconds(farmObj.TimePerStageSec);
        Debug.Log("성장 끝! 드랍!");
        //다음 단계
        GrowthStage++;
        //드랍
        dropper.DropOnHit(farmObj.DropGroupID, transform.position);

        yield return new WaitForSeconds(0.1f);

        Init(GrowthStage);
    }

    public override void TakeDamage(InteractionContext context)
    {
        base.TakeDamage(context);

        if (IsDepleted) return;

    }
    protected override void OnDepleted()
    {
        //TODO KJ 

        Destroy(gameObject);
    }
}
