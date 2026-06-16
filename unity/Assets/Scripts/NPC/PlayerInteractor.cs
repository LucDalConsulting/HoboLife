using UnityEngine;

// HoboLife — on the Player. Finds the nearest NPC in range, shows the talk prompt,
// and opens dialogue on Q. (Building 'E' interaction is added with the city.)
public class PlayerInteractor : MonoBehaviour
{
    public float talkRange = 3.5f;

    void Update()
    {
        if (DialogueUI.Instance == null) return;
        if (DialogueUI.Instance.IsOpen) return;

        NpcWander nearest = FindNearest();
        if (nearest != null)
        {
            DialogueUI.Instance.ShowPrompt("Press Q to talk to " + nearest.displayName);
            if (Input.GetKeyDown(KeyCode.Q))
                DialogueUI.Instance.Open(nearest.treeId, nearest.displayName);
        }
        else
        {
            DialogueUI.Instance.HidePrompt();
        }
    }

    NpcWander FindNearest()
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
}
