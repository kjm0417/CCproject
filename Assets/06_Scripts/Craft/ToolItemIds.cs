/// <summary>
/// 도구 아이템 ID 규칙 (2 X G YY): X = 도구 종류, G = 등급(1 일반, 2 고급...), YY = 고유 번호.
/// 강화 도구(등급 2 이상)도 같은 종류의 기본 도구처럼 판정하기 위해 등급 자리를 1로 바꾼 기본 ID를 쓴다.
/// </summary>
public static class ToolItemIds
{
    /// <summary>
    /// 등급 자리를 1로 바꾼 기본 도구 ID (21201 -> 21101). 도구 ID가 아니면 그대로 반환
    /// </summary>
    public static string ToBaseId(string itemId)
    {
        if (!IsToolId(itemId) || itemId[2] == '1') return itemId;
        return itemId.Substring(0, 2) + "1" + itemId.Substring(3);
    }

    /// <summary>
    /// 같은 종류의 도구인지 (등급 무시)
    /// </summary>
    public static bool IsSameTool(string itemId, string baseToolId)
    {
        return !string.IsNullOrEmpty(itemId) && ToBaseId(itemId) == baseToolId;
    }

    /// <summary>
    /// 아이콘 Resources 경로 (Resources/ItemIcons/{ItemID})
    /// </summary>
    public static string IconPath(string itemId)
    {
        return "ItemIcons/" + itemId;
    }

    private static bool IsToolId(string itemId)
    {
        return !string.IsNullOrEmpty(itemId) && itemId.Length == 5 && itemId[0] == '2';
    }
}
