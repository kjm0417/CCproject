using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillTree", menuName = "Scriptable Objects/Skills/Skill Tree")]
public class SkillTreeData : ScriptableObject
{
    [Serializable]
    public class Node
    {
        [SerializeField] private SkillData skill;
        [Tooltip("시작 스킬은 비워 둡니다. 나머지는 같은 트리의 부모 스킬을 지정하세요.")]
        [SerializeField] private SkillData parent;

        public SkillData Skill => skill;
        public SkillData Parent => parent;
    }

    [SerializeField] private string displayName;
    [SerializeField] private List<Node> nodes = new List<Node>();

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public IReadOnlyList<Node> Nodes => nodes.AsReadOnly();

    public bool TryValidate(out string error)
    {
        error = null;
        if (nodes == null || nodes.Count == 0)
        {
            error = "시작 스킬을 포함한 노드를 추가하세요.";
            return false;
        }

        var bySkill = new Dictionary<SkillData, Node>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        int rootCount = 0;
        foreach (Node node in nodes)
        {
            if (node == null || node.Skill == null || string.IsNullOrWhiteSpace(node.Skill.SkillId))
            {
                error = "모든 노드에 스킬과 고유 Skill Id가 필요합니다.";
                return false;
            }
            if (bySkill.ContainsKey(node.Skill) || !ids.Add(node.Skill.SkillId))
            {
                error = "같은 스킬 또는 Skill Id가 중복되었습니다.";
                return false;
            }
            bySkill.Add(node.Skill, node);
            if (node.Parent == null) rootCount++;
        }

        if (rootCount == 0)
        {
            error = "Parent가 없는 시작 스킬이 하나 이상 필요합니다.";
            return false;
        }

        var childCounts = new Dictionary<SkillData, int>();
        foreach (Node node in nodes)
        {
            if (node.Parent != null)
            {
                if (!bySkill.ContainsKey(node.Parent))
                {
                    error = "부모 스킬도 같은 트리의 Nodes에 등록해야 합니다.";
                    return false;
                }
                childCounts.TryGetValue(node.Parent, out int count);
                childCounts[node.Parent] = count + 1;
                if (count >= 2)
                {
                    error = "한 스킬에는 최대 두 개의 자식 스킬을 연결할 수 있습니다.";
                    return false;
                }
            }

            var path = new HashSet<SkillData>();
            Node current = node;
            while (current != null)
            {
                if (!path.Add(current.Skill))
                {
                    error = "부모 연결이 순환합니다. 자기 자신이나 후손을 부모로 지정할 수 없습니다.";
                    return false;
                }
                if (current.Parent == null) break;
                if (!bySkill.TryGetValue(current.Parent, out current))
                {
                    error = "부모 스킬이 트리에 없습니다.";
                    return false;
                }
            }
        }
        return true;
    }
}
