using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 타이틀 씬 버튼 이벤트 처리
/// 버튼 추가 시 : 필드 추가 -> Bind 등록 -> 핸들러 메서드 작성
/// </summary>
public class TitleButtonHandler : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] Button startButton;
    [SerializeField] Button optionButton;
    [SerializeField] Button quitButton;   //추후 사용

    [Header("Option")]
    [SerializeField] OptionUI optionUI;

    [Header("Start Text")]
    [SerializeField] TMP_Text startButtonText;
    [SerializeField] string newGameText = "게임 시작";
    [SerializeField] string continueText = "이어서 하기";

    private const string HasStartedKey = "HasStartedGame"; //게임 시작 여부 저장 키 (PlayerPrefs)

    [Header("Scene")]
    [SerializeField] string startSceneName = "KJ_DungeonScene"; //시작 버튼 클릭 시 이동할 씬

    private void Awake()
    {
        Bind(startButton, OnClickStart);
        Bind(optionButton, OnClickOption);
        Bind(quitButton, OnClickQuit);

        if (optionUI != null) optionUI.gameObject.SetActive(false);
        RefreshStartText();
    }

    //한 번이라도 시작했으면 "이어서 하기"
    private void RefreshStartText()
    {
        if (startButtonText == null) return;
        bool hasStarted = PlayerPrefs.GetInt(HasStartedKey, 0) == 1;
        startButtonText.text = hasStarted ? continueText : newGameText;
    }

    //버튼이 할당된 경우에만 리스너 등록
    private void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.AddListener(action);
    }

    private void OnClickStart()
    {
        if (string.IsNullOrEmpty(startSceneName))
        {
            Debug.LogWarning("시작 씬 이름이 비어있음");
            return;
        }

        PlayerPrefs.SetInt(HasStartedKey, 1);
        PlayerPrefs.Save();

        SceneFader.LoadScene(startSceneName);
    }

    private void OnClickOption()
    {
        if (optionUI == null) return;
        optionUI.Open();
    }

    private void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
