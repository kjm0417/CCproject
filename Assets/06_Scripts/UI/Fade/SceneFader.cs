using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 씬 전환 연출 담당. TitleScene에 배치, 씬 전환 후에도 유지
/// 사용 : SceneFader.LoadScene("씬이름")
/// 흐름 : 원 닫힘(큰 원 -> 작은 원) -> 로딩 UI -> 씬 로드 -> 로딩 UI 숨김 -> 원 열림(작은 원 -> 큰 원)
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

    private const float MaxFadeDelta = 1f / 30f; //로드 직후 프레임 튐 방지용 델타 상한

    [Header("원형 와이프")]
    [SerializeField] private Image irisImage;        //화면 전체 Image (IrisWipe 셰이더)
    [SerializeField] private Shader irisShader;
    [SerializeField] private Vector2 irisCenter = new Vector2(0.5f, 0.5f); //화면 비율 좌표
    [SerializeField] private float irisCloseTime = 1f;
    [SerializeField] private float irisOpenTime = 1f;

    [Header("로딩")]
    [SerializeField] private CanvasGroup loadingGroup; //걷는 캐릭터는 Walker의 Animator(Unscaled Time)로 재생
    [SerializeField] private float loadingFadeTime = 0.25f;
    [SerializeField] private float minLoadingTime = 1f; //로딩이 빨라도 최소 노출 시간

    [Header("공통")]
    [SerializeField] private float holdTime = 0.2f; //로드 직후 프레임 안정화 대기

    private Material irisMaterial;
    private bool isTransitioning;

    //전환 중 중복 호출 방지용 (던전 입장 아이템 이중 소모 등)
    public static bool IsTransitioning => instance != null && instance.isTransitioning;

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

        irisMaterial = new Material(irisShader);
        irisImage.material = irisMaterial;

        SetIris(0f);
        SetLoadingAlpha(0f);
    }

    private void OnDestroy()
    {
        if (irisMaterial != null)
        {
            Destroy(irisMaterial);
        }
    }

    private void Start()
    {
        //첫 씬도 닫힌 원에서 열림
        StartCoroutine(FirstSceneRoutine());
    }

    /// <summary>
    /// 원 닫힘 -> 로딩 -> 씬 로드 -> 원 열림
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

    private IEnumerator FirstSceneRoutine()
    {
        isTransitioning = true;
        yield return Hold();
        yield return Tween(irisOpenTime, t => SetIris(t));
        isTransitioning = false;
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        isTransitioning = true;

        //큰 원 -> 작은 원
        yield return Tween(irisCloseTime, t => SetIris(1f - t));

        yield return Tween(loadingFadeTime, t => SetLoadingAlpha(t));
        float loadingStart = Time.unscaledTime;

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogWarning($"씬 로드 실패 : {sceneName} (Build Settings 확인)");
        }
        else
        {
            while (!op.isDone)
            {
                yield return null;
            }
        }

        yield return Hold();

        while (Time.unscaledTime - loadingStart < minLoadingTime)
        {
            yield return null;
        }

        yield return Tween(loadingFadeTime, t => SetLoadingAlpha(1f - t));

        //작은 원 -> 큰 원
        yield return Tween(irisOpenTime, t => SetIris(t));

        isTransitioning = false;
    }

    //검은 화면 유지 - 씬 시작 직후 무거운 프레임을 넘긴 뒤 진행
    private IEnumerator Hold()
    {
        yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(holdTime);
    }

    //timeScale 영향 없이 0 -> 1 진행 (일시정지/보스 연출 중에도 동작)
    private IEnumerator Tween(float duration, Action<float> apply)
    {
        float t = 0f;
        apply(0f);

        while (t < duration)
        {
            //로드 직후 deltaTime이 크게 튀면 한 프레임에 끝나버리므로 상한 적용
            t += Mathf.Min(Time.unscaledDeltaTime, MaxFadeDelta);
            apply(Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }

        apply(1f);
    }

    //open : 0 = 완전히 닫힘(검은 화면), 1 = 완전히 열림
    private void SetIris(float open)
    {
        Rect rect = irisImage.rectTransform.rect;
        float aspect = rect.height > 0f ? rect.width / rect.height : 16f / 9f;

        irisMaterial.SetFloat("_Aspect", aspect);
        irisMaterial.SetVector("_Center", irisCenter);
        irisMaterial.SetFloat("_Radius", GetMaxRadius(aspect) * open);

        //완전히 열리면 끄기 -> 입력 막지 않음 (프리팹에서 꺼둬도 동작하도록 오브젝트 단위로 제어)
        irisImage.gameObject.SetActive(open < 1f);
    }

    //중심에서 가장 먼 화면 모서리까지 거리 (화면 높이 기준)
    private float GetMaxRadius(float aspect)
    {
        float dx = Mathf.Max(irisCenter.x, 1f - irisCenter.x) * aspect;
        float dy = Mathf.Max(irisCenter.y, 1f - irisCenter.y);
        return Mathf.Sqrt(dx * dx + dy * dy) + 0.02f;
    }

    private void SetLoadingAlpha(float alpha)
    {
        loadingGroup.alpha = alpha;
        loadingGroup.gameObject.SetActive(alpha > 0f);
    }
}
