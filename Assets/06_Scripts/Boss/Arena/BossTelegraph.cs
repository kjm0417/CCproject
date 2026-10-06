using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 경고 테두리 스타일 - 패턴별로 인스펙터에서 설정
/// </summary>
[Serializable]
public class TelegraphStyle
{
    public Color color = Color.red;
    [Min(0.01f)] public float width = 0.08f;
    public string sortingLayerName = "Default";
    public int sortingOrder = 100;
    [Tooltip("비어 있으면 기본 스프라이트 머티리얼 사용")]
    public Material material;
}

/// <summary>
/// 공격 예고용 빨간 테두리 ( LineRenderer ) - 별도 프리팹 없이 런타임 생성
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class BossTelegraph : MonoBehaviour
{
    private static Material defaultMaterial;

    private LineRenderer line;

    public static BossTelegraph Create(ZoneShape shape, TelegraphStyle style, Transform parent)
    {
        GameObject go = new GameObject("BossTelegraph");
        go.transform.SetParent(parent, false);

        BossTelegraph telegraph = go.AddComponent<BossTelegraph>();
        telegraph.Setup(style);
        telegraph.SetShape(shape);
        telegraph.SetVisible(false);
        return telegraph;
    }

    private void Setup(TelegraphStyle style)
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.positionCount = 4;
        line.startWidth = line.endWidth = style.width;
        line.startColor = line.endColor = style.color;
        line.sortingLayerName = style.sortingLayerName;
        line.sortingOrder = style.sortingOrder;
        line.sharedMaterial = style.material != null ? style.material : GetDefaultMaterial();
    }

    public void SetShape(ZoneShape shape)
    {
        line.SetPositions(shape.GetCorners());
    }

    public void SetVisible(bool visible)
    {
        line.enabled = visible;
    }

    /// <summary> count 회 점멸 후 꺼진 상태로 종료 </summary>
    public IEnumerator Blink(int count, float onTime, float offTime)
    {
        for (int i = 0; i < count; i++)
        {
            SetVisible(true);
            yield return new WaitForSeconds(onTime);
            SetVisible(false);
            yield return new WaitForSeconds(offTime);
        }
    }

    public void Release()
    {
        Destroy(gameObject);
    }

    private static Material GetDefaultMaterial()
    {
        if (defaultMaterial != null) return defaultMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        defaultMaterial = new Material(shader);
        return defaultMaterial;
    }
}
