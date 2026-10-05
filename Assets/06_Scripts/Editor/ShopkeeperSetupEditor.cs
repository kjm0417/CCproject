using NavMeshPlus.Components;
using NavMeshPlus.Extensions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ShopkeeperSetupEditor
{
    [MenuItem("Tools/Shopkeeper/Create Scene Setup")]
    private static void CreateSceneSetup()
    {
        ShopkeeperAI shopkeeper = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<ShopkeeperAI>() : null;
        if (shopkeeper == null || EditorUtility.IsPersistent(shopkeeper)
            || PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            Debug.LogWarning("씬에 배치한 Squirrel 오브젝트를 선택한 뒤 실행해 주세요.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create Shopkeeper Scene Setup");

        GameObject setup = CreateObject(shopkeeper.name + "_ShopSetup", null, shopkeeper.transform.position);
        GameObject standPoint = CreateObject("ShopStandPoint", setup.transform, shopkeeper.transform.position);
        GameObject areaObject = CreateObject("ShopArea", setup.transform, shopkeeper.transform.position + Vector3.down);
        BoxCollider2D areaCollider = Undo.AddComponent<BoxCollider2D>(areaObject);
        areaCollider.isTrigger = true;
        areaCollider.size = new Vector2(4f, 3f);
        Rigidbody2D areaBody = Undo.AddComponent<Rigidbody2D>(areaObject);
        areaBody.bodyType = RigidbodyType2D.Kinematic;
        areaBody.gravityScale = 0f;
        ShopArea area = Undo.AddComponent<ShopArea>(areaObject);

        GameObject pointsRoot = CreateObject("WanderPoints", setup.transform, shopkeeper.transform.position);
        Vector3[] offsets = { Vector3.left * 4f, Vector3.right * 4f, Vector3.up * 4f, Vector3.down * 4f };
        Transform[] points = new Transform[offsets.Length];
        for (int pointIndex = 0; pointIndex < offsets.Length; pointIndex++)
        {
            points[pointIndex] = CreateObject("WanderPoint_" + (pointIndex + 1), pointsRoot.transform,
                shopkeeper.transform.position + offsets[pointIndex]).transform;
        }

        Undo.RecordObject(shopkeeper, "Assign Shopkeeper Destinations");
        shopkeeper.Configure(points, standPoint.transform, area);
        PrefabUtility.RecordPrefabInstancePropertyModifications(shopkeeper);
        EditorUtility.SetDirty(shopkeeper);
        EditorSceneManager.MarkSceneDirty(shopkeeper.gameObject.scene);
        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = setup;
        Debug.Log("Shopkeeper: 복귀 지점, 상점 영역, 배회 지점을 만들었습니다. 맵에 맞춰 배치한 뒤 2D NavMesh를 Bake해 주세요.", setup);
    }

    [MenuItem("Tools/Shopkeeper/Create 2D Navigation Surface")]
    private static void CreateNavigationSurface()
    {
        GameObject navigation = CreateObject("ShopkeeperNavigation", null, Vector3.zero);
        navigation.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        NavMeshSurface surface = Undo.AddComponent<NavMeshSurface>(navigation);
        surface.collectObjects = CollectObjects.All;
        surface.overrideVoxelSize = true;
        surface.voxelSize = 0.05f;
        Undo.AddComponent<CollectSources2d>(navigation);
        Selection.activeGameObject = navigation;
    }

    [MenuItem("Tools/Shopkeeper/Mark Selected As Walkable")]
    private static void MarkWalkable()
    {
        MarkSelectedArea(0);
    }

    [MenuItem("Tools/Shopkeeper/Mark Selected As Obstacle")]
    private static void MarkObstacle()
    {
        MarkSelectedArea(1);
    }

    private static void MarkSelectedArea(int area)
    {
        foreach (GameObject selected in Selection.gameObjects)
        {
            NavMeshModifier modifier = selected.GetComponent<NavMeshModifier>();
            if (modifier == null) modifier = Undo.AddComponent<NavMeshModifier>(selected);
            Undo.RecordObject(modifier, "Mark Navigation Area");
            modifier.overrideArea = true;
            modifier.area = area;
            modifier.ignoreFromBuild = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(modifier);
            EditorUtility.SetDirty(modifier);
            EditorSceneManager.MarkSceneDirty(selected.scene);
        }
    }

    private static GameObject CreateObject(string objectName, Transform parent, Vector3 position)
    {
        GameObject created = new GameObject(objectName);
        Undo.RegisterCreatedObjectUndo(created, "Create " + objectName);
        if (parent != null) Undo.SetTransformParent(created.transform, parent, "Parent " + objectName);
        created.transform.position = position;
        return created;
    }
}
