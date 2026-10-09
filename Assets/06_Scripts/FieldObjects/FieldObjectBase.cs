using UnityEngine;

public abstract class FieldObjectBase : MonoBehaviour
{
    protected virtual bool UseYAxisSorting => true;

    protected virtual void OnEnable()
    {
        if (UseYAxisSorting)
        {
            YAxisSorting.EnsureAttached(gameObject);
        }
    }
}
