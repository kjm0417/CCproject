using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>
/// 상호작용 UI 관리
/// 말풍선 표시 활성화 + 버튼 콜백 주입만 담당
/// </summary>
public class InteractionStructureUI : MonoBehaviour
{
    [SerializeField]
    private GameObject interactionImage;
    [SerializeField]
    private TextMeshProUGUI interactionDetail;
    [SerializeField]
    private Button interactionBtn;

    public static event Action<PlayerContext> OnButtonClickCallback;


    private void OnEnable()
    {
        InteractionStructureObject.OnInteractionStart += OnInteraction;
        InteractionStructureObject.OnUIShow += OnInteractionUIShow;
        InteractionStructureObject.OnUIHide += OnInteractionUIHide;
    }

    private void OnInteraction(PlayerContext context)
    {
        interactionBtn.onClick.AddListener(() =>
        {
            OnButtonClickCallback?.Invoke(context);

            interactionBtn.onClick.RemoveAllListeners();
        });
    }

    private void OnDisable()
    {
        InteractionStructureObject.OnInteractionStart -= OnInteraction;
        InteractionStructureObject.OnUIShow -= OnInteractionUIShow;
        InteractionStructureObject.OnUIHide -= OnInteractionUIHide;
    }

    private void OnInteractionUIShow(string interactionDetail)
    {
        interactionBtn.gameObject.SetActive(true);
        interactionImage.gameObject.SetActive(true);
        this.interactionDetail.text = interactionDetail;

    }

    private void OnInteractionUIHide()
    {
        interactionBtn.gameObject.SetActive(false);
        interactionImage.gameObject.SetActive(false);
        this.interactionDetail.text = "";
    }
}
