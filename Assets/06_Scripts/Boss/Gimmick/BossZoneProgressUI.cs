using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BossActivationZone 머무름 진행도를 원형 게이지로 표시 ( fill Image : Filled / Radial360 )
/// 게이지가 꽉 차면 구역 활성화 ( 씨앗 생성 등 )
/// </summary>
public class BossZoneProgressUI : MonoBehaviour
{
    [SerializeField] private BossActivationZone zone;
    [SerializeField, Tooltip("Image Type : Filled / Radial 360")]
    private Image fill;
    [SerializeField, Tooltip("표시 / 숨김 대상 ( 비우면 이 오브젝트의 첫 번째 자식 )")]
    private GameObject visualRoot;
    [SerializeField, Tooltip("진행도 0 이면 숨김")]
    private bool hideWhenEmpty = true;
    [SerializeField, Tooltip("활성화 ( 꽉 참 ) 후 숨김 - 스턴 종료로 리셋되면 다시 표시")]
    private bool hideWhenActivated = true;
    [SerializeField, Tooltip("플레이어가 구역 밖이면 숨김 ( 진행도는 유지 )")]
    private bool hideWhenOutside = true;

    private float progress;

    private void Awake()
    {
        if (zone == null) zone = GetComponentInParent<BossActivationZone>();
        if (visualRoot == null && transform.childCount > 0) visualRoot = transform.GetChild(0).gameObject;
    }

    private void OnEnable()
    {
        if (zone == null) return;
        zone.OnProgressChanged += Refresh;
        zone.OnActivated += HandleActivated;
        Refresh(zone.Progress);
    }

    private void OnDisable()
    {
        if (zone == null) return;
        zone.OnProgressChanged -= Refresh;
        zone.OnActivated -= HandleActivated;
    }

    private void Update()
    {
        if (zone != null && hideWhenOutside) UpdateVisible();
    }

    private void HandleActivated()
    {
        Refresh(1f);
    }

    private void Refresh(float value)
    {
        progress = value;
        if (fill != null) fill.fillAmount = progress;
        UpdateVisible();
    }

    private void UpdateVisible()
    {
        bool visible = !(hideWhenEmpty && progress <= 0f)
                       && !(hideWhenActivated && zone.IsActivated)
                       && !(hideWhenOutside && !zone.IsPlayerInside);
        if (visualRoot != null && visualRoot.activeSelf != visible) visualRoot.SetActive(visible);
    }

    #region [이전] 진행도 / 활성화만으로 표시 판단
    // private void Refresh(float progress)
    // {
    //     if (fill != null) fill.fillAmount = progress;
    //
    //     bool visible = !(hideWhenEmpty && progress <= 0f) && !(hideWhenActivated && zone.IsActivated);
    //     if (visualRoot != null && visualRoot.activeSelf != visible) visualRoot.SetActive(visible);
    // }
    #endregion
}
