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
    private AudioSource sfxPitchSource;

    private BGMType currentBGM = BGMType.None;
    private Coroutine bgmFadeCoroutine;

    // 옵션 볼륨 저장 키 (PlayerPrefs)
    private const string BGMVolumeKey = "Option_BGMVolume";
    private const string SFXVolumeKey = "Option_SFXVolume";

    private float bgmVolume = 1f;   //옵션 배경음 볼륨 (0~1)
    private float sfxVolume = 1f;   //옵션 효과음 볼륨 (0~1)
    private float bgmLevel;         //페이드 중인 BGM 볼륨 (옵션 볼륨 적용 전)

    public BGMType CurrentBGM => currentBGM;
    public float BGMVolume => bgmVolume;
    public float SFXVolume => sfxVolume;

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

        sfxPitchSource = gameObject.AddComponent<AudioSource>();
        sfxPitchSource.loop = false;
        sfxPitchSource.playOnAwake = false;
        sfxPitchSource.outputAudioMixerGroup = sfxGroup;

        gameObject.AddComponent<UIClickSoundPlayer>();

        bgmVolume = PlayerPrefs.GetFloat(BGMVolumeKey, 1f);
        sfxVolume = PlayerPrefs.GetFloat(SFXVolumeKey, 1f);
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
            yield return FadeVolume(bgmLevel, 0f, bgmFadeTime * 0.5f);
        }

        bgmSource.clip = clip;
        SetBGMLevel(0f);
        bgmSource.Play();

        yield return FadeVolume(0f, targetVolume, bgmFadeTime * 0.5f);
        bgmFadeCoroutine = null;
    }

    private IEnumerator FadeOutBGM()
    {
        yield return FadeVolume(bgmLevel, 0f, bgmFadeTime * 0.5f);
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
            SetBGMLevel(Mathf.Lerp(from, to, time / duration));
            yield return null;
        }
        SetBGMLevel(to);
    }

    // 옵션 볼륨 곱해서 실제 소스에 적용
    private void SetBGMLevel(float level)
    {
        bgmLevel = level;
        bgmSource.volume = bgmLevel * bgmVolume;
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
        AudioClip clip = entry != null ? entry.GetClip() : null;
        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] SFX 클립 없음: {type}");
            return;
        }

        if (entry.pitchRandom > 0f)
        {
            // 피치 변경은 공용 소스에 영향 주지 않도록 별도 소스 사용
            sfxPitchSource.pitch = 1f + Random.Range(-entry.pitchRandom, entry.pitchRandom);
            sfxPitchSource.PlayOneShot(clip, entry.volume * sfxVolume);
            return;
        }

        sfxSource.PlayOneShot(clip, entry.volume * sfxVolume);
    }
    #endregion

    #region 볼륨
    /// <summary>
    /// 옵션 배경음 볼륨 설정 (0~1). PlayerPrefs에 기록, 앱 종료 후에도 유지
    /// </summary>
    public void SetBGMVolume(float volume01)
    {
        bgmVolume = Mathf.Clamp01(volume01);
        bgmSource.volume = bgmLevel * bgmVolume;
        PlayerPrefs.SetFloat(BGMVolumeKey, bgmVolume);
    }

    /// <summary>
    /// 옵션 효과음 볼륨 설정 (0~1). PlayerPrefs에 기록, 앱 종료 후에도 유지
    /// </summary>
    public void SetSFXVolume(float volume01)
    {
        sfxVolume = Mathf.Clamp01(volume01);
        PlayerPrefs.SetFloat(SFXVolumeKey, sfxVolume);
    }

    /// <summary>
    /// 볼륨 설정 디스크 저장. 슬라이더 드래그마다 쓰지 않도록 옵션 닫을 때 호출
    /// </summary>
    public void SaveVolume()
    {
        PlayerPrefs.Save();
    }

    // 모바일은 홈 버튼 / 화면 꺼짐 후 강제 종료될 수 있으므로 백그라운드 진입 시 저장
    private void OnApplicationPause(bool pause)
    {
        if (pause) SaveVolume();
    }

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
