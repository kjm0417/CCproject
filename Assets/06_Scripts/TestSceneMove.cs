using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 테스트용 
/// </summary>
public class TestSceneMove : MonoBehaviour
{
    public void MovePlayScene()
    {
        SceneManager.LoadSceneAsync("KJ_Scene");
    }
}
