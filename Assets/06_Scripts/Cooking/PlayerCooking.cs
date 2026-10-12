using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CookingRecordData
{
    public int RecipeID;
    public int SuccessCount;
}

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInventory))]
public class PlayerCooking : MonoBehaviour, IPlayerComponent
{
    [SerializeField] private CookingData cookingData;
    private readonly Dictionary<int, int> successes = new Dictionary<int, int>();
    private PlayerInventory inventory;
    private RecipeData pendingRecipe;
    private Coroutine autoRoutine;
    private int sessionID;
    private bool isAutomatic;
    private bool isCompleting;

    public event Action OnCookingChanged;
    public bool IsBusy => pendingRecipe != null;
    public string LastMessage { get; private set; } = string.Empty;
    public CookingData Data => cookingData;

    public void Initialize(PlayerContext context) => inventory = context.Inventory;

    public int GetSuccessCount(RecipeData recipe) => recipe != null
        && successes.TryGetValue(recipe.CraftRecipeID, out int count) ? count : 0;

    public bool CanAutoCook(RecipeData recipe) => GetSuccessCount(recipe) > 0;

    public bool CanCook(RecipeData recipe, bool automatic, out string reason)
    {
        reason = string.Empty;
        if (IsBusy) { reason = "조리 중입니다."; return false; }
        if (cookingData == null || !cookingData.IsRecipeUnlocked(recipe))
        { reason = "사용할 수 없는 레시피입니다."; return false; }
        if (automatic && !CanAutoCook(recipe))
        { reason = "먼저 미니게임으로 조리를 한 번 성공해 주세요."; return false; }
        if (inventory == null) inventory = GetComponent<PlayerInventory>();
        if (inventory == null) { reason = "인벤토리를 찾을 수 없습니다."; return false; }
        return inventory.CanCook(recipe, out reason);
    }

    public bool TryStartManual(RecipeData recipe, out int requestID, out string reason)
    {
        requestID = 0;
        if (!CanCook(recipe, false, out reason)) return false;
        pendingRecipe = recipe;
        isAutomatic = false;
        requestID = ++sessionID;
        LastMessage = "미니게임을 성공하면 요리가 완성됩니다.";
        OnCookingChanged?.Invoke();
        return true;
    }

    public void CompleteManual(int requestID, bool success)
    {
        if (!IsBusy || isAutomatic || isCompleting || requestID != sessionID) return;
        Complete(success);
    }

    public bool TryAutoCook(RecipeData recipe, out string reason)
    {
        if (!CanCook(recipe, true, out reason)) return false;
        pendingRecipe = recipe;
        isAutomatic = true;
        int requestID = ++sessionID;
        LastMessage = $"자동 조리 중 ({recipe.CraftTimeSec:0.#}초)";
        OnCookingChanged?.Invoke();
        autoRoutine = StartCoroutine(AutoCook(recipe.CraftTimeSec, requestID));
        return true;
    }

    private IEnumerator AutoCook(float duration, int requestID)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, duration));
        autoRoutine = null;
        if (IsBusy && isAutomatic && requestID == sessionID) Complete(true);
    }

    private void Complete(bool success)
    {
        isCompleting = true;
        RecipeData recipe = pendingRecipe;
        try
        {
            if (!success) LastMessage = "조리에 실패했습니다. 재료는 소모되지 않았습니다.";
            else if (!inventory.TryCook(recipe, out string reason)) LastMessage = reason;
            else
            {
                int count = GetSuccessCount(recipe);
                successes[recipe.CraftRecipeID] = count == int.MaxValue ? count : count + 1;
                LastMessage = $"{recipe.ResultItem.ItemName} {recipe.ResultCount}개 완성!";
            }
        }
        finally
        {
            pendingRecipe = null;
            isCompleting = false;
            OnCookingChanged?.Invoke();
        }
    }

    public void CancelCooking()
    {
        if (isCompleting) return;
        ++sessionID; // 이전 미니게임의 지연 콜백은 무시한다.
        if (autoRoutine != null) StopCoroutine(autoRoutine);
        autoRoutine = null;
        pendingRecipe = null;
        LastMessage = string.Empty;
        OnCookingChanged?.Invoke();
    }

    private void OnDisable() => CancelCooking();

    public List<CookingRecordData> CreateSaveData()
    {
        var result = new List<CookingRecordData>();
        foreach (var entry in successes)
            result.Add(new CookingRecordData { RecipeID = entry.Key, SuccessCount = entry.Value });
        result.Sort((a, b) => a.RecipeID.CompareTo(b.RecipeID));
        return result;
    }

    public void Load(IEnumerable<CookingRecordData> records)
    {
        CancelCooking();
        successes.Clear();
        if (records != null)
            foreach (CookingRecordData record in records)
                if (record != null && record.RecipeID > 0 && record.SuccessCount > 0)
                    successes[record.RecipeID] = successes.TryGetValue(record.RecipeID, out int previous)
                        ? Mathf.Max(previous, record.SuccessCount) : record.SuccessCount;
        OnCookingChanged?.Invoke();
    }
}
