using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SkillTreePanelUI : MonoBehaviour
{
    [SerializeField] private PlayerSkills playerSkills;
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private SkillTreeNodeUI nodeTemplate;
    [SerializeField] private SkillTreeData[] defaultTrees;
    [SerializeField] private Vector2 nodeSpacing = new Vector2(240f, 210f);
    [SerializeField] private Vector2 padding = new Vector2(80f, 80f);

    private readonly Dictionary<SkillData, SkillTreeNodeUI> views = new Dictionary<SkillData, SkillTreeNodeUI>();
    private readonly Dictionary<SkillData, List<SkillData>> children = new Dictionary<SkillData, List<SkillData>>();
    private readonly Dictionary<SkillData, SkillData> parents = new Dictionary<SkillData, SkillData>();
    private readonly Dictionary<SkillData, RectTransform> lines = new Dictionary<SkillData, RectTransform>();
    private readonly Dictionary<SkillData, float> widths = new Dictionary<SkillData, float>();
    private readonly List<SkillData> roots = new List<SkillData>();
    private SkillTreeNodeUI[] manualNodes;
    private PlayerSkills subscribedPlayer;
    private float deepestY;
    private bool isInitialized;

    private void Start()
    {
        if (isInitialized) return;
        if (playerSkills == null)
        {
            PlayerContext context = FindFirstObjectByType<PlayerContext>();
            if (context != null) playerSkills = context.Skills;
        }
        Initialize(playerSkills);
        if (playerSkills == null)
            Debug.LogWarning("SkillTreePanelUI: 플레이어 루트에 PlayerSkills를 추가하세요.", this);
    }

    public void Initialize(PlayerSkills player)
    {
        isInitialized = true;
        Unsubscribe();
        playerSkills = player;
        if (playerSkills != null && playerSkills.SkillTrees.Count == 0 && defaultTrees != null)
            playerSkills.ConfigureTrees(defaultTrees);
        BuildViews();
        if (isActiveAndEnabled) Subscribe();
        Refresh();
        if (scrollRect != null)
        {
            scrollRect.StopMovement();
            scrollRect.normalizedPosition = new Vector2(0f, 1f);
        }
    }

    private void BuildViews()
    {
        if (content == null || nodeTemplate == null)
        {
            manualNodes = GetComponentsInChildren<SkillTreeNodeUI>(true);
            foreach (SkillTreeNodeUI node in manualNodes) node.Bind(playerSkills);
            return;
        }
        // Content는 생성된 스킬/연결선 전용 컨테이너이다.
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Transform child = content.GetChild(i);
            child.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
        views.Clear();
        children.Clear();
        parents.Clear();
        roots.Clear();
        lines.Clear();
        nodeTemplate.gameObject.SetActive(false);
        if (playerSkills == null) return;
        foreach (SkillTreeData tree in playerSkills.SkillTrees)
        {
            if (tree == null || !tree.TryValidate(out _)) continue;
            foreach (SkillTreeData.Node node in tree.Nodes)
            {
                if (views.ContainsKey(node.Skill)) continue;
                SkillTreeNodeUI view = Instantiate(nodeTemplate, content);
                view.name = "Skill_" + node.Skill.SkillId;
                view.Bind(playerSkills, node.Skill);
                views.Add(node.Skill, view);
                children.Add(node.Skill, new List<SkillData>());
                parents.Add(node.Skill, node.Parent);
            }
        }
        foreach (var entry in parents)
        {
            if (entry.Value == null) roots.Add(entry.Key);
            else if (children.TryGetValue(entry.Value, out List<SkillData> siblings))
            {
                siblings.Add(entry.Key);
                GameObject line = new GameObject("Line_" + entry.Key.SkillId, typeof(RectTransform), typeof(Image));
                RectTransform rect = (RectTransform)line.transform;
                rect.SetParent(content, false);
                rect.SetAsFirstSibling();
                Image image = line.GetComponent<Image>();
                image.raycastTarget = false;
                image.color = new Color(0.35f, 0.65f, 0.6f, 0.8f);
                lines.Add(entry.Key, rect);
            }
        }
        content.sizeDelta = Vector2.zero;
    }

    private void OnEnable() { Subscribe(); Refresh(); }
    private void OnDisable() => Unsubscribe();
    private void OnDestroy() => Unsubscribe();

    private void Subscribe()
    {
        Unsubscribe();
        if (playerSkills == null) return;
        subscribedPlayer = playerSkills;
        subscribedPlayer.OnSkillsChanged += Refresh;
    }

    private void Unsubscribe()
    {
        if (subscribedPlayer != null) subscribedPlayer.OnSkillsChanged -= Refresh;
        subscribedPlayer = null;
    }

    private void Refresh()
    {
        if (pointsText != null) pointsText.text = $"SP: {(playerSkills != null ? playerSkills.SkillPoints : 0)}";
        if (manualNodes != null)
            foreach (SkillTreeNodeUI node in manualNodes) if (node != null) node.Refresh();
        foreach (SkillTreeNodeUI node in views.Values) node.Refresh();
        if (content == null || views.Count == 0) return;
        LayoutVisibleNodes();
    }

    private float Measure(SkillData skill)
    {
        if (!views[skill].gameObject.activeSelf) return 0f;
        float width = 0f;
        foreach (SkillData child in children[skill]) width += Measure(child);
        width = Mathf.Max(nodeSpacing.x, width);
        widths[skill] = width;
        return width;
    }

    private void Place(SkillData skill, float left, int depth)
    {
        if (!views[skill].gameObject.activeSelf) return;
        RectTransform rect = (RectTransform)views[skill].transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        float y = padding.y + rect.sizeDelta.y * 0.5f + depth * nodeSpacing.y;
        rect.anchoredPosition = new Vector2(left + widths[skill] * 0.5f, -y);
        deepestY = Mathf.Max(deepestY, y + rect.sizeDelta.y * 0.5f);
        float childLeft = left;
        foreach (SkillData child in children[skill])
        {
            if (!views[child].gameObject.activeSelf) continue;
            Place(child, childLeft, depth + 1);
            childLeft += widths[child];
        }
    }

    private void LayoutVisibleNodes()
    {
        widths.Clear();
        float total = 0f;
        foreach (SkillData root in roots) total += Measure(root);
        float left = padding.x;
        deepestY = 0f;
        foreach (SkillData root in roots)
        {
            Place(root, left, 0);
            if (widths.TryGetValue(root, out float width)) left += width;
        }

        Vector2 minimum = scrollRect != null && scrollRect.viewport != null
            ? scrollRect.viewport.rect.size : new Vector2(960f, 600f);
        Vector2 required = new Vector2(Mathf.Max(minimum.x, total + padding.x * 2f),
            Mathf.Max(minimum.y, deepestY + padding.y));
        content.sizeDelta = Vector2.Max(content.sizeDelta, required);

        foreach (var entry in lines)
        {
            bool visible = views[entry.Key].gameObject.activeSelf;
            entry.Value.gameObject.SetActive(visible);
            if (!visible) continue;
            RectTransform child = (RectTransform)views[entry.Key].transform;
            RectTransform parent = (RectTransform)views[parents[entry.Key]].transform;
            Vector2 start = parent.anchoredPosition + Vector2.down * parent.sizeDelta.y * 0.5f;
            Vector2 end = child.anchoredPosition + Vector2.up * child.sizeDelta.y * 0.5f;
            Vector2 delta = end - start;
            RectTransform line = entry.Value;
            line.anchorMin = line.anchorMax = new Vector2(0f, 1f);
            line.pivot = new Vector2(0f, 0.5f);
            line.anchoredPosition = start;
            line.sizeDelta = new Vector2(delta.magnitude, 4f);
            line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }
    }

    public void Open(PlayerSkills player)
    {
        if (!isInitialized || playerSkills != player) Initialize(player);
        Open();
    }

    public void Open() => gameObject.SetActive(true);
    public void Close() => gameObject.SetActive(false);
}
