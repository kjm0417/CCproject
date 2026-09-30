using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 성장 단계 공통 로직 (단계별 대기 -> 단계 완료 알림 -> 다음 단계).
/// 단계가 끝났을 때 무엇을 할지(드랍, 모습 변경 등)는 사용하는 오브젝트가 이벤트로 정한다.
/// 설정값은 각 오브젝트의 직렬화 필드에 두고 생성자로 넘긴다.
/// </summary>
public class GrowthController
{
    private readonly MonoBehaviour runner; //코루틴 실행용
    private readonly List<FarmObjData> stageDatas;
    private readonly int maxStage;
    private Coroutine growRoutine;

    public int CurrentStage { get; set; } = 1;

    /// <summary>
    /// 최대 단계 대기까지 끝났는지 여부
    /// </summary>
    public bool IsGrowthComplete { get; set; }

    public bool IsMaxStage => CurrentStage >= maxStage;

    public event Action<FarmObjData> OnStageCompleted; //한 단계 대기 시간이 끝남
    public event Action<int> OnStageChanged; //단계가 바뀜 (바뀐 단계)
    public event Action OnGrowthCompleted; //최대 단계까지 끝남

    public GrowthController(MonoBehaviour runner, List<FarmObjData> stageDatas, int maxStage)
    {
        this.runner = runner;
        this.stageDatas = stageDatas ?? new List<FarmObjData>();
        this.maxStage = maxStage;
    }

    /// <summary>
    /// 현재 단계부터 성장 시작
    /// </summary>
    public void StartGrowth()
    {
        Stop();
        StartStage(CurrentStage);
    }

    /// <summary>
    /// 지정 단계로 되돌리고 성장 재시작
    /// </summary>
    public void ResetTo(int stage)
    {
        Stop();
        CurrentStage = stage;
        IsGrowthComplete = false;
        OnStageChanged?.Invoke(CurrentStage);
        StartStage(CurrentStage);
    }

    public void Stop()
    {
        if (growRoutine != null && runner != null)
        {
            runner.StopCoroutine(growRoutine);
        }
        growRoutine = null;
    }

    /// <summary>
    /// 단계에 해당하는 데이터 찾기 (CropID 형식: "XXX_단계")
    /// </summary>
    public FarmObjData GetStageData(int stage)
    {
        foreach (FarmObjData farmObj in stageDatas)
        {
            if (farmObj == null) continue;

            string[] cropID = farmObj.CropID.Split('_');
            if (cropID.Length > 1 && cropID[1] == stage.ToString())
                return farmObj;
        }
        return null;
    }

    private void StartStage(int stage)
    {
        if (stage > maxStage || IsGrowthComplete) return;

        FarmObjData stageData = GetStageData(stage);
        if (stageData == null) return;

        growRoutine = runner.StartCoroutine(GrowRoutine(stageData));
    }

    private IEnumerator GrowRoutine(FarmObjData stageData)
    {
        yield return new WaitForSeconds(stageData.TimePerStageSec);

        OnStageCompleted?.Invoke(stageData);

        if (IsMaxStage)
        {
            IsGrowthComplete = true;
            growRoutine = null;
            OnGrowthCompleted?.Invoke();
            yield break;
        }

        CurrentStage++;
        OnStageChanged?.Invoke(CurrentStage);

        yield return new WaitForSeconds(0.1f);

        StartStage(CurrentStage);
    }
}
