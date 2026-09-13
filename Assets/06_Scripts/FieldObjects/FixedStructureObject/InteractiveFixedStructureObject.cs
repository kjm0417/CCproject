// 상호작용이 필요한 고정 구조물만 이 클래스를 사용한다.
public class InteractiveFixedStructureObject : FixedStructureObject, IInteractable
{
    public virtual bool CanInteract(InteractionContext context)
    {
        return true;
    }

    public virtual void Interact(InteractionContext context)
    {
    }
}
