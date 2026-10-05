using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ShopArea : MonoBehaviour
{
    private readonly HashSet<Collider2D> playerColliders = new HashSet<Collider2D>();

    public bool HasPlayer => isActiveAndEnabled && playerColliders.Count > 0;

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void Update()
    {
        playerColliders.RemoveWhere(playerCollider => playerCollider == null
            || !playerCollider.enabled || !playerCollider.gameObject.activeInHierarchy
            || playerCollider.GetComponentInParent<PlayerContext>() == null);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TrackPlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TrackPlayer(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        playerColliders.Remove(other);
    }

    private void OnDisable()
    {
        playerColliders.Clear();
    }

    private void TrackPlayer(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerContext>() != null)
        {
            playerColliders.Add(other);
        }
    }
}
