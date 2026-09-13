// 던전/기믹 오브젝트는 상호작용을 기본으로 사용한다.
public class DungeonGimmickObject : FieldObjectBase, IInteractable
{
    public virtual bool CanInteract(InteractionContext context)
    {
        return true;
    }

    public virtual void Interact(InteractionContext context)
    {
    }
}
