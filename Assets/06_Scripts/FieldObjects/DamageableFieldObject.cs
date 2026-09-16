using System;
using System.Collections.Generic;
using UnityEngine;

public class DamageableFieldObject : FieldObjectBase, IInteractable, IDamageableFieldObject, 
    IDropProvider, IToolInteractionTarget , IRespawnProvier
{
    #region 자원 리스폰 ( IRespawnProvier )
    public ResourceSpawnEntry ResourceEntry { get; private set; } //이 오브젝트가 뭔지 정의
    public event Action<GameObject,ResourceSpawnEntry, float> OnDestroyed;
    #endregion

    public event Action<float> OnDamaged;

    [SerializeField]
    private ObjBaseData data;

    public ObjBaseData Data => data;

    protected int MaxHp => data != null ? data.HP : 0;
    protected int DataDropGroupId => data != null ? data.DropGroupID : 0;

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

    public int CurrentHp { get; private set; }
    public bool IsDepleted { get; private set; }
    public int DropGroupId => DataDropGroupId;
    public ToolType PreferredToolType => preferredToolType;


    private FieldObjectDropper fieldObjectDropper; //공통 Dropper 정의
    protected virtual void Awake()
    {
        ResetRuntimeState();
    }


    public void InfoResource(ResourceSpawnEntry entry)
    {
        this.ResourceEntry = entry;
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

    public virtual void TakeDamage(InteractionContext context)
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
        OnDamaged?.Invoke(CurrentHp);
       
        if (CurrentHp <= 0)
        {
            IsDepleted = true;
            OnDepleted();

            float respawnTime = UnityEngine.Random.Range(Data.RespawnTimeMin, Data.RespawnTimeMax);
            OnDestroyed?.Invoke(this.gameObject,ResourceEntry, respawnTime);
        }
    }

    /// <summary>
    /// 공통 Dropper 1회 주입
    /// </summary>
    public void InitDropper(FieldObjectDropper fieldObjectDropper)
    {
        this.fieldObjectDropper = fieldObjectDropper;
    }
    protected virtual void OnDepleted()
    {
        if (fieldObjectDropper != null)
        {
            fieldObjectDropper.DropOnDeath(DropGroupId, transform.position);
        }

        Destroy(gameObject);
    }

    private void ResetRuntimeState()
    {
        CurrentHp = Mathf.Max(0, MaxHp);
        IsDepleted = false;
    }
}
