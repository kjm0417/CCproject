using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

// 사용하는 도구 종류
public enum ToolType
{
    None,
    Axe,
    Pickaxe,
    Shovel,
    Sword,
}

// 플레이어가 오브젝트와 상호작용할 때 전달되는 정보
public readonly struct InteractionContext
{
    public InteractionContext(PlayerContext player, ToolType toolType, float damage, Vector3 hitPoint)
    {
        Player = player;
        ToolType = toolType;
        Damage = damage;
        HitPoint = hitPoint;
    }

    public PlayerContext Player { get; }
    public ToolType ToolType { get; }
    public float Damage { get; }
    public Vector3 HitPoint { get; }
}

// 모든 필드 오브젝트가 공통으로 가지는 최소 정보
public interface IFieldObject
{
    FieldObjBaseData Data { get; }
}

// 가까이 갔을 때 플레이어가 사용할 수 있는 오브젝트
public interface IInteractable
{
    bool CanInteract(InteractionContext context);
    void Interact(InteractionContext context);
}

// HP가 있고 데미지를 받을 수 있는 오브젝트
public interface IDamageableFieldObject
{
    int CurrentHp { get; }
    bool IsDepleted { get; }
    void TakeDamage(InteractionContext context);
}

// 파괴되거나 수확될 때 드랍 그룹을 사용하는 오브젝트
public interface IDropProvider
{
    int DropGroupId { get; }
}

public interface IRespawnProvier
{
    /// <summary>
    /// 자원 리스폰 하기 위한 정보 전달
    /// </summary>
    void InfoResource(ResourceSpawnEntry resourceSpawnEntry);

    /// <summary>
    /// Destroy 시 리스폰 시키기 위한 이벤트 
    /// ( GameObject : 자원 객체 , ResourceSpawnEntry : 자원 리스폰 정보 , float : 자원 리스폰 시간 )
    /// </summary>
    public event Action<GameObject,ResourceSpawnEntry, float> OnDestroyed;
}

// 오브젝트가 기대하는 도구를 알려준다.
public interface IToolInteractionTarget
{
    ToolType PreferredToolType { get; }
}

// 시간이나 단계에 따라 성장하는 오브젝트
public interface IGrowable
{
    int GrowthStage { get; }
    bool IsHarvestable { get; }
}
