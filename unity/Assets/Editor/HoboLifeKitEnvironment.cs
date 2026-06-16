using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// HoboLife — swaps the primitive city dressing for real Kenney models (CC0):
// buildings -> City Kit Commercial, palms -> Nature Kit trees, blocky parked cars
// -> Car Kit vehicles. Building gameplay is preserved: each landmark keeps its
// GameObject + BoxCollider + BuildingDoor; only the box MESH is hidden and a
// uniformly-scaled Kenney building is dropped in as a sibling (so the parent's
// non-uniform box scale doesn't distort it). Re-runnable.
public static class HoboLifeKitEnvironment
{
    const string Com = "Assets/Kit/city-kit-commercial/";
    const string Nat = "Assets/Kit/nature-kit/";
    const string Car = "Assets/Kit/car-kit/";

    // landmark -> Kenney model (skyscrapers for the tall/important ones)
    static readonly Dictionary<string, string> BuildingMap = new Dictionary<string, string>
    {
        {"bank","building-skyscraper-b"}, {"casino","building-skyscraper-d"},
        {"university","building-skyscraper-a"}, {"hospital","building-skyscraper-e"},
        {"gym","building-h"}, {"diner","building-d"}, {"clothing","building-k"},
        {"cardealer","building-g"}, {"realtor","building-f"}, {"pawn","building-c"},
    };
    static readonly string[] Trees = { "tree_default", "tree_oak", "tree_tall", "tree_fat", "tree_detailed", "tree_pineDefaultA" };
    static readonly string[] Cars = { "sedan", "taxi", "police", "van", "suv", "hatchback-sports", "sedan-sports", "delivery" };

    [MenuItem("HoboLife/Setup Kenney Environment (buildings+cars+trees)")]
    public static void Setup()
    {
        SwapBuildings();
        SwapTrees();
        SwapCars();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[HoboLife] Kenney environment built: buildings + trees + cars.");
    }

    static void SwapBuildings()
    {
        var city = GameObject.Find("City");
        if (city == null) return;
        var oldRoot = GameObject.Find("KenneyBuildings");
        if (oldRoot) Object.DestroyImmediate(oldRoot);
        var root = new GameObject("KenneyBuildings").transform;
        int i = 0;
        foreach (Transform b in city.transform)
        {
            if (!b.name.StartsWith("Building_")) continue;
            var key = b.name.Substring(9);
            var mr = b.GetComponent<MeshRenderer>();
            float primH = mr != null ? mr.bounds.size.y : 10f;
            float primW = mr != null ? Mathf.Max(mr.bounds.size.x, mr.bounds.size.z) : 11f;
            if (mr) mr.enabled = false;                      // hide the box, keep collider + BuildingDoor

            string model = BuildingMap.ContainsKey(key) ? BuildingMap[key] : "building-a";
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(Com + model + ".fbx");
            if (pf == null) pf = AssetDatabase.LoadAssetAtPath<GameObject>(Com + "building-a.fbx");
            if (pf == null) continue;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(pf);
            inst.name = "KB_" + key;
            inst.transform.SetParent(root, false);
            inst.transform.localScale = Vector3.one;
            var size = Measure(inst);
            // scale so the model roughly matches the lot: keep height in family, but don't exceed lot width too much
            float byH = size.y > 0.01f ? primH / size.y : 5f;
            float byW = Mathf.Max(size.x, size.z) > 0.01f ? primW / Mathf.Max(size.x, size.z) : byH;
            float scale = Mathf.Min(byH, byW) * 1.15f;       // fit, leaning to fill the lot a bit
            inst.transform.localScale = Vector3.one * scale;
            inst.transform.position = new Vector3(b.position.x, 0f, b.position.z);
            // face the plaza (origin)
            Vector3 toCenter = new Vector3(-b.position.x, 0f, -b.position.z);
            if (toCenter.sqrMagnitude > 0.01f) inst.transform.rotation = Quaternion.LookRotation(toCenter.normalized, Vector3.up);
            i++;
        }
        Debug.Log("[HoboLife] swapped " + i + " buildings to Kenney models.");
    }

    static void SwapTrees()
    {
        var props = GameObject.Find("Props");
        if (props == null) return;
        var oldRoot = GameObject.Find("KenneyTrees");
        if (oldRoot) Object.DestroyImmediate(oldRoot);
        var root = new GameObject("KenneyTrees").transform;
        int i = 0;
        var palms = new List<Transform>();
        foreach (Transform ch in props.transform) if (ch.name.StartsWith("PalmTree")) palms.Add(ch);
        foreach (var palm in palms)
        {
            string t = Trees[i % Trees.Length];
            var pf = LoadTree(t);
            if (pf == null) { i++; continue; }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(pf);
            inst.name = "KTree";
            inst.transform.SetParent(root, false);
            inst.transform.localScale = Vector3.one;
            float h = Measure(inst).y;
            float scale = h > 0.01f ? 5.0f / h : 3f;
            inst.transform.localScale = Vector3.one * scale * Random.Range(0.85f, 1.2f);
            inst.transform.position = new Vector3(palm.position.x, 0f, palm.position.z);
            inst.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            palm.gameObject.SetActive(false);                // hide the primitive palm
            i++;
        }
        Debug.Log("[HoboLife] swapped " + i + " palms to Kenney trees.");
    }

    static void SwapCars()
    {
        var sd = GameObject.Find("StreetDetail");
        if (sd == null) return;
        var oldRoot = GameObject.Find("KenneyCars");
        if (oldRoot) Object.DestroyImmediate(oldRoot);
        var root = new GameObject("KenneyCars").transform;
        int i = 0;
        var cars = new List<Transform>();
        foreach (Transform ch in sd.transform) if (ch.name.StartsWith("ParkedCar")) cars.Add(ch);
        foreach (var car in cars)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(Car + Cars[i % Cars.Length] + ".fbx");
            if (pf == null) { i++; continue; }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(pf);
            inst.name = "KCar";
            inst.transform.SetParent(root, false);
            inst.transform.localScale = Vector3.one;
            float len = Mathf.Max(Measure(inst).x, Measure(inst).z);
            float scale = len > 0.01f ? 4.2f / len : 1.4f;
            inst.transform.localScale = Vector3.one * scale;
            inst.transform.position = new Vector3(car.position.x, 0f, car.position.z);
            inst.transform.rotation = car.rotation;          // inherit the parked orientation
            car.gameObject.SetActive(false);
            i++;
        }
        Debug.Log("[HoboLife] swapped " + i + " parked cars to Kenney vehicles.");
    }

    static GameObject LoadTree(string name)
    {
        var pf = AssetDatabase.LoadAssetAtPath<GameObject>(Nat + name + ".fbx");
        if (pf == null) pf = AssetDatabase.LoadAssetAtPath<GameObject>(Nat + "tree_default.fbx");
        return pf;
    }

    static Vector3 Measure(GameObject go)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return Vector3.one;
        var b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);
        return b.size;
    }
}
