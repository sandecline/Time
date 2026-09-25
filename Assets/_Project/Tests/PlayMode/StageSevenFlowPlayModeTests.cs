using System.Collections;
using NUnit.Framework;
using TimeMetroidvania.Flow;
using TimeMetroidvania.Level;
using TimeMetroidvania.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TimeMetroidvania.Tests
{
    public sealed class StageSevenFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator 标题菜单冻结游戏且开始测试不会创建持久存档()
        {
            SceneManager.LoadScene("SevenRoomSlice");
            yield return null;

            StageSevenFlowController flow =
                Object.FindFirstObjectByType<StageSevenFlowController>();
            DemoProgressService progress =
                Object.FindFirstObjectByType<DemoProgressService>();
            PlayerMovement movement = GameObject.Find("Player")
                .GetComponent<PlayerMovement>();
            Assert.That(flow.State, Is.EqualTo(GameFlowState.StartMenu));
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(movement.enabled, Is.False);
            Assert.That(flow.PersistenceEnabled, Is.False);

            flow.StartNewGame();
            yield return null;
            Assert.That(flow.State, Is.EqualTo(GameFlowState.Playing));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(movement.enabled, Is.True);
            Assert.That(progress.IsRoomDiscovered("R1"), Is.True);
        }

        [UnityTest]
        public IEnumerator 暂停和地图都会冻结世界并可恢复()
        {
            yield return DemoFlowTestHelper.LoadStartedSevenRoomSlice();
            StageSevenFlowController flow =
                Object.FindFirstObjectByType<StageSevenFlowController>();

            flow.PauseGame();
            Assert.That(flow.State, Is.EqualTo(GameFlowState.Paused));
            Assert.That(Time.timeScale, Is.Zero);
            flow.ResumeGame();
            Assert.That(flow.State, Is.EqualTo(GameFlowState.Playing));
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            flow.OpenMap();
            Assert.That(flow.State, Is.EqualTo(GameFlowState.Map));
            Assert.That(Time.timeScale, Is.Zero);
            flow.ResumeGame();
            Assert.That(flow.State, Is.EqualTo(GameFlowState.Playing));
        }

        [UnityTest]
        public IEnumerator 房间发现只保留在本次运行内()
        {
            yield return DemoFlowTestHelper.LoadStartedSevenRoomSlice();
            StageFiveLevelController level =
                Object.FindFirstObjectByType<StageFiveLevelController>();
            DemoProgressService progress =
                Object.FindFirstObjectByType<DemoProgressService>();
            level.EnterRoom("R2");
            Assert.That(progress.IsRoomDiscovered("R2"), Is.True);

            SceneManager.LoadScene("SevenRoomSlice");
            yield return null;
            Object.FindFirstObjectByType<StageSevenFlowController>().StartNewGame();
            progress = Object.FindFirstObjectByType<DemoProgressService>();
            Assert.That(progress.IsRoomDiscovered("R1"), Is.True);
            Assert.That(progress.IsRoomDiscovered("R2"), Is.False);
        }
    }
}
