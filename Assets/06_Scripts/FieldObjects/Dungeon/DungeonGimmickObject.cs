// 던전/기믹 오브젝트는 상호작용을 기본으로 사용한다.
public class DungeonGimmickObject : FieldObjectBase, IInteractable
{
    // 바닥 타일과 겹친 뿌리는 기믹 전용 정렬 순서를 유지한다.
    protected override bool UseYAxisSorting => false;

    public virtual bool CanInteract(InteractionContext context)
    {
        return true;
    }

    public virtual void Interact(InteractionContext context)
    {
    }
}
