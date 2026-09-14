using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

/// <summary>
/// 모바일 터치 입력으로 카메라 줌 및 이동 제어
/// 1-finger 드래그 (위/아래): 줌 인/아웃
/// 2-finger 드래그 (상/하/좌/우): 카메라 이동
/// </summary>
public class PlayerCameraMove : MonoBehaviour
{
    [SerializeField]
    private CinemachineCamera cinemachineCamera;

    [SerializeField]
    private AreaCameraZoom playerCameraZoom;

    [SerializeField]
    private float dragSensitivity = 0.1f; // 드래그 감도

    [SerializeField]
    private float zoomSensitivity = 0.01f; // 줌 감도

    private Vector2 touchStartPos;
    private Vector2 prevTouchPos;
    private Transform prevFollowTarget; // Follow 복구용

    private void Update()
    {
        // 에디터: 마우스 입력도 처리 (Unity Remote 호환)
        HandleMouseInput();

        // 모바일/Unity Remote: 터치 입력 처리
        if (Input.touchCount == 1)
        {
            Handle1FingerDrag();
        }
        else if (Input.touchCount == 2)
        {
            Handle2FingerDrag();
        }
    }

    /// <summary>
    /// 에디터 테스트용 마우스 입력 (InputSystem)
    /// </summary>
    private void HandleMouseInput()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.isPressed)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                touchStartPos = Mouse.current.position.ReadValue();
                prevTouchPos = touchStartPos;
                Debug.Log("[PlayerCameraMove] 마우스 클릭");
            }

            Vector2 currentPos = Mouse.current.position.ReadValue();
            float dragDelta = currentPos.y - prevTouchPos.y;

            // 드래그 거리가 충분하면 줌
            if (Mathf.Abs(dragDelta) > 5f)
            {
                float zoomDelta = -dragDelta * zoomSensitivity;
                if (playerCameraZoom != null)
                {
                    float currentSize = cinemachineCamera.Lens.OrthographicSize;
                    float targetSize = currentSize + zoomDelta;
                    playerCameraZoom.SetZoomImmediate(targetSize);
                    Debug.Log($"[PlayerCameraMove] 마우스 줌: dragDelta={dragDelta}, target={targetSize}");
                }
            }

            prevTouchPos = currentPos;
        }
    }

    /// <summary>
    /// 1-finger 드래그: 상/하/좌/우 → 카메라 이동
    /// </summary>
    private void Handle1FingerDrag()
    {
        Touch touch = Input.GetTouch(0);

        if (touch.phase == UnityEngine.TouchPhase.Began)
        {
            touchStartPos = touch.position;
            prevTouchPos = touch.position;
            Debug.Log("[PlayerCameraMove] 1-finger 시작 (카메라 이동)");
        }
        else if (touch.phase == UnityEngine.TouchPhase.Moved)
        {
            Vector2 dragDelta = touch.position - prevTouchPos;

            // 현재 활성 카메라 가져오기
            var brain = FindAnyObjectByType<CinemachineBrain>();
            if (brain != null && brain.ActiveVirtualCamera != null)
            {
                var virtualCam = brain.ActiveVirtualCamera as CinemachineCamera;
                if (virtualCam != null)
                {
                    // 월드 좌표로 변환 (스크린 → 월드)
                    float orthoSize = virtualCam.Lens.OrthographicSize;
                    Vector3 worldDragDelta = new Vector3(-dragDelta.x * orthoSize / Screen.height, -dragDelta.y * orthoSize / Screen.height, 0) * dragSensitivity;

                    // Follow 대상(앵커) 위치 이동
                    if (virtualCam.Follow != null)
                    {
                        virtualCam.Follow.position += worldDragDelta;
                        Debug.Log($"[PlayerCameraMove] 1-finger 앵커 이동: {worldDragDelta}, Camera: {virtualCam.Name}");
                    }
                }
            }

            prevTouchPos = touch.position;
        }
    }

    /// <summary>
    /// 2-finger 드래그: 위/아래 → 줌 조절
    /// </summary>
    private void Handle2FingerDrag()
    {
        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);

        if (touch0.phase == UnityEngine.TouchPhase.Began || touch1.phase == UnityEngine.TouchPhase.Began)
        {
            prevTouchPos = (touch0.position + touch1.position) / 2f;
            Debug.Log("[PlayerCameraMove] 2-finger 시작 (줌)");
        }
        else if (touch0.phase == UnityEngine.TouchPhase.Moved || touch1.phase == UnityEngine.TouchPhase.Moved)
        {
            Vector2 currentMidpoint = (touch0.position + touch1.position) / 2f;
            float dragDelta = currentMidpoint.y - prevTouchPos.y;

            // 위로 드래그: 줌 인 (음수)
            // 아래로 드래그: 줌 아웃 (양수)
            float zoomDelta = -dragDelta * zoomSensitivity;

            // PlayerCameraZoom의 targetOrthographicSize 조정
            if (playerCameraZoom != null)
            {
                float currentSize = cinemachineCamera.Lens.OrthographicSize;
                float targetSize = currentSize + zoomDelta;
                playerCameraZoom.SetZoomImmediate(targetSize);
                Debug.Log($"[PlayerCameraMove] 2-finger 줌: dragDelta={dragDelta}, zoomDelta={zoomDelta}, target={targetSize}");
            }

            prevTouchPos = currentMidpoint;
        }
    }
}
