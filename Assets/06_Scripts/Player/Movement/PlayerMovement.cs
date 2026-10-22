using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour, IPlayerComponent
{
    [Header("입력")]
    [SerializeField]
    private VariableJoystick joystick; // UI 조이스틱. 인스펙터에서 연결한다.
    public VariableJoystick JoyStick => joystick; //외부 접근 용도

    private Rigidbody2D rb;
    private PlayerStats stats;
    private bool joystickWasEnabled = true;

    // 윈도우 버전 키보드 이동 (InputSystem_Actions의 Player/Move = 방향키)
    private InputAction keyboardMoveAction;

    public Vector2 MoveInput { get; private set; }
    public bool IsInputBlocked { get; private set; }

    public Vector2 FacingDirection { get; private set; } = Vector2.down;

    public bool IsMoving => MoveInput.sqrMagnitude > 0.0001f;

    // PlayerContext가 호출. 필요한 참조를 캐싱하고 탑다운 이동에 맞게 Rigidbody2D를 설정한다.
    public void Initialize(PlayerContext context)
    {
        rb = GetComponent<Rigidbody2D>();
        stats = context.Stats;

        // 탑다운(2D 부감) 이동: 중력 제거, 물리 충돌로 인한 회전 고정
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        keyboardMoveAction = InputSystem.actions != null ? InputSystem.actions.FindAction("Player/Move") : null;
    }

    private void Update()
    {
        if (IsInputBlocked)
        {
            MoveInput = Vector2.zero;
            return;
        }

        // 입력 읽기는 Update에서 (프레임마다 최신 입력 확보). 조이스틱을 쓰지 않으면 방향키 입력을 쓴다.
        MoveInput = (joystick != null) ? joystick.Direction : Vector2.zero;
        if (MoveInput.sqrMagnitude <= 0.0001f)
        {
            MoveInput = ReadKeyboardMove();
        }

        if (MoveInput.sqrMagnitude > 0.0001f)
        {
            FacingDirection = MoveInput.normalized;
        }
    }

    private void FixedUpdate()
    {
        // 물리 이동은 FixedUpdate에서. 즉시 이동(관성 없음): 원하는 속도를 그대로 지정한다.
        float speed = (stats != null) ? stats.MoveSpeed : 0f;
        rb.linearVelocity = IsInputBlocked ? Vector2.zero : MoveInput * speed;
    }

    private Vector2 ReadKeyboardMove()
    {
        if (keyboardMoveAction == null) return Vector2.zero;
        if (!keyboardMoveAction.enabled) keyboardMoveAction.Enable();
        return Vector2.ClampMagnitude(keyboardMoveAction.ReadValue<Vector2>(), 1f);
    }

    public void SetInputBlocked(bool blocked)
    {
        if (IsInputBlocked == blocked) return;

        IsInputBlocked = blocked;
        MoveInput = Vector2.zero;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (joystick == null) return;

        if (blocked)
        {
            joystickWasEnabled = joystick.enabled;
            joystick.OnPointerUp(null);
            joystick.enabled = false;
        }
        else
        {
            joystick.enabled = joystickWasEnabled;
        }
    }
}
