#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TimeMetroidvania.Editor
{
    /// <summary>执行显式请求的迁移或测试，将结果写入忽略提交的日志目录。</summary>
    [InitializeOnLoad]
    internal static class V2VerificationRunner
    {
        private const string Request = "Temp/V2Polish.request";
        private static TestRunnerApi api;
        static V2VerificationRunner()
        {
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Results());
            EditorApplication.update += Poll;
        }
        private static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.isPlaying = false; return; }
            string task = File.ReadAllText(Request).Trim();
            File.Delete(Request);
            Directory.CreateDirectory("Logs/V2Polish");
            try
            {
                if (task == "migrate")
                {
                    V2GameplayPolish.ApplyToCurrentScene(true);
                    File.WriteAllText("Logs/V2Polish/Migration.txt", "PASS", Encoding.UTF8);
                }
                else if (task == "edit" || task == "play")
                {
                    SessionState.SetString("V2Polish.TestMode", task);
                    api.Execute(new ExecutionSettings(new Filter { testMode = task == "edit" ? TestMode.EditMode : TestMode.PlayMode }));
                }
            }
            catch (Exception e) { File.WriteAllText("Logs/V2Polish/Failure.txt", e.ToString(), Encoding.UTF8); Debug.LogException(e); }
        }
        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor test) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.TestStatus == TestStatus.Failed)
                    File.AppendAllText("Logs/V2Polish/TestFailures.txt", result.Name + "\n" + result.Message + "\n" + result.StackTrace + "\n", Encoding.UTF8);
            }
            public void RunFinished(ITestResultAdaptor result)
            {
                string mode = SessionState.GetString("V2Polish.TestMode", "unknown");
                File.WriteAllText("Logs/V2Polish/" + mode + ".txt",
                    result.TestStatus + " Passed=" + result.PassCount + " Failed=" + result.FailCount + " Skipped=" + result.SkipCount, Encoding.UTF8);
                EditorApplication.delayCall += () => {
                    if (!EditorApplication.isPlayingOrWillChangePlaymode)
                        EditorSceneManager.OpenScene("Assets/_Project/Scenes/V2/EightRoomGreybox.unity");
                };
            }
        }
    }
}
#endif
