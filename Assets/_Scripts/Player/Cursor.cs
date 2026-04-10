using UnityEngine;

public class MouseFollower : MonoBehaviour
{
    public GameObject cursor;

    private void Start()
    {
        UnityEngine.Cursor.visible = false;
    }

    private void LateUpdate()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = cursor.transform.position.z;
        cursor.transform.position = mousePosition;
    }
}