using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class BuildAutomation
{
    public static void BuildAndroidAPK()
    {
        Debug.Log("[Build] 안드로이드 자동 빌드를 시작합니다...");

        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("[Build] 빌드할 씬이 Build Settings에 존재하지 않습니다.");
            EditorApplication.Exit(1);
            return;
        }

        string buildFolder = Path.Combine(Environment.CurrentDirectory, "Build/Android");
        if (!Directory.Exists(buildFolder))
        {
            Directory.CreateDirectory(buildFolder);
        }
        string outputPath = Path.Combine(buildFolder, "Game.apk");

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.buildAppBundle = false; // APK 빌드

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.Development | BuildOptions.AllowDebugging
        };

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[Build] 빌드 성공! 크기: {summary.totalSize / (1024 * 1024)} MB");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"[Build] 빌드 실패: {summary.result}");
            EditorApplication.Exit(1);
        }
    }
}
