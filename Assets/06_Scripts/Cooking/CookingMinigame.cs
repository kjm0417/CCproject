using System;
using UnityEngine;

// 다른 미니게임으로 교체할 때 이 컴포넌트를 상속해 성공 여부를 한 번만 전달한다.
public abstract class CookingMinigame : MonoBehaviour
{
    public abstract void Begin(CookingRecipeData recipe, Action<bool> onCompleted);
    public abstract void Cancel();
}
