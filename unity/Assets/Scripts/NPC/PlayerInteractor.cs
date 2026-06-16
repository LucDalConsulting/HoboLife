using UnityEngine;

// HoboLife — central interaction on the Player. Talk to the nearest NPC (Q) or
// enter the nearest landmark building (E). NPCs take priority when one is in
// talk range; otherwise a nearby building door offers entry.
public class PlayerInteractor : MonoBehaviour
{
    public float talkRange = 3.5f;
    PlayerStats stats;

    void Awake() { stats = GetComponent<PlayerStats>(); }

    void Update()
    {
        if (DialogueUI.Instance == null || DialogueUI.Instance.IsOpen) return;
        if (BuildingPanel.Instance != null && BuildingPanel.Instance.IsOpen) { DialogueUI.Instance.HidePrompt(); return; }
        if (JobMiniGame.Instance != null && JobMiniGame.Instance.IsOpen) { DialogueUI.Instance.HidePrompt(); return; }

        NpcWander npc = FindNearestNpc();
        if (npc != null)
        {
            DialogueUI.Instance.ShowPrompt("Press Q to talk to " + npc.displayName);
            if (Input.GetKeyDown(KeyCode.Q))
                DialogueUI.Instance.Open(npc.treeId, npc.displayName);
            return;
        }

        BuildingDoor door = FindNearestDoor();
        if (door != null)
        {
            DialogueUI.Instance.ShowPrompt("Press E to enter " + door.displayName);
            if (Input.GetKeyDown(KeyCode.E) && BuildingPanel.Instance != null)
                BuildingPanel.Instance.Open(door);
            return;
        }

        DialogueUI.Instance.HidePrompt();
    }

    NpcWander FindNearestNpc()
    {
        NpcWander best = null;
        float bestSqr = talkRange * talkRange;
        foreach (var n in Object.FindObjectsByType<NpcWander>(FindObjectsSortMode.None))
        {
            float d = (n.transform.position - transform.position).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = n; }
        }
        return best;
    }

    BuildingDoor FindNearestDoor()
    {
        BuildingDoor best = null;
        float bestDist = float.MaxValue;
        foreach (var b in Object.FindObjectsByType<BuildingDoor>(FindObjectsSortMode.None))
        {
            float d = Vector3.Distance(b.transform.position, transform.position);
            if (d <= b.enterRange && d < bestDist) { bestDist = d; best = b; }
        }
        return best;
    }
}
