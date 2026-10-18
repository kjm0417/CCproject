using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 월드에 뜨는 획득 텍스트 ( 위로 올라가며 흐려지고 사라짐 )
/// - ObjectPoolManager로 생성 / 반납
/// </summary>
[RequireComponent(typeof(TextMeshPro))]
public class FloatingText : MonoBehaviour, IPoolable
{
    [SerializeField] private float riseDistance = 1f; // 올라가는 높이
    [SerializeField] private float duration = 1f;     // 전체 연출 시간

    private TextMeshPro tmp;
    private Sequence sequence;

    private void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
    }

    public void Play(string text, Color color)
    {
        tmp.text = text;
        tmp.color = color;

        sequence?.Kill();
        sequence = DOTween.Sequence()
            .Join(transform.DOLocalMoveY(transform.localPosition.y + riseDistance, duration).SetEase(Ease.OutCubic))
            .Join(tmp.DOFade(0f, duration).SetEase(Ease.InQuad))
            .OnComplete(() => ObjectPoolManager.Despawn(gameObject));
    }

    public void OnSpawned() { }

    public void OnDespawned()
    {
        sequence?.Kill();
        sequence = null;
    }
}
