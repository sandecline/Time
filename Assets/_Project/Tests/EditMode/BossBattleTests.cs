using NUnit.Framework;
using TimeMetroidvania.Boss;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Level;
using TimeMetroidvania.Player;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TimeMetroidvania.Tests
{
    public sealed class BossBattleTests
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/Prototype/SevenRoomSlice.unity";

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [Test]
        public void 房间七包含两阶段Boss和专用弹幕池()
        {
            BossBrain[] bosses = Object.FindObjectsByType<BossBrain>(
                FindObjectsSortMode.None);
            Assert.That(bosses.Length, Is.EqualTo(1));
            BossBrain boss = bosses[0];
            Assert.That(boss.name, Is.EqualTo("Boss_TimeWarden"));
            Assert.That(boss.GetComponentInParent<StageFiveRoom>().RoomId,
                Is.EqualTo("R7"));
            Assert.That(boss.GetComponent<HealthSystem>().MaxHealth, Is.EqualTo(28));
            Assert.That(boss.GetComponent<LocalTimeController>(), Is.Not.Null);
            Assert.That(boss.GetComponent<TimeAwareDamageReceiver>().MaxPendingDamage,
                Is.EqualTo(6));
            Assert.That(boss.GetComponent<ContactDamage2D>(), Is.Not.Null);

            BossProjectilePool pool =
                Object.FindFirstObjectByType<BossProjectilePool>();
            Assert.That(pool, Is.Not.Null);
            Assert.That(pool.Capacity, Is.EqualTo(24));
            Assert.That(GameObject.Find("BossArenaFloorMarker"), Is.Not.Null);
            Assert.That(GameObject.Find("BossArenaMarker"), Is.Null);
        }

        [Test]
        public void Boss初始状态是一阶段且关卡尚未确认胜利()
        {
            BossBrain boss = Object.FindFirstObjectByType<BossBrain>();
            StageFiveLevelController level =
                Object.FindFirstObjectByType<StageFiveLevelController>();
            Assert.That(boss.Phase, Is.EqualTo(BossPhase.One));
            Assert.That(boss.CurrentAttack, Is.EqualTo(BossAttackType.None));
            Assert.That(level.BossDefeated, Is.False);
        }
    }
}
