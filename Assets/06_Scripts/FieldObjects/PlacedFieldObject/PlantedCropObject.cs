using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 흙에 심은 작물.
/// 성장은 GrowthController가 담당, 단계마다 모습만 바뀌고 다 자란 뒤 상호작용하면 수확 드랍.
/// </summary>
public class PlantedCropObject : InteractivePlacedFieldObject, IGrowable
{
    [Tooltip("수확 시 한 번만 드랍하므로 SimpleFieldObjectDropper면 충분 (deathDropTable에 마지막 단계 DropGroupID 포함)")]
    [SerializeField]
    private FieldObjectDropper dropper;

    [SerializeField]
    private List<FarmObjData> farmObjDatas = new();

    [SerializeField]
    private int maxGrowthStage = 5; //마지막 단계

    [Tooltip("재성장 작물(마지막 단계 FarmObjData.Regrowable > 0)이 수확 후 돌아갈 단계")]
    [SerializeField]
    private int regrowStage = 1;

    [Header("단계별 모습")]
    private SpriteRenderer spriteRenderer;

    [Tooltip("index 0 = 1단계. 비어 있으면 모습 변경 안 함")]
    [SerializeField]
    private List<Sprite> stageSprites = new();

    private GrowthController growth;

    /// <summary>
    /// 수확 후 제거될 때 알림 (심은 칸 점유 해제용)
    /// </summary>
    public event Action<PlantedCropObject> OnRemoved;

    public int GrowthStage
    {
        get => growth.CurrentStage;
        set
        {
            growth.CurrentStage = value;
            ApplyStageSprite(value);
        }
    }

    public bool IsGrowthComplete
    {
        get => growth.IsGrowthComplete;
        set => growth.IsGrowthComplete = value;
    }

    public bool IsHarvestable => growth.IsGrowthComplete;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        growth = new GrowthController(this, farmObjDatas, maxGrowthStage);
        growth.OnStageChanged += ApplyStageSprite;
    }

    private void Start()
    {
        // 로드 시 GrowthStage/IsGrowthComplete가 Start 전에 채워지므로 그 단계부터 이어서 성장
        ApplyStageSprite(growth.CurrentStage);
        growth.StartGrowth();
    }

    public override bool CanInteract(InteractionContext context)
    {
        return IsHarvestable;
    }

    public override void Interact(InteractionContext context)
    {
        if (!CanInteract(context)) return;

        Harvest();
    }

    /// <summary>
    /// 수확 - 마지막 단계 드랍 후 재성장 또는 제거
    /// </summary>
    private void Harvest()
    {
        FarmObjData lastStageData = growth.GetStageData(maxGrowthStage);

        if (dropper != null && lastStageData != null)
        {
            dropper.DropFinal(lastStageData.DropGroupID, transform.position);
        }

        if (lastStageData != null && lastStageData.Regrowable > 0)
        {
            growth.ResetTo(regrowStage);
            return;
        }

        growth.Stop();
        OnRemoved?.Invoke(this);
        Destroy(gameObject);
    }

    private void ApplyStageSprite(int stage)
    {
        if (spriteRenderer == null) return;

        int index = stage - 1;
        if (index < 0 || index >= stageSprites.Count || stageSprites[index] == null) return;

        spriteRenderer.sprite = stageSprites[index];
    }
}
