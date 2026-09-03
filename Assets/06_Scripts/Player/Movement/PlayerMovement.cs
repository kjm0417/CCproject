using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour, IPlayerComponent
{
    [Header("입력")]
    [SerializeField]
    private VariableJoystick joystick; // UI 조이스틱. 인스펙터에서 연결한다.

    private Rigidbody2D rb;
    private PlayerStats stats;

    public Vector2 MoveInput { get; private set; }

    public bool IsMoving => MoveInput.sqrMagnitude > 0.0001f;

    // PlayerContext가 호출. 필요한 참조를 캐싱하고 탑다운 이동에 맞게 Rigidbody2D를 설정한다.
    public void Initialize(PlayerContext context)
    {
        rb = GetComponent<Rigidbody2D>();
        stats = context.Stats;

        // 탑다운(2D 부감) 이동: 중력 제거, 물리 충돌로 인한 회전 고정
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void Update()
    {
        // 입력 읽기는 Update에서 (프레임마다 최신 입력 확보). 조이스틱 미연결이면 정지.
        MoveInput = (joystick != null) ? joystick.Direction : Vector2.zero;
    }

    private void FixedUpdate()
    {
        // 물리 이동은 FixedUpdate에서. 즉시 이동(관성 없음): 원하는 속도를 그대로 지정한다.
        float speed = (stats != null) ? stats.MoveSpeed : 0f;
        rb.linearVelocity = MoveInput * speed;
    }
}
