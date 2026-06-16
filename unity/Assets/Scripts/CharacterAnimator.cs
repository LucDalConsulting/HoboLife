using UnityEngine;

// HoboLife — procedural character animation with weight: eased limb swing, a
// vertical body bob synced to footfalls, a forward lean while moving, idle
// breathing + weight-shift sway, and subtle idle head movement. Speed-driven,
// no clips/rig. Drives the rounded humanoid from HumanoidFactory.
public class CharacterAnimator : MonoBehaviour
{
    public Transform body;           // bob + lean container
    public Transform armL, armR, legL, legR, torso, head;

    public float walkCadence = 8f;
    public float maxSwingDeg = 42f;
    public float refSpeed = 4.5f;
    public float bobHeight = 0.07f;
    public float leanDeg = 9f;

    CharacterController cc;
    float phase, gaitSmooth;
    Vector3 bodyBasePos;
    Quaternion bodyBaseRot;

    void Awake()
    {
        cc = GetComponentInParent<CharacterController>();
        if (cc == null) cc = GetComponent<CharacterController>();
        if (body != null) { bodyBasePos = body.localPosition; bodyBaseRot = body.localRotation; }
    }

    void Update()
    {
        float speed = 0f;
        if (cc != null) { Vector3 v = cc.velocity; v.y = 0f; speed = v.magnitude; }
        float gait = Mathf.Clamp01(speed / refSpeed);
        gaitSmooth = Mathf.Lerp(gaitSmooth, gait, Time.deltaTime * 8f);
        float t = Time.time;

        if (speed > 0.15f)
            phase += Time.deltaTime * walkCadence * Mathf.Clamp(speed, 0.6f, refSpeed);

        float swing = Mathf.Sin(phase) * maxSwingDeg * gaitSmooth;
        SetPitch(legL, swing);
        SetPitch(legR, -swing);
        SetPitch(armL, -swing * 0.9f);
        SetPitch(armR, swing * 0.9f);

        float breathe = Mathf.Sin(t * 1.6f) * (1f - gaitSmooth);
        if (torso) torso.localRotation = Quaternion.Euler(2.5f * gaitSmooth, swing * 0.12f, breathe * 1.6f);

        if (body != null)
        {
            float bob = Mathf.Abs(Mathf.Sin(phase)) * bobHeight * gaitSmooth + breathe * 0.012f;
            body.localPosition = bodyBasePos + new Vector3(0f, bob, 0f);
            float lean = leanDeg * gaitSmooth;
            float idleSway = Mathf.Sin(t * 0.8f) * 2f * (1f - gaitSmooth);
            body.localRotation = bodyBaseRot * Quaternion.Euler(lean, 0f, idleSway);
        }

        if (head != null)
            head.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 0.5f) * 11f * (1f - gaitSmooth), 0f);
    }

    void SetPitch(Transform tr, float deg)
    {
        if (tr != null) tr.localRotation = Quaternion.Euler(deg, 0f, 0f);
    }
}
