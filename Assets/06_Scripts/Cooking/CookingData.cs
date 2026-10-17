using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CookingData", menuName = "Game/Cooking/Database")]
public class CookingData : ScriptableObject
{
    [SerializeField] private List<CookingRecipeData> recipes = new List<CookingRecipeData>();

    public IReadOnlyList<CookingRecipeData> Recipes => recipes.AsReadOnly();

    public CookingRecipeData FindRecipe(int recipeID)
    {
        foreach (CookingRecipeData recipe in recipes)
            if (recipe != null && recipe.CraftRecipeID == recipeID) return recipe;
        return null;
    }

    // 현재 정책: 등록된 레시피는 모두 해금. 재료 보유/시설 조건과는 별개다.
    // 추후 플레이어별 해금 정보를 도입할 때 이 조회 지점에 연결한다.
    public bool IsRecipeUnlocked(CookingRecipeData recipe)
    {
        return recipe != null && recipes.Contains(recipe);
    }

    // 시설 ID를 생략하거나 0을 넘기면 모든 시설의 해금된 레시피를 조회한다.
    public IEnumerable<CookingRecipeData> GetUnlockedRecipes(int craftFacilityID = 0)
    {
        foreach (CookingRecipeData recipe in recipes)
        {
            if (!IsRecipeUnlocked(recipe)) continue;
            if (craftFacilityID != 0 && recipe.CraftFacilityID != craftFacilityID) continue;
            yield return recipe;
        }
    }
}
