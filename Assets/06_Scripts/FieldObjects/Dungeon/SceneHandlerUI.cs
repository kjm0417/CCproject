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
        PlayerSaveData saveData = PlayerSaveData.Save(playerContext);

        SaveManager.Instance.Save(saveData);

        TerritorySaveData territorySaveData = TerritoryManager.Instance.Save();
        SaveManager.Instance.SaveTerritory(territorySaveData);

        switch (type)
        {
            case SceneMoveStrucutreType.Dungeon:
                SceneManager.LoadSceneAsync("KJ_DungeonScene");
                break;
            
        }


    }
}
