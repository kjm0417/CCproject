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
    [SerializeField] private TMP_Text txtItemName;
    [SerializeField] private Button button;
    [SerializeField] private Outline selectionOutline;
    [SerializeField] private Color selectedOutlineColor = new Color(1f, 0.72f, 0.15f, 1f);
    [SerializeField] private Vector2 selectedOutlineDistance = new Vector2(3f, -3f);

    private UnityAction clickAction;
    private Color defaultOutlineColor;
    private Vector2 defaultOutlineDistance;
    private bool outlineDefaultsCaptured;

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
            txtNumber.gameObject.SetActive(true);
        }

        ClearClickAction();
        SetSelected(false);
    }

    public void SetItem(InvenItemData item, int count, Sprite icon, UnityAction onClick = null)
    {
        SetIcon(icon);
        SetCount(item != null && count > 0 ? count.ToString() : string.Empty);
        SetClickAction(onClick);

        if (button != null)
        {
            button.interactable = item != null;
            button.enabled = onClick != null;
        }
    }

    public void SetTool(bool isOwned, Sprite icon)
    {
        SetIcon(isOwned ? icon : null);
        SetCount(string.Empty);
        SetItemName(string.Empty);

        if (button != null)
        {
            button.interactable = isOwned;
            button.enabled = false;
        }
    }

    public void SetCommand(Sprite icon, UnityAction onClick)
    {
        SetIcon(icon);
        SetCount(string.Empty);
        SetItemName(string.Empty);
        SetClickAction(onClick);

        if (button != null)
        {
            button.interactable = onClick != null;
            button.enabled = true;
        }
    }

    public void Clear()
    {
        SetIcon(null);
        SetCount(string.Empty);
        SetItemName(string.Empty);
        ClearClickAction();
        SetSelected(false);

        if (button != null)
        {
            button.interactable = false;
            button.enabled = false;
        }
    }

    public void Release()
    {
        ClearClickAction();
    }

    public void SetNumberVisible(bool isVisible)
    {
        if (txtNumber != null)
        {
            txtNumber.gameObject.SetActive(isVisible);
        }
    }

    public void SetSelected(bool isSelected)
    {
        AutoAssignReferences();
        if (selectionOutline == null) return;

        selectionOutline.effectColor = isSelected
            ? selectedOutlineColor
            : defaultOutlineColor;
        selectionOutline.effectDistance = isSelected
            ? selectedOutlineDistance
            : defaultOutlineDistance;
    }

    public void SetItemName(string value)
    {
        if (txtItemName == null && !string.IsNullOrEmpty(value))
        {
            txtItemName = CreateItemNameText();
        }

        if (txtItemName == null) return;
        txtItemName.text = value;
        txtItemName.gameObject.SetActive(!string.IsNullOrEmpty(value));
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

        if (selectionOutline == null)
        {
            selectionOutline = GetComponent<Outline>();
        }

        if (selectionOutline != null && !outlineDefaultsCaptured)
        {
            defaultOutlineColor = selectionOutline.effectColor;
            defaultOutlineDistance = selectionOutline.effectDistance;
            outlineDefaultsCaptured = true;
        }

        if (imgIcon == null)
        {
            Transform iconTransform = transform.Find("Img_Icon");
            if (iconTransform != null)
            {
                imgIcon = iconTransform.GetComponent<Image>();
            }
        }

        if (txtCount == null)
        {
            Transform countTransform = transform.Find("Txt_Count");
            if (countTransform != null)
            {
                txtCount = countTransform.GetComponent<TMP_Text>();
            }
        }

        if (txtCount == null)
        {
            txtCount = CreateCountText();
        }
    }

    private TMP_Text CreateCountText()
    {
        GameObject countObject = new GameObject("Txt_Count", typeof(RectTransform));
        countObject.layer = gameObject.layer;

        RectTransform rect = countObject.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0.45f, 0f);
        rect.anchorMax = new Vector2(1f, 0.38f);
        rect.offsetMin = new Vector2(0f, 3f);
        rect.offsetMax = new Vector2(-5f, 0f);

        TextMeshProUGUI countText = countObject.AddComponent<TextMeshProUGUI>();
        countText.text = string.Empty;
        countText.alignment = TextAlignmentOptions.BottomRight;
        countText.fontStyle = FontStyles.Bold;
        countText.enableAutoSizing = true;
        countText.fontSizeMin = 12f;
        countText.fontSizeMax = 22f;
        countText.color = Color.white;
        countText.raycastTarget = false;
        countObject.SetActive(false);
        return countText;
    }

    private TMP_Text CreateItemNameText()
    {
        GameObject nameObject = new GameObject("Txt_ItemName", typeof(RectTransform));
        nameObject.layer = gameObject.layer;

        RectTransform rect = nameObject.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0.05f, 0.72f);
        rect.anchorMax = new Vector2(0.95f, 0.98f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI nameText = nameObject.AddComponent<TextMeshProUGUI>();
        nameText.text = string.Empty;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontStyle = FontStyles.Bold;
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = 8f;
        nameText.fontSizeMax = 14f;
        nameText.color = Color.white;
        nameText.raycastTarget = false;
        nameObject.SetActive(false);
        return nameText;
    }
}
