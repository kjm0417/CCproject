using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RecipeIngredient
{
    public InvenItemData Item;
    [Min(1)] public int Count = 1;
}

// 음식의 이름/회복량은 ResultItem에, 만드는 방법은 이 SO에 저장한다.
[CreateAssetMenu(fileName = "RecipeData", menuName = "Game/Cooking/Recipe")]
public class RecipeData : ScriptableObject
{
    public int CraftRecipeID;
    public InvenItemData ResultItem;
    [Min(1)] public int ResultCount = 1;
    public int CraftFacilityID;
    [Min(0f)] public float CraftTimeSec;
    public List<RecipeIngredient> Ingredients = new List<RecipeIngredient>();
}
