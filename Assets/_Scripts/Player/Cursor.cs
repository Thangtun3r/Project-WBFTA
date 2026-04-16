using UnityEngine;

public class MouseFollower : MonoBehaviour
{
    public GameObject cursor;
    
    [Header("Sensitivity Settings")]
    public float sensitivity = 2.0f;
    
    // This tracks where our "virtual" mouse is on the screen
    private Vector2 virtualScreenPos;

    private void Start()
    {
        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;

        virtualScreenPos = new Vector2(Screen.width / 2f, Screen.height / 2f);
    }

    private void LateUpdate()
    {

        float deltaX = Input.GetAxis("Mouse X") * sensitivity;
        float deltaY = Input.GetAxis("Mouse Y") * sensitivity;

        virtualScreenPos.x += deltaX;
        virtualScreenPos.y += deltaY;
        
        virtualScreenPos.x = Mathf.Clamp(virtualScreenPos.x, 0, Screen.width);
        virtualScreenPos.y = Mathf.Clamp(virtualScreenPos.y, 0, Screen.height);

        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(new Vector3(virtualScreenPos.x, virtualScreenPos.y, 10f));


        worldPoint.z = cursor.transform.position.z;
        cursor.transform.position = worldPoint;
    }
}