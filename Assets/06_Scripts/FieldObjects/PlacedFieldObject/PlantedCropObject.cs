using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 흙에 심은 작물.
/// 성장은 GrowthController가 담당, 단계마다 모습만 바뀌고 다 자란 뒤 상호작용하면 수확 드랍.
/// </summary>
public class PlantedCropObject : InteractivePlacedFieldObject, IGrowable, IPoolable
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
    private bool hasStarted; //Start 실행 여부 ( 풀 재사용 판정 )

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

    /// <summary>
    /// UI 아이콘용 Sprite. prefab의 SpriteRenderer Sprite, 없으면 1단계 Sprite
    /// </summary>
    public Sprite IconSprite
    {
        get
        {
            SpriteRenderer renderer = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null) return renderer.sprite;
            return stageSprites.Count > 0 ? stageSprites[0] : null;
        }
    }

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
        hasStarted = true;
    }

    /// <summary>
    /// 풀 재사용 - 1단계부터 다시 성장 ( 로드 시 단계 세팅 후 시작되게 Start 와 같이 다음 프레임 )
    /// </summary>
    public void OnSpawned()
    {
        if (!hasStarted) return; //첫 생성은 Start 에서 시작

        growth.Stop();
        growth.CurrentStage = 1;
        growth.IsGrowthComplete = false;
        StartCoroutine(BeginGrowthNextFrame());
    }

    /// <summary>
    /// 풀 반납 - 성장 중지 / 칸 점유 구독 정리 ( 재사용 시 중복 방지 )
    /// </summary>
    public void OnDespawned()
    {
        growth.Stop();
        OnRemoved = null;
    }

    private IEnumerator BeginGrowthNextFrame()
    {
        yield return null;

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
        ObjectPoolManager.Despawn(gameObject);
        #region [이전] Instantiate / Destroy
        // Destroy(gameObject);
        #endregion
    }

    private void ApplyStageSprite(int stage)
    {
        if (spriteRenderer == null) return;

        int index = stage - 1;
        if (index < 0 || index >= stageSprites.Count || stageSprites[index] == null) return;

        spriteRenderer.sprite = stageSprites[index];
    }
}
