using UnityEditor;
using UnityEngine;

[System.Serializable]
public class InvenItemDTO
{
    public string ItemID;
    public string ItemName;
    public string ItemType;
    public string SubType;
    public string Description;
    public int MaxStack;
    public int SellPrice;
    public int MinBuyGold;
    public int MaxBuyGold;
    public int BuyDiamond;
}
public class InvenItemImporter : BaseDataImporter<InvenItemDTO, InvenItemData>
{
    protected override string JsonFilePath => "/02_Data/Json/InvenItem/InvenItem.json";
    public override string SOFolderPath => "Assets/02_Data/SO/InvenItem/";

    protected override void MapDTOToSO(InvenItemDTO dto, InvenItemData so)
    {
        so.ItemID = dto.ItemID;
        so.ItemName = dto.ItemName;
        so.ItemType = dto.ItemType;
        so.SubType = dto.SubType;
        so.Description = dto.Description;
        so.MaxStack = dto.MaxStack;
        so.SellPrice = dto.SellPrice;
        so.MinBuyGold = dto.MinBuyGold;
        so.MaxBuyGold = dto.MaxBuyGold;
        so.BuyDiamond = dto.BuyDiamond;
    }

    protected override string GetDTOID(InvenItemDTO dto)
    {
        return dto.ItemID;
    }

    [MenuItem("Tools/InvenItem 가져오기")]
    public static void Import()
    {
        BaseDataImporter<InvenItemDTO, InvenItemData> importer = new InvenItemImporter();
        importer.Import();
    }
}
