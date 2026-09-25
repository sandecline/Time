using NUnit.Framework;
using TimeMetroidvania.Flow;
using TimeMetroidvania.Level;
using TimeMetroidvania.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TimeMetroidvania.Tests
{
    public sealed class StageSevenFlowTests
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/Prototype/SevenRoomSlice.unity";

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [Test]
        public void 场景包含无持久化流程地图和统一Hud()
        {
            StageSevenFlowController flow =
                Object.FindFirstObjectByType<StageSevenFlowController>();
            DemoProgressService progress =
                Object.FindFirstObjectByType<DemoProgressService>();
            StageSevenHud hud = Object.FindFirstObjectByType<StageSevenHud>();
            Assert.That(flow, Is.Not.Null);
            Assert.That(progress, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);
            Assert.That(progress.PersistenceEnabled, Is.False);
            Assert.That(new SerializedObject(flow).FindProperty("startInMenu").boolValue,
                Is.True);
        }

        [Test]
        public void 旧调试Hud已关闭并由阶段七Hud接管()
        {
            PrototypeHud prototype = Object.FindFirstObjectByType<PrototypeHud>();
            StageFiveLevelController level =
                Object.FindFirstObjectByType<StageFiveLevelController>();
            Assert.That(prototype.enabled, Is.False);
            Assert.That(new SerializedObject(level).FindProperty("showHud").boolValue,
                Is.False);
        }
    }
}
