using UnityEngine;

/// <summary>
/// 자연 생성물 - 복합 자원 오브젝트
/// </summary>
public class NaturalComplexObject : DamageableFieldObject
{
    private FieldComplexObjData complexData;
    [SerializeField]
    private ComplexFieldObjectDropper dropper;

    protected override void Awake()
    {
        base.Awake();

        InitDropper(dropper);
    }
    void Start()
    {
        complexData = Data as FieldComplexObjData;
    }

    public override void TakeDamage(InteractionContext context)
    {
        base.TakeDamage(context);

        if (IsDepleted) return;

        dropper.DropOnHit(complexData.HitDropGroupID, transform.position);
    }
}
