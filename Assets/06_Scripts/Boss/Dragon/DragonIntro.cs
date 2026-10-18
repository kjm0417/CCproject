using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 용 보스 등장 연출 - 보스 맵 진입 ( 보스 활성화 ) 시 재생, 끝나면 AI ( 패턴 ) 시작
/// 1. 맵 전체 검은 화면
/// 2. 검은 화면 페이드 아웃 -> 화면 흔들림 shakeCount 회
/// 3. DragonPass 애니 ( 이동 포함 )
/// 4. 본체 Y descendStartY -> descendEndY 천천히 하강
/// </summary>
public class DragonIntro : MonoBehaviour
{
    [SerializeField, Tooltip("비우면 이 오브젝트의 BossBase")]
    private BossBase boss;
    [SerializeField, Tooltip("비우면 자식의 DragonPassPattern")]
    private DragonPassPattern passPattern;
    [SerializeField, Tooltip("하강시킬 보스 본체 ( Sprite_ani ). 비우면 보스 하위 Animator")]
    private Transform bossVisual;
    [SerializeField, Tooltip("한 번만 재생 ( 맵 재진입 시 생략 )")]
    private bool playOnce = true;
    [SerializeField, Tooltip("연출 동안 숨길 UI ( HUD Bottom 패널 등 ) - 연출 끝나면 다시 표시")]
    private GameObject[] hideDuringIntro;

    [Header("1. 검은 화면")]
    [SerializeField, Tooltip("가릴 영역 ( 비우면 부모 DungeonMap 의 CamBound )")]
    private Collider2D coverArea;
    [SerializeField] private Color coverColor = Color.black;
    [SerializeField, Tooltip("가림 영역 바깥 여유 ( 흔들림 대비 )")]
    private float coverMargin = 3f;
    [SerializeField] private string coverSortingLayer = "UI";
    [SerializeField] private int coverSortingOrder = 1000;

    [Header("2. 페이드 아웃")]
    [SerializeField, Tooltip("검은 화면 유지 시간 ( 페이드 시작까지 )")]
    private float fadeDelay = 0.3f;
    [SerializeField] private float fadeDuration = 1f;

    [Header("흔들림 ( 페이드 아웃 후 )")]
    [SerializeField, Tooltip("페이드 끝난 뒤 첫 흔들림까지")]
    private float shakeDelay = 0.1f;
    [SerializeField] private int shakeCount = 2;
    [SerializeField, Tooltip("흔들림 1회 시간")]
    private float shakeDuration = 0.3f;
    [SerializeField, Tooltip("흔들림 사이 간격")]
    private float shakeInterval = 0.2f;
    [SerializeField, Tooltip("흔들림 세기 ( 월드 단위, 화면 높이 약 31 )")]
    private float shakeStrength = 1.2f;

    [Header("4. 하강 ( 본체 local Y )")]
    [SerializeField] private float descendStartY = 45.9f;
    [SerializeField] private float descendEndY = 41f;
    [SerializeField] private float descendDuration = 2f;

    private SpriteRenderer cover;
    private CinemachineCameraOffset cameraOffset;
    private Coroutine introRoutine;
    private bool played;

    private void Awake()
    {
        if (boss == null) boss = GetComponent<BossBase>();
        if (passPattern == null) passPattern = GetComponentInChildren<DragonPassPattern>(true);
        if (bossVisual == null)
        {
            Animator animator = GetComponentInChildren<Animator>(true);
            if (animator != null) bossVisual = animator.transform;
        }
        if (coverArea == null)
        {
            DungeonMap map = GetComponentInParent<DungeonMap>(true);
            if (map != null) coverArea = map.CamBound;
        }
    }

    private void OnEnable()
    {
        if (boss == null || boss.IsDead || (playOnce && played)) return;

        // 맵 활성화 프레임에 바로 가림 + AI 정지 ( 첫 프레임부터 패턴 / 맵 노출 X )
        boss.BeginIntro();
        SetHiddenUI(false);
        ShowCover(1f);
        introRoutine = StartCoroutine(IntroRoutine());
    }

    private void OnDisable()
    {
        if (introRoutine == null) return;

        // 연출 도중 비활성화 - 다음 진입 시 처음부터
        StopCoroutine(introRoutine);
        introRoutine = null;
        HideCover();
        SetHiddenUI(true);
        if (cameraOffset != null) cameraOffset.Offset = Vector3.zero;
        SetVisualY(descendEndY);
        if (passPattern != null) passPattern.EndIntroPass();
        if (boss != null) boss.EndIntro();
    }

