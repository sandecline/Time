using System.Linq;
using NUnit.Framework;
using TimeMetroidvania.Boss;
using TimeMetroidvania.Enemy;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Level;
using TimeMetroidvania.Player;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TimeMetroidvania.Tests
{
    public sealed class V2EightRoomGreyboxTests
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/V2/EightRoomGreybox.unity";

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [Test]
        public void 八房间拓扑与核心内容完整生成()
        {
            StageFiveRoom[] rooms = Object.FindObjectsByType<StageFiveRoom>(
                FindObjectsSortMode.None);
            Assert.That(rooms.Select(room => room.RoomId).OrderBy(id => id),
                Is.EqualTo(new[] { "R1", "R2", "R3", "R4", "R5", "R6", "R7", "R8" }));
            Assert.That(Object.FindObjectsByType<RoomCameraBounds2D>(
                FindObjectsSortMode.None).Length, Is.EqualTo(8));
            Assert.That(Object.FindObjectsByType<StageFiveDoor>(
                FindObjectsSortMode.None).Length, Is.EqualTo(16));
            Assert.That(Object.FindObjectsByType<StageFiveCheckpoint>(
                FindObjectsSortMode.None).Single().CheckpointId, Is.EqualTo("CP_R7_Boss"));
            Assert.That(Object.FindObjectsByType<StageFivePickup>(
                FindObjectsSortMode.None).Single().Kind, Is.EqualTo(StageFivePickupKind.TimeStop));
            Assert.That(Object.FindFirstObjectByType<BossBrain>().name,
                Is.EqualTo("Boss_TimeWarden_R8"));
        }

        [Test]
        public void 房间镜头尺寸与Boss强化配置已保存到手工场景()
        {
            RoomCameraBounds2D[] cameras = Object.FindObjectsByType<RoomCameraBounds2D>(
                FindObjectsSortMode.None);
            Assert.That(cameras.Single(room => room.RoomId == "R8").OrthographicSize,
                Is.EqualTo(5.5f).Within(0.001f));
            Assert.That(cameras.Where(room => room.RoomId != "R8")
                .All(room => Mathf.Approximately(room.OrthographicSize, 5f)), Is.True);

            BossBrain boss = Object.FindFirstObjectByType<BossBrain>();
            Assert.That(boss.GetComponent<HealthSystem>().MaxHealth, Is.EqualTo(96));
            Assert.That(boss.VolleyWaveCount, Is.EqualTo(5));
            Assert.That(boss.VolleyShotCount, Is.EqualTo(28));
            Assert.That(boss.GetComponentInParent<StageFiveRoom>()
                .GetComponentInChildren<BossProjectilePool>().Capacity, Is.EqualTo(512));
        }

        [Test]
        public void 草图指定的敌人和三类机关均已接入()
        {
            Assert.That(Object.FindObjectsByType<V2FlyingEnemy>(
                FindObjectsSortMode.None).Length, Is.EqualTo(7));
            Assert.That(Object.FindObjectsByType<EnemyBrain>(
                FindObjectsSortMode.None).Length, Is.EqualTo(15));
            Assert.That(GameObject.Find("R3_TimeStopWater")
                .GetComponent<V2TimeStopWater>(), Is.Not.Null);
            Assert.That(GameObject.Find("R4_ShortcutGate")
                .GetComponent<V2ShortcutGate>(), Is.Not.Null);
            Assert.That(GameObject.Find("R4_TimeRotor")
                .GetComponent<V2TimeRotor>(), Is.Not.Null);
        }

        [Test]
        public void 时停冻结水面且捷径门开启后保持开放()
        {
            GameObject player = GameObject.Find("Player");
            WorldTimeService world = Object.FindFirstObjectByType<WorldTimeService>();
            TimeGauge gauge = player.GetComponent<TimeGauge>();
            V2TimeStopWater water = Object.FindFirstObjectByType<V2TimeStopWater>();
            Collider2D surface = GameObject.Find("R3_FrozenSurface")
                .GetComponent<Collider2D>();
            Assert.That(surface.enabled, Is.False);

            world.UnlockTimeStop();
            gauge.Fill();
            Assert.That(world.TryStartTimeStop(gauge), Is.True);
            water.ApplyWorldTime(world.CurrentMode, world.CurrentWorldScale);
            Assert.That(water.IsFrozen, Is.True);
            Assert.That(surface.enabled, Is.True);
            world.EndTimeStopEarly();
            water.ApplyWorldTime(world.CurrentMode, world.CurrentWorldScale);
            Assert.That(surface.enabled, Is.False);

            V2ShortcutGate gate = Object.FindFirstObjectByType<V2ShortcutGate>();
            gate.Open();
            Assert.That(gate.IsOpen, Is.True);
            Assert.That(GameObject.Find("SolidGate")
                .GetComponent<Collider2D>().enabled, Is.False);
        }

        [Test]
        public void R1到R2落点和R3底部入口没有被实心墙覆盖()
        {
            Transform r1 = GameObject.Find("Room_R1").transform;
            Transform r2 = GameObject.Find("Room_R2").transform;
            Transform r3 = GameObject.Find("Room_R3").transform;
            Vector2[] clearPoints =
            {
                r1.Find("Entry_R2").position,
                r2.Find("Entry_R1").position,
                GameObject.Find("Door_R3_R2").transform.position,
                GameObject.Find("Door_R3_R4").transform.position
            };

            Physics2D.SyncTransforms();
            foreach (Vector2 point in clearPoints)
            {
                Collider2D[] overlaps = Physics2D.OverlapBoxAll(
                    point, new Vector2(0.6f, 0.6f), 0f);
                Assert.That(overlaps.Any(collider => !collider.isTrigger),
                    Is.False, $"入口 {point} 被实心碰撞体覆盖");
            }
        }

        [Test]
        public void Boss场地锁使用R8而非旧R7判断()
        {
            StageFiveLevelController level =
                Object.FindFirstObjectByType<StageFiveLevelController>();
            StageFiveDoor bossExit = GameObject.Find("Door_R8_R6")
                .GetComponent<StageFiveDoor>();
            Assert.That(level.BossRoomId, Is.EqualTo("R8"));
            Assert.That(bossExit.IsBossBattleBlocked, Is.False);
            level.BeginBossBattle();
            Assert.That(bossExit.IsBossBattleBlocked, Is.True);
            level.EndBossBattle();
            Assert.That(bossExit.IsBossBattleBlocked, Is.False);
        }
    }
}
