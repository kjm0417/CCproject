using System;
using System.Collections.Generic;
using UnityEngine;

public class DamageableFieldObject : FieldObjectBase, IInteractable, IDamageableFieldObject, IDropProvider, IToolInteractionTarget , IRespawnProvier
{
    public ResourceSpawnEntry ResourceEntry { get; private set; } //이 오브젝트가 뭔지 정의
    public event Action<GameObject,ResourceSpawnEntry, float> OnDestroyed;


    [Header("상호작용 테스트 값")]
    [Tooltip("임시 테스트 값입니다. 나중에는 장착한 도구 데이터에서 받아오면 됩니다.")]
    [SerializeField]
    private ToolType preferredToolType = ToolType.None;

    [Tooltip("임시 테스트 값입니다. 기획서 기준으로 맞는 도구를 쓰면 1.5배 데미지를 줍니다.")]
    [SerializeField]
    private float preferredToolMultiplier = 1.5f;

    [Tooltip("상호작용 정보에서 데미지를 받지 못했을 때만 사용하는 기본 데미지입니다.")]
    [SerializeField]
    private int fallbackDamage = 1;

    [Header("선택 연결")]
    [Tooltip("비워두면 같은 오브젝트에 붙어 있는 FieldObjectDropper를 자동으로 찾아서 사용합니다.")]
    [SerializeField]
    private FieldObjectDropper dropper;

    public int CurrentHp { get; private set; }
    public bool IsDepleted { get; private set; }
    public int DropGroupId => DataDropGroupId;
    public ToolType PreferredToolType => preferredToolType;

    protected override void Awake()
    {
        base.Awake();
        ResetRuntimeState();

        if (dropper == null)
        {
            dropper = GetComponent<FieldObjectDropper>();
        }
    }

    public void InfoResource(ResourceSpawnEntry entry)
    {
        this.ResourceEntry = entry;
    }

    public override void Configure(FieldObjData fieldObjData)
    {
        base.Configure(fieldObjData);
        ResetRuntimeState();
    }

    public bool CanInteract(InteractionContext context)
    {
        return !IsDepleted && CurrentHp > 0;
    }

    public void Interact(InteractionContext context)
    {
        if (!CanInteract(context)) return;

        TakeDamage(context);
    }

    public void TakeDamage(InteractionContext context)
    {
        float baseDamage = context.Damage > 0f ? context.Damage : fallbackDamage;
        if (preferredToolType != ToolType.None && context.ToolType == preferredToolType)
        {
            baseDamage *= preferredToolMultiplier;
        }

        ReduceHp(Mathf.CeilToInt(baseDamage));
    }

    protected void ReduceHp(int amount)
    {
        if (IsDepleted || amount <= 0) return;

        CurrentHp = Mathf.Max(0, CurrentHp - amount);
        if (CurrentHp <= 0)
        {
            IsDepleted = true;
            OnDepleted();

            float respawnTime = UnityEngine.Random.Range(Data.RespawnTimeMin, Data.RespawnTimeMax);
            OnDestroyed?.Invoke(this.gameObject,ResourceEntry, respawnTime);
        }
    }

    protected virtual void OnDepleted()
    {
        if (dropper != null)
        {
            dropper.Drop(DropGroupId, transform.position);
        }

        Destroy(gameObject);
    }

    private void ResetRuntimeState()
    {
        CurrentHp = Mathf.Max(0, MaxHp);
        IsDepleted = false;
    }
}
