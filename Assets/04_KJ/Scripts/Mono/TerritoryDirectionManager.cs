using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 영토 확장 시스템 - 연출 전용(카메라 등)
/// </summary>
public class TerritoryDirectionManager : MonoBehaviour
{
    private static TerritoryDirectionManager instance;
    public static TerritoryDirectionManager Instance
    {
        get
        {
            return instance;
        }
    }

    CinemachineCamera cinemachineCamera;
    CinemachineConfiner2D cinemachineConfiner2D;

    BoxCollider2D prevConfinerBound;

    [SerializeField]
    private BoxCollider2D zoomOutConfinerBound; //줌 아웃할 때 카메라 영역

    [SerializeField] 
    private float panSpeed = 5f; //스와이프 속도

    [SerializeField]
    private float zoomoutOrthSize; //줌 아웃할 때 카메라 렌더링 크기

    [SerializeField]
    GameObject SwipeBackButtons;

    [SerializeField]
    Button backBtn;

    public event Action OnBackClick;


    private float initOrthSize; //초기 카메라 렌더링 크기
    private Vector2 activeDirection = Vector2.zero;
    private GameObject Player;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

    }
    private void OnEnable()
    {
        backBtn.onClick.AddListener(() =>
        {
            ZoomOutClearTerritory();
            OnBackClick?.Invoke();
        });
    }
    private void OnDisable()
    {
        backBtn.onClick.RemoveAllListeners();
    }

    private void Start()
    {
        cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();
        cinemachineConfiner2D = FindAnyObjectByType<CinemachineConfiner2D>();
        Player = GameObject.FindGameObjectWithTag("Player").gameObject;

        initOrthSize = cinemachineCamera.Lens.OrthographicSize;
    }

    private void Update()
    {
        if (activeDirection != Vector2.zero)
        {
            PanCamera(activeDirection);
        }
    }
    public void PanCamera(Vector2 direction)
    {
        cinemachineCamera.transform.position += (Vector3)(direction * panSpeed * Time.deltaTime);
    }
    #region 버튼 클릭 이벤트(에디터에서 설정)
    public void StartPan(Vector2 direction) => activeDirection = direction;
    public void StopPan() => activeDirection = Vector2.zero;
    public void StartPanUp() => StartPan(Vector2.up);
    public void StartPanDown() => StartPan(Vector2.down);
    public void StartPanLeft() => StartPan(Vector2.left);
    public void StartPanRight() => StartPan(Vector2.right);
    #endregion
    /// <summary>
    /// 줌 아웃 했을 때 카메라 설정
    /// </summary>
    public void ZoomOutTerritory()
    {
        prevConfinerBound = (BoxCollider2D)cinemachineConfiner2D.BoundingShape2D;

        cinemachineCamera.Lens.OrthographicSize = zoomoutOrthSize;
        cinemachineConfiner2D.BoundingShape2D = zoomOutConfinerBound;
        cinemachineCamera.Follow = null;

        SwipeBackButtons.SetActive(true);
    }

    /// <summary>
    /// 줌 아웃 해제
    /// </summary>
    public void ZoomOutClearTerritory()
    {
        cinemachineCamera.Lens.OrthographicSize = initOrthSize;

        cinemachineConfiner2D.BoundingShape2D = prevConfinerBound;

        cinemachineCamera.Follow = Player.transform;

        SwipeBackButtons.SetActive(false);
    }

    /// <summary>
    /// 해금되지 않는 영토에 줌 인 했을 때 
    /// </summary>
    public void ZoomInTerritory(BoxCollider2D bound,Transform anchor)
    {
        cinemachineCamera.Lens.OrthographicSize = initOrthSize;
        cinemachineConfiner2D.BoundingShape2D = bound;
        cinemachineCamera.Follow = anchor;
    }

    public void SwitchActiveBound(BoxCollider2D bound)
    {
        cinemachineConfiner2D.BoundingShape2D = bound;
    }
}
