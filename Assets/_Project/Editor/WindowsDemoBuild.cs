using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TimeMetroidvania.Editor
{
    /// <summary>
    /// 将当前美术预览场景打包为 Windows 可玩 Demo。
    /// </summary>
    public static class WindowsDemoBuild
    {
        private const string ScenePath = "Assets/_Project/Scenes/V2/EightRoomArtPreview.unity";
        private const string DefaultOutput = "Builds/Windows/时序旅人.exe";

        [MenuItem("Tools/时序旅人/构建 Windows Demo")]
        public static void BuildFromMenu()
        {
            Build(Path.Combine(ProjectRoot(), DefaultOutput));
        }

        public static void BuildFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            int outputArgument = Array.IndexOf(args, "-demoBuildOutput");
            if (outputArgument < 0 || outputArgument + 1 >= args.Length)
            {
                throw new ArgumentException("缺少 -demoBuildOutput 参数及目标文件夹路径。");
            }

            Build(Path.Combine(args[outputArgument + 1], "时序旅人.exe"));
        }

        private static string ProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static void Build(string output)
        {
            string sceneFile = Path.Combine(ProjectRoot(), ScenePath);
            if (!File.Exists(sceneFile))
            {
                throw new FileNotFoundException("找不到美术预览场景。", sceneFile);
            }

            string executable = Path.GetFullPath(output);
            Directory.CreateDirectory(Path.GetDirectoryName(executable));
            PlayerSettings.productName = "时序旅人";

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = executable,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Windows Demo 构建失败：{report.summary.result}，错误数 {report.summary.totalErrors}。");
            }

            Debug.Log($"Windows Demo 构建完成：{executable}");
        }
    }
}
