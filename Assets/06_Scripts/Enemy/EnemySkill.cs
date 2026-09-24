using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 스킬 저장소
/// </summary>
public class EnemySkillManager
{
    private List<EnemySkill> skills = new(); //등록 순서 = 우선순위

    public void RegisterSkill(EnemySkill skill)
    {
        skills.Add(skill);
    }

    /// <summary>
    /// 쿨타임이 끝난 스킬 중 우선순위가 가장 높은 스킬 반환
    /// </summary>
    public bool TryGetReadySkill(out EnemySkill readySkill)
    {
        foreach (EnemySkill skill in skills)
        {
            if (skill.IsReady)
            {
                readySkill = skill;
                return true;
            }
        }

        readySkill = null;
        return false;
    }
}
/// <summary>
/// 적 공통 - 스킬 실행 및 쿨타임
/// </summary>
public abstract class EnemySkill
{
    protected EnemyContext ctx;
    protected MonoBehaviour owner; //모노에서 지원하는 메서드나 기능들 사용하기 위함

    private float lastUseTime = float.NegativeInfinity;

    protected float SkillAttackCoolDown;

    public EnemySkill(EnemyContext ctx, MonoBehaviour owner)
    {
        this.ctx = ctx;
        this.owner = owner;
    }

    /// <summary>
    /// 쿨타임 끝났는지 여부
    /// </summary>
    public bool IsReady => Time.time >= lastUseTime + SkillAttackCoolDown;

    /// <summary>
    /// 쿨타임 시작 후 스킬 실행 ( 코루틴으로 실행 )
    /// </summary>
    public IEnumerator Use()
    {
        lastUseTime = Time.time;
        return Execute();
    }

    /// <summary>
    /// 스킬 동작 - 코루틴이 끝나면 스킬 종료
    /// </summary>
    protected abstract IEnumerator Execute();

    /// <summary>
    /// 스킬 종료 시 정리 ( 정상 종료, 피격 등으로 중단된 경우 모두 호출 )
    /// </summary>
    public virtual void OnEnd() { }

    ///// <summary>
    ///// 스킬 쿨타임 설정 ( 각 스킬 클래스가 재정의 )
    ///// </summary>
    ///// <returns></returns>
    //public virtual float GetCooldown() => 1f;
}
