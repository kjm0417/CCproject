using System.Collections.Generic;

/// <summary>
/// ESC로 닫을 수 있는 UI 창. 창을 열 때 UIEscapeStack.Push(this)로 등록한다.
/// </summary>
public interface IEscapeClosable
{
    // 지금 화면에 열려 있는지
    bool IsEscapeOpen { get; }

    // 다른 단축키(상호작용·퀵슬롯·인벤토리·스킬)를 막는 창인지 (상점·요리·제작·상자 등)
    bool BlocksShortcuts { get; }

    // ESC를 눌렀을 때 닫기 버튼과 같은 처리를 한다. (제작 중처럼 닫을 수 없으면 그대로 둔다)
    void CloseByEscape();
}

/// <summary>
/// 열린 UI 창 목록. ESC는 가장 나중에 연 창부터 하나씩 닫는다.
/// 창이 닫기 버튼 등 다른 경로로 닫혀도 IsEscapeOpen으로 걸러내므로 따로 제거하지 않아도 된다.
/// </summary>
public static class UIEscapeStack
{
    private static readonly List<IEscapeClosable> windows = new List<IEscapeClosable>();

    /// <summary>
    /// 창이 열릴 때 호출. 이미 있으면 맨 위로 올린다.
    /// </summary>
    public static void Push(IEscapeClosable window)
    {
        if (window == null) return;
        windows.Remove(window);
        windows.Add(window);
    }

    /// <summary>
    /// 가장 위에 열린 창 하나를 닫는다. 닫을 창이 있었으면 true.
    /// </summary>
    public static bool CloseTop()
    {
        RemoveClosed();
        if (windows.Count == 0) return false;

        IEscapeClosable top = windows[windows.Count - 1];
        top.CloseByEscape();
        if (!IsAlive(top) || !top.IsEscapeOpen) windows.Remove(top);
        return true;
    }

    /// <summary>
    /// 열린 창이 하나라도 있는지
    /// </summary>
    public static bool HasOpenWindow()
    {
        RemoveClosed();
        return windows.Count > 0;
    }

    /// <summary>
    /// 단축키를 막는 창(상점·요리 등)이 열려 있는지
    /// </summary>
    public static bool HasBlockingWindow()
    {
        RemoveClosed();
        for (int i = 0; i < windows.Count; i++)
        {
            if (windows[i].BlocksShortcuts) return true;
        }
        return false;
    }

    private static void RemoveClosed()
    {
        for (int i = windows.Count - 1; i >= 0; i--)
        {
            IEscapeClosable window = windows[i];
            if (!IsAlive(window) || !window.IsEscapeOpen) windows.RemoveAt(i);
        }
    }

    // 씬 전환으로 파괴된 창 (Unity 오브젝트의 가짜 null) 걸러내기
    private static bool IsAlive(IEscapeClosable window)
    {
        if (window == null) return false;
        if (window is UnityEngine.Object unityObject) return unityObject != null;
        return true;
    }
}
