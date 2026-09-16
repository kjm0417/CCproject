using UnityEngine;

public abstract class FieldObjectBase : MonoBehaviour, IFieldObject
{
    [Header("데이터 연결 - 나중에 연결")]
    [Tooltip("KJ가 필드 오브젝트 데이터를 완성하면 여기에 FieldObjData 에셋을 연결합니다.")]
    [SerializeField]
    private FieldObjBaseData data;

    public FieldObjBaseData Data => data;

    protected int MaxHp => data != null ? data.HP : 0;
    protected int DataDropGroupId => data != null ? data.DropGroupID : 0;

    protected virtual void Awake()
    {

    }

    public virtual void Configure(FieldObjBaseData fieldObjData)
    {
        data = fieldObjData;
    }
}