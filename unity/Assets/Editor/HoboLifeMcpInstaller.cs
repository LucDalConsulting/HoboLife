using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

// Ensures the Unity MCP bridge package (CoplayDev) is installed so Claude can
// drive the editor over a local socket without controlling the screen.
//
// Idempotent: on each domain reload it lists packages and only calls Add when
// the bridge is missing. Logs the outcome so the result is visible in the log.
[InitializeOnLoad]
public static class HoboLifeMcpInstaller
{
    const string Pkg = "com.coplaydev.unity-mcp";
    const string GitId = "com.coplaydev.unity-mcp@https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main";

    static ListRequest _list;
    static AddRequest _add;

    static HoboLifeMcpInstaller()
    {
        _list = Client.List(true, false);
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (_list != null && _list.IsCompleted)
        {
            bool present = false;
            if (_list.Status == StatusCode.Success)
                foreach (var p in _list.Result)
                    if (p.name == Pkg) { present = true; break; }
            _list = null;

            if (present)
            {
                Debug.Log("[HoboLife MCP] Bridge package already present.");
                EditorApplication.update -= Tick;
                return;
            }
            Debug.Log("[HoboLife MCP] Bridge package missing - installing from Git...");
            _add = Client.Add(GitId);
        }

        if (_add != null && _add.IsCompleted)
        {
            if (_add.Status == StatusCode.Success)
                Debug.Log("[HoboLife MCP] INSTALLED " + _add.Result.packageId);
            else
                Debug.LogError("[HoboLife MCP] INSTALL FAILED: " +
                               (_add.Error != null ? _add.Error.message : "unknown error"));
            _add = null;
            EditorApplication.update -= Tick;
        }
    }
}
