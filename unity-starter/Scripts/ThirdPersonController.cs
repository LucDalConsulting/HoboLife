using UnityEngine;

// HoboLife — third-person movement. Camera-relative WASD/arrow movement using a
// CharacterController. Movement is derived directly from the camera basis, which
// avoids the reversed-strafe bug we hit in the web prototype.
//
// Setup (your local Claude can do this): put this on the Player object (which
// needs a CharacterController), and drag the Main Camera into `cameraTransform`.
[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    public Transform cameraTransform;
    public float moveSpeed = 4.5f;
    public float turnSpeed = 12f;
    public float gravity = -20f;

    private CharacterController cc;
    private float verticalVel;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        Vector3 camF = cameraTransform.forward; camF.y = 0f; camF.Normalize();
        Vector3 camR = cameraTransform.right;   camR.y = 0f; camR.Normalize();

        float h = Input.GetAxisRaw("Horizontal"); // A/D, Left/Right
        float v = Input.GetAxisRaw("Vertical");   // W/S, Up/Down

        Vector3 wish = camF * v + camR * h;
        if (wish.sqrMagnitude > 1f) wish.Normalize();

        if (wish.sqrMagnitude > 0.0001f)
        {
            Quaternion target = Quaternion.LookRotation(wish);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        verticalVel = cc.isGrounded ? -1f : verticalVel + gravity * Time.deltaTime;
        Vector3 velocity = wish * moveSpeed + Vector3.up * verticalVel;
        cc.Move(velocity * Time.deltaTime);
    }
}
