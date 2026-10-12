using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 모든 UI 버튼 누를 때 클릭 효과음 재생. SoundManager가 자동으로 붙임
/// 효과음 제외할 버튼은 UIClickSoundIgnore 컴포넌트 추가
/// </summary>
public class UIClickSoundPlayer : MonoBehaviour
{
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
    private PointerEventData pointerData;
    private EventSystem cachedEventSystem;

    private void Update()
    {
        if (!TryGetPressPosition(out Vector2 position)) return;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) return;

        if (pointerData == null || cachedEventSystem != eventSystem)
        {
            pointerData = new PointerEventData(eventSystem);
            cachedEventSystem = eventSystem;
        }
        pointerData.position = position;

        raycastResults.Clear();
        eventSystem.RaycastAll(pointerData, raycastResults);
        if (raycastResults.Count == 0) return;

        // 가장 위에 있는 UI 기준
        Button button = raycastResults[0].gameObject.GetComponentInParent<Button>();
        if (button == null || !button.IsInteractable()) return;
        if (button.GetComponent<UIClickSoundIgnore>() != null) return;

        SoundManager.Instance.PlaySFX(SFXType.ButtonClick);
    }

    // 이번 프레임에 눌린 터치/마우스 위치
    private bool TryGetPressPosition(out Vector2 position)
    {
        Touchscreen touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
        {
            position = touch.primaryTouch.position.ReadValue();
            return true;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            position = mouse.position.ReadValue();
            return true;
        }

        position = Vector2.zero;
        return false;
    }
}
