using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 도구로 타일을 다른 타일로 바꾸는 규칙 하나 (예: 삽 + 평평한 흙 -> 파낸 흙)
/// </summary>
[Serializable]
public class TileConversionRule
{
    public ToolType ToolType = ToolType.Shovel; //사용해야 하는 도구
    public TileBase FromTile; //바꾸기 전 타일
    public List<TileBase> ToTiles = new List<TileBase>(); //바꾼 후 타일 후보 (랜덤으로 하나)

    /// <summary>
    /// 비어 있지 않은 후보 중 랜덤으로 하나 (없으면 null)
    /// </summary>
    public TileBase GetRandomToTile()
    {
        if (ToTiles == null || ToTiles.Count == 0) return null;

        int validCount = 0;
        foreach (TileBase tile in ToTiles)
        {
            if (tile != null) validCount++;
        }
        if (validCount == 0) return null;

        int pick = UnityEngine.Random.Range(0, validCount);
        foreach (TileBase tile in ToTiles)
        {
            if (tile == null) continue;
            if (pick-- == 0) return tile;
        }
        return null;
    }
}

/// <summary>
/// 도구별 타일 변환 규칙 목록
/// </summary>
[CreateAssetMenu(fileName = "TileConversionData", menuName = "Scriptable Objects/TileConversionData")]
public class TileConversionData : ScriptableObject
{
    [SerializeField]
    private List<TileConversionRule> rules = new List<TileConversionRule>();

    /// <summary>
    /// 도구와 현재 타일에 맞는 변환 결과 타일 찾기 (후보가 여러 개면 랜덤)
    /// </summary>
    public bool TryGetResult(ToolType toolType, TileBase currentTile, out TileBase resultTile)
    {
        resultTile = null;
        if (currentTile == null) return false;

        foreach (TileConversionRule rule in rules)
        {
            if (rule.ToolType != toolType || rule.FromTile != currentTile) continue;

            resultTile = rule.GetRandomToTile();
            if (resultTile != null) return true;
        }
        return false;
    }

    /// <summary>
    /// 저장된 타일 이름으로 변환 결과 타일 찾기 (로드용)
    /// </summary>
    public TileBase FindResultTile(string tileName)
    {
        foreach (TileConversionRule rule in rules)
        {
            if (rule.ToTiles == null) continue;

            foreach (TileBase tile in rule.ToTiles)
            {
                if (tile != null && tile.name == tileName)
                    return tile;
            }
        }
        return null;
    }
}
