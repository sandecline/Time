#if UNITY_EDITOR
using TimeMetroidvania.Flow;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Level;
using TimeMetroidvania.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimeMetroidvania.Editor
{
    /// <summary>
    /// 为测试 Demo 接入不落盘的标题、暂停、地图、结尾和统一 HUD。
    /// </summary>
    internal static class PhaseSevenFlowBuilder
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/Prototype/SevenRoomSlice.unity";

        [MenuItem("Tools/Time Metroidvania/Build Phase 7 Demo Flow")]
        public static void Build()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            GameObject player = GameObject.Find("Player");
            WorldTimeService worldTime = Object.FindFirstObjectByType<WorldTimeService>();
            StageFiveLevelController level =
                Object.FindFirstObjectByType<StageFiveLevelController>();
            if (player == null || worldTime == null || level == null)
            {
                throw new MissingReferenceException(
                    "阶段七流程需要玩家、世界时间服务和七房间关卡控制器。");
            }

            DestroyAll("Stage7_RuntimeProgress");
            DestroyAll("Stage7_FlowController");
            DestroyAll("Stage7_Hud");

            PrototypeHud prototypeHud = Object.FindFirstObjectByType<PrototypeHud>();
            if (prototypeHud != null)
            {
                prototypeHud.enabled = false;
            }
            level.SetHudVisible(false);

            DemoProgressService progress = new GameObject("Stage7_RuntimeProgress")
                .AddComponent<DemoProgressService>();
            progress.Configure(level, worldTime);

            StageSevenFlowController flow = new GameObject("Stage7_FlowController")
                .AddComponent<StageSevenFlowController>();
            flow.Configure(
                player,
                player.GetComponent<HealthSystem>(),
                player.GetComponent<TimeGauge>(),
                worldTime,
                level,
                progress);

            StageSevenHud hud = new GameObject("Stage7_Hud")
                .AddComponent<StageSevenHud>();
            hud.Configure(player, worldTime, level, flow);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log($"阶段 7 测试流程（无持久化存档）已生成：{ScenePath}");
        }

        private static void DestroyAll(string name)
        {
            GameObject target;
            while ((target = GameObject.Find(name)) != null)
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
#endif
