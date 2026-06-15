using UnityEditor;
using UnityEngine;
using MCPForUnity.Editor.Services;
using MCPForUnity.Editor.Clients.Configurators;
using MCPForUnity.Editor.Services.Transport.Transports;

// One-time wiring of the Unity MCP connector so Claude (the Cowork desktop app)
// can drive this editor over a local socket with NO screen control:
//   1. point the bridge at the installed uvx.exe (no PATH dependency)
//   2. write the Claude Desktop MCP config (the mcpServers entry the app reads)
//   3. enable auto-start so the bridge listens whenever the editor is open
//   4. start the bridge listener now
//
// Auto-runs once (guarded by an EditorPrefs flag); also re-runnable from the
// HoboLife menu. Every step is isolated in try/catch and logged, so one failure
// never blocks the others and the outcome is visible in the Editor log.
[InitializeOnLoad]
public static class HoboLifeMcpSetup
{
    const string UvxPath = @"C:\Users\dalim\.local\bin\uvx.exe";
    const string DoneKey = "HoboLife.McpSetup.Done.v1";

    static HoboLifeMcpSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorPrefs.GetBool(DoneKey, false)) Run("auto");
        };
    }

    [MenuItem("HoboLife/Setup MCP Connector (Claude)")]
    public static void Menu() => Run("manual");

    static void Run(string how)
    {
        try
        {
            EditorPrefs.SetString("MCPForUnity.UvxPath", UvxPath);
            Debug.Log("[HoboLife MCP] UvxPath -> " + UvxPath);
        }
        catch (System.Exception e) { Debug.LogError("[HoboLife MCP] UvxPath set failed: " + e.Message); }

        try
        {
            EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);
            Debug.Log("[HoboLife MCP] AutoStartOnLoad = true");
        }
        catch (System.Exception e) { Debug.LogError("[HoboLife MCP] AutoStartOnLoad failed: " + e.Message); }

        try
        {
            new ClientConfigurationService().ConfigureClient(new ClaudeDesktopConfigurator());
            Debug.Log("[HoboLife MCP] Claude Desktop config written (mcpServers entry).");
        }
        catch (System.Exception e) { Debug.LogError("[HoboLife MCP] Configure Claude Desktop failed: " + e.Message); }

        try
        {
            StdioBridgeHost.StartAutoConnect();
            Debug.Log("[HoboLife MCP] Bridge StartAutoConnect() called.");
        }
        catch (System.Exception e) { Debug.LogError("[HoboLife MCP] StartAutoConnect failed: " + e.Message); }

        EditorPrefs.SetBool(DoneKey, true);
        Debug.Log("[HoboLife MCP] Setup complete (" + how + "). Restart the Claude desktop app to load the connector.");
    }
}
