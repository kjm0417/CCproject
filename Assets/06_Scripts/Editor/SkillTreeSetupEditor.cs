using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 명시적인 메뉴 또는 일회성 요청 파일로만 실행한다. 기존 씬은 수정하지 않는다.
[InitializeOnLoad]
public static class SkillTreeSetupEditor
{
    private const string PrefabPath = "Assets/04_Prefabs/UI-Canvas/Canvas - SkillTree.prefab";
    private const string DataFolder = "Assets/02_Data/SO/SkillTree/HoldSample";
    private const string RequestPath = "Temp/SkillTreeBuild.request";
    private const string ReportPath = "Temp/SkillTreeBuild.result.txt";

    static SkillTreeSetupEditor()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(RequestPath) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(RequestPath);
            try { Create(); RepairLoadedGauges(); Validate(); }
            catch (Exception exception) { File.WriteAllText(ReportPath, exception.ToString()); Debug.LogException(exception); }
        };
    }

    [MenuItem("Tools/Skill Tree/Repair Loaded Gauges")]
    public static void RepairLoadedGauges()
    {
        // 프리팹 재임포트 전부터 열려 있던 씬/Prefab Mode의 메모리 참조도 정리한다.
        // 다른 UI의 Image나 Sprite에는 접근하지 않는다.
        foreach (SkillTreeNodeUI node in Resources.FindObjectsOfTypeAll<SkillTreeNodeUI>())
        {
            if (EditorUtility.IsPersistent(node)) continue;
            var nodeObject = new SerializedObject(node);
            Image gauge = nodeObject.FindProperty("holdGauge").objectReferenceValue as Image;
            if (gauge == null) continue;
            var gaugeObject = new SerializedObject(gauge);
            gaugeObject.FindProperty("m_Sprite").objectReferenceValue = null;
            gaugeObject.FindProperty("m_Type").enumValueIndex = (int)Image.Type.Simple;
            gaugeObject.ApplyModifiedPropertiesWithoutUndo();
            gauge.overrideSprite = null;
            RectTransform rect = gauge.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(node.HoldProgress, 1f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            gauge.SetAllDirty();
            if (PrefabUtility.IsPartOfPrefabInstance(gauge))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(gauge);
                PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
            }
        }
    }

    [MenuItem("Tools/Skill Tree/Create Canvas Prefab")]
    public static void Create()
    {
        Directory.CreateDirectory(DataFolder);
        AssetDatabase.Refresh();
        SkillTreeData tree = CreateSample();
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            Debug.Log("Skill Tree: 기존 프리팹을 보존했습니다: " + PrefabPath);
            return;
        }

        var scene = EditorSceneManager.NewPreviewScene();
        GameObject root = new GameObject("Canvas - SkillTree", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 30;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
            RectTransform panel = Rect("SkillTreePanel", root.transform, Vector2.zero, Vector2.zero);
            Stretch(panel, new Vector2(35f, 35f), new Vector2(-35f, -35f));
            panel.gameObject.AddComponent<Image>().color = new Color(0.055f, 0.075f, 0.105f, 0.98f);
            SkillTreePanelUI ui = panel.gameObject.AddComponent<SkillTreePanelUI>();
            Label("Title", panel, "스킬 트리", new Vector2(170f, -45f), new Vector2(260f, 50f), 32);
            Label("Help", panel, "길게 눌러 습득 · 드래그하여 트리 이동", new Vector2(335f, -92f), new Vector2(590f, 36f), 20);
            TMP_Text points = Label("Points", panel, "SP: 0", new Vector2(770f, -45f), new Vector2(200f, 50f), 28);
            RectTransform close = Rect("CloseButton", panel, new Vector2(-48f, -45f), new Vector2(54f, 48f));
            close.anchorMin = close.anchorMax = Vector2.one;
            close.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.25f, 0.3f);
            Button closeButton = close.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = close.GetComponent<Image>();
            TMP_Text closeLabel = Label("Label", close, "X", Vector2.zero, new Vector2(54f, 48f), 24);
            closeLabel.rectTransform.anchorMin = closeLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            UnityEventTools.AddPersistentListener(closeButton.onClick, ui.Close);

            RectTransform scroll = Rect("Scroll View", panel, Vector2.zero, Vector2.zero);
            Stretch(scroll, new Vector2(20f, 20f), new Vector2(-20f, -130f));
            ScrollRect scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 45f;
            RectTransform viewport = Rect("Viewport", scroll, Vector2.zero, Vector2.zero);
            Stretch(viewport, Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<Image>().color = new Color(0.075f, 0.105f, 0.135f);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = Rect("Content", viewport, Vector2.zero, new Vector2(1120f, 600f));
            content.pivot = new Vector2(0f, 1f);
            scrollRect.viewport = viewport;
            scrollRect.content = content;

            SkillTreeNodeUI template = CreateNode(panel);
            Set(ui, "pointsText", points);
            Set(ui, "scrollRect", scrollRect);
            Set(ui, "content", content);
            Set(ui, "nodeTemplate", template);
            SerializedObject serialized = new SerializedObject(ui);
            serialized.FindProperty("defaultTrees").arraySize = 1;
            serialized.FindProperty("defaultTrees").GetArrayElementAtIndex(0).objectReferenceValue = tree;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            template.gameObject.SetActive(false);
            int index = 0;
            foreach (SkillTreeData.Node node in tree.Nodes)
            {
                if (node.Parent != null) continue;
                SkillTreeNodeUI preview = UnityEngine.Object.Instantiate(template, content);
                preview.name = "Preview_" + node.Skill.SkillId;
                Set(preview, "skill", node.Skill);
                ((RectTransform)preview.transform).anchoredPosition = new Vector2(200f + 240f * index++, -155f);
                SerializedObject so = new SerializedObject(preview);
                ((TMP_Text)so.FindProperty("nameText").objectReferenceValue).text = node.Skill.DisplayName;
                preview.gameObject.SetActive(true);
            }
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Skill Tree Canvas 생성 완료: " + PrefabPath);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    private static SkillTreeData CreateSample()
    {
        string treePath = DataFolder + "/FourRoots.asset";
        SkillTreeData existing = AssetDatabase.LoadAssetAtPath<SkillTreeData>(treePath);
        if (existing != null) return existing;
        string[] categories = { "전투", "생존", "채집", "탐험" };
        var skills = new List<SkillData>();
        var parentIndices = new List<int>();
        for (int group = 0; group < 4; group++)
        {
            int start = skills.Count;
            for (int node = 0; node < 5; node++)
            {
                var skill = ScriptableObject.CreateInstance<SkillData>();
                var so = new SerializedObject(skill);
                so.FindProperty("skillId").stringValue = $"sample_{group}_{node}";
                so.FindProperty("displayName").stringValue = node == 0 ? categories[group] : $"{categories[group]} {node}";
                so.FindProperty("description").stringValue = "스킬 트리 동작 확인용. 실제 효과는 추후 연결합니다.";
                so.FindProperty("pointCost").intValue = 1;
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(skill, $"{DataFolder}/Skill_{group}_{node}.asset");
                skills.Add(skill);
                parentIndices.Add(node == 0 ? -1 : node <= 2 ? start : start + 1);
            }
        }
        SkillTreeData tree = ScriptableObject.CreateInstance<SkillTreeData>();
        var treeObject = new SerializedObject(tree);
        treeObject.FindProperty("displayName").stringValue = "기본 스킬 트리 (4종 샘플)";
        SerializedProperty nodes = treeObject.FindProperty("nodes");
        nodes.arraySize = skills.Count;
        for (int i = 0; i < skills.Count; i++)
        {
            SerializedProperty node = nodes.GetArrayElementAtIndex(i);
            node.FindPropertyRelative("skill").objectReferenceValue = skills[i];
            node.FindPropertyRelative("parent").objectReferenceValue = parentIndices[i] < 0 ? null : skills[parentIndices[i]];
        }
        treeObject.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(tree, treePath);
        return tree;
    }

    private static SkillTreeNodeUI CreateNode(Transform parent)
    {
        RectTransform rect = Rect("NodeTemplate", parent, Vector2.zero, new Vector2(204f, 150f));
        Image background = rect.gameObject.AddComponent<Image>();
        background.color = new Color(0.14f, 0.23f, 0.29f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        SkillTreeNodeUI node = rect.gameObject.AddComponent<SkillTreeNodeUI>();
        TMP_Text name = Label("Name", rect, "스킬", new Vector2(102f, -32f), new Vector2(184f, 40f), 25);
        TMP_Text cost = Label("Cost", rect, "1 SP", new Vector2(102f, -68f), new Vector2(184f, 30f), 20);
        TMP_Text status = Label("Status", rect, "길게 눌러 습득", new Vector2(102f, -101f), new Vector2(190f, 28f), 17);
        RectTransform gaugeBack = Rect("GaugeBackground", rect, new Vector2(102f, -132f), new Vector2(180f, 10f));
        gaugeBack.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.08f, 0.1f);
        Image gauge = Rect("HoldGauge", gaugeBack, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
        Stretch(gauge.rectTransform, Vector2.zero, Vector2.zero);
        // Sprite를 사용하지 않는 단색 게이지. 노드가 anchorMax.x로 진행률을 표시한다.
        gauge.type = Image.Type.Simple;
        gauge.rectTransform.anchorMax = new Vector2(0f, 1f);
        gauge.rectTransform.offsetMin = Vector2.zero;
        gauge.rectTransform.offsetMax = Vector2.zero;
        gauge.color = new Color(0.35f, 0.95f, 0.72f);
        gauge.raycastTarget = false;
        Set(node, "nameText", name);
        Set(node, "costText", cost);
        Set(node, "statusText", status);
        Set(node, "holdGauge", gauge);
        return node;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = min;
        rect.offsetMax = max;
    }

    private static TMP_Text Label(string name, Transform parent, string text, Vector2 position, Vector2 size, float fontSize)
    {
        TextMeshProUGUI label = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/08_Pont/yuhanKimbery SDF.asset");
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.9f, 0.95f, 0.97f);
        label.raycastTarget = false;
        return label;
    }

    private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("Tools/Skill Tree/Validate Hold And Branches")]
    public static void Validate()
    {
        File.WriteAllText(ReportPath, "RUNNING: hold, branch and UI rendering validation.");
        var renderingErrors = new List<string>();
        Application.LogCallback captureError = (message, stack, type) =>
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                renderingErrors.Add(message + "\n" + stack);
        };
        Application.logMessageReceived += captureError;
        var scene = EditorSceneManager.NewPreviewScene();
        GameObject fixture = new GameObject("SkillTestFixture");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fixture, scene);
        try
        {
            PlayerProgression progression = fixture.AddComponent<PlayerProgression>();
            PlayerSkills player = fixture.AddComponent<PlayerSkills>();
            typeof(PlayerSkills).GetField("progression", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, progression);
            SkillTreeData tree = AssetDatabase.LoadAssetAtPath<SkillTreeData>(DataFolder + "/FourRoots.asset");
            Check(tree.TryValidate(out string error), error);
            player.ConfigureTrees(new[] { tree });
            GameObject canvas = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
            SkillTreePanelUI panel = canvas.GetComponentInChildren<SkillTreePanelUI>(true);
            VerifyGraphics(canvas);
            panel.Initialize(player);
            Check(Visible(canvas) == 4, "Initially four roots");
            SkillTreeNodeUI view = Array.Find(canvas.GetComponentsInChildren<SkillTreeNodeUI>(), n => n.Skill == tree.Nodes[0].Skill);
            var pointer = new PointerEventData(null) { pointerId = -1, button = PointerEventData.InputButton.Left };
            MethodInfo advance = typeof(SkillTreeNodeUI).GetMethod("AdvanceHold", BindingFlags.Instance | BindingFlags.NonPublic);
            view.OnPointerDown(pointer);
            advance.Invoke(view, new object[] { 0.5f });
            Check(view.HoldProgress > 0f && !player.HasLearned(view.Skill), "Partial hold does not learn");
            Image gauge = (Image)new SerializedObject(view).FindProperty("holdGauge").objectReferenceValue;
            Check(gauge.type == Image.Type.Simple &&
                new SerializedObject(gauge).FindProperty("m_Sprite").objectReferenceValue == null,
                "Gauge does not depend on a Sprite");
            Check(Mathf.Approximately(gauge.rectTransform.anchorMax.x, 0.5f), "Half hold renders half width");
            VerifyGraphics(canvas);
            view.OnPointerUp(pointer);
            Check(view.HoldProgress == 0f, "Release resets gauge");
            Check(gauge.rectTransform.anchorMax.x == 0f, "Release resets visible width");
            view.OnPointerDown(pointer);
            advance.Invoke(view, new object[] { 1.1f });
            Check(!player.HasLearned(view.Skill) && view.HoldProgress == 0f && Visible(canvas) == 4, "Insufficient points resets without unlock");
            progression.Load(1, 0, 5);
            advance.Invoke(view, new object[] { 2f });
            Check(!player.HasLearned(view.Skill), "Failed hold cannot retry without release");
            view.OnPointerUp(pointer);
            view.OnPointerDown(pointer);
            advance.Invoke(view, new object[] { 1.1f });
            Check(player.HasLearned(view.Skill) && progression.SkillPoints == 4 && Visible(canvas) == 6, "Hold learns once and reveals two children");
            Check(!player.TryLearn(view.Skill) && progression.SkillPoints == 4, "Duplicate does not charge");
            SkillTreeNodeUI child = Array.Find(canvas.GetComponentsInChildren<SkillTreeNodeUI>(), n => n.Skill == tree.Nodes[1].Skill);
            child.OnPointerDown(pointer);
            advance.Invoke(child, new object[] { 0.5f });
            child.OnBeginDrag(pointer);
            advance.Invoke(child, new object[] { 1.1f });
            Check(!player.HasLearned(child.Skill) && child.HoldProgress == 0f, "Scroll drag cancels hold");
            child.OnEndDrag(pointer);
            Check(player.TryLearn(child.Skill) && Visible(canvas) == 8, "Next depth reveals grandchildren");
            var saved = player.CreateSaveData();
            player.Load(null);
            player.Load(saved);
            Check(player.HasLearned(view.Skill) && player.HasLearned(child.Skill), "Save restore");
            RectTransform content = canvas.GetComponentInChildren<ScrollRect>().content;
            Check(content.sizeDelta.x > 1120f && content.sizeDelta.y > 600f, "Content expands in both directions");
            VerifyGraphics(canvas);
            panel.Close();
            panel.Open();
            panel.Initialize(player);
            Check(Visible(canvas) == 8, "Rebuild preserves visible nodes");
            VerifyGraphics(canvas);
        }
        catch (Exception exception)
        {
            File.WriteAllText(ReportPath, "FAIL: " + exception);
            throw;
        }
        finally
        {
            try { EditorSceneManager.ClosePreviewScene(scene); }
            finally { Application.logMessageReceived -= captureError; }
        }
        if (renderingErrors.Count > 0)
        {
            string failure = "FAIL: Unity logged UI errors:\n" + string.Join("\n", renderingErrors);
            File.WriteAllText(ReportPath, failure);
            throw new InvalidOperationException(failure);
        }
        File.WriteAllText(ReportPath, "PASS: sprite-free gauge rendering, half/zero width, prefab instantiation, canvas mesh rebuild, panel reopen/reinitialize, four roots, hold/release, insufficient points, retry guard, point spending, duplicate guard, child/grandchild reveal, drag cancellation, save restore, content expansion. Unity errors/exceptions: 0.");
        Debug.Log("Skill Tree validation: PASS (including UI rendering; errors/exceptions: 0)");
    }

    private static void VerifyGraphics(GameObject canvas)
    {
        Canvas.ForceUpdateCanvases();
        foreach (Image image in canvas.GetComponentsInChildren<Image>(true))
        {
            // 에디터는 정상적으로 비워 둔 필드도 fake-null 래퍼로 반환할 수 있다.
            // 직렬화된 참조를 확인한 뒤 실제 텍스처/메시 생성까지 검사한다.
            Sprite sprite = image.sprite;
            UnityEngine.Object spriteReference = new SerializedObject(image).FindProperty("m_Sprite").objectReferenceValue;
            if (spriteReference != null) Check(sprite != null && sprite.texture != null, image.name + ": broken sprite");
            Sprite overridden = image.overrideSprite;
            if (overridden != null) Check(overridden.texture != null, image.name + ": broken override sprite");
        }
        foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>())
        {
            graphic.SetAllDirty();
            graphic.Rebuild(CanvasUpdate.PreRender);
        }
        Canvas.ForceUpdateCanvases();
    }

    private static int Visible(GameObject canvas) => canvas.GetComponentsInChildren<SkillTreeNodeUI>().Length;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Skill Tree validation failed: " + message);
    }
}
