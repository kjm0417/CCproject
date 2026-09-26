using UnityEngine;
using UnityEngine.UI;

public class UIAttackButton : MonoBehaviour
{
    [Header("UI ������  �� ��ư ")]
    [SerializeField] private Image imgIcon;
    [SerializeField] private Button btnAttack;

    private PlayerContext context;
    private IInteractable currentTarget;

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
    }

    public void UpdateTarget(IInteractable target,ToolType toolType)
    {
        currentTarget = target;

        // Ÿ�� ������ ���� ��ư ���־� ����
        switch(toolType)
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

        bool canInteract = target != null;
        IToolInteractionTarget toolTarget = target as IToolInteractionTarget;
        if (toolTarget != null && toolTarget.PreferredToolType != ToolType.None && toolType == ToolType.None)
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
        if (context == null || context.Interaction == null) return;
        context.Interaction.Interact();
    }
}
