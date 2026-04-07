using System;
using UnityEngine;

public class MouseFollower : MonoBehaviour
{
    public float followSpeed = 50f; // Speed of following the mouse
    private Rigidbody2D rb;
    public GameObject cursor;

    private void Start()
    {
        rb = cursor.GetComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Prevents fast movement from clipping
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Smooth movement
    }

    private void Update()
    {
        Cursor.visible = false;
         Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        rb.MovePosition(Vector2.Lerp(rb.position, mousePosition, followSpeed * Time.fixedDeltaTime));

        // Zero out velocity so collision force is always consistent
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    private void FixedUpdate()
    {
       
    }
}