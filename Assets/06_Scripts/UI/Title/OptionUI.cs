using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 옵션 패널 - 배경음 / 효과음 볼륨 조절
/// 값은 SoundManager가 PlayerPrefs에 저장 ( 앱 종료 후에도 유지 )
/// </summary>
public class OptionUI : MonoBehaviour, IEscapeClosable
{
    [Header("Sliders")]
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;

    [Header("Buttons")]
    [SerializeField] Button closeButton;

    private void Awake()
    {
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    //열릴 때마다 저장된 값으로 슬라이더 맞춤
    // ESC: 옵션 창 닫기 (볼륨 저장 포함)
    public bool IsEscapeOpen => gameObject.activeInHierarchy;
    public bool BlocksShortcuts => true;
    public void CloseByEscape() => Close();

    private void OnEnable()
    {
        UIEscapeStack.Push(this);
        SoundManager sound = SoundManager.Instance;
        if (sound == null) return;

        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(sound.BGMVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(sound.SFXVolume);
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.SaveVolume();
        gameObject.SetActive(false);
    }

    private void OnBGMChanged(float value)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.SetBGMVolume(value);
    }

    private void OnSFXChanged(float value)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.SetSFXVolume(value);
    }
}
