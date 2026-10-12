using TMPro;
using UnityEngine;

/// <summary>
/// 던전 진행 시간 표시 ( mm:ss ) - 클리어 시 클리어 시간에서 멈춤
/// </summary>
public class DungeonTimerUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI timeText;

    private int lastSeconds = -1;

    private void Update()
    {
        DungeonProgressManager manager = DungeonProgressManager.Instance;
        if (manager == null) return;

        int seconds = Mathf.FloorToInt(manager.ElapsedTime);
        if (seconds == lastSeconds) return; //초 단위 변경 시에만 갱신

        lastSeconds = seconds;
        timeText.text = Format(manager.ElapsedTime);
    }

    /// <summary>
    /// 03:42 / 1시간 이상은 1:03:42
    /// </summary>
    public static string Format(float time)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(time));
        int hours = total / 3600;
        int minutes = total % 3600 / 60;
        int seconds = total % 60;

        return hours > 0 ? $"{hours}:{minutes:00}:{seconds:00}" : $"{minutes:00}:{seconds:00}";
    }
}
