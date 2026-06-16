using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// HoboLife — reproducible Windows build (menu HoboLife > Build Windows, or
// -batchmode -executeMethod BuildScript.BuildWindows). Output: Build/HoboLife.exe
// (the Build/ folder is gitignored).
public static class BuildScript
{
    [MenuItem("HoboLife/Build Windows")]
    public static void BuildWindows()
    {
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Milestone1.unity" },
            locationPathName = "Build/HoboLife.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(opts);
        BuildSummary s = report.summary;
        Debug.Log("[HoboLife] Build " + s.result + " — " + (s.totalSize / (1024 * 1024)) + " MB, "
                  + s.totalErrors + " errors, " + s.totalWarnings + " warnings -> " + opts.locationPathName);
    }
}
