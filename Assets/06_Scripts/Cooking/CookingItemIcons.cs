using UnityEngine;

public static class CookingItemIcons
{
    public static Sprite Resolve(InvenItemData item)
    {
        if (item == null) return null;
        if (item.Icon != null) return item.Icon;
        string path;
        switch (item.ItemID)
        {
            case "11001": path = "Log"; break;
            case "11002": path = "Gravel"; break;
            case "11003": path = "Coal"; break;
            case "13001": path = "Apple"; break;
            case "13002": path = "Broil_Apple"; break;
            case "13003": path = "Garlic"; break;
            case "13004": path = "Plum"; break;
            case "13005": path = "Berly"; break;
            case "13006": path = "Onion"; break;
            case "13007": path = "Mullbery"; break;
            default: return null;
        }
        return Resources.Load<Sprite>("Inven_Drop/" + path);
    }
}
