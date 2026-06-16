using UnityEngine;

// Drives a real rigged character's Animator from the Player's CharacterController.
// Feeds a "Speed" float (planar m/s) into a 1-D blend tree (idle -> walk -> run),
// and forwards jump/grounded so the locomotion reads as weighty and grounded.
// This replaces the old primitive-based CharacterAnimator once a rigged mesh
// (RobotExpressive now, a Higgsfield-generated hobo next) is parented to the Player.
[RequireComponent(typeof(Animator))]
public class CharacterRigDriver : MonoBehaviour
{
    public CharacterController controller;   // movement source (on the parent Player)
    [Tooltip("How fast the blend reacts to speed changes.")]
    public float damping = 12f;

    Animator _anim;
    float _speed;
    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int GroundedHash = Animator.StringToHash("Grounded");

    void Awake()
    {
        _anim = GetComponent<Animator>();
        _anim.applyRootMotion = false;       // movement comes from the CharacterController, not the clip
        if (controller == null) controller = GetComponentInParent<CharacterController>();
    }

    void Update()
    {
        float target = 0f;
        bool grounded = true;
        if (controller != null)
        {
            Vector3 v = controller.velocity; v.y = 0f;
            target = v.magnitude;
            grounded = controller.isGrounded;
        }
        _speed = Mathf.Lerp(_speed, target, Time.deltaTime * damping);
        if (_speed < 0.06f) _speed = 0f;
        _anim.SetFloat(SpeedHash, _speed);
        _anim.SetBool(GroundedHash, grounded);
    }
}
