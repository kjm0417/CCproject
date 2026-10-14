using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 삽 등으로 바뀔 타일을 반투명 잔상으로 보여주는 컴포넌트.
/// GroundTileModifier와 같은 오브젝트에 붙인다. ( 미리보기 Tilemap은 실행 중 자동 생성 )
/// </summary>
[RequireComponent(typeof(GroundTileModifier))]
public class GroundTilePreview : MonoBehaviour
{
    [Tooltip("잔상 색 ( 알파 = 기본 투명도 )")]
    [SerializeField]
    private Color previewColor = new Color(1f, 1f, 1f, 0.5f);

    [Tooltip("깜빡임 속도 ( 0이면 깜빡임 없음 )")]
    [SerializeField]
    private float pulseSpeed = 4f;

    [Tooltip("깜빡임 폭 ( 알파 증감량 )")]
    [SerializeField, Range(0f, 0.5f)]
    private float pulseAmount = 0.15f;

    [Tooltip("Ground 타일맵보다 위에 그릴 정렬 순서 차이")]
    [SerializeField]
    private int sortingOrderOffset = 1;

    private Tilemap previewTilemap;
    private TilemapRenderer previewRenderer;
    private Tilemap targetGround;

    private bool isShowing;
    private Vector3Int shownCell;
    private TileBase shownTile;

    /// <summary>
    /// ground 타일맵의 cell 위치에 tile 잔상 표시
    /// </summary>
    public void Show(Tilemap ground, Vector3Int cell, TileBase tile)
    {
        if (ground == null || tile == null)
        {
            Hide();
            return;
        }

        EnsurePreviewTilemap(ground);

        if (isShowing && cell == shownCell && tile == shownTile) return;

        if (isShowing) previewTilemap.SetTile(shownCell, null);
        previewTilemap.SetTile(cell, tile);

        shownCell = cell;
        shownTile = tile;
        isShowing = true;
    }

    public void Hide()
    {
        if (!isShowing) return;

        if (previewTilemap != null) previewTilemap.SetTile(shownCell, null);
        shownTile = null;
        isShowing = false;
    }

    private void Update()
    {
        if (!isShowing || previewTilemap == null) return;

        Color color = previewColor;
        if (pulseSpeed > 0f)
        {
            color.a = Mathf.Clamp01(previewColor.a + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount);
        }
        previewTilemap.color = color;
    }

    /// <summary>
    /// 대상 Ground 타일맵과 같은 위치 / 정렬로 미리보기 타일맵 준비 ( 구역이 바뀌면 옮김 )
    /// </summary>
    private void EnsurePreviewTilemap(Tilemap ground)
    {
        if (previewTilemap == null)
        {
            GameObject go = new GameObject("GroundTilePreview");
            previewTilemap = go.AddComponent<Tilemap>();
            previewRenderer = go.AddComponent<TilemapRenderer>();
        }

        if (targetGround == ground) return;

        // 이전 구역에 남은 잔상 정리
        Hide();
        targetGround = ground;

        Transform previewTransform = previewTilemap.transform;
        previewTransform.SetParent(ground.transform.parent, false);
        previewTransform.localPosition = ground.transform.localPosition;
        previewTransform.localRotation = ground.transform.localRotation;
        previewTransform.localScale = ground.transform.localScale;

        previewTilemap.tileAnchor = ground.tileAnchor;
        previewTilemap.orientation = ground.orientation;
        previewTilemap.color = previewColor;

        TilemapRenderer groundRenderer = ground.GetComponent<TilemapRenderer>();
        if (groundRenderer != null)
        {
            previewRenderer.sharedMaterial = groundRenderer.sharedMaterial;
            previewRenderer.sortingLayerID = groundRenderer.sortingLayerID;
            previewRenderer.sortingOrder = groundRenderer.sortingOrder + sortingOrderOffset;
        }
    }
}
