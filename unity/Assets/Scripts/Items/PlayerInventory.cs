using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// HoboLife — two hands + a grid pack on the Player. Left-click uses the left
// hand, right-click the right hand (click, not camera-drag). I opens the grid.
// Food is consumed on use; weapons set the combat move set (read by combat).
public class PlayerInventory : MonoBehaviour
{
    public const int PackSize = 12;

    public string leftHand;
    public string rightHand = "cardboard_sign";
    public string[] pack = new string[PackSize];

    PlayerStats stats;
    float[] downTime = new float[2];
    Vector3[] downPos = new Vector3[2];

    void Start()
    {
        stats = GetComponent<PlayerStats>();
        AddToPack("smoothie");
        AddToPack("burger");
        RefreshHud();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I) && InventoryUI.Instance != null) InventoryUI.Instance.Toggle();

        bool blocked = (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)
                    || (DialogueUI.Instance != null && DialogueUI.Instance.IsOpen);
        if (blocked) return;

        TrackClick(0, true);
        TrackClick(1, false);
    }

    void TrackClick(int btn, bool left)
    {
        if (Input.GetMouseButtonDown(btn)) { downTime[btn] = Time.time; downPos[btn] = Input.mousePosition; }
        if (Input.GetMouseButtonUp(btn))
        {
            float dt = Time.time - downTime[btn];
            float dist = (Input.mousePosition - downPos[btn]).magnitude;
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (dt < 0.3f && dist < 10f && !overUI) UseHand(left);
        }
    }

    public void UseHand(bool left)
    {
        string id = left ? leftHand : rightHand;
        var item = ItemCatalog.Get(id);
        if (item == null) return;

        if (item.kind == ItemKind.Food)
        {
            if (stats != null)
            {
                stats.hunger = Mathf.Min(HoboBalance.HUNGER_MAX, stats.hunger + item.hungerRestore);
                stats.health = Mathf.Min(stats.maxHealth, stats.health + item.healthRestore);
            }
            if (left) leftHand = null; else rightHand = null;
            Debug.Log("[HoboLife] Ate " + item.name);
            RefreshHud();
        }
        else
        {
            Debug.Log("[HoboLife] Wielding " + item.name + " (" + item.combatStyle + ")");
        }
    }

    public bool AddToPack(string id)
    {
        for (int i = 0; i < pack.Length; i++)
            if (string.IsNullOrEmpty(pack[i])) { pack[i] = id; RefreshHud(); return true; }
        return false;
    }

    // Click a pack slot in the inventory UI -> equip to a free hand (or swap with right).
    public void EquipFromPack(int slot)
    {
        if (slot < 0 || slot >= pack.Length || string.IsNullOrEmpty(pack[slot])) return;
        string id = pack[slot];
        var def = ItemCatalog.Get(id);
        if (string.IsNullOrEmpty(leftHand) && !(def != null && def.twoHanded)) { leftHand = id; pack[slot] = null; }
        else { string prev = rightHand; rightHand = id; pack[slot] = prev; }
        if (def != null && def.twoHanded) { /* two-handed: clear the other hand into pack */ if (!string.IsNullOrEmpty(leftHand)) { AddToPack(leftHand); leftHand = null; } }
        RefreshHud();
    }

    // Click a hand slot -> use food, else stash to pack.
    public void HandSlotClicked(bool left)
    {
        string id = left ? leftHand : rightHand;
        var def = ItemCatalog.Get(id);
        if (def == null) return;
        if (def.kind == ItemKind.Food) { UseHand(left); return; }
        if (AddToPack(id)) { if (left) leftHand = null; else rightHand = null; RefreshHud(); }
    }

    public void RefreshHud()
    {
        SetHandLabel("HandLKey", leftHand, "L");
        SetHandLabel("HandRKey", rightHand, "R");
        if (InventoryUI.Instance != null) InventoryUI.Instance.Rebuild(this);
    }

    static void SetHandLabel(string objName, string itemId, string fallback)
    {
        var go = GameObject.Find(objName);
        if (go == null) return;
        var t = go.GetComponent<Text>();
        if (t == null) return;
        var item = ItemCatalog.Get(itemId);
        t.text = item != null ? item.icon : fallback;
    }
}
