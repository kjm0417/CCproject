using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HP가 존재하는 오브젝트의 체력 UI
/// </summary>
public class DamageObjectUI : MonoBehaviour
{
    [SerializeField]
    Slider hpSlider;

    private DamageableFieldObject damageableFieldObject;

    private void Awake()
    {
        damageableFieldObject = GetComponentInParent<DamageableFieldObject>();
    }

    private void OnEnable()
    {
        damageableFieldObject.OnDamaged += OnEventDamaged;
    }

    private void OnDisable()
    {
        damageableFieldObject.OnDamaged -= OnEventDamaged;
    }

    private void OnEventDamaged(float currentHP)
    {
        hpSlider.value = currentHP / damageableFieldObject.Data.HP;
    }
}
