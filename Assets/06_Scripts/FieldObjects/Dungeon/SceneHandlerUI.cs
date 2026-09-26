using System;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 씬 이동이 필요한 구조물 타입
/// </summary>
public enum SceneMoveStrucutreType
{
    Dungeon,
}

/// <summary>
/// 상호작용에 의해 씬 이동 및 씬 이동 전 데이터 저장
/// </summary>
public class SceneHandlerUI : MonoBehaviour
{
    [SerializeField]
    SceneMoveStrucutreType type;

    [SerializeField]
    DungeonData dungeonData; //입장할 던전 ( type == Dungeon 일 때 )

    private void OnEnable()
    {
        InteractionStructureUI.OnButtonClickCallback += OnInteractionClick;

    }


    private void OnDisable()
    {
        InteractionStructureUI.OnButtonClickCallback -= OnInteractionClick;
    }

    private void OnInteractionClick(PlayerContext playerContext)
    {
        //던전 입장 아이템 체크 및 소모 - 부족하면 입장 불가
        if (type == SceneMoveStrucutreType.Dungeon && dungeonData != null)
        {
            PlayerInventory inventory = playerContext.GetComponentInChildren<PlayerInventory>();
            if (!DungeonSession.TryEnter(dungeonData, inventory))
            {
                Debug.Log($"던전 입장 불가 - 입장 아이템 부족 : {dungeonData.DungeonName}");
                return;
            }
        }

        //플레이어 데이터 저장
        PlayerSaveData saveData = PlayerSaveData.Save(playerContext);
        SaveManager.Instance.Save(saveData);

        //해금된 영토 및 생성된 자원 저장
        TerritorySaveData territorySaveData = TerritoryManager.Instance.Save();
        SaveManager.Instance.SaveTerritory(territorySaveData);

        switch (type)
        {
            case SceneMoveStrucutreType.Dungeon:
                string sceneName = dungeonData != null && !string.IsNullOrEmpty(dungeonData.SceneName)
                    ? dungeonData.SceneName : "KJ_DungeonScene";
                SceneManager.LoadSceneAsync(sceneName);
                break;
            
        }


    }
}
