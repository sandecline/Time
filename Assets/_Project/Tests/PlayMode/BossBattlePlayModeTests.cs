using System.Collections;
using System.Linq;
using NUnit.Framework;
using TimeMetroidvania.Boss;
using TimeMetroidvania.Flow;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Level;
using TimeMetroidvania.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TimeMetroidvania.Tests
{
    public sealed class BossBattlePlayModeTests
    {
        [UnityTest]
        public IEnumerator Boss半血进入二阶段并释放时间锚弹幕()
        {
            yield return DemoFlowTestHelper.LoadStartedSevenRoomSlice();

            GameObject player = GameObject.Find("Player");
            BossBrain boss = Object.FindFirstObjectByType<BossBrain>();
            BossProjectilePool pool =
                Object.FindFirstObjectByType<BossProjectilePool>();
            StageFiveLevelController level =
                Object.FindFirstObjectByType<StageFiveLevelController>();
            level.EnterRoom("R7");
            MovePlayer(player, boss.transform.position + Vector3.left * 4f);
            yield return null;
            Assert.That(boss.IsEngaged, Is.True);

            TimeAwareDamageReceiver receiver =
                boss.GetComponent<TimeAwareDamageReceiver>();
            Assert.That(receiver.TryTakeDamage(new DamageInfo(
                player, 14, boss.transform.position, Vector2.zero)), Is.True);
            Assert.That(boss.Phase, Is.EqualTo(BossPhase.Two));

            BossProjectile anchored = null;
            for (int index = 0; index < 50 && anchored == null; index++)
            {
                boss.Advance(0.5f);
                anchored = pool.GetComponentsInChildren<BossProjectile>(true)
                    .FirstOrDefault(projectile =>
                        projectile.gameObject.activeSelf && projectile.IsTimeAnchored);
            }
            Assert.That(anchored, Is.Not.Null);
            Assert.That(anchored.IsBreakable, Is.False);
            Assert.That(anchored.TryDestroyByAttack(player), Is.False);

            BossProjectile[] ordinaryProjectiles = pool
                .GetComponentsInChildren<BossProjectile>(true)
                .Where(projectile =>
                    projectile.gameObject.activeSelf && !projectile.IsTimeAnchored)
                .ToArray();
            BossProjectile breakable = ordinaryProjectiles
                .FirstOrDefault(projectile => projectile.IsBreakable);
            BossProjectile unbreakable = ordinaryProjectiles
                .FirstOrDefault(projectile => !projectile.IsBreakable);
            Assert.That(breakable, Is.Not.Null);
            Assert.That(unbreakable, Is.Not.Null);
            Assert.That(breakable.TryDestroyByAttack(player), Is.True);
            Assert.That(unbreakable.TryDestroyByAttack(player), Is.False);

            WorldTimeService world = Object.FindFirstObjectByType<WorldTimeService>();
            TimeGauge gauge = player.GetComponent<TimeGauge>();
            world.UnlockTimeStop();
            gauge.Gain(60f);
            Assert.That(world.TryStartTimeStop(gauge), Is.True);
            Vector3 before = anchored.transform.position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(anchored.transform.position, Is.Not.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator 单次时停最多缓存六点Boss伤害()
        {
            yield return DemoFlowTestHelper.LoadStartedSevenRoomSlice();

            GameObject player = GameObject.Find("Player");
            BossBrain boss = Object.FindFirstObjectByType<BossBrain>();
            TimeAwareDamageReceiver receiver =
                boss.GetComponent<TimeAwareDamageReceiver>();
            WorldTimeService world = Object.FindFirstObjectByType<WorldTimeService>();
            TimeGauge gauge = player.GetComponent<TimeGauge>();
            world.UnlockTimeStop();
            gauge.Gain(60f);
            Assert.That(world.TryStartTimeStop(gauge), Is.True);

            for (int index = 0; index < 8; index++)
            {
                receiver.TryTakeDamage(new DamageInfo(
                    player, 1, boss.transform.position, Vector2.zero));
            }
            Assert.That(receiver.PendingDamage, Is.EqualTo(6));
            world.ResetToNormal();
            Assert.That(boss.CurrentHealth, Is.EqualTo(boss.MaxHealth - 6));
        }

        [UnityTest]
        public IEnumerator 玩家死亡会让未击败Boss满血返回第一阶段()
        {
            yield return DemoFlowTestHelper.LoadStartedSevenRoomSlice();

            GameObject player = GameObject.Find("Player");
            HealthSystem playerHealth = player.GetComponent<HealthSystem>();
            BossBrain boss = Object.FindFirstObjectByType<BossBrain>();
            StageFiveLevelController level =
                Object.FindFirstObjectByType<StageFiveLevelController>();
            level.EnterRoom("R7");
            MovePlayer(player, boss.transform.position + Vector3.left * 4f);
            yield return null;
            boss.GetComponent<TimeAwareDamageReceiver>().TryTakeDamage(
                new DamageInfo(player, 14, boss.transform.position, Vector2.zero));
            Assert.That(boss.Phase, Is.EqualTo(BossPhase.Two));

            playerHealth.TryTakeDamage(new DamageInfo(
                boss.gameObject, playerHealth.MaxHealth,
                player.transform.position, Vector2.zero));
            StageSevenFlowController flow =
                Object.FindFirstObjectByType<StageSevenFlowController>();
            Assert.That(flow.IsDeathVisible, Is.True);
            Assert.That(boss.Phase, Is.EqualTo(BossPhase.One));
            Assert.That(boss.CurrentHealth, Is.EqualTo(boss.MaxHealth));
            Assert.That(boss.IsEngaged, Is.False);
            Assert.That(level.BossBattleActive, Is.False);
            yield return new WaitForSeconds(1.3f);
            Assert.That(playerHealth.IsFullHealth, Is.True);
            Assert.That(flow.IsDeathVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator Boss激活后封锁双向入口且胜利后解除()
        {
            yield return DemoFlowTestHelper.LoadStartedSevenRoomSlice();

            GameObject player = GameObject.Find("Player");
            BossBrain boss = Object.FindFirstObjectByType<BossBrain>();
            StageFiveLevelController level =
                Object.FindFirstObjectByType<StageFiveLevelController>();
            StageFiveDoor entrance = GameObject.Find("Door_R5_R7")
                .GetComponent<StageFiveDoor>();
            StageFiveDoor exit = GameObject.Find("Door_R7_R5")
                .GetComponent<StageFiveDoor>();

            level.EnterRoom("R7");
            MovePlayer(player, boss.transform.position + Vector3.left * 4f);
            yield return null;
            Assert.That(level.BossBattleActive, Is.True);
            Assert.That(entrance.IsBossBattleBlocked, Is.True);
            Assert.That(exit.IsBossBattleBlocked, Is.True);

            MovePlayer(player, exit.transform.position);
            Assert.That(exit.TryTravel(player), Is.False);
            Assert.That(level.CurrentRoomId, Is.EqualTo("R7"));

            boss.GetComponent<TimeAwareDamageReceiver>().TryTakeDamage(
                new DamageInfo(player, boss.MaxHealth, boss.transform.position, Vector2.zero));
            Assert.That(level.BossDefeated, Is.True);
            Assert.That(level.BossBattleActive, Is.False);
            Assert.That(exit.IsBossBattleBlocked, Is.False);
        }

        [UnityTest]
        public IEnumerator 已击败Boss不会被房间刷新复活()
        {
            yield return DemoFlowTestHelper.LoadStartedSevenRoomSlice();

            GameObject player = GameObject.Find("Player");
            BossBrain boss = Object.FindFirstObjectByType<BossBrain>();
            StageFiveLevelController level =
                Object.FindFirstObjectByType<StageFiveLevelController>();
            boss.GetComponent<TimeAwareDamageReceiver>().TryTakeDamage(
                new DamageInfo(player, boss.MaxHealth, boss.transform.position, Vector2.zero));
            Assert.That(boss.Phase, Is.EqualTo(BossPhase.Defeated));
            Assert.That(level.BossDefeated, Is.True);
            Assert.That(Object.FindFirstObjectByType<StageSevenFlowController>().State,
                Is.EqualTo(GameFlowState.Ending));

            boss.GetComponentInParent<StageFiveRoom>().RefreshEnemies();
            Assert.That(boss.Phase, Is.EqualTo(BossPhase.Defeated));
            Assert.That(boss.GetComponent<HealthSystem>().IsDead, Is.True);
        }

        private static void MovePlayer(GameObject player, Vector3 position)
        {
            player.transform.position = position;
            player.GetComponent<Rigidbody2D>().position = position;
            Physics2D.SyncTransforms();
        }
    }
}
