using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 Animator 파라미터 호출 담당 - BossBase 가 상태 변화 시 호출
/// 컨트롤러에 없는 파라미터는 무시 ( 리소스 없는 애님은 경고 없이 건너뜀 )
/// </summary>
[Serializable]
public class BossAnimator
{
    [SerializeField] private Animator animator;

    [Header("파라미터 이름")]
    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private string dieTrigger = "Die";
    [SerializeField] private string stunTrigger = "Stun";
    [SerializeField, Tooltip("스턴 유지 여부 ( false 가 되면 Idle 로 복귀 )")]
    private string stunBool = "IsStunned";

    private HashSet<int> parameters;

    public void Initialize()
    {
        parameters = new HashSet<int>();
        if (animator == null) return;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            parameters.Add(p.nameHash);
        }
    }

    public void PlayHit() => SetTrigger(hitTrigger);
    public void PlayDie() => SetTrigger(dieTrigger);

    public void SetStunned(bool stunned)
    {
        if (stunned) SetTrigger(stunTrigger);
        SetBool(stunBool, stunned);
    }

    public void SetTrigger(string name)
    {
        if (!CanUse(name)) return;
        animator.SetTrigger(name);
    }

    public void ResetTrigger(string name)
    {
        if (!CanUse(name)) return;
        animator.ResetTrigger(name);
    }

    private void SetBool(string name, bool value)
    {
        if (!CanUse(name)) return;
        animator.SetBool(name, value);
    }

    // 비활성 오브젝트 ( 지나가용 중 숨김 등 ) 의 Animator 는 호출해도 무시됨
    private bool CanUse(string name)
    {
        return animator != null && animator.isActiveAndEnabled && !string.IsNullOrEmpty(name)
               && parameters != null && parameters.Contains(Animator.StringToHash(name));
    }
}
