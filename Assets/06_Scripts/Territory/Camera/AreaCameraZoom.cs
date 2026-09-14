using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class AreaCameraZoom : MonoBehaviour
{
    [SerializeField]
    private CinemachineCamera cinemachineCamera;

    [SerializeField]
    private float minOrthographicSize;

    [SerializeField]
    private float maxOrthographicSize;

    [SerializeField]
    private float zoomSpeed; //스크롤 1칸당 줌 크기

    [SerializeField]
    private float smoothSpeed = 5f; //Lerp 속도

    private float targetOrthographicSize;

    public bool IsZoomMode { get; private set; }
    private void Start()
    {
        if (cinemachineCamera == null)
            cinemachineCamera = GetComponent<CinemachineCamera>();

        targetOrthographicSize = cinemachineCamera.Lens.OrthographicSize;
    }

    private void Update()
    {
        if (!IsZoomMode) return;
        // 일반 탐색 모드에서도 줌 기능 동작 (IsZoomMode 체크 제거)
        HandleZoomInput();
        ApplySmoothZoom();
    }

    public void IsZoomModeChanged(bool isMode)
    {
        IsZoomMode = isMode;
    }

    /// <summary>
    /// 줌 입력 처리 (PC 에디터 테스트용 마우스 스크롤만 처리, 터치는 PlayerCameraMove에서 처리)
    /// </summary>
    private void HandleZoomInput()
    {
        // 마우스 스크롤휠
        float scrollInput = Mouse.current.scroll.ReadValue().y;

        if (scrollInput != 0)
        {
            targetOrthographicSize -= (scrollInput / 120f) * zoomSpeed;
            targetOrthographicSize = Mathf.Clamp(targetOrthographicSize, minOrthographicSize, maxOrthographicSize);

            Debug.Log($"[PlayerCameraZoom] 마우스 줌 크기: {targetOrthographicSize}");
        }
    }

    private void ApplySmoothZoom()
    {
        if (cinemachineCamera == null) return;

        var lens = cinemachineCamera.Lens;
        lens.OrthographicSize = Mathf.Lerp(
            lens.OrthographicSize,
            targetOrthographicSize,
            Time.deltaTime * smoothSpeed
        );
        cinemachineCamera.Lens = lens;
    }

    /// <summary>
    /// 즉시 줌 적용
    /// </summary>
    public void SetZoomImmediate(float size)
    {
        maxOrthographicSize = size;

        targetOrthographicSize = Mathf.Clamp(size, minOrthographicSize, maxOrthographicSize);
        var lens = cinemachineCamera.Lens;
        lens.OrthographicSize = targetOrthographicSize;
        cinemachineCamera.Lens = lens;
    }

    /// <summary>
    /// 부드러운 줌 적용 (Lerp)
    /// </summary>
    public void SetZoomTarget(float size)
    {
        targetOrthographicSize = Mathf.Clamp(size, minOrthographicSize, maxOrthographicSize);
        // ApplySmoothZoom()에서 Lerp로 부드럽게 변경됨
    }

    public void SetLenSize(float size)
    {
        var lens = cinemachineCamera.Lens;
        lens.OrthographicSize = size;
        cinemachineCamera.Lens = lens;
    }

}
