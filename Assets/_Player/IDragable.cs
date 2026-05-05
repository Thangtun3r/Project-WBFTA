using UnityEngine;

public interface IDragable
{
    void OnStartDrag();
    void OnDrag(Vector2 position);
    void OnEndDrag(Vector2 velocity);
    Rigidbody2D GetRigidbody();
    Transform GetTransform();
}