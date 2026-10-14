using UnityEngine;

/// <summary>
/// 영역 채움 ( 런타임 메쉬 ) - hole 이 있으면 테두리 띠만 채움
/// VFX 프리팹이 없을 때 피해 영역 기본 표시용
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class BossZoneFill : MonoBehaviour
{
    private Mesh mesh;
    private Color[] colors;

    public static BossZoneFill Create(ZoneShape shape, ZoneShape hole, Color color, string sortingLayerName, int sortingOrder)
    {
        GameObject go = new GameObject("BossZoneFill");
        BossZoneFill fill = go.AddComponent<BossZoneFill>();
        fill.Build(shape, hole, sortingLayerName, sortingOrder);
        fill.SetColor(color);
        return fill;
    }

    private void Build(ZoneShape shape, ZoneShape hole, string sortingLayerName, int sortingOrder)
    {
        Vector3[] outer = shape.GetCorners();
        Vector3[] vertices;
        int[] triangles;

        if (hole == null)
        {
            vertices = outer;
            triangles = new[] { 0, 1, 2, 0, 2, 3 };
        }
        else
        {
            // 바깥 4 + 안쪽 4, 변마다 사각형 띠
            Vector3[] inner = hole.GetCorners();
            vertices = new Vector3[8];
            for (int i = 0; i < 4; i++)
            {
                vertices[i] = outer[i];
                vertices[i + 4] = inner[i];
            }

            triangles = new int[24];
            for (int i = 0; i < 4; i++)
            {
                int next = (i + 1) % 4;
                int t = i * 6;
                triangles[t] = i;
                triangles[t + 1] = next;
                triangles[t + 2] = next + 4;
                triangles[t + 3] = i;
                triangles[t + 4] = next + 4;
                triangles[t + 5] = i + 4;
            }
        }

        mesh = new Mesh { name = "BossZoneFill" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        colors = new Color[vertices.Length];

        GetComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = BossTelegraph.GetDefaultMaterial();
        meshRenderer.sortingLayerName = sortingLayerName;
        meshRenderer.sortingOrder = sortingOrder;
    }

    public void SetColor(Color color)
    {
        for (int i = 0; i < colors.Length; i++) colors[i] = color;
        mesh.colors = colors;
    }

    private void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
    }
}
