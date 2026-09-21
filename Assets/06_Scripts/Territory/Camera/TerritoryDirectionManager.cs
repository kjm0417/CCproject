using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// 영역 확인 시스템 - 영역 관리(카메라 제어)
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

    CinemachineBrain brain;
    [Header("플레이어 카메라")]
    [SerializeField]
    CinemachineCamera cinemachineCameraPlayer;
    [SerializeField]
    CinemachineConfiner2D cinemachineConfiner2DPlayer;
    [SerializeField]
    private BoxCollider2D defalutBound; //기본 플레이어 카메라 범위

    private float initcinemachineCameraPlayerSize; //플레이어 카메라의 초기 사이즈
    [Header("영역 카메라")]
    [SerializeField]
    CinemachineCamera cinemachineCameraZone;
    [SerializeField]
    CinemachineConfiner2D cinemachineConfiner2DZone;
    [SerializeField]
    AreaCameraZoom areaCameraZoom;

    [SerializeField]
    private BoxCollider2D zoomOutBound; //줌 아웃시 줌 카메라 범위

    [SerializeField]
    private float focusingduration; //줌 진입 -> 줌 포커싱, 줌 진출 -> 줌 복귀시간
    [SerializeField]
    private float zoomModeMaxSize; //줌 접근이 되어있을 때, 영역 줌 크기를 제어할 수 있는 최대 사이즈
    BoxCollider2D prevConfinerBound;

    [SerializeField, Tooltip("영역의 최대 크기 = 정사각형 18X18 ")]
    Vector2 maxZoneSize = new Vector2(18, 18);

    private GameObject Player;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

    }

    private void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player").gameObject;

        brain = FindAnyObjectByType<CinemachineBrain>();

        // 초기 상태: Player 카메라만 활성화
        cinemachineCameraPlayer.enabled = true;
        cinemachineCameraZone.enabled = false;

        // Priority 초기값
        cinemachineCameraPlayer.Priority = 10;
        cinemachineCameraZone.Priority = 0;

        initcinemachineCameraPlayerSize = cinemachineCameraPlayer.Lens.OrthographicSize;
    }

    #region 줌 진입/줌 탈출
    private Transform prevCamAnchor;
    /// <summary>
    /// 줌 진입/탈출 처리 중 처리하는 부분
    /// </summary>
    public void ZoomModeActive(Transform camAnchor)
    {
        Player.GetComponent<PlayerMovement>().JoyStick.GetComponentInParent<Canvas>().enabled = false;

        prevCamAnchor = camAnchor;

        areaCameraZoom.IsZoomModeChanged(true); //줌 모드 변경 또는 변경해제

        areaCameraZoom.SetZoomTarget(zoomModeMaxSize); 

        //1.camAnchor 기준으로 Bound 설정
        zoomOutBound.size = maxZoneSize;
        zoomOutBound.offset = Vector2.zero;
       // zoomOutBound.gameObject.transform.position = camAnchor.position;

        // Priority 변경 (높은 우선순위로 Zone 카메라 활성화)
        cinemachineCameraPlayer.Priority = 10;
        cinemachineCameraZone.Priority = 11;

        cinemachineCameraPlayer.enabled = false;
        cinemachineCameraZone.enabled = true;

        brain.ResetState();

        SetFollowTargetUpdate(camAnchor);
    }

    #endregion

    #region Zoom 카메라 Target 설정
    /// <summary>
    /// Zoom 카메라의 Target 설정
    /// </summary>
    /// <param name="target"></param>
    private void SetFollowTargetUpdate(Transform target)
    {
        StopCoroutine(nameof(SetFollowAndUpdateNextFrame));
        StartCoroutine(SetFollowAndUpdateNextFrame(target));
    }
    private IEnumerator SetFollowAndUpdateNextFrame(Transform target)
    {
        // 1프레임 대기
        yield return null;

        if (cinemachineCameraZone != null  && target != null)
        {
            cinemachineCameraZone.Follow = target;
            cinemachineCameraZone.LookAt = target;

            cinemachineCameraZone.ForceCameraPosition(target.position, Quaternion.identity);
        }
    }
    #endregion
    #region Player 카메라 Target 설정
    /// <summary>
    /// Player 카메라의 Target 설정
    /// </summary>
    /// <param name="target"></param>
    private void SetPlayerFollowTargetUpdate(Transform target)
    {
        StopCoroutine(nameof(SetPlayerFollowAndUpdateNextFrame));
        StartCoroutine(SetPlayerFollowAndUpdateNextFrame(target));
    }
    private IEnumerator SetPlayerFollowAndUpdateNextFrame(Transform target)
    {
        // 1프레임 대기
        yield return null;

        if (cinemachineCameraPlayer != null && target != null)
        {
            cinemachineCameraPlayer.Follow = target;
            cinemachineCameraPlayer.LookAt = target;

            cinemachineCameraPlayer.ForceCameraPosition(target.position, Quaternion.identity);
        }
    }
    #endregion


    #region 영역 선택시 버튼 클릭 이벤트
    [SerializeField]
    float boundOffset;
    public void ZoneIconClicked(TerritoryZone approachZone,Action onComplete = null)
    {
       
        Player.GetComponent<PlayerMovement>().JoyStick.GetComponentInParent<Canvas>().enabled = false;

        SetFollowTargetUpdate(approachZone.GetComponent<TerritoryCamera>().TerritoryMiddleAnchor);

        StopCoroutine(nameof(SmoothZoomBound));

        TerritoryZone playerZone = PlayerZoneDetector.GetCurrentZone();

        if (playerZone == null) return;

        TerrirotyDirection dir = TerritoryManager.Instance.GetDirection(playerZone, approachZone);

        Vector2 targetSize = zoomOutBound.size;
        Vector2 targetOffset = Vector2.zero;

        //상하좌우 방향에 따라 다름
        switch (dir)
        {
            case TerrirotyDirection.Up:
                targetSize = new Vector2(targetSize.x, targetSize.y + boundOffset);
                targetOffset += new Vector2(0, boundOffset / 2);
                break;
            case TerrirotyDirection.Down:
                targetSize = new Vector2(targetSize.x, targetSize.y + boundOffset);
                targetOffset += new Vector2(0, -boundOffset / 2);
                break;
            case TerrirotyDirection.Left:
                targetSize = new Vector2(targetSize.x + boundOffset, targetSize.y);
                targetOffset += new Vector2(-boundOffset / 2, 0);
                break;
            case TerrirotyDirection.Right:
                targetSize = new Vector2(targetSize.x + boundOffset, targetSize.y);
                targetOffset += new Vector2(boundOffset / 2, 0);
                break;
        }

        //Lerp 처리
        StartCoroutine(SmoothZoomBound(targetSize, targetOffset, focusingduration , onComplete));
    }

    private IEnumerator SmoothZoomBound(Vector2 targetSize, Vector2 targetOffset, float duration, Action onComplete = null)
    {
        float elapsed = 0;
        Vector2 startSize = zoomOutBound.size;
        Vector2 startOffset = zoomOutBound.offset;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            zoomOutBound.size = Vector2.Lerp(startSize, targetSize, t);
            zoomOutBound.offset = Vector2.Lerp(startOffset, targetOffset, t);

            yield return null;
        }

        // 최종값 설정
        zoomOutBound.size = targetSize;
        zoomOutBound.offset = targetOffset;

        onComplete?.Invoke();

        areaCameraZoom.IsZoomModeChanged(false);
    }
    #endregion

    #region 카메라 복귀 처리

    /// <summary>
    ///  구역 해금 후, 해당 구역의 밝기가 모두 밝아진 경우 시작 되는 카메라 연출         ||          뒤로가기 버튼 클릭 시
    ///  shouldExpandDefaultBound : true  - 구역별 확장 진행 , shouldExpandDefaultBound : false - 구역별 확장 진행 x
    /// </summary>
    public void ResetBackClicked(TerritoryZone approachZone , bool shouldExpandDefaultBound)
    {
        // ZoomToPlayerCamera();
        Player.GetComponent<PlayerMovement>().JoyStick.GetComponentInParent<Canvas>().enabled = true;

        StartCoroutine(UnLockCoroutine(approachZone, shouldExpandDefaultBound));
    }

    /// <summary>
    /// 구역 해금 후, 해당 구역의 밝기가 모두 밝아진 경우 시작 되는 카메라 연출
    /// </summary>
    /// <returns></returns>
    private IEnumerator UnLockCoroutine(TerritoryZone approachZone,bool shouldExpandDefaultBound)
    {
        areaCameraZoom.IsZoomModeChanged(false);
        areaCameraZoom.SetZoomTarget(initcinemachineCameraPlayerSize);

        zoomOutBound.size = maxZoneSize;
        zoomOutBound.offset = Vector2.zero;
        //zoomOutBound.gameObject.transform.position = prevCamAnchor.position;

        cinemachineCameraZone.enabled = false;
        cinemachineCameraPlayer.enabled = true;

        if(shouldExpandDefaultBound)
        {
            TerritoryZone playerZone = PlayerZoneDetector.GetCurrentZone();
            ExpandDefalutBoundByDirection(playerZone, approachZone);
        }


        yield return new WaitForSeconds(3.0f);

        // 2. Priority 변경 (이때 Blend 시작)
        cinemachineCameraZone.Priority = 10;
        cinemachineCameraPlayer.Priority = 11;

        brain.ResetState();

        areaCameraZoom.SetLenSize(3);


    }

    /// <summary>
    /// DefaultBound 방향별 확장
    /// </summary>
    /// <param name="approachZone"></param>
    private void ExpandDefalutBoundByDirection(TerritoryZone playerZone, TerritoryZone approachZone)
    {
        if (playerZone == null) return;

        TerrirotyDirection dir = TerritoryManager.Instance.GetDirection(playerZone, approachZone);

        float expand = 5f;

        switch (dir)
        {
            case TerrirotyDirection.Up:
                defalutBound.size = new Vector2(defalutBound.size.x, defalutBound.size.y +
    expand);
                defalutBound.offset += new Vector2(0, expand / 2);
                break;
            case TerrirotyDirection.Down:
                defalutBound.size = new Vector2(defalutBound.size.x, defalutBound.size.y +
    expand);
                defalutBound.offset += new Vector2(0, -expand / 2);
                break;
            case TerrirotyDirection.Left:
                defalutBound.size = new Vector2(defalutBound.size.x + expand,
    defalutBound.size.y);
                defalutBound.offset += new Vector2(-expand / 2, 0);
                break;
            case TerrirotyDirection.Right:
                defalutBound.size = new Vector2(defalutBound.size.x + expand,
    defalutBound.size.y);
                defalutBound.offset += new Vector2(expand / 2, 0);
                break;
        }

        // Confiner 캐시 리셋 (변경된 Bound 적용)
        cinemachineConfiner2DPlayer.InvalidateBoundingShapeCache();

        //4. 닫기 투표하려던 인접 영역 포커싱 UI 제거
        approachZone.TerritoryZoneUIManager.HideFocusingIcon();
    }
    #endregion
}
