using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 구조물 UI 공통
/// 말풍선 대사 활성화 + 상호작용 버튼 이벤트만 담당
/// </summary>
public class InteractionStructureUI : MonoBehaviour
{
    [SerializeField]
    private GameObject interactionImage;
    [SerializeField]
    private TextMeshProUGUI interactionDetail;
    [SerializeField]
    private Button interactionBtn;

    private void OnEnable()
    {
        InteractionStructureObject.OnInteractionStart += OnInteractionStarted;
        InteractionStructureObject.OnInteractionEnd += OnInteractionEnd;

        interactionBtn.onClick.AddListener(OnInteractionClick);
    }
    private void OnDisable()
    {
        InteractionStructureObject.OnInteractionStart -= OnInteractionStarted;
        InteractionStructureObject.OnInteractionEnd -= OnInteractionEnd;

        interactionBtn.onClick.RemoveAllListeners();
    }

    private void OnInteractionStarted(string interactionDetail)
    {
        interactionImage.gameObject.SetActive(true);
        this.interactionDetail.text = interactionDetail;
    }

    /// <summary>
    /// 상호작용 버튼 클릭 이벤트
    /// </summary>
    private void OnInteractionEnd()
    {
        interactionImage.gameObject.SetActive(false);
        this.interactionDetail.text = "";
    }

    private void OnInteractionClick()
    {

    }
}
