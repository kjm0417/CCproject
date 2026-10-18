using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class SkillDTO
{
    public int SkillID;
    public string SkillName;
    public string SkillTree;
    public string SkillTreeName;
    public int Order;
    public int GrowthStage;
    public string GrowthStageName;
    public string Effect;
}

public class SkillDataImporter
{
    private const string JsonPath = "/02_Data/Json/Skill/Skill.json";
    private const string SkillSOFolder = "Assets/02_Data/SO/Skills/";
    private const string TreeSOFolder = "Assets/02_Data/SO/SkillTree/";

    [MenuItem("Tools/Skill/Import Skills")]
    public static void Import()
    {
        // JSON 읽기
        string path = Application.dataPath + JsonPath;
        string json = System.IO.File.ReadAllText(path);
        string wrappedJson = "{\"items\":" + json + "}";
        DTOList<SkillDTO> data = JsonUtility.FromJson<DTOList<SkillDTO>>(wrappedJson);

        if (data == null || data.items == null || data.items.Count == 0)
        {
            Debug.LogError("SkillDataImporter: JSON을 읽지 못했습니다.");
            return;
        }

        // 폴더 생성
        EnsureFolder(SkillSOFolder);
        EnsureFolder(TreeSOFolder);

        // 기존 생성된 에셋 정리 (Skills 폴더만 — HoldSample 등은 건드리지 않음)
        CleanFolder(SkillSOFolder);

        // 트리별로 DTO 분류
        var treeMap = new Dictionary<string, List<SkillDTO>>();
        foreach (SkillDTO dto in data.items)
        {
            if (!treeMap.ContainsKey(dto.SkillTree))
                treeMap[dto.SkillTree] = new List<SkillDTO>();
            treeMap[dto.SkillTree].Add(dto);
        }

        // 1) SkillData SO 생성
        var skillDataMap = new Dictionary<int, SkillData>();
        foreach (SkillDTO dto in data.items)
        {
            string assetPath = SkillSOFolder + dto.SkillID + ".asset";
            SkillData so = ScriptableObject.CreateInstance<SkillData>();
            AssetDatabase.CreateAsset(so, assetPath);

            SerializedObject serialized = new SerializedObject(so);
            serialized.FindProperty("skillId").stringValue = dto.SkillID.ToString();
            serialized.FindProperty("displayName").stringValue = dto.SkillName;
            serialized.FindProperty("description").stringValue = dto.Effect;
            serialized.FindProperty("pointCost").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(so);

            skillDataMap[dto.SkillID] = so;
        }

        // SkillData를 먼저 디스크에 확정 — 이후 SkillTreeData에서 참조할 때 안정적
        AssetDatabase.SaveAssets();

        // 2) SkillTreeData SO 생성 (트리별)
        var createdTrees = new List<SkillTreeData>();
        foreach (var kvp in treeMap)
        {
            string treeName = kvp.Key;
            List<SkillDTO> skills = kvp.Value;
            skills.Sort((a, b) => a.Order.CompareTo(b.Order));

            // GrowthStage별 그룹화
            var stageGroups = new Dictionary<int, List<SkillDTO>>();
            foreach (SkillDTO dto in skills)
            {
                if (!stageGroups.ContainsKey(dto.GrowthStage))
                    stageGroups[dto.GrowthStage] = new List<SkillDTO>();
                stageGroups[dto.GrowthStage].Add(dto);
            }

            // 기존 에셋이 있으면 삭제 후 재생성
            string treeAssetPath = TreeSOFolder + treeName + ".asset";
            if (AssetDatabase.LoadAssetAtPath<SkillTreeData>(treeAssetPath) != null)
                AssetDatabase.DeleteAsset(treeAssetPath);

            SkillTreeData treeSO = ScriptableObject.CreateInstance<SkillTreeData>();
            AssetDatabase.CreateAsset(treeSO, treeAssetPath);

            SerializedObject treeSerialized = new SerializedObject(treeSO);
            treeSerialized.FindProperty("displayName").stringValue =
                skills.Count > 0 ? skills[0].SkillTreeName : treeName;

            SerializedProperty nodesProp = treeSerialized.FindProperty("nodes");
            nodesProp.ClearArray();

            var stageKeys = new List<int>(stageGroups.Keys);
            stageKeys.Sort();

            foreach (int stage in stageKeys)
            {
                List<SkillDTO> stageSkills = stageGroups[stage];
                int prevStage = stage - 1;
                bool hasPrevStage = stageGroups.ContainsKey(prevStage);

                for (int col = 0; col < stageSkills.Count; col++)
                {
                    SkillDTO dto = stageSkills[col];
                    if (!skillDataMap.ContainsKey(dto.SkillID)) continue;

                    int idx = nodesProp.arraySize;
                    nodesProp.InsertArrayElementAtIndex(idx);
                    SerializedProperty nodeProp = nodesProp.GetArrayElementAtIndex(idx);

                    nodeProp.FindPropertyRelative("skill").objectReferenceValue =
                        skillDataMap[dto.SkillID];

                    // 부모: 스테이지1은 루트(null), 이후는 이전 스테이지의 같은 col
                    SkillData parentSkill = null;
                    if (hasPrevStage)
                    {
                        List<SkillDTO> prevSkills = stageGroups[prevStage];
                        if (col < prevSkills.Count)
                        {
                            int parentId = prevSkills[col].SkillID;
                            skillDataMap.TryGetValue(parentId, out parentSkill);
                        }
                    }
                    nodeProp.FindPropertyRelative("parent").objectReferenceValue = parentSkill;
                }
            }

            treeSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(treeSO);
            createdTrees.Add(treeSO);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 검증 로그
        foreach (SkillTreeData tree in createdTrees)
        {
            bool valid = tree.TryValidate(out string error);
            Debug.Log($"  {tree.name}: {tree.Nodes.Count}개 노드, 유효={valid}" +
                (valid ? "" : $" ({error})"));
        }

        Debug.Log($"SkillDataImporter: {skillDataMap.Count}개 SkillData, {createdTrees.Count}개 SkillTreeData 생성 완료.");

        // 3) Player.prefab과 Canvas - SkillTree에 연결
        // FourRoots(기존 샘플)도 포함하여 연결
        var allTrees = new List<SkillTreeData>();
        // 기존 FourRoots 샘플 유지
        SkillTreeData fourRoots = AssetDatabase.LoadAssetAtPath<SkillTreeData>(
            "Assets/02_Data/SO/SkillTree/HoldSample/FourRoots.asset");
        if (fourRoots != null) allTrees.Add(fourRoots);
        allTrees.AddRange(createdTrees);

        ApplyToPlayerPrefab(allTrees);
        ApplyToSkillTreePanel(allTrees);
    }

    private static void ApplyToPlayerPrefab(List<SkillTreeData> treeSOs)
    {
        const string playerPrefabPath = "Assets/04_Prefabs/Player.prefab";
        if (treeSOs == null || treeSOs.Count == 0) return;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("SkillDataImporter: Player.prefab을 찾을 수 없습니다.");
            return;
        }

        using (var editScope = new PrefabUtility.EditPrefabContentsScope(playerPrefabPath))
        {
            PlayerSkills skills = editScope.prefabContentsRoot.GetComponentInChildren<PlayerSkills>();
            if (skills == null)
            {
                Debug.LogWarning("SkillDataImporter: Player.prefab에 PlayerSkills 컴포넌트가 없습니다.");
                return;
            }

            SerializedObject so = new SerializedObject(skills);
            SerializedProperty prop = so.FindProperty("skillTrees");
            prop.ClearArray();
            for (int i = 0; i < treeSOs.Count; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).objectReferenceValue = treeSOs[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        Debug.Log($"SkillDataImporter: Player.prefab → PlayerSkills에 {treeSOs.Count}개 SkillTreeData 연결 완료.");
    }

    private static void ApplyToSkillTreePanel(List<SkillTreeData> treeSOs)
    {
        const string skillTreePrefabPath = "Assets/04_Prefabs/UI-Canvas/Canvas - SkillTree.prefab";
        if (treeSOs == null || treeSOs.Count == 0) return;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(skillTreePrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("SkillDataImporter: Canvas - SkillTree.prefab을 찾을 수 없습니다.");
            return;
        }

        using (var editScope = new PrefabUtility.EditPrefabContentsScope(skillTreePrefabPath))
        {
            SkillTreePanelUI panelUI = editScope.prefabContentsRoot.GetComponentInChildren<SkillTreePanelUI>(true);
            if (panelUI == null)
            {
                Debug.LogWarning("SkillDataImporter: Canvas - SkillTree에 SkillTreePanelUI가 없습니다.");
                return;
            }

            SerializedObject so = new SerializedObject(panelUI);
            SerializedProperty prop = so.FindProperty("defaultTrees");
            prop.ClearArray();
            for (int i = 0; i < treeSOs.Count; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).objectReferenceValue = treeSOs[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        Debug.Log($"SkillDataImporter: Canvas - SkillTree → defaultTrees에 {treeSOs.Count}개 SkillTreeData 연결 완료.");
    }

    private static void CleanFolder(string folderPath)
    {
        string trimmed = folderPath.TrimEnd('/');
        string[] guids = AssetDatabase.FindAssets("", new[] { trimmed });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            // 폴더 자체는 삭제하지 않음
            if (!AssetDatabase.IsValidFolder(assetPath))
                AssetDatabase.DeleteAsset(assetPath);
        }
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Replace("\\", "/").TrimEnd('/').Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
