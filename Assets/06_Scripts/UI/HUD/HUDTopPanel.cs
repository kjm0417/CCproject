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

    [Header("Currency")]
    [SerializeField] private TMP_Text txtGold;

    [Header("Skill Window")]
    [SerializeField] private GameObject skillTreeCanvasPrefab;

    private GameObject skillTreeCanvasInstance;
    private SkillTreePanelUI skillTreePanel;

    public void OpenSkills()
    {
        if (!IsInitialized) return;

        if (skillTreePanel == null)
        {
            if (skillTreeCanvasPrefab == null)
            {
                Debug.LogWarning("HUDTopPanel: 스킬 창 프리팹이 연결되지 않았습니다.", this);
                return;
            }

            skillTreeCanvasInstance = Instantiate(skillTreeCanvasPrefab);
            skillTreePanel = skillTreeCanvasInstance.GetComponentInChildren<SkillTreePanelUI>(true);
            if (skillTreePanel == null)
            {
                Destroy(skillTreeCanvasInstance);
                Debug.LogError("HUDTopPanel: 스킬 창 프리팹에 SkillTreePanelUI가 없습니다.", this);
                return;
            }
        }

        skillTreePanel.Open(Context.Skills);
    }

    /// <summary>
    /// 키보드 단축키(K): 스킬 창이 열려 있으면 닫고, 닫혀 있으면 연다.
    /// </summary>
    public void ToggleSkills()
    {
        if (!isActiveAndEnabled) return;

        if (skillTreePanel != null && skillTreePanel.gameObject.activeInHierarchy)
        {
            skillTreePanel.Close();
            return;
        }

        OpenSkills();
    }

    protected override void OnRelease()
    {
        if (skillTreePanel != null) skillTreePanel.Close();
    }

    private void OnDestroy()
    {
        if (skillTreeCanvasInstance != null) Destroy(skillTreeCanvasInstance);
    }

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

        if (Context.Wallet != null)
        {
            Context.Wallet.OnChanged += HandleCurrencyChanged;
        }
    }

    protected override void UnsubscribeEvents()
    {
        if (skillTreePanel != null) skillTreePanel.Close();
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

        if (Context.Wallet != null)
        {
            Context.Wallet.OnChanged -= HandleCurrencyChanged;
        }
    }

    protected override void Refresh()
    {
        RefreshProgression();
        RefreshVitals();
        RefreshGold();
    }

    private void HandleLevelUp(int level)
    {
        RefreshProgression();
    }

    private void HandleHungerTierChanged(HungerTier tier)
    {
        RefreshVitals();
    }

    private void HandleCurrencyChanged(CurrencyType type)
    {
        if (type == CurrencyType.Gold)
            RefreshGold();
    }

    private void RefreshGold()
    {
        PlayerWallet wallet = Context != null ? Context.Wallet : null;
        if (wallet == null)
        {
            SetText(txtGold, "0");
            return;
        }

        int gold = wallet.GetBalance(CurrencyType.Gold);
        SetText(txtGold, gold.ToString("N0"));
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
