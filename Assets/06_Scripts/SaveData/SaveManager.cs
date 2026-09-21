using Newtonsoft.Json;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

/// <summary>
/// 플레이어/게임데이터 저장 및 로드 담당
/// </summary>
public class SaveManager : MonoBehaviour
{
    #region 싱글톤
    private static SaveManager instance;
    public static SaveManager Instance
    {
        get
        {
            return instance;
        }
    }
    #endregion

    private string playerSavePath;
    private string territorySavePath;

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

        playerSavePath = Path.Combine(Application.persistentDataPath, "PlayerSave.json");
        territorySavePath = Path.Combine(Application.persistentDataPath, "TerritorySave.json");

        // 현재 씬 확인
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.name == "KJ_Scene")
        {
            LoadGameState();
        }
    }

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
        if (scene.name == "KJ_Scene")
        {
            LoadGameState();
        }
    }

    private void LoadGameState()
    {
        //플레이어 데이터 실제 로드
        PlayerContext playerContext = FindAnyObjectByType<PlayerContext>();
        if (playerContext != null && playerContext.Vitals != null)
        {
            Debug.Log("플레이어 데이터 로드");
            PlayerSaveData playerData = Load();
            if (playerData != null)
            {
                Debug.Log("플레이어 데이터 로드1");
                PlayerSaveData.Load(playerData, playerContext);
            }
        }

        TerritoryManager territoryManager = TerritoryManager.Instance;
        if (territoryManager != null)
        {
            Debug.Log("영토 데이터 로드");
            TerritorySaveData territoryData = LoadTerritory();
            if (territoryData != null)
            {
                Debug.Log("영토 데이터 로드1");
                territoryManager.Load(territoryData);
            }
        }
    }
    #region Json 데이터 역직렬화
    /// <summary>
    /// 플레이어 데이터 역직렬화
    /// </summary>
    /// <returns></returns>
    public PlayerSaveData Load()
    {
        if (!File.Exists(playerSavePath))
        {
            Debug.LogWarning("플레이어 저장 파일이 없습니다.");
            return null;
        }

        string json = File.ReadAllText(playerSavePath);
        PlayerSaveData data = JsonConvert.DeserializeObject<PlayerSaveData>(json);
        Debug.Log($"플레이어 데이터 로드 완료: {playerSavePath}");
        return data;
    }
    /// <summary>
    /// 영토 데이터 역직렬화
    /// </summary>
    /// <returns></returns>
    public TerritorySaveData LoadTerritory()
    {
        if (!File.Exists(territorySavePath))
        {
            Debug.LogWarning("영토 저장 파일이 없습니다.");
            return null;
        }

        string json = File.ReadAllText(territorySavePath);
        TerritorySaveData data = JsonConvert.DeserializeObject<TerritorySaveData>(json);
        Debug.Log($"영토 데이터 로드 완료: {territorySavePath}");
        return data;
    }
    #endregion
    #region Json 데이터 직렬화
    /// <summary>
    /// 플레이어 데이터 직렬화
    /// </summary>
    /// <param name="playerSaveData"></param>
    public void Save(PlayerSaveData playerSaveData)
    {
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        string json = JsonConvert.SerializeObject(playerSaveData, Formatting.Indented, settings);
        File.WriteAllText(playerSavePath, json);
        Debug.Log($"플레이어 데이터 저장 완료: {playerSavePath}");
    }

    /// <summary>
    /// 영토 데이터 직렬화
    /// </summary>
    /// <param name="territorySaveData"></param>
    public void SaveTerritory(TerritorySaveData territorySaveData)
    {
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        string json = JsonConvert.SerializeObject(territorySaveData, Formatting.Indented, settings);
        File.WriteAllText(territorySavePath, json);
        Debug.Log($"영토 데이터 저장 완료: {territorySavePath}");
    }
    #endregion
}