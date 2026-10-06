using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

// 보스 공용 Behavior Graph 조건 노드 - Conditional Guard / Conditional Branch 에서 사용

[Serializable, GeneratePropertyBag]
[Condition(name: "Boss Is Stunned", story: "[Self] is stunned", category: "Boss", id: "b4b480997c35444d814a3a8957b9d1ca")]
public partial class BossIsStunnedCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    public override bool IsTrue()
    {
        BossBase boss = BossBTUtil.GetBoss(Self);
        return boss != null && boss.IsStunned;
    }
}

[Serializable, GeneratePropertyBag]
[Condition(name: "Boss Is Dead", story: "[Self] is dead", category: "Boss", id: "442a4c4e6b04414e90b60750dfca0051")]
public partial class BossIsDeadCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    public override bool IsTrue()
    {
        BossBase boss = BossBTUtil.GetBoss(Self);
        return boss == null || boss.IsDead;
    }
}

/// <summary>
/// HP 비율 이하 - 페이즈 분기용 ( 0.5 = 50% )
/// </summary>
[Serializable, GeneratePropertyBag]
[Condition(name: "Boss Hp Below", story: "[Self] hp ratio is below [Ratio]", category: "Boss", id: "1980a36db87f42ec98d9c614f92dd9d3")]
public partial class BossHpBelowCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<float> Ratio = new BlackboardVariable<float>(0.5f);

    public override bool IsTrue()
    {
        BossBase boss = BossBTUtil.GetBoss(Self);
        return boss != null && boss.HpRatio <= Ratio.Value;
    }
}
