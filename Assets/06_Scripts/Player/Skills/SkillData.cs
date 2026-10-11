using UnityEngine;

// 스킬의 공통 정의. 플레이어별 습득 상태는 이 SO에 저장하지 않는다.
[CreateAssetMenu(fileName = "Skill", menuName = "Scriptable Objects/Skills/Skill")]
public class SkillData : ScriptableObject
{
    [Tooltip("저장에 사용하는 고유 ID. 출시 후에는 변경하지 마세요.")]
    [SerializeField] private string skillId;
    [SerializeField] private string displayName;
    [TextArea] [SerializeField] private string description;
    [SerializeField] private Sprite icon;
    [Min(0)] [SerializeField] private int pointCost = 1;

    public string SkillId => skillId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public int PointCost => Mathf.Max(0, pointCost);
}
