using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using static UnityEngine.UI.Image;

/// <summary>
/// 자원 드랍 공통
/// </summary>
public abstract class FieldObjectDropper : MonoBehaviour
{
    [Header("드랍 데이터 / 최종 드랍(파괴 및 수확)")]
    [SerializeField]
    protected DropTableData finalDropTable;
    [SerializeField]
    protected PickupItem finalPickupPrefab;

    [Header("드랍 테스트 값")]
    [SerializeField]
    private float scatterRadius = 0.35f;

    [Header("드랍 연출")]
    [SerializeField]
    private float jumpPower = 0.8f;
    [SerializeField]
    private float jumpDuration = 0.5f;
    [SerializeField]
    private int spinCount = 1;


    protected void CreatePickUpItem(PickupItem item, List<ItemDrop> drops, Vector3 origin)
    {

        for (int i = 0; i < drops.Count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * scatterRadius;
            PickupItem pickup = ObjectPoolManager.Spawn(item, origin, Quaternion.identity);
            #region [이전] Instantiate / Destroy
            // PickupItem pickup = Instantiate(item, origin, Quaternion.identity);
            #endregion
            pickup.Initialize(drops[i].Item, drops[i].Count);
            pickup.PrefabName = item.name;

            PlayDropTween(pickup, origin + (Vector3)offset);
        }
    }

    /// <summary>
    /// 회전하면서 위로 튀어올랐다가 착지 지점으로 떨어지는 연출 (연출 중에는 줍기 불가)
    /// </summary>
    private void PlayDropTween(PickupItem pickup, Vector3 landPos)
    {
        Transform t = pickup.transform;
        Collider2D col = pickup.GetComponent<Collider2D>();
        col.enabled = false;

        DOTween.Sequence()
            .Append(t.DOJump(landPos, jumpPower, 1, jumpDuration).SetEase(Ease.Linear))
            .Join(t.DORotate(new Vector3(0f, 0f, 360f * spinCount), jumpDuration, RotateMode.FastBeyond360).SetEase(Ease.Linear))
            .OnComplete(() =>
            {
                t.rotation = Quaternion.identity;
                col.enabled = true;
            })
            .SetLink(pickup.gameObject, LinkBehaviour.KillOnDisable); //풀 반납 ( 비활성 ) 시 연출 종료
            #region [이전] 파괴 시에만 연출 종료
            // .SetLink(pickup.gameObject);
            #endregion
    }

    public void DropFinal(int dropGroupId, Vector3 origin)
    {
        if (dropGroupId <= 0 || finalDropTable == null || finalPickupPrefab == null) return;

        List<ItemDrop> drops = finalDropTable.Roll(dropGroupId);

        CreatePickUpItem(finalPickupPrefab, drops, origin);
    }

}
