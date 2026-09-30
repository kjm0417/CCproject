using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIAttackButton : MonoBehaviour
{
    [Header("UI ������  �� ��ư ")]
    [SerializeField] private Image imgIcon;
    [SerializeField] private Button btnAttack;

    private PlayerContext context;
    private IInteractable currentTarget;
    private ToolType currentToolType;
    private bool hasQuickSlotOverride;
    private UnityAction quickSlotAction;

    public void Initialize(PlayerContext ctx)
    {
        context = ctx;
        if (btnAttack == null)
        {
            btnAttack = GetComponent<Button>();
        }

        if (btnAttack != null)
        {
            btnAttack.onClick.RemoveListener(OnClick);
            btnAttack.onClick.AddListener(OnClick);
        }
    }

    public void Release()
    {
        if (btnAttack != null)
        {
            btnAttack.onClick.RemoveListener(OnClick);
        }

        context = null;
        currentTarget = null;
        currentToolType = ToolType.None;
        hasQuickSlotOverride = false;
        quickSlotAction = null;
    }

    public void UpdateTarget(IInteractable target,ToolType toolType)
    {
        currentTarget = target;
        currentToolType = toolType;
        if (hasQuickSlotOverride) return;

        ApplyTargetState();
    }

    public void SetQuickSlotOverride(Sprite icon, UnityAction onClick, bool isInteractable)
    {
        hasQuickSlotOverride = true;
        quickSlotAction = onClick;

        if (imgIcon != null)
        {
            imgIcon.sprite = icon;
            imgIcon.enabled = icon != null;
        }

        if (btnAttack != null)
        {
            btnAttack.interactable = isInteractable && onClick != null;
        }
    }

    public void ClearQuickSlotOverride()
    {
        hasQuickSlotOverride = false;
        quickSlotAction = null;
        ApplyTargetState();
    }

    private void ApplyTargetState()
    {

        // Ÿ�� ������ ���� ��ư ���־� ����
        switch(currentToolType)
        {
            case ToolType.None:
                if (imgIcon != null) imgIcon.sprite = null;
                break;
            case ToolType.Sword:
                if (imgIcon != null) imgIcon.sprite = Resources.Load<Sprite>("Equipment/Sword");
                break;
            case ToolType.Axe:
                if (imgIcon != null) imgIcon.sprite = Resources.Load<Sprite>("Equipment/Axe");
                break;
            case ToolType.Pickaxe:
                if (imgIcon != null) imgIcon.sprite = Resources.Load<Sprite>("Equipment/Pick");
                break;
            case ToolType.Shovel:
                if (imgIcon != null) imgIcon.sprite = Resources.Load<Sprite>("Equipment/Shovel");
                break;
            default:
                break;
        }

        if (imgIcon != null)
        {
            imgIcon.enabled = imgIcon.sprite != null;
        }

        bool canInteract = currentTarget != null || currentToolType == ToolType.Shovel;
        IToolInteractionTarget toolTarget = currentTarget as IToolInteractionTarget;
        if (toolTarget != null
            && toolTarget.PreferredToolType != ToolType.None
            && currentToolType == ToolType.None)
        {
            canInteract = false;
        }

        if (btnAttack != null)
        {
            btnAttack.interactable = canInteract;
        }
    }

    //Ÿ�� �Ǿ� �ִ� ������Ʈ ����
    private void OnClick()
    {
        if (hasQuickSlotOverride)
        {
            quickSlotAction?.Invoke();
            return;
        }

        if (context == null || context.Interaction == null) return;
        context.Interaction.Interact();
    }
}
