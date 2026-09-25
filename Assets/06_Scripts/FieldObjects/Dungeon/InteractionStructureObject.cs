using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// 상호작용 가능한 구조물에 대한 클래스. 
/// 상호작용 UI 활성화/비활성화  + 상호작용에 의해 씬이 전환이 될 때 사용
/// </summary>
public class InteractionStructureObject : MonoBehaviour
{
    [SerializeField]
    StructObjData structObjData;

    [SerializeField]
    private Vector2 size;
    public static event Action<PlayerContext> OnInteractionStart;
    public static event Action<string> OnUIShow;
    public static event Action<PlayerContext> OnButtonClick;
    public static event Action OnUIHide;   

    private bool IsInteraction; //상호작용 진행 중
    private bool IsInteractioned; //상호작용 진행 끝

    private ContactFilter2D contactFilter2D;
    private List<Collider2D> colliders2D = new();


    private void Start()
    {
        contactFilter2D.layerMask = LayerMask.GetMask("Player");
        contactFilter2D.useLayerMask = true;
    }

    private void Update()
    {
        Detection();

        if(IsInteraction && !IsInteractioned)
        {
            IsInteractioned = true;
        }
        else if(!IsInteraction && IsInteractioned)
        {
            IsInteractioned = false;
        }
    }
    private void Detection()
    {
        colliders2D.Clear();

        int count = Physics2D.OverlapBox(transform.position, size, 0, contactFilter2D, colliders2D);

        if(count > 0)
        {
            if (!IsInteractioned)  // 한 번만 발행
            {
                PlayerContext player = colliders2D[0].gameObject.GetComponent<PlayerContext>();
                if (player != null)
                {
                    OnUIShow?.Invoke(structObjData.InteractionDetail);
                    OnInteractionStart?.Invoke(player);
                }
                IsInteractioned = true;  // ← 이 부분이 중요
            }
            IsInteraction = true;
        }
        else
        {
            OnUIHide?.Invoke();

            IsInteraction = false;
        }

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireCube(transform.position, size);
    }
}
