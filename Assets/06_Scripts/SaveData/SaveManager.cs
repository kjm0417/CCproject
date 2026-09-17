using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// 플레이어/오브젝트 등 관련 데이터 저장 관리
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
        }
        #endregion

    }

    public void Save(PlayerSaveData playerSaveData)
    {
        Debug.Log("데이터 저장");
       // JsonConvert.SerializeObject<PlayerSaveData>(playerSaveData);
    }
}
