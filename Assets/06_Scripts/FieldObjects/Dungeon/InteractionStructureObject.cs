using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상호작용 가능한 구조물에 대한 클래스. 
/// 상호작용 UI 활성화/비활성화 하는 것만 담당
/// </summary>
public class InteractionStructureObject : MonoBehaviour
{
    [SerializeField]
    StructObjData structObjData;

    [SerializeField]
    private Vector2 size;
    public static event Action<string> OnInteractionStart;
    public static event Action OnInteractionEnd;

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
            OnInteractionStart?.Invoke(structObjData.InteractionDetail);
            IsInteractioned = true;
        }
        else if(!IsInteraction && IsInteractioned)
        {
            OnInteractionEnd?.Invoke();
            IsInteractioned = false;
        }
    }
    private void Detection()
    {
        colliders2D.Clear();

        int count = Physics2D.OverlapBox(transform.position, size, 0, contactFilter2D, colliders2D);

        if(count > 0)
        {
            IsInteraction = true;
        }
        else
        {
            IsInteraction = false;
        }

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireCube(transform.position, size);
    }
}
