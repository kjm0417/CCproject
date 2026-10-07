using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 씬 전환 Fade In/Out 담당. 게임 시작 시 자동 생성되어 씬 전환 후에도 유지
/// 사용 : SceneFader.LoadScene("씬이름") -> 페이드 아웃 -> 씬 로드 -> 페이드 인
/// </summary>
public class SceneFader : MonoBehaviour
{
    #region 싱글톤
    private static SceneFader instance;
    public static SceneFader Instance
    {
        get
        {
            return instance;
        }
    }
    #endregion

    private const int SortingOrder = 32767; //모든 UI 위에 그리기

    private const float MaxFadeDelta = 1f / 30f; //로드 직후 프레임 튐 방지용 델타 상한

    [SerializeField] private float fadeOutTime = 1f;
    [SerializeField] private float fadeInTime = 1f;
    [SerializeField] private float holdTime = 0.2f; //검은 화면 유지 시간 (로드 직후 프레임 안정화)
    [SerializeField] private Color fadeColor = Color.black;

    private CanvasGroup canvasGroup;
    private bool isTransitioning;

    //전환 중 중복 호출 방지용 (던전 입장 아이템 이중 소모 등)
    public static bool IsTransitioning => instance != null && instance.isTransitioning;

    //게임 시작 시 첫 씬 로드 전에 자동 생성 -> 어느 씬에서 시작해도 동작
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        if (instance != null) return;

        GameObject go = new GameObject("SceneFader");
        go.AddComponent<SceneFader>();
    }

    private void Awake()
    {
        #region 싱글톤
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        #endregion

        BuildOverlay();
    }

    private void Start()
    {
        //첫 씬도 검은 화면에서 페이드 인
        StartCoroutine(FirstSceneRoutine());
    }

    private IEnumerator FirstSceneRoutine()
    {
        isTransitioning = true;
        yield return Hold();
        yield return Fade(1f, 0f, fadeInTime);
        isTransitioning = false;
    }

    //검은 화면 유지 - 씬 시작 직후 무거운 프레임을 넘긴 뒤 페이드 시작
    private IEnumerator Hold()
    {
        yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(holdTime);
    }

    //최상단 Canvas + 화면 전체 Image 생성
    private void BuildOverlay()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        gameObject.AddComponent<GraphicRaycaster>();

        canvasGroup = gameObject.AddComponent<CanvasGroup>();

        GameObject imageGo = new GameObject("FadeImage");
        imageGo.transform.SetParent(transform, false);

        Image image = imageGo.AddComponent<Image>();
        image.color = fadeColor;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        SetAlpha(1f);
    }

    /// <summary>
    /// 페이드 아웃 후 씬 로드, 로드 완료 후 페이드 인
    /// </summary>
    public static void LoadScene(string sceneName)
    {
        if (instance == null)
        {
            SceneManager.LoadSceneAsync(sceneName);
            return;
        }

        if (instance.isTransitioning) return;

        instance.StartCoroutine(instance.LoadRoutine(sceneName));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        isTransitioning = true;

        yield return Fade(canvasGroup.alpha, 1f, fadeOutTime);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogWarning($"씬 로드 실패 : {sceneName} (Build Settings 확인)");
            yield return Fade(1f, 0f, fadeInTime);
            isTransitioning = false;
            yield break;
        }

        while (!op.isDone)
        {
            yield return null;
        }

        yield return Hold();
        yield return Fade(1f, 0f, fadeInTime);

        isTransitioning = false;
    }

    //timeScale 영향 없이 진행 (일시정지/보스 연출 중에도 동작)
    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        SetAlpha(from);

        while (t < duration)
        {
            //로드 직후 deltaTime이 크게 튀면 한 프레임에 끝나버리므로 상한 적용
            t += Mathf.Min(Time.unscaledDeltaTime, MaxFadeDelta);
            SetAlpha(Mathf.Lerp(from, to, t / duration));
            yield return null;
        }

        SetAlpha(to);
    }

    //보이는 동안 입력 차단
    private void SetAlpha(float alpha)
    {
        canvasGroup.alpha = alpha;
        canvasGroup.blocksRaycasts = alpha > 0f;
    }
}
