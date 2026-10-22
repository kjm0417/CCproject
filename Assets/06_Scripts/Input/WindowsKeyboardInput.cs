using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 윈도우(키보드) 단축키. InputSystem_Actions(프로젝트 전역 액션)의 Player 맵을 읽어
/// HUD 버튼을 누른 것과 같은 동작을 실행한다. 이동(방향키)은 PlayerMovement가 직접 읽는다.
///
///   Space  : 상호작용 버튼          1~8 : 퀵슬롯 1~8번 (8번 = 인벤토리 버튼)
///   I      : 인벤토리 열기/닫기      K   : 스킬 트리 열기/닫기
///   ESC    : 가장 나중에 연 UI 닫기
///
/// 씬에 배치할 필요 없이 게임 시작 시 자동으로 하나 생성되어 씬 전환 후에도 유지된다.
/// </summary>
public class WindowsKeyboardInput : MonoBehaviour
{
    private const string MapName = "Player";
    private const int QuickSlotKeyCount = 8;

    private static WindowsKeyboardInput instance;

    private InputAction interactAction;
    private InputAction inventoryAction;
    private InputAction skillTreeAction;
    private InputAction closeUIAction;
    private readonly InputAction[] quickSlotActions = new InputAction[QuickSlotKeyCount];

    private UIHUD hud;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (instance != null) return;

        GameObject go = new GameObject(nameof(WindowsKeyboardInput));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<WindowsKeyboardInput>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        BindActions();
    }

    private void BindActions()
    {
        InputActionAsset actions = InputSystem.actions;
        if (actions == null)
        {
            Debug.LogWarning("WindowsKeyboardInput: 프로젝트 전역 Input Actions가 설정되지 않았습니다. (Project Settings > Input System Package)");
            enabled = false;
            return;
        }

        interactAction = Find(actions, "Interact");
        inventoryAction = Find(actions, "Inventory");
        skillTreeAction = Find(actions, "SkillTree");
        closeUIAction = Find(actions, "CloseUI");
        for (int i = 0; i < QuickSlotKeyCount; i++)
        {
            quickSlotActions[i] = Find(actions, $"QuickSlot{i + 1}");
        }
    }

    private static InputAction Find(InputActionAsset actions, string actionName)
    {
        InputAction action = actions.FindAction($"{MapName}/{actionName}");
        if (action == null) Debug.LogWarning($"WindowsKeyboardInput: {MapName}/{actionName} 액션이 없습니다.");
        return action;
    }

    private void Update()
    {
        ClearButtonFocus();

        // ESC는 어떤 상황에서도 가장 위의 창을 닫는다.
        if (WasPressed(closeUIAction))
        {
            UIEscapeStack.CloseTop();
            return;
        }

        bool inventoryPressed = WasPressed(inventoryAction);
        bool skillTreePressed = WasPressed(skillTreeAction);
        bool interactPressed = WasPressed(interactAction);
        int quickSlotPressed = -1;
        for (int i = 0; i < QuickSlotKeyCount; i++)
        {
            if (WasPressed(quickSlotActions[i]) && quickSlotPressed < 0) quickSlotPressed = i;
        }

        if (!inventoryPressed && !skillTreePressed && !interactPressed && quickSlotPressed < 0) return;

        // 상점·요리·제작·상자 등이 열려 있으면 ESC 외 단축키는 쓰지 않는다.
        if (UIEscapeStack.HasBlockingWindow()) return;

        UIHUD currentHud = ResolveHud();
        if (currentHud == null || !currentHud.IsInitialized) return;

        HUDBottomPanel bottom = currentHud.BottomPanel;
        HUDTopPanel top = currentHud.TopPanel;

        if (inventoryPressed && bottom != null) bottom.ToggleInventoryByKey();
        if (skillTreePressed && top != null) top.ToggleSkills();
        if (quickSlotPressed >= 0 && bottom != null) bottom.PressQuickSlot(quickSlotPressed);

        // 상호작용은 인벤토리·스킬 창도 닫혀 있고 플레이어가 묶여 있지 않을 때만
        if (interactPressed && bottom != null && CanInteract(currentHud))
        {
            bottom.PressInteractButton();
        }
    }

    private static bool CanInteract(UIHUD currentHud)
    {
        if (UIEscapeStack.HasOpenWindow()) return false;

        PlayerContext context = currentHud.Context;
        return context == null || context.Movement == null || !context.Movement.IsInputBlocked;
    }

    // 마우스로 누른 버튼이 선택 상태로 남으면 방향키·Enter가 그 버튼을 움직이거나 다시 누른다.
    // 게임 키와 겹치지 않도록 버튼 선택은 매 프레임 해제한다. (글자 입력칸은 유지)
    private static void ClearButtonFocus()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) return;

        GameObject selected = eventSystem.currentSelectedGameObject;
        if (selected == null) return;
        if (selected.GetComponent<TMPro.TMP_InputField>() != null) return;
        if (selected.GetComponent<UnityEngine.UI.InputField>() != null) return;

        eventSystem.SetSelectedGameObject(null);
    }

    private UIHUD ResolveHud()
    {
        if (hud == null) hud = FindAnyObjectByType<UIHUD>();
        return hud;
    }

    private static bool WasPressed(InputAction action)
    {
        if (action == null) return false;

        // 플레이어의 PlayerInput이 꺼지면(씬 전환 등) 같은 액션 에셋이 비활성화될 수 있어 다시 켠다.
        if (!action.enabled) action.Enable();
        return action.WasPressedThisFrame();
    }
}
