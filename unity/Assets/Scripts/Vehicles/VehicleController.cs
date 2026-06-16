using UnityEngine;

// HoboLife — press V to hop in/out of an owned vehicle for faster travel. A car
// needs a license; a skateboard doesn't. Ownership + license persist on the
// account (survive death); the active mount is dropped on death.
public class VehicleController : MonoBehaviour
{
    ThirdPersonController tpc;
    GameStateController gsc;
    float baseSpeed;
    bool mounted;

    public bool IsMounted => mounted;

    void Start()
    {
        tpc = GetComponent<ThirdPersonController>();
        gsc = Object.FindFirstObjectByType<GameStateController>();
        if (tpc != null) baseSpeed = tpc.moveSpeed;
    }

    void Update()
    {
        if (tpc == null || gsc == null || gsc.Data == null) return;
        if (Input.GetKeyDown(KeyCode.V)) Toggle();
    }

    void Toggle()
    {
        if (mounted) { mounted = false; tpc.moveSpeed = baseSpeed; Debug.Log("[HoboLife] Hopped out of your vehicle."); return; }

        var d = gsc.Data;
        if (d.ownsCar && d.hasLicense) { mounted = true; tpc.moveSpeed = baseSpeed * 3f; Debug.Log("[HoboLife] Driving your car (V to exit)."); }
        else if (d.ownsCar) { Debug.Log("[HoboLife] You need a driver's license to drive the car (take the test at the dealer)."); }
        else if (d.ownsSkateboard) { mounted = true; tpc.moveSpeed = baseSpeed * 1.6f; Debug.Log("[HoboLife] Riding your skateboard (V to exit)."); }
        else { Debug.Log("[HoboLife] You don't own a vehicle yet. Buy one at Honest Hal Autos."); }
    }

    void OnDisable() { if (tpc != null) tpc.moveSpeed = baseSpeed; }
}
