using System.Collections.Generic;

// HoboLife — item definitions. Short text "icon" tags render in the default font
// (emoji don't). Foods are consumed on use; weapons set the combat move set.
public enum ItemKind { Food, Weapon, Tool, Clothing, Misc }

public class ItemDef
{
    public string id, name, icon;
    public ItemKind kind;
    public bool twoHanded;
    public float hungerRestore, healthRestore;
    public int damageBonus;
    public string combatStyle = "fists"; // "fists" | "knife" | "gun"

    public ItemDef(string id, string name, string icon, ItemKind kind)
    { this.id = id; this.name = name; this.icon = icon; this.kind = kind; }
}

public static class ItemCatalog
{
    public static readonly Dictionary<string, ItemDef> All = Build();

    public static ItemDef Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return All.TryGetValue(id, out var d) ? d : null;
    }

    static Dictionary<string, ItemDef> Build()
    {
        var d = new Dictionary<string, ItemDef>();
        void Add(ItemDef i) { d[i.id] = i; }

        Add(new ItemDef("smoothie", "Fruit Smoothie", "SMO", ItemKind.Food) { hungerRestore = 20f, healthRestore = 5f });
        Add(new ItemDef("burger", "Greasy Burger", "BUR", ItemKind.Food) { hungerRestore = 35f });
        Add(new ItemDef("sandwich", "Sandwich", "SAN", ItemKind.Food) { hungerRestore = 18f });
        Add(new ItemDef("knife", "Knife", "KNF", ItemKind.Weapon) { damageBonus = 12, combatStyle = "knife" });
        Add(new ItemDef("pistol", "Pistol", "PST", ItemKind.Weapon) { damageBonus = 28, combatStyle = "gun" });
        Add(new ItemDef("bat", "Baseball Bat", "BAT", ItemKind.Weapon) { damageBonus = 16, twoHanded = true });
        Add(new ItemDef("cardboard_sign", "Cardboard Sign", "SGN", ItemKind.Tool));
        Add(new ItemDef("tshirt", "T-Shirt", "TEE", ItemKind.Clothing));

        return d;
    }
}