    private IEnumerator IntroRoutine()
    {
        // 1. 검은 화면 유지
        yield return new WaitForSeconds(fadeDelay);

        // 2. 페이드 아웃 ( 어두움 -> 밝음 )
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            ShowCover(1f - Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }
        HideCover();

        // 화면 흔들림 ( 밝아진 뒤 )
        yield return new WaitForSeconds(shakeDelay);
        for (int i = 0; i < shakeCount; i++)
        {
            if (i > 0) yield return new WaitForSeconds(shakeInterval);
            yield return Shake();
        }

        // 3. DragonPass ( 이동 포함 )
        if (passPattern != null)
        {
            yield return passPattern.PlayIntroPass();
            passPattern.EndIntroPass();
        }

        // 4. 위에서 제자리로 천천히 하강
        elapsed = 0f;
        while (elapsed < descendDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / descendDuration));
            SetVisualY(Mathf.Lerp(descendStartY, descendEndY, t));
            yield return null;
        }
        SetVisualY(descendEndY);

        // 5. 연출 끝 -> 패턴 시작
        played = true;
        introRoutine = null;
        SetHiddenUI(true);
        boss.EndIntro();
    }

    /// <summary> CanvasGroup 으로 숨김 ( SetActive 는 UIHUD.Show 등이 다시 켜버림 ) </summary>
    private void SetHiddenUI(bool visible)
    {
        if (hideDuringIntro == null) return;

        foreach (GameObject go in hideDuringIntro)
        {
            if (go == null) continue;

            if (!go.TryGetComponent(out CanvasGroup group)) group = go.AddComponent<CanvasGroup>();
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }

    #region [이전] SetActive 로 숨김 - 씬 시작 시 UIHUD.Start 의 Show 가 다시 켜서 안 꺼짐
    // private void SetHiddenUI(bool active)
    // {
    //     if (hideDuringIntro == null) return;
    //
    //     foreach (GameObject go in hideDuringIntro)
    //     {
    //         if (go != null) go.SetActive(active);
    //     }
    // }
    #endregion

    /// <summary> CinemachineCameraOffset 로 흔들림 ( 카메라 위치를 직접 옮기면 Cinemachine / Confiner 가 덮어씀 ) </summary>
    private IEnumerator Shake()
    {
        if (cameraOffset == null && !SetupCameraOffset()) yield break;

        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float strength = shakeStrength * (1f - Mathf.Clamp01(elapsed / shakeDuration)); //점점 약하게
            cameraOffset.Offset = Random.insideUnitCircle * strength;
            yield return null;
        }
        cameraOffset.Offset = Vector3.zero;
    }

    private bool SetupCameraOffset()
    {
        CinemachineCamera vcam = FindAnyObjectByType<CinemachineCamera>();
        if (vcam == null) return false;

        if (!vcam.TryGetComponent(out cameraOffset))
        {
            cameraOffset = vcam.gameObject.AddComponent<CinemachineCameraOffset>();
            cameraOffset.ApplyAfter = CinemachineCore.Stage.Noise; //Confiner ( Body ) 이후 적용
            cameraOffset.PreserveComposition = false;
        }
        return true;
    }

    private void SetVisualY(float y)
    {
        if (bossVisual == null) return;

        Vector3 pos = bossVisual.localPosition;
        pos.y = y;
        bossVisual.localPosition = pos;
    }

    private void ShowCover(float alpha)
    {
        if (cover == null) CreateCover();

        cover.gameObject.SetActive(true);
        Color color = coverColor;
        color.a = coverColor.a * alpha;
        cover.color = color;
    }

    private void HideCover()
    {
        if (cover != null) cover.gameObject.SetActive(false);
    }

    private void CreateCover()
    {
        Texture2D tex = Texture2D.whiteTexture;
        GameObject go = new GameObject("IntroCover");
        go.transform.SetParent(transform, false);

        cover = go.AddComponent<SpriteRenderer>();
        cover.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        cover.sortingLayerName = coverSortingLayer;
        cover.sortingOrder = coverSortingOrder;

        // 가림 영역 ( CamBound ) 크기 + 여유, 없으면 카메라 화면 기준
        Bounds bounds = coverArea != null ? coverArea.bounds : new Bounds();
        if (bounds.size.sqrMagnitude < 0.01f) //콜라이더 없음 / 비활성
        {
            Camera cam = Camera.main;
            float height = cam != null ? cam.orthographicSize * 2f : 20f;
            bounds = new Bounds(cam != null ? cam.transform.position : transform.position, new Vector3(height * 2f, height, 0f));
        }
        go.transform.position = new Vector3(bounds.center.x, bounds.center.y, 0f);
        go.transform.localScale = new Vector3(bounds.size.x + coverMargin * 2f, bounds.size.y + coverMargin * 2f, 1f);
    }
}
