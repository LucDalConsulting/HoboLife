using System;
using System.IO;
using UnityEngine;

// HoboLife — reads/writes the save as JSON in Application.persistentDataPath.
public static class SaveSystem
{
    public static string SavePath => Path.Combine(Application.persistentDataPath, "hobolife.save.v1.json");

    public static void Write(SaveData d)
    {
        try
        {
            d.savedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            d.version = HoboBalance.VERSION;
            File.WriteAllText(SavePath, JsonUtility.ToJson(d, true));
        }
        catch (Exception e) { Debug.LogWarning("[HoboLife] Save failed: " + e.Message); }
    }

    public static SaveData Load()
    {
        try
        {
            if (!File.Exists(SavePath)) return null;
            var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            return (d != null && d.id != null) ? d : null;
        }
        catch { return null; }
    }

    public static void Delete()
    {
        try { if (File.Exists(SavePath)) File.Delete(SavePath); } catch { }
    }

    // SSN in XXX-XX-XXXX form, matching the web build's ranges.
    public static string GenerateSSN()
    {
        return UnityEngine.Random.Range(100, 900) + "-"
             + UnityEngine.Random.Range(10, 100).ToString("00") + "-"
             + UnityEngine.Random.Range(1000, 10000);
    }
}
