using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// BGM/효과음 재생 담당. TitleScene에 배치, 씬 전환 시 유지
/// </summary>
public class SoundManager : MonoBehaviour
{
    #region 싱글톤
    private static SoundManager instance;
    public static SoundManager Instance
    {
        get
        {
            return instance;
        }
    }
    #endregion

    [Header("데이터")]
    [SerializeField] private SoundDatabase database;

    [Header("믹서")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("BGM")]
    [Tooltip("BGM 전환 시 페이드 시간(초)")]
    [SerializeField] private float bgmFadeTime = 1f;

    private AudioSource bgmSource;
    private AudioSource sfxSource;

    private BGMType currentBGM = BGMType.None;
    private Coroutine bgmFadeCoroutine;

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

        if (database != null)
        {
            database.Init();
        }
        else
        {
            Debug.LogWarning("[SoundManager] SoundDatabase 미연결");
        }

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.outputAudioMixerGroup = bgmGroup;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.outputAudioMixerGroup = sfxGroup;
    }

    // 시작 씬(TitleScene)도 Awake/OnEnable 이후 sceneLoaded가 호출되므로 별도 처리 불필요
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (instance != this) return;
        if (mode == LoadSceneMode.Additive) return;

        PlaySceneBGM(scene.name);
    }

    private void PlaySceneBGM(string sceneName)
    {
        if (database == null) return;

        // 매핑 없는 씬은 기존 BGM 유지
        if (database.TryGetSceneBGM(sceneName, out BGMType bgm))
        {
            PlayBGM(bgm);
        }
    }

    #region BGM
    /// <summary>
    /// BGM 재생. 같은 BGM이 재생 중이면 무시, None이면 정지
    /// </summary>
    public void PlayBGM(BGMType type)
    {
        if (type == BGMType.None)
        {
            StopBGM();
            return;
        }

        if (type == currentBGM && bgmSource.isPlaying) return;

        SoundDatabase.BGMEntry entry = database != null ? database.GetBGM(type) : null;
        if (entry == null || entry.clip == null)
        {
            Debug.LogWarning($"[SoundManager] BGM 클립 없음: {type}");
            return;
        }

        currentBGM = type;
        StartBGMFade(FadeToBGM(entry.clip, entry.volume));
    }

    public void StopBGM()
    {
        currentBGM = BGMType.None;
        StartBGMFade(FadeOutBGM());
    }

    private void StartBGMFade(IEnumerator routine)
    {
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }
        bgmFadeCoroutine = StartCoroutine(routine);
    }

    private IEnumerator FadeToBGM(AudioClip clip, float targetVolume)
    {
        if (bgmSource.isPlaying)
        {
            yield return FadeVolume(bgmSource.volume, 0f, bgmFadeTime * 0.5f);
        }

        bgmSource.clip = clip;
        bgmSource.volume = 0f;
        bgmSource.Play();

        yield return FadeVolume(0f, targetVolume, bgmFadeTime * 0.5f);
        bgmFadeCoroutine = null;
    }

    private IEnumerator FadeOutBGM()
    {
        yield return FadeVolume(bgmSource.volume, 0f, bgmFadeTime * 0.5f);
        bgmSource.Stop();
        bgmSource.clip = null;
        bgmFadeCoroutine = null;
    }

    private IEnumerator FadeVolume(float from, float to, float duration)
    {
        // 일시정지(timeScale 0) 중에도 페이드되도록 unscaled 사용
        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(from, to, time / duration);
            yield return null;
        }
        bgmSource.volume = to;
    }
    #endregion

    #region SFX
    /// <summary>
    /// 효과음 재생 (겹쳐서 재생 가능)
    /// </summary>
    public void PlaySFX(SFXType type)
    {
        if (type == SFXType.None) return;

        SoundDatabase.SFXEntry entry = database != null ? database.GetSFX(type) : null;
        if (entry == null || entry.clip == null)
        {
            Debug.LogWarning($"[SoundManager] SFX 클립 없음: {type}");
            return;
        }

        sfxSource.PlayOneShot(entry.clip, entry.volume);
    }
    #endregion

    #region 볼륨
    /// <summary>
    /// 믹서 볼륨 설정 (0~1). parameterName은 AudioMixer에서 Expose한 파라미터 이름
    /// </summary>
    public void SetMixerVolume(string parameterName, float volume01)
    {
        if (mixer == null) return;

        // 0이면 log10 불가 → -80dB(무음)
        float dB = volume01 > 0.0001f ? Mathf.Log10(volume01) * 20f : -80f;
        mixer.SetFloat(parameterName, dB);
    }
    #endregion
}
