using System.Collections;
using NUnit.Framework;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Level;
using TimeMetroidvania.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TimeMetroidvania.Tests
{
    public sealed class V2EightRoomGreyboxPlayModeTests
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/V2/EightRoomGreybox.unity";

        [UnityTest]
        public IEnumerator 开放缺口切换到R2并刷新镜头房间()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            yield return null;
            GameObject player = GameObject.Find("Player");
            StageFiveLevelController level =
                Object.FindFirstObjectByType<StageFiveLevelController>();
            StageFiveDoor door = GameObject.Find("Door_R1_R2")
                .GetComponent<StageFiveDoor>();
            Rigidbody2D actor = player.GetComponent<Rigidbody2D>();
            actor.position = door.transform.position;
            player.transform.position = door.transform.position;
            Physics2D.SyncTransforms();
            Assert.That(door.TryTravel(player), Is.False, "接触入口不能立即换房");
            float halfWidth = player.GetComponent<Collider2D>().bounds.extents.x;
            Vector3 crossed = door.transform.position + (Vector3)door.OutwardNormal * (halfWidth + 0.12f);
            actor.position = crossed;
            player.transform.position = crossed;
            Physics2D.SyncTransforms();
            Assert.That(door.TryTravel(player), Is.True);
            Assert.That(level.CurrentRoomId, Is.EqualTo("R2"));
            Assert.That(Vector2.Distance(player.transform.position,
                GameObject.Find("Entry_R1").transform.position), Is.LessThan(0.05f));
        }

        [UnityTest]
        public IEnumerator 玩家在水下发动时停会与水面一起冻结()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            yield return null;
            GameObject player = GameObject.Find("Player");
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            V2TimeStopWater water = Object.FindFirstObjectByType<V2TimeStopWater>();
            WorldTimeService world = Object.FindFirstObjectByType<WorldTimeService>();
            TimeGauge gauge = player.GetComponent<TimeGauge>();
            body.position = water.transform.position;
            player.transform.position = water.transform.position;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();

            world.UnlockTimeStop();
            gauge.Fill();
            Assert.That(world.TryStartTimeStop(gauge), Is.True);
            yield return null;
            Assert.That(water.IsFrozen, Is.True);
            Assert.That(body.constraints, Is.EqualTo(RigidbodyConstraints2D.FreezeAll));
            Assert.That(GameObject.Find("R3_FrozenSurface")
                .GetComponent<Collider2D>().enabled, Is.True);

            Assert.That(world.EndTimeStopEarly(), Is.True);
            yield return null;
            Assert.That(body.constraints, Is.Not.EqualTo(RigidbodyConstraints2D.FreezeAll));
        }
    }
}