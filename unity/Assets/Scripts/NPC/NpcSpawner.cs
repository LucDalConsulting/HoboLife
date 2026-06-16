using UnityEngine;

// HoboLife — populates the block with wandering NPCs of mixed roles: pedestrians
// (chit-chat / panhandle), thugs (fight), and dateable folks (romance tree).
// (A 200+ pooled crowd is the next perf pass; this is a lively starter set.)
public class NpcSpawner : MonoBehaviour
{
    public int count = 24;

    // Real rigged character models (Kenney Mini Characters) + a shared locomotion
    // controller, populated by the HoboLife/Setup Kenney Characters menu. When set,
    // NPCs use these instead of the old primitive HumanoidFactory body.
    public GameObject[] characterPrefabs;
    public RuntimeAnimatorController animController;
    public float charScale = 2.68f;        // ~1.8 m tall
    public float charYOffset = -1.0f;      // feet to capsule bottom

    static readonly string[] MaleFirst = { "Marcus", "Derek", "Luis", "Vince", "Hank", "Eddie", "Tyrone", "Sam", "Carl", "Ray" };
    static readonly string[] FemaleFirst = { "Maria", "Jasmine", "Nicole", "Priya", "Sofia", "Hannah", "Destiny", "Mei", "Carla", "Rosa" };
    static readonly string[] Last = { "Reyes", "Cole", "Nguyen", "Park", "Okafor", "Romano", "Diaz", "Webb", "Flores", "Banks" };
    static readonly Color[] Skins = { new Color(0.85f, 0.72f, 0.55f), new Color(0.74f, 0.55f, 0.40f), new Color(0.55f, 0.40f, 0.28f) };
    static readonly Color[] Shirts = { new Color(0.36f, 0.50f, 0.72f), new Color(0.30f, 0.60f, 0.45f), new Color(0.72f, 0.40f, 0.40f), new Color(0.50f, 0.50f, 0.56f) };

    void Start()
    {
        int thugs = 0, dates = 0;
        for (int i = 0; i < count; i++)
        {
            int m = i % 6;
            string role = m == 5 ? "thug" : m == 2 ? "date" : "pedestrian";

            var go = new GameObject("NPC_" + role + "_" + i);
            float ang = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.25f, 0.25f);
            float rad = Random.Range(10f, 46f);
            go.transform.position = new Vector3(Mathf.Cos(ang) * rad, 1.1f, Mathf.Sin(ang) * rad);

            var cc = go.AddComponent<CharacterController>();
            cc.center = Vector3.zero; cc.height = 2f; cc.radius = 0.45f;

            if (characterPrefabs != null && characterPrefabs.Length > 0 && animController != null)
            {
                var pf = characterPrefabs[Random.Range(0, characterPrefabs.Length)];
                var vis = Instantiate(pf, go.transform);
                vis.transform.localPosition = new Vector3(0f, charYOffset, 0f);
                vis.transform.localRotation = Quaternion.identity;
                vis.transform.localScale = Vector3.one * charScale;
                var anim = vis.GetComponent<Animator>();
                if (anim == null) anim = vis.AddComponent<Animator>();
                anim.runtimeAnimatorController = animController;
                anim.applyRootMotion = false;
                var drv = vis.GetComponent<CharacterRigDriver>();
                if (drv == null) drv = vis.AddComponent<CharacterRigDriver>();
                drv.controller = cc;
            }
            else
            {
                Color skin = Skins[Random.Range(0, Skins.Length)];
                Color shirt = role == "thug" ? new Color(0.17f, 0.18f, 0.26f)
                            : role == "date" ? new Color(0.86f, 0.45f, 0.60f)
                            : Shirts[Random.Range(0, Shirts.Length)];
                Color hair = new Color(Random.Range(0.12f, 0.45f), Random.Range(0.08f, 0.30f), Random.Range(0.05f, 0.22f));
                HumanoidFactory.BuildBody(go.transform, skin, shirt, hair);
            }

            var w = go.AddComponent<NpcWander>();
            w.treeId = role;
            if (role == "thug") { w.displayName = "Street Thug"; thugs++; }
            else if (role == "date") { w.displayName = FemaleFirst[Random.Range(0, FemaleFirst.Length)] + " " + Last[Random.Range(0, Last.Length)]; dates++; }
            else w.displayName = MaleFirst[Random.Range(0, MaleFirst.Length)] + " " + Last[Random.Range(0, Last.Length)];
        }
        Debug.Log("[HoboLife] Spawned " + count + " NPCs (" + thugs + " thugs, " + dates + " dateable, rest pedestrians).");
    }
}
