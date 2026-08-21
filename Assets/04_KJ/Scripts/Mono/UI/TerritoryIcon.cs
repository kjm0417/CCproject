using TMPro;
using UnityEngine;

public class TerritoryIcon : MonoBehaviour
{
    [SerializeField]
    GameObject icon;
    [SerializeField]
    TextMeshProUGUI priceTMP;

    [SerializeField]
    GameObject Arrows;

    private void Start()
    {
        ArrowUI(false);
    }


    /// <summary>
    /// 구역 잠금 UI 처리 ( 아이콘 활성화 및 TMP 활성화 처리 등등 )
    /// </summary>
    public void LockUI(float price)
    {
        icon.gameObject.SetActive(true);
        priceTMP.text = $"필요 골드: {price}";
    }

    /// <summary>
    /// 구역 해금 UI 처리 ( 아이콘 비활성화 및 TMP 비활성화 처리 등등 )
    /// </summary>
    public void UnLockUI()
    {
        icon.gameObject.SetActive(false);
        priceTMP.text = "";
    }

    /// <summary>
    /// 구역 화살표 UI 활성화
    /// </summary>
    /// <param name="isActive"></param>
    public void ArrowUI(bool isActive)
    {
        if(isActive)
        {
            Arrows.gameObject.SetActive(true);
        }
        else
        {
            Arrows.gameObject.SetActive(false);
        }
    }

}
