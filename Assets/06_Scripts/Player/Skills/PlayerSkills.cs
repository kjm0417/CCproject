using System;
using System.Collections.Generic;
using UnityEngine;

public enum SkillState
{
    Hidden,
    InsufficientPoints,
    Available,
    Learned
}

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerProgression))]
public class PlayerSkills : MonoBehaviour, IPlayerComponent
{
    [SerializeField] private List<SkillTreeData> skillTrees = new List<SkillTreeData>();

    public event Action OnSkillsChanged;
    // 새로 습득할 때만 발생. 저장 복원은 OnSkillsChanged로 확인한다.
    public event Action<SkillData> OnSkillLearned;

    private readonly Dictionary<SkillData, SkillTreeData.Node> nodes = new Dictionary<SkillData, SkillTreeData.Node>();
    private readonly Dictionary<string, SkillData> skillsById = new Dictionary<string, SkillData>(StringComparer.Ordinal);
    private readonly HashSet<string> learnedIds = new HashSet<string>(StringComparer.Ordinal);
    private PlayerProgression progression;
    private bool isLearning;

    public int SkillPoints => progression != null ? progression.SkillPoints : 0;
    public IReadOnlyList<SkillTreeData> SkillTrees => skillTrees.AsReadOnly();

    public void ConfigureTrees(IEnumerable<SkillTreeData> trees)
    {
        skillTrees = trees != null ? new List<SkillTreeData>(trees) : new List<SkillTreeData>();
        BuildTrees();
        OnSkillsChanged?.Invoke();
    }

    public void Initialize(PlayerContext context)
    {
        if (progression != null) progression.OnSkillPointsChanged -= HandlePointsChanged;
        progression = context.Progression;
        BuildTrees();
        if (isActiveAndEnabled && progression != null)
            progression.OnSkillPointsChanged += HandlePointsChanged;
        OnSkillsChanged?.Invoke();
    }

    private void OnEnable()
    {
        if (progression != null)
        {
            progression.OnSkillPointsChanged -= HandlePointsChanged;
            progression.OnSkillPointsChanged += HandlePointsChanged;
        }
    }

    private void OnDisable()
    {
        if (progression != null) progression.OnSkillPointsChanged -= HandlePointsChanged;
    }

    private void HandlePointsChanged()
    {
        if (!isLearning) OnSkillsChanged?.Invoke();
    }

    private void BuildTrees()
    {
        nodes.Clear();
        skillsById.Clear();
        foreach (SkillTreeData tree in skillTrees)
        {
            if (tree == null) continue;
            if (!tree.TryValidate(out string error))
            {
                Debug.LogError($"PlayerSkills: {tree.name}: {error}", tree);
                continue;
            }

            bool duplicate = false;
            foreach (SkillTreeData.Node node in tree.Nodes)
            {
                if (skillsById.ContainsKey(node.Skill.SkillId))
                {
                    Debug.LogError($"PlayerSkills: 트리 사이에 Skill Id가 중복되었습니다: {node.Skill.SkillId}", tree);
                    duplicate = true;
                    break;
                }
            }
            if (duplicate) continue;

            foreach (SkillTreeData.Node node in tree.Nodes)
            {
                nodes.Add(node.Skill, node);
                skillsById.Add(node.Skill.SkillId, node.Skill);
            }
        }
    }

    public bool HasLearned(SkillData skill)
    {
        return skill != null && nodes.ContainsKey(skill) && learnedIds.Contains(skill.SkillId);
    }

    public SkillState GetState(SkillData skill)
    {
        if (skill == null || !nodes.TryGetValue(skill, out SkillTreeData.Node node))
            return SkillState.Hidden;
        if (HasLearned(skill)) return SkillState.Learned;
        if (node.Parent != null && !HasLearned(node.Parent)) return SkillState.Hidden;
        return progression != null && SkillPoints >= skill.PointCost
            ? SkillState.Available : SkillState.InsufficientPoints;
    }

    public bool TryLearn(SkillData skill)
    {
        if (isLearning || GetState(skill) != SkillState.Available) return false;

        // 포인트 변경 이벤트 안에서 다시 습득을 요청해도 중복 차감되지 않는다.
        isLearning = true;
        try
        {
            learnedIds.Add(skill.SkillId);
            if (skill.PointCost > 0 && !progression.TrySpendSkillPoints(skill.PointCost))
            {
                learnedIds.Remove(skill.SkillId);
                return false;
            }
        }
        finally
        {
            isLearning = false;
        }

        OnSkillsChanged?.Invoke();
        OnSkillLearned?.Invoke(skill);
        return true;
    }

    public List<string> CreateSaveData()
    {
        var result = new List<string>(learnedIds);
        result.Sort(StringComparer.Ordinal);
        return result;
    }

    public void Load(IEnumerable<string> savedIds)
    {
        learnedIds.Clear();
        if (savedIds != null)
        {
            // 현재 씬에 없는 트리의 ID도 보존하여 씬 전환 중 저장 손실을 막는다.
            foreach (string id in savedIds)
                if (!string.IsNullOrWhiteSpace(id)) learnedIds.Add(id);
        }
        OnSkillsChanged?.Invoke();
    }
}
