using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 구조물 오브젝트 시트 중 던전 입구 행 ( DungeonID 있는 행 )
/// </summary>
[System.Serializable]
public class StructDungeonDTO
{
    public string StructID;
    public string DungeonID;
    public string DungeonName;
    public string SceneName;
    public int RecommendLevel;
    public string MonsterInfo;
    public int RequireSubmit; //1 : 첫 클리어 전까지 제출 / 0 : 제출 없음
    public string SubmitItems; //아이템ID:수량;아이템ID:수량
    public float FailRefundRatio;
    public float FirstExp;
    public int FirstGold;
    public string FirstItems;
    public float RepeatExp;
    public int RepeatGold;
    public string RepeatItems;
}

/// <summary>
/// StructObj.json -> DungeonData SO
/// 같은 DungeonID 의 기존 에셋이 있으면 덮어쓰지 않고 값만 갱신 ( 씬 / 프리팹 참조 유지 )
/// 시트에 없는 값 ( 랜덤 보상 테이블 등 ) 은 그대로 유지
/// </summary>
public static class DungeonDataImporter
{
    private const string JsonFilePath = "/02_Data/Json/StructObj/StructObj.json";
    private const string SOFolderPath = "Assets/02_Data/SO/DungeonData/";
    private const string ItemDatabasePath = "Assets/02_Data/SO/ItemDatabase/ItemDatabase.asset";

    [MenuItem("Tools/DungeonData 가져오기")]
    public static void Import()
    {
        string json = System.IO.File.ReadAllText(Application.dataPath + JsonFilePath);
        DTOList<StructDungeonDTO> data = JsonUtility.FromJson<DTOList<StructDungeonDTO>>("{\"items\":" + json + "}");

        ItemDatabase itemDatabase = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);
        Dictionary<string, DungeonData> existing = LoadExisting();

        int count = 0;
        foreach (StructDungeonDTO dto in data.items)
        {
            if (string.IsNullOrEmpty(dto.DungeonID)) continue; //던전 입구가 아닌 구조물

            if (!existing.TryGetValue(dto.DungeonID, out DungeonData so))
            {
                so = ScriptableObject.CreateInstance<DungeonData>();
                AssetDatabase.CreateAsset(so, SOFolderPath + dto.DungeonID + ".asset");
            }

            Map(dto, so, itemDatabase);
            EditorUtility.SetDirty(so);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[DungeonDataImporter] 던전 {count}개 갱신");
    }

    private static Dictionary<string, DungeonData> LoadExisting()
    {
        Dictionary<string, DungeonData> result = new Dictionary<string, DungeonData>();
        foreach (string guid in AssetDatabase.FindAssets("t:DungeonData", new[] { SOFolderPath.TrimEnd('/') }))
        {
            DungeonData so = AssetDatabase.LoadAssetAtPath<DungeonData>(AssetDatabase.GUIDToAssetPath(guid));
            if (so != null && !string.IsNullOrEmpty(so.DungeonID)) result[so.DungeonID] = so;
        }
        return result;
    }

    private static void Map(StructDungeonDTO dto, DungeonData so, ItemDatabase itemDatabase)
    {
        so.DungeonID = dto.DungeonID;
        so.DungeonName = dto.DungeonName;
        so.SceneName = dto.SceneName;
        so.RecommendLevel = dto.RecommendLevel;
        so.MonsterInfo = dto.MonsterInfo;
        so.RequireSubmitBeforeFirstClear = dto.RequireSubmit == 1;
        so.EntryCosts = ParseItems(dto.SubmitItems, itemDatabase, dto.DungeonID);
        so.FailRefundRatio = Mathf.Clamp01(dto.FailRefundRatio);

        MapReward(so.FirstClearReward, dto.FirstExp, dto.FirstGold, ParseItems(dto.FirstItems, itemDatabase, dto.DungeonID));
        MapReward(so.RepeatClearReward, dto.RepeatExp, dto.RepeatGold, ParseItems(dto.RepeatItems, itemDatabase, dto.DungeonID));
    }

    private static void MapReward(DungeonReward reward, float exp, int gold, List<DungeonItemAmount> items)
    {
        reward.Exp = exp;
        reward.Items = items;

        //골드만 시트 값으로 교체 ( 다른 재화는 유지 )
        reward.Currencies.RemoveAll(c => c.Type == CurrencyType.Gold);
        if (gold > 0) reward.Currencies.Add(new CurrencyAmount { Type = CurrencyType.Gold, Amount = gold });
    }

    /// <summary>
    /// "아이템ID:수량;아이템ID:수량" 파싱
    /// </summary>
    private static List<DungeonItemAmount> ParseItems(string text, ItemDatabase itemDatabase, string dungeonID)
    {
        List<DungeonItemAmount> result = new List<DungeonItemAmount>();
        if (string.IsNullOrWhiteSpace(text)) return result;

        foreach (string entry in text.Split(';'))
        {
            if (string.IsNullOrWhiteSpace(entry)) continue;

            string[] pair = entry.Split(':');
            string itemID = pair[0].Trim();
            int amount = pair.Length > 1 && int.TryParse(pair[1].Trim(), out int parsed) ? parsed : 1;

            InvenItemData item = FindItem(itemID, itemDatabase);
            if (item == null)
            {
                Debug.LogError($"[DungeonDataImporter] {dungeonID} - 아이템 {itemID} 없음");
                continue;
            }

            result.Add(new DungeonItemAmount { Item = item, Count = amount });
        }
        return result;
    }

    private static InvenItemData FindItem(string itemID, ItemDatabase itemDatabase)
    {
        InvenItemData item = itemDatabase != null ? itemDatabase.FindById(itemID) : null;
        if (item != null) return item;

        //ItemDatabase 미등록 아이템 - SO 폴더에서 직접 검색
        foreach (string guid in AssetDatabase.FindAssets("t:InvenItemData"))
        {
            InvenItemData candidate = AssetDatabase.LoadAssetAtPath<InvenItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (candidate != null && candidate.ItemID == itemID) return candidate;
        }
        return null;
    }
}
