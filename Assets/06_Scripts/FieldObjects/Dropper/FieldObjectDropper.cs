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

    [Header("다중 드랍 연출 (같은 아이템 2개 이상)")]
    [Tooltip("개수만큼 나눠서 떨어뜨릴 최대 개수. 초과분은 나눠진 아이템들에 합쳐짐")]
    [SerializeField, Min(1)]
    private int maxSplitCount = 5;
    [Tooltip("나눠진 아이템끼리 떨어지는 간격")]
    [SerializeField]
    private float splitSpacing = 0.2f;
    [Tooltip("착지 후 회전 각도 범위 (±도)")]
    [SerializeField]
    private float splitRotationRange = 25f;


    protected void CreatePickUpItem(PickupItem item, List<ItemDrop> drops, Vector3 origin)
    {

        for (int i = 0; i < drops.Count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * scatterRadius;

            // 2개 이상이면 여러 개로 나눠서 위치/회전을 살짝씩 다르게 떨어뜨린다.
            int totalCount = Mathf.Max(1, drops[i].Count);
            int pieces = Mathf.Min(totalCount, maxSplitCount);
            float startAngle = Random.Range(0f, 360f);

            for (int p = 0; p < pieces; p++)
            {
                // 개수를 균등 분배 (나머지는 앞쪽부터 1개씩)
                int pieceCount = totalCount / pieces + (p < totalCount % pieces ? 1 : 0);

                Vector2 pieceOffset = Vector2.zero;
                float landAngle = 0f;
                if (pieces > 1)
                {
                    float angle = startAngle + 360f / pieces * p + Random.Range(-15f, 15f);
                    pieceOffset = (Vector2)(Quaternion.Euler(0f, 0f, angle) * Vector2.right) * splitSpacing;
                    landAngle = Random.Range(-splitRotationRange, splitRotationRange);
                }

                PickupItem pickup = ObjectPoolManager.Spawn(item, origin, Quaternion.identity);
                #region [이전] Instantiate / Destroy
                // PickupItem pickup = Instantiate(item, origin, Quaternion.identity);
                #endregion
                pickup.Initialize(drops[i].Item, pieceCount);
                pickup.PrefabName = item.name;

                PlayDropTween(pickup, origin + (Vector3)(offset + pieceOffset), landAngle);
            }

            #region [이전] 드랍 1건 = 픽업 1개 (개수와 상관없이 한 개로 보임)
            // PickupItem pickup = ObjectPoolManager.Spawn(item, origin, Quaternion.identity);
            // pickup.Initialize(drops[i].Item, drops[i].Count);
            // pickup.PrefabName = item.name;
            // PlayDropTween(pickup, origin + (Vector3)offset);
            #endregion
        }
    }

    /// <summary>
    /// 회전하면서 위로 튀어올랐다가 착지 지점으로 떨어지는 연출 (연출 중에는 줍기 불가)
    /// landAngle: 착지 후 Z 회전 각도 (여러 개 드랍 시 서로 다르게 보이도록)
    /// </summary>
    private void PlayDropTween(PickupItem pickup, Vector3 landPos, float landAngle = 0f)
    {
        Transform t = pickup.transform;
        Collider2D col = pickup.GetComponent<Collider2D>();
        col.enabled = false;

        DOTween.Sequence()
            .Append(t.DOJump(landPos, jumpPower, 1, jumpDuration).SetEase(Ease.Linear))
            .Join(t.DORotate(new Vector3(0f, 0f, 360f * spinCount + landAngle), jumpDuration, RotateMode.FastBeyond360).SetEase(Ease.Linear))
            .OnComplete(() =>
            {
                t.rotation = Quaternion.Euler(0f, 0f, landAngle);
                #region [이전] 착지 시 회전 초기화
                // t.rotation = Quaternion.identity;
                #endregion
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
