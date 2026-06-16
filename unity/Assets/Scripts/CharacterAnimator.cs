using UnityEngine;

// HoboLife — simple procedural character animation (deliberately RuneScape-simple).
// Swings arms/legs while walking (speed-driven) and adds a gentle idle sway.
// No animation clips or rig needed: it drives limb "pivot" transforms directly,
// so it works on the blocky humanoid built by HoboLifeCharacterBuilder.
public class CharacterAnimator : MonoBehaviour
{
    public Transform armL, armR, legL, legR, torso, head;

    [Tooltip("Swing cadence (radians/sec) scaled by speed.")]
    public float walkCadence = 9f;
    [Tooltip("Max limb swing angle (deg) at full speed.")]
    public float maxSwingDeg = 38f;
    [Tooltip("Speed at which the swing is maxed out.")]
    public float refSpeed = 4.5f;
    public float idleSwayDeg = 4f;
    public float idleSpeed = 1.6f;

    private CharacterController cc;
    private float phase;

    void Awake()
    {
        cc = GetComponentInParent<CharacterController>();
        if (cc == null) cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        float speed = 0f;
        if (cc != null) { Vector3 v = cc.velocity; v.y = 0f; speed = v.magnitude; }
        float t = Time.time;

        if (speed > 0.15f)
        {
            phase += Time.deltaTime * walkCadence * Mathf.Clamp(speed, 0.5f, refSpeed);
            float amt = Mathf.Clamp01(speed / refSpeed);
            float swing = Mathf.Sin(phase) * maxSwingDeg * amt;
            SetPitch(armL, swing);
            SetPitch(armR, -swing);   // arms contralateral to legs
            SetPitch(legL, -swing);
            SetPitch(legR, swing);
            if (torso) torso.localRotation = Quaternion.Euler(Mathf.Abs(Mathf.Sin(phase)) * 2.5f, 0f, 0f);
        }
        else
        {
            float s = Mathf.Sin(t * idleSpeed) * idleSwayDeg;
            SetPitch(armL, s * 0.4f);
            SetPitch(armR, s * 0.4f);
            SetPitch(legL, 0f);
            SetPitch(legR, 0f);
            if (torso) torso.localRotation = Quaternion.Euler(s * 0.2f, 0f, 0f);
            if (head) head.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * idleSpeed * 0.7f) * 3f, 0f);
        }
    }

    void SetPitch(Transform tr, float deg)
    {
        if (tr != null) tr.localRotation = Quaternion.Euler(deg, 0f, 0f);
    }
}
