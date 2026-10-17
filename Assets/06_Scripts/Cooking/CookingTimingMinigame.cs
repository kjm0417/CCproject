using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CookingTimingMinigame : CookingMinigame
{
    [SerializeField, Range(0.1f, 0.5f)] private float targetWidth = 0.22f;
    [SerializeField, Min(0.1f)] private float speed = 0.8f;
    [SerializeField, Min(1f)] private float timeLimit = 8f;
    private GameObject overlay;
    private RectTransform marker;
    private RectTransform target;
    private TMP_Text title;
    private TMP_FontAsset font;
    private Action<bool> completed;
    private float elapsed;
    private float position;
    private float targetStart;

    public override void Begin(CookingRecipeData recipe, Action<bool> onCompleted)
    {
        Cancel();
        if (overlay == null) CreateOverlay();
        completed = onCompleted;
        elapsed = 0f;
        position = 0f;
        targetStart = UnityEngine.Random.Range(0.15f, 0.85f - targetWidth);
        target.anchorMin = new Vector2(targetStart, 0f);
        target.anchorMax = new Vector2(targetStart + targetWidth, 1f);
        marker.anchorMin = marker.anchorMax = new Vector2(0f, 0.5f);
        title.text = $"{recipe.ResultItem.ItemName}\n초록 구간에서 멈춰 주세요!";
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
    }

    private void Update()
    {
        if (completed == null) return;
        elapsed += Time.unscaledDeltaTime;
        position = Mathf.PingPong(elapsed * speed, 1f);
        marker.anchorMin = marker.anchorMax = new Vector2(position, 0.5f);
        if (elapsed >= timeLimit) Finish(false);
    }

    private void Stop() => Finish(position >= targetStart && position <= targetStart + targetWidth);
    public override void Cancel() => Finish(false);
    private void OnDisable() => Cancel();

    private void Finish(bool success)
    {
        Action<bool> callback = completed;
        completed = null;
        if (overlay != null) overlay.SetActive(false);
        callback?.Invoke(success);
    }

    private void CreateOverlay()
    {
        TMP_Text existingText = GetComponentInChildren<TMP_Text>(true);
        font = existingText != null ? existingText.font : TMP_Settings.defaultFontAsset;
        RectTransform root = Rect("CookingMinigame", transform, Vector2.zero, Vector2.zero);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
        overlay = root.gameObject;
        RectTransform panel = Rect("Panel", root, Vector2.zero, new Vector2(620f, 300f));
        panel.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.18f, 0.16f);
        title = Label("Instructions", panel, new Vector2(0f, 95f), new Vector2(520f, 70f), "");
        RectTransform bar = Rect("TimingBar", panel, new Vector2(0f, 5f), new Vector2(500f, 40f));
        bar.gameObject.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);
        target = Rect("Target", bar, Vector2.zero, Vector2.zero);
        target.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.8f, 0.35f);
        marker = Rect("Marker", bar, Vector2.zero, new Vector2(8f, 55f));
        marker.gameObject.AddComponent<Image>().color = Color.yellow;
        MakeButton("Stop", panel, new Vector2(0f, -85f), new Vector2(210f, 55f), "멈추기", Stop);
        MakeButton("Cancel", panel, new Vector2(275f, 120f), new Vector2(45f, 40f), "X", Cancel);
    }

    private RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = gameObject.layer;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private TMP_Text Label(string name, Transform parent, Vector2 position, Vector2 size, string text)
    {
        TMP_Text label = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = 24f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private void MakeButton(string name, Transform parent, Vector2 position, Vector2 size,
        string caption, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rect = Rect(name, parent, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.25f, 0.5f, 0.35f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        Label("Label", rect, Vector2.zero, size, caption);
    }
}
