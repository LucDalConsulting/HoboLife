using UnityEngine;

// HoboLife — runtime builder for a rounded low-poly humanoid (capsule limbs/torso,
// sphere head with a face, neck, hands, feet). Far less "Minecraft" than cubes.
// Wires a CharacterAnimator (incl. the Body bob/lean container) on the root.
// Sized for a CharacterController height 2 centered at 0 (feet at local y=-1).
public static class HumanoidFactory
{
    public static void BuildBody(Transform root, Color skin, Color clothes, Color hair)
    {
        Material mSkin = Mat(skin), mCloth = Mat(clothes), mHair = Mat(hair);
        Material mEye = Mat(new Color(0.08f, 0.07f, 0.07f));
        Material mShoe = Mat(new Color(0.15f, 0.14f, 0.16f));

        // Body = the bob/lean container the animator drives.
        var body = new GameObject("Body").transform;
        body.SetParent(root, false);
        body.localPosition = Vector3.zero;

        // Hips / underwear
        Prim(body, "Hips", PrimitiveType.Sphere, new Vector3(0f, -0.40f, 0f), new Vector3(0.46f, 0.30f, 0.36f), mCloth);

        // Torso pivot
        var torso = Empty(body, "Torso", new Vector3(0f, -0.30f, 0f));
        Prim(torso, "Chest", PrimitiveType.Capsule, new Vector3(0f, 0.34f, 0f), new Vector3(0.46f, 0.34f, 0.40f), mSkin);
        Prim(torso, "Neck", PrimitiveType.Cylinder, new Vector3(0f, 0.60f, 0f), new Vector3(0.14f, 0.08f, 0.14f), mSkin);

        // Head
        var head = Empty(torso, "Head", new Vector3(0f, 0.76f, 0f));
        Prim(head, "HeadMesh", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.34f, 0.38f, 0.34f), mSkin);
        Prim(head, "Hair", PrimitiveType.Sphere, new Vector3(0f, 0.07f, -0.02f), new Vector3(0.37f, 0.30f, 0.38f), mHair);
        Prim(head, "EyeL", PrimitiveType.Sphere, new Vector3(0.08f, 0.0f, -0.15f), new Vector3(0.06f, 0.07f, 0.04f), mEye);
        Prim(head, "EyeR", PrimitiveType.Sphere, new Vector3(-0.08f, 0.0f, -0.15f), new Vector3(0.06f, 0.07f, 0.04f), mEye);

        // Arms (pivot at shoulder)
        var armL = Empty(torso, "ArmL", new Vector3(0.33f, 0.50f, 0f));
        Prim(armL, "UpperArmL", PrimitiveType.Capsule, new Vector3(0f, -0.26f, 0f), new Vector3(0.14f, 0.30f, 0.14f), mSkin);
        Prim(armL, "HandL", PrimitiveType.Sphere, new Vector3(0f, -0.54f, 0f), new Vector3(0.13f, 0.13f, 0.13f), mSkin);
        var armR = Empty(torso, "ArmR", new Vector3(-0.33f, 0.50f, 0f));
        Prim(armR, "UpperArmR", PrimitiveType.Capsule, new Vector3(0f, -0.26f, 0f), new Vector3(0.14f, 0.30f, 0.14f), mSkin);
        Prim(armR, "HandR", PrimitiveType.Sphere, new Vector3(0f, -0.54f, 0f), new Vector3(0.13f, 0.13f, 0.13f), mSkin);

        // Legs (pivot at hip)
        var legL = Empty(body, "LegL", new Vector3(0.14f, -0.40f, 0f));
        Prim(legL, "ThighL", PrimitiveType.Capsule, new Vector3(0f, -0.28f, 0f), new Vector3(0.18f, 0.32f, 0.18f), mSkin);
        Prim(legL, "FootL", PrimitiveType.Capsule, new Vector3(0f, -0.56f, 0.06f), new Vector3(0.16f, 0.10f, 0.28f), mShoe);
        var legR = Empty(body, "LegR", new Vector3(-0.14f, -0.40f, 0f));
        Prim(legR, "ThighR", PrimitiveType.Capsule, new Vector3(0f, -0.28f, 0f), new Vector3(0.18f, 0.32f, 0.18f), mSkin);
        Prim(legR, "FootR", PrimitiveType.Capsule, new Vector3(0f, -0.56f, 0.06f), new Vector3(0.16f, 0.10f, 0.28f), mShoe);

        var anim = root.GetComponent<CharacterAnimator>();
        if (anim == null) anim = root.gameObject.AddComponent<CharacterAnimator>();
        anim.body = body; anim.torso = torso; anim.head = head;
        anim.armL = armL; anim.armR = armR; anim.legL = legL; anim.legR = legR;
    }

    static Transform Empty(Transform parent, string name, Vector3 lp)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = lp;
        return g.transform;
    }

    static void Prim(Transform parent, string name, PrimitiveType type, Vector3 lp, Vector3 scale, Material m)
    {
        var c = GameObject.CreatePrimitive(type);
        c.name = name;
        c.transform.SetParent(parent, false);
        c.transform.localPosition = lp;
        c.transform.localScale = scale;
        var col = c.GetComponent<Collider>();
        if (col) Object.DestroyImmediate(col);
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
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.2f);
        return m;
    }
}
