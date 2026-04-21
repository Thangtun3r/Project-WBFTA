using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class TriggerButton2D : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("The tag of the object that is allowed to press this button.")]
    public string targetTag = "Player";

    [Header("Input Settings")]
    [Tooltip("If true, the left mouse button will trigger the button when the player is inside the collider.")]
    public bool useLeftMouseClick = true;

    [Tooltip("The key the player must press to 'click' the button if mouse click is disabled.")]
    public KeyCode interactKey = KeyCode.E; // Default is the 'E' key

    [Header("Color Settings")]
    [Tooltip("The default color of the 2D object.")]
    public Color defaultColor = Color.white;
    [Tooltip("The color when the player is in the trigger, showing it can be clicked.")]
    public Color hoverColor = new Color(0.8f, 0.8f, 0.8f);

    [Header("Button Events")]
    [Tooltip("Drag and drop scripts here to call their methods when triggered.")]
    public UnityEvent onClick;

    public UnityEvent onRelease;

    private SpriteRenderer spriteRenderer;
    private bool isReadyToClick = false; // Tracks if the player is currently inside the trigger

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = defaultColor;
        }
    }

    private void Update()
    {
        if (!isReadyToClick)
        {
            return;
        }

        if (useLeftMouseClick)
        {
            if (Input.GetMouseButtonDown(0))
            {
                onClick?.Invoke();
            }

            if (Input.GetMouseButtonUp(0))
            {
                onRelease?.Invoke();
            }
        }
        else
        {
            if (Input.GetKeyDown(interactKey))
            {
                onClick?.Invoke();
            }

            if (Input.GetKeyUp(interactKey))
            {
                onRelease?.Invoke();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Player enters the trigger zone
        if (collision.CompareTag(targetTag))
        {
            isReadyToClick = true; // Unlock the ability to click

            // Change to hover color to show the player they can interact with it
            if (spriteRenderer != null)
            {
                spriteRenderer.color = hoverColor;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // Player leaves the trigger zone
        if (collision.CompareTag(targetTag))
        {
            isReadyToClick = false; // Lock the ability to click

            // Revert back to the default color
            if (spriteRenderer != null)
            {
                spriteRenderer.color = defaultColor;
            }
        }
    }
}