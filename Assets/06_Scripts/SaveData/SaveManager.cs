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
    private string dungeonSavePath;

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
        dungeonSavePath = Path.Combine(Application.persistentDataPath, "DungeonSave.json");

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
        else if (DungeonSession.IsInDungeon || scene.name == "KJ_DungeonScene")
        {
            //던전에서도 영지에서 가져온 플레이어 상태(레벨/재화 등) 유지
            LoadPlayerState();
        }
    }

    private void LoadGameState()
    {
        LoadPlayerState();

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

    private void LoadPlayerState()
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
    /// <summary>
    /// 던전 기록 역직렬화 - 파일 없으면 빈 기록
    /// </summary>
    /// <returns></returns>
    public DungeonSaveData LoadDungeon()
    {
        if (!File.Exists(dungeonSavePath))
        {
            return new DungeonSaveData();
        }

        string json = File.ReadAllText(dungeonSavePath);
        DungeonSaveData data = JsonConvert.DeserializeObject<DungeonSaveData>(json) ?? new DungeonSaveData();
        Debug.Log($"던전 데이터 로드 완료: {dungeonSavePath}");
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

    /// <summary>
    /// 던전 기록 직렬화
    /// </summary>
    /// <param name="dungeonSaveData"></param>
    public void SaveDungeon(DungeonSaveData dungeonSaveData)
    {
        string json = JsonConvert.SerializeObject(dungeonSaveData, Formatting.Indented);
        File.WriteAllText(dungeonSavePath, json);
        Debug.Log($"던전 데이터 저장 완료: {dungeonSavePath}");
    }
    #endregion
}