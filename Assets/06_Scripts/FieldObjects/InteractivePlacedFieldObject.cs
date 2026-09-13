// 상호작용이 필요한 플레이어 배치 오브젝트만 이 클래스를 사용한다.
public class InteractivePlacedFieldObject : PlacedFieldObject, IInteractable
{
    public virtual bool CanInteract(InteractionContext context)
    {
        return true;
    }

    public virtual void Interact(InteractionContext context)
    {
    }
}
