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
    [SerializeField] Button optionButton; //추후 사용
    [SerializeField] Button quitButton;   //추후 사용

    [Header("Scene")]
    [SerializeField] string startSceneName = "KJ_DungeonScene"; //시작 버튼 클릭 시 이동할 씬

    private void Awake()
    {
        Bind(startButton, OnClickStart);
        Bind(optionButton, OnClickOption);
        Bind(quitButton, OnClickQuit);
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

        SceneManager.LoadSceneAsync(startSceneName);
    }

    private void OnClickOption()
    {
        //TODO : 옵션 패널 열기
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
