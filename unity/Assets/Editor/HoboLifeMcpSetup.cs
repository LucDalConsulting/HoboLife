using UnityEditor;
using UnityEngine;
using MCPForUnity.Editor.Services;
using MCPForUnity.Editor.Clients.Configurators;
using MCPForUnity.Editor.Services.Transport.Transports;

// Wires the Unity MCP connector so Claude (the Cowork desktop app) can drive this
// editor over a local socket with NO screen control.
//
// The Claude Desktop client only supports the STDIO transport, so we must run the
// stdio bridge (a TCP listener the server discovers by port) — NOT the HTTP server.
// This runs on every domain reload so the bridge is always up while the editor is
// open; the one-time client config write is guarded by an EditorPrefs flag.
[InitializeOnLoad]
public static class HoboLifeMcpSetup
{
    const string UvxPath = @"C:\Users\dalim\.local\bin\uvx.exe";
    const string ConfigDoneKey = "HoboLife.McpConfig.Done.v2";

    static HoboLifeMcpSetup()
    {
        EditorApplication.delayCall += () =>
        {
            EnsureStdioBridge();              // every load
            if (!EditorPrefs.GetBool(ConfigDoneKey, false))
            {
                WriteClaudeDesktopConfig();   // once
                EditorPrefs.SetBool(ConfigDoneKey, true);
            }
        };
    }

    [MenuItem("HoboLife/Restart MCP Bridge (stdio)")]
    public static void Menu() => EnsureStdioBridge();

    static void EnsureStdioBridge()
    {
        try
        {
            // Force stdio transport and turn off the HTTP auto-start so the bridge
            // the Claude Desktop config expects is the one that runs.
            EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", false);
            EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", false);

            StdioBridgeHost.StartAutoConnect();
            Debug.Log("[HoboLife MCP] stdio bridge started on port " + StdioBridgeHost.GetCurrentPort());
        }
        catch (System.Exception e)
        {
            Debug.LogError("[HoboLife MCP] EnsureStdioBridge failed: " + e.Message);
        }
    }

    static void WriteClaudeDesktopConfig()
    {
        try
        {
            EditorPrefs.SetString("MCPForUnity.UvxPath", UvxPath);
            new ClientConfigurationService().ConfigureClient(new ClaudeDesktopConfigurator());
            Debug.Log("[HoboLife MCP] Claude Desktop config written (stdio).");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[HoboLife MCP] WriteClaudeDesktopConfig failed: " + e.Message);
        }
    }
}
