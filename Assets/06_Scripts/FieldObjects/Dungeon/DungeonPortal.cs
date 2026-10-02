using UnityEngine;

/// <summary>
/// 던전 층 이동 포탈 - 상호작용 시 지정된 맵으로 이동
/// 같은 층이라도 포탈마다 다른 맵으로 연결 가능
/// </summary>
public class DungeonPortal : DungeonGimmickObject
{
    [SerializeField, Tooltip("이동할 맵")]
    private DungeonMap targetMap;
    [SerializeField, Tooltip("이 포탈을 관리하는 던전. 비우면 씬에서 자동 탐색 ( 씬에 하나 )")]
    private DungeonBase dungeon;

    public DungeonMap TargetMap => targetMap;


    public override bool CanInteract(InteractionContext context)
    {
        return targetMap != null && dungeon != null;
    }

    public override void Interact(InteractionContext context)
    {
        if (!CanInteract(context)) return;

        dungeon?.MoveToMap(targetMap, context.Player);
    }
}
