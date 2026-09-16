// 자연 생성물은 HP, 도구 데미지, 드랍을 기본으로 사용한다.
using UnityEngine;

/// <summary>
/// 자연 생성물 - 단순 자원 오브젝트
/// </summary>
public class NaturalSimpleObject : DamageableFieldObject
{
    [SerializeField]
    private SimpleFieldObjectDropper dropper;

    protected override void Awake()
    {
        base.Awake();

        InitDropper(dropper);
    }
}