using UnityEngine;

// HoboLife — runtime builder for the blocky humanoid body (used for NPCs).
// Mirrors HoboLifeCharacterBuilder.BuildBody but is callable at runtime and
// wires a CharacterAnimator on the root.
public static class HumanoidFactory
{
    public static void BuildBody(Transform root, Color skin, Color undies, Color hair)
    {
        Material mSkin = Mat(skin), mUnd = Mat(undies), mHair = Mat(hair);

        var body = new GameObject("Body").transform;
        body.SetParent(root, false);
        body.localPosition = Vector3.zero;

        Cube(body, "Pelvis", new Vector3(0f, -0.30f, 0f), new Vector3(0.52f, 0.30f, 0.32f), mUnd);
        var torso = Empty(body, "Torso", new Vector3(0f, -0.15f, 0f));
        Cube(torso, "Chest", new Vector3(0f, 0.30f, 0f), new Vector3(0.56f, 0.60f, 0.34f), mSkin);
        var head = Empty(torso, "Head", new Vector3(0f, 0.70f, 0f));
        Cube(head, "HeadMesh", Vector3.zero, new Vector3(0.34f, 0.36f, 0.34f), mSkin);
        Cube(head, "Hair", new Vector3(0f, 0.18f, -0.02f), new Vector3(0.38f, 0.12f, 0.40f), mHair);
        var armL = Empty(torso, "ArmL", new Vector3(0.40f, 0.45f, 0f));
        Cube(armL, "ArmLMesh", new Vector3(0f, -0.30f, 0f), new Vector3(0.15f, 0.60f, 0.15f), mSkin);
        var armR = Empty(torso, "ArmR", new Vector3(-0.40f, 0.45f, 0f));
        Cube(armR, "ArmRMesh", new Vector3(0f, -0.30f, 0f), new Vector3(0.15f, 0.60f, 0.15f), mSkin);
        var legL = Empty(body, "LegL", new Vector3(0.16f, -0.35f, 0f));
        Cube(legL, "LegLMesh", new Vector3(0f, -0.325f, 0f), new Vector3(0.20f, 0.65f, 0.20f), mSkin);
        var legR = Empty(body, "LegR", new Vector3(-0.16f, -0.35f, 0f));
        Cube(legR, "LegRMesh", new Vector3(0f, -0.325f, 0f), new Vector3(0.20f, 0.65f, 0.20f), mSkin);

        var anim = root.GetComponent<CharacterAnimator>();
        if (anim == null) anim = root.gameObject.AddComponent<CharacterAnimator>();
        anim.armL = armL; anim.armR = armR; anim.legL = legL; anim.legR = legR;
        anim.torso = torso; anim.head = head;
    }

    static Transform Empty(Transform parent, string name, Vector3 lp)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = lp;
        return g.transform;
    }

    static void Cube(Transform parent, string name, Vector3 lp, Vector3 scale, Material m)
    {
        var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.name = name;
        c.transform.SetParent(parent, false);
        c.transform.localPosition = lp;
        c.transform.localScale = scale;
        var col = c.GetComponent<Collider>();
        if (col) Object.Destroy(col);
        c.GetComponent<Renderer>().sharedMaterial = m;
    }

    static Material Mat(Color c)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        m.color = c;
        return m;
    }
}
