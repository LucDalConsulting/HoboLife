using UnityEngine;

// HoboLife — marker on a landmark building. PlayerInteractor reads this to show
// the "Press E to enter" prompt and route to CityServices.
public class BuildingDoor : MonoBehaviour
{
    public string displayName = "Building";
    public string kind = "generic";
    public float enterRange = 9f;
}
