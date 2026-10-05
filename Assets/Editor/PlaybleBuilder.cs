using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Playble.EditorTools
{
    /// <summary>
    /// Сборка standalone-версии. Путь можно переопределить аргументом
    /// -buildOutput, чтобы один и тот же метод работал и вручную, и из CI.
    /// </summary>
    public static class PlaybleBuilder
    {
        const string DefaultOutput = "Build/Windows/PlaybleEchoLoop.exe";

        [MenuItem("Playble/Собрать игру для Windows")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, DefaultOutput);

        [MenuItem("Playble/Собрать игру для Linux")]
        public static void BuildLinux() => Build(BuildTarget.StandaloneLinux64, "Build/Linux/PlaybleEchoLoop.x86_64");

        static void Build(BuildTarget target, string defaultPath)
        {
            string output = ReadArgument("-buildOutput") ?? defaultPath;

            string directory = Path.GetDirectoryName(output);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { PlaybleSetup.ScenePath },
                locationPathName = output,
                target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log(string.Format(
                    "[PlaybleBuilder] Сборка {0} успешна: {1} ({2:0.0} МБ)",
                    target, output, summary.totalSize / (1024f * 1024f)));
            }
            else
            {
                Debug.LogError(string.Format(
                    "[PlaybleBuilder] Сборка {0} провалилась: {1}, ошибок: {2}",
                    target, summary.result, summary.totalErrors));
            }

            if (Application.isBatchMode) EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        static string ReadArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            }
            return null;
        }
    }
}