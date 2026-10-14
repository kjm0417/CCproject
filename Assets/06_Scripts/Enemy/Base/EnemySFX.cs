using UnityEngine;

/// <summary>
/// 적 효과음. Animator 붙은 오브젝트에 부착 (애니메이션 이벤트 수신)
/// Attack/Skill : 애니메이션 이벤트 / Hit/Die : EnemyBase 코드에서 호출
/// </summary>
public class EnemySFX : MonoBehaviour
{
    [SerializeField] private SFXType attackSFX;
    [SerializeField] private SFXType skillSFX;
    [SerializeField] private SFXType hitSFX;
    [SerializeField] private SFXType dieSFX;

    // 애니메이션 이벤트
    public void OnAttackSFX() => Play(attackSFX);
    public void OnSkillSFX() => Play(skillSFX);

    public void PlayHit() => Play(hitSFX);
    public void PlayDie() => Play(dieSFX);

    private void Play(SFXType type)
    {
        if (SoundManager.Instance == null) return;
        SoundManager.Instance.PlaySFX(type);
    }
}
