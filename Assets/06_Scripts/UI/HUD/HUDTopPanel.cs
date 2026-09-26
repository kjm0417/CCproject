using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDTopPanel : PlayerHUDPanelBase
{
    [Header("Status Fill Images")]
    [SerializeField] private Image imgExpFill;
    [SerializeField] private Image imgHealthFill;
    [SerializeField] private Image imgHungerFill;

    [Header("Status Sliders")]
    [SerializeField] private Slider sliderHealth;
    [SerializeField] private Slider sliderHunger;

    [Header("Optional Texts")]
    [SerializeField] private TMP_Text txtLevel;
    [SerializeField] private TMP_Text txtExp;
    [SerializeField] private TMP_Text txtHealth;
    [SerializeField] private TMP_Text txtHunger;

    protected override void SubscribeEvents()
    {
        if (Context.Progression != null)
        {
            Context.Progression.OnExpChanged += RefreshProgression;
            Context.Progression.OnLevelUp += HandleLevelUp;
        }

        if (Context.Vitals != null)
        {
            Context.Vitals.OnHealthChanged += RefreshVitals;
            Context.Vitals.OnHungerChanged += RefreshVitals;
            Context.Vitals.OnHungerTierChanged += HandleHungerTierChanged;
        }
    }

    protected override void UnsubscribeEvents()
    {
        if (Context == null) return;

        if (Context.Progression != null)
        {
            Context.Progression.OnExpChanged -= RefreshProgression;
            Context.Progression.OnLevelUp -= HandleLevelUp;
        }

        if (Context.Vitals != null)
        {
            Context.Vitals.OnHealthChanged -= RefreshVitals;
            Context.Vitals.OnHungerChanged -= RefreshVitals;
            Context.Vitals.OnHungerTierChanged -= HandleHungerTierChanged;
        }
    }

    protected override void Refresh()
    {
        RefreshProgression();
        RefreshVitals();
    }

    private void HandleLevelUp(int level)
    {
        RefreshProgression();
    }

    private void HandleHungerTierChanged(HungerTier tier)
    {
        RefreshVitals();
    }

    private void RefreshProgression()
    {
        PlayerProgression progression = Context != null ? Context.Progression : null;
        if (progression == null)
        {
            SetFill(imgExpFill, 0f);
            SetText(txtLevel, string.Empty);
            SetText(txtExp, string.Empty);
            return;
        }

        SetFill(imgExpFill, progression.ExpPercent01);
        SetText(txtLevel, $"Lv.{progression.Level}");
        SetText(txtExp, $"{progression.ExpInLevel} / {progression.ExpToNext}");
    }

    private void RefreshVitals()
    {
        PlayerVitals vitals = Context != null ? Context.Vitals : null;
        if (vitals == null)
        {
            SetProgress(sliderHealth, imgHealthFill, 0f);
            SetProgress(sliderHunger, imgHungerFill, 0f);
            SetText(txtHealth, string.Empty);
            SetText(txtHunger, string.Empty);
            return;
        }

        SetProgress(sliderHealth, imgHealthFill, vitals.HpPercent01);
        SetProgress(sliderHunger, imgHungerFill, vitals.HungerPercent01);
        SetText(txtHealth, $"{vitals.Hp:0} / {vitals.MaxHp:0}");
        SetText(txtHunger, $"{vitals.HungerValue:0} / {vitals.MaxHunger:0}");
    }

    private void SetFill(Image image, float value)
    {
        if (image == null) return;
        image.fillAmount = Mathf.Clamp01(value);
    }

    private void SetProgress(Slider slider, Image image, float value)
    {
        float normalizedValue = Mathf.Clamp01(value);

        if (slider != null)
        {
            slider.normalizedValue = normalizedValue;
        }

        if (image != null && image.type == Image.Type.Filled)
        {
            image.fillAmount = normalizedValue;
        }
    }

    private void SetText(TMP_Text text, string value)
    {
        if (text == null) return;
        text.text = value;
    }
}
