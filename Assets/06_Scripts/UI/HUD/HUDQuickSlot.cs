using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class HUDQuickSlot : MonoBehaviour
{
    [SerializeField] private Image imgIcon;
    [SerializeField] private TMP_Text txtCount;
    [SerializeField] private TMP_Text txtNumber;
    [SerializeField] private Button button;

    private UnityAction clickAction;

    private void Awake()
    {
        AutoAssignReferences();
    }

    public void Initialize(int slotNumber)
    {
        AutoAssignReferences();

        if (txtNumber != null)
        {
            txtNumber.text = slotNumber.ToString();
        }

        ClearClickAction();
    }

    public void SetItem(InvenItemData item, int count, Sprite icon)
    {
        SetIcon(icon);
        SetCount(count > 1 ? count.ToString() : string.Empty);

        if (button != null)
        {
            button.interactable = item != null;
        }
    }

    public void SetTool(bool isOwned, Sprite icon)
    {
        SetIcon(isOwned ? icon : null);
        SetCount(string.Empty);

        if (button != null)
        {
            button.interactable = false;
        }
    }

    public void SetCommand(Sprite icon, UnityAction onClick)
    {
        SetIcon(icon);
        SetCount(string.Empty);
        SetClickAction(onClick);

        if (button != null)
        {
            button.interactable = onClick != null;
        }
    }

    public void Clear()
    {
        SetIcon(null);
        SetCount(string.Empty);
        ClearClickAction();

        if (button != null)
        {
            button.interactable = false;
        }
    }

    public void Release()
    {
        ClearClickAction();
    }

    private void SetIcon(Sprite sprite)
    {
        if (imgIcon == null) return;

        imgIcon.sprite = sprite;
        imgIcon.enabled = sprite != null;
    }

    private void SetCount(string value)
    {
        if (txtCount == null) return;

        txtCount.text = value;
        txtCount.gameObject.SetActive(!string.IsNullOrEmpty(value));
    }

    private void SetClickAction(UnityAction onClick)
    {
        ClearClickAction();
        clickAction = onClick;

        if (button != null && clickAction != null)
        {
            button.onClick.AddListener(clickAction);
        }
    }

    private void ClearClickAction()
    {
        if (button != null && clickAction != null)
        {
            button.onClick.RemoveListener(clickAction);
        }

        clickAction = null;
    }

    private void AutoAssignReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (imgIcon == null)
        {
            Transform iconTransform = transform.Find("Img_Icon");
            if (iconTransform != null)
            {
                imgIcon = iconTransform.GetComponent<Image>();
            }
        }
    }
}
