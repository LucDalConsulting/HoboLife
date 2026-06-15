using UnityEngine;

// HoboLife — RuneScape/GTA-style third-person orbit camera. Hold any mouse button
// and drag to spin a full 360°, scroll to zoom. Attach to the Main Camera and
// assign `target` to the Player.
public class OrbitCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 6f;
    public float minDistance = 2f;
    public float maxDistance = 14f;
    public float sensitivity = 5f;
    public float yaw = 180f;
    public float pitch = 20f;
    public float minPitch = -5f;
    public float maxPitch = 80f;
    public Vector3 targetOffset = new Vector3(0f, 1.6f, 0f);

    void LateUpdate()
    {
        if (target == null) return;

        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * sensitivity;
            pitch -= Input.GetAxis("Mouse Y") * sensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel") * 6f, minDistance, maxDistance);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focus = target.position + targetOffset;
        transform.position = focus - rotation * Vector3.forward * distance;
        transform.LookAt(focus);
    }
}
