using UnityEngine;

// HoboLife — a simple wandering NPC. Picks random points on the block and walks
// to them via a CharacterController, facing travel direction. The shared
// CharacterAnimator gives it a walk cycle for free. Holds its own identity
// (name + dialogue tree) for the talk system.
[RequireComponent(typeof(CharacterController))]
public class NpcWander : MonoBehaviour
{
    public string displayName = "Stranger";
    public string treeId = "pedestrian";
    public float speed = 1.6f;
    public float wanderRadius = 16f;

    Vector3 home, target;
    float repathTimer, verticalVel;
    CharacterController cc;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        home = transform.position;
        PickTarget();
    }

    void PickTarget()
    {
        Vector2 r = Random.insideUnitCircle * wanderRadius;
        target = new Vector3(
            Mathf.Clamp(home.x + r.x, -55f, 55f),
            transform.position.y,
            Mathf.Clamp(home.z + r.y, -55f, 55f));
        repathTimer = Random.Range(4f, 9f);
    }

    void Update()
    {
        Vector3 to = target - transform.position; to.y = 0f;
        if (to.magnitude < 1f || (repathTimer -= Time.deltaTime) <= 0f) { PickTarget(); return; }

        Vector3 dir = to.normalized;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);
        verticalVel = cc.isGrounded ? -1f : verticalVel - 20f * Time.deltaTime;
        cc.Move((dir * speed + Vector3.up * verticalVel) * Time.deltaTime);
    }
}
