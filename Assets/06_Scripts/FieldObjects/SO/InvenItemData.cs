using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "InvenItemData", menuName = "Game/InvenItemData")]
public class InvenItemData : ScriptableObject
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

