using UnityEngine;

// HoboLife — spawns a handful of wandering NPCs (the slice subset of the web
// build's pedestrians) around the block, each a blocky humanoid with a random
// identity. The last one is a thug (its own dialogue tree) so both trees show.
public class NpcSpawner : MonoBehaviour
{
    public int count = 5;

    static readonly string[] First = { "Marcus", "Tanya", "Derek", "Luis", "Aisha", "Vince", "Carla", "Hank", "Rosa", "Eddie" };
    static readonly string[] Last = { "Reyes", "Cole", "Nguyen", "Park", "Okafor", "Romano", "Diaz", "Webb", "Flores", "Banks" };
    static readonly Color[] Skins = { new Color(0.85f, 0.72f, 0.55f), new Color(0.74f, 0.55f, 0.40f), new Color(0.55f, 0.40f, 0.28f) };
    static readonly Color[] Shirts = { new Color(0.36f, 0.50f, 0.72f), new Color(0.30f, 0.60f, 0.45f), new Color(0.72f, 0.40f, 0.40f), new Color(0.50f, 0.50f, 0.56f) };

    void Start()
    {
        for (int i = 0; i < count; i++)
        {
            bool thug = (i == count - 1);
            var go = new GameObject(thug ? "NPC_Thug" : "NPC_" + i);

            float ang = (i / (float)count) * Mathf.PI * 2f;
            float rad = Random.Range(10f, 28f);
            go.transform.position = new Vector3(Mathf.Cos(ang) * rad, 1.1f, Mathf.Sin(ang) * rad);

            var cc = go.AddComponent<CharacterController>();
            cc.center = Vector3.zero; cc.height = 2f; cc.radius = 0.45f;

            Color skin = Skins[Random.Range(0, Skins.Length)];
            Color shirt = thug ? new Color(0.17f, 0.18f, 0.26f) : Shirts[Random.Range(0, Shirts.Length)];
            Color hair = new Color(Random.Range(0.10f, 0.35f), Random.Range(0.08f, 0.25f), Random.Range(0.05f, 0.18f));
            HumanoidFactory.BuildBody(go.transform, skin, shirt, hair);

            var w = go.AddComponent<NpcWander>();
            w.treeId = thug ? "thug" : "pedestrian";
            w.displayName = thug ? "Street Thug" : (First[Random.Range(0, First.Length)] + " " + Last[Random.Range(0, Last.Length)]);
        }
        Debug.Log("[HoboLife] Spawned " + count + " NPCs.");
    }
}
