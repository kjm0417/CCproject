using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Random = UnityEngine.Random;

// 보스 공용 Behavior Graph 액션 노드 - 모든 보스 ( BossBase 상속 ) 에서 재사용
// Self 는 그래프 기본 변수 Self ( BehaviorGraphAgent 오브젝트 ) 연결

/// <summary>
/// 가중치 랜덤으로 다음 패턴 선택 - 사용 가능한 패턴 없으면 Failure
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Select Boss Pattern", story: "[Self] selects next pattern", category: "Action/Boss", id: "a5be9be9895f4b24b6d90b2dd6dcbc1d")]
public partial class SelectBossPatternAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    protected override Status OnStart()
    {
        BossBase boss = BossBTUtil.GetBoss(Self);
        if (boss == null || boss.IsDead || boss.IsStunned) return Status.Failure;

        return boss.SelectNextPattern() ? Status.Success : Status.Failure;
    }
}

/// <summary>
/// 선택된 패턴 실행 - 끝나면 Success, 스턴 / 사망으로 끊기면 Failure
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Execute Boss Pattern", story: "[Self] executes selected pattern", category: "Action/Boss", id: "6ab8700cf242428ea34df18e6d09fd77")]
public partial class ExecuteBossPatternAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    private BossBase boss;
    private bool finished;

    protected override Status OnStart()
    {
        finished = true;
        boss = BossBTUtil.GetBoss(Self);
        if (boss == null) return Status.Failure;

        finished = !boss.StartSelectedPattern();
        return finished ? Status.Failure : Status.Running;
    }

    protected override Status OnUpdate()
    {
        Status status = BossBTUtil.UpdatePattern(boss);
        finished = status != Status.Running;
        return status;
    }

    protected override void OnEnd()
    {
        // 그래프가 중간에 노드를 끊은 경우 ( Abort 등 ) 패턴도 같이 정리
        if (boss != null && !finished) boss.CancelPattern();
    }
}

/// <summary>
/// ID 로 지정한 패턴 실행 - 등장 연출 / 페이즈 전환 같은 고정 패턴용
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Execute Boss Pattern By Id", story: "[Self] executes pattern [PatternId]", category: "Action/Boss", id: "1fc7f22bb89340c9ac98d92c0de9ff11")]
public partial class ExecuteBossPatternByIdAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<string> PatternId;

    private BossBase boss;
    private bool finished;

    protected override Status OnStart()
    {
        finished = true;
        boss = BossBTUtil.GetBoss(Self);
        if (boss == null || !boss.SelectPatternById(PatternId.Value))
        {
            Debug.LogWarning($"[BossBT] 패턴 ID 없음 : {PatternId.Value}");
            return Status.Failure;
        }

        finished = !boss.StartSelectedPattern();
        return finished ? Status.Failure : Status.Running;
    }

    protected override Status OnUpdate()
    {
        Status status = BossBTUtil.UpdatePattern(boss);
        finished = status != Status.Running;
        return status;
    }

    protected override void OnEnd()
    {
        if (boss != null && !finished) boss.CancelPattern();
    }
}

/// <summary>
/// 패턴 사이 대기 ( BossData.idleIntervalRange ) - 스턴 / 사망 시 Failure
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Boss Idle", story: "[Self] idles between patterns", category: "Action/Boss", id: "8188c0f7e6554aa3bd9ff6195fc51aaf")]
public partial class BossIdleAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    private BossBase boss;
    private float endTime;

    protected override Status OnStart()
    {
        boss = BossBTUtil.GetBoss(Self);
        if (boss == null) return Status.Failure;

        Vector2 range = boss.Data.idleIntervalRange;
        endTime = Time.time + Random.Range(range.x, range.y);
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (boss.IsDead || boss.IsStunned) return Status.Failure;
        return Time.time >= endTime ? Status.Success : Status.Running;
    }
}

/// <summary>
/// 스턴이 끝날 때까지 대기
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Wait Boss Stun", story: "[Self] waits until stun ends", category: "Action/Boss", id: "9b20c55ab779487ca60a67bf6f2b233e")]
public partial class WaitBossStunAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    private BossBase boss;

    protected override Status OnStart()
    {
        boss = BossBTUtil.GetBoss(Self);
        return boss == null ? Status.Failure : Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (boss.IsDead) return Status.Failure;
        return boss.IsStunned ? Status.Running : Status.Success;
    }
}

public static class BossBTUtil
{
    public static BossBase GetBoss(BlackboardVariable<GameObject> self)
    {
        if (self == null || self.Value == null) return null;
        return self.Value.GetComponent<BossBase>();
    }

    public static Node.Status UpdatePattern(BossBase boss)
    {
        if (boss.IsPatternRunning) return Node.Status.Running;
        if (boss.IsDead || boss.IsStunned) return Node.Status.Failure;
        return Node.Status.Success;
    }
}
