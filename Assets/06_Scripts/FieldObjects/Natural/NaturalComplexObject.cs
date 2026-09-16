using UnityEngine;

/// <summary>
/// 자연 생성물 - 복합 자원 오브젝트 ( 성장 단계가 존재 함)
/// </summary>
public class NaturalComplexObject : DamageableFieldObject, IGrowable
{
    public virtual int GrowthStage => 0;
    public virtual bool IsHarvestable => false;

    [SerializeField]
    private ComplexFieldObjectDropper dropper;

    private FieldComplexObjData fieldComplexObjData;
    protected override void Awake()
    {
        base.Awake();

        InitDropper(dropper);

    }

    private void Start()
    {
        fieldComplexObjData = Data as FieldComplexObjData;
    }
    public override void TakeDamage(InteractionContext context)
    {
        base.TakeDamage(context);

        if (IsDepleted) return;

        dropper.DropOnHit(fieldComplexObjData.HitDropGroupID, transform.position);
    }
}
