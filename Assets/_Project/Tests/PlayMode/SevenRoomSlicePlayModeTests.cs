using System.Collections;
using NUnit.Framework;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Enemy;
using TimeMetroidvania.Level;
using TimeMetroidvania.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TimeMetroidvania.Tests
{
    public sealed class SevenRoomSlicePlayModeTests
    {
        [UnityTest]
        public IEnumerator 走入房门会自动进入下一房间且刷新进出房间敌人()
        {
            yield return DemoFlowTestHelper.LoadStartedSevenRoomSlice();

            GameObject player = GameObject.Find("Player");
            Assert.That(player.GetComponent<Collider2D>().sharedMaterial.friction,
                Is.EqualTo(0f));
            StageFiveDoor door =
                GameObject.Find("Door_R1_R2").GetComponent<StageFiveDoor>();
            StageFiveLevelController level =
                Object.FindFirstObjectByType<StageFiveLevelController>();
            EnemyBrain sourceEnemy = GameObject.Find("R1_Guard").GetComponent<EnemyBrain>();
            EnemyBrain targetEnemy = GameObject.Find("R2_Bug_A").GetComponent<EnemyBrain>();
            sourceEnemy.GetComponent<HealthSystem>().TryTakeDamage(new DamageInfo(
                player, sourceEnemy.GetComponent<HealthSystem>().MaxHealth,
                Vector2.zero, Vector2.zero));
            targetEnemy.GetComponent<HealthSystem>().TryTakeDamage(new DamageInfo(
                player, targetEnemy.GetComponent<HealthSystem>().MaxHealth,
                Vector2.zero, Vector2.zero));
            player.GetComponent<Rigidbody2D>().position = door.transform.position;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(level.CurrentRoomId, Is.EqualTo("R2"));
            Assert.That(sourceEnemy.GetComponent<HealthSystem>().IsDead, Is.False);
            Assert.That(targetEnemy.GetComponent<HealthSystem>().IsDead, Is.False);
            Assert.That(sourceEnemy.GetComponent<Collider2D>().enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator 坐椅子和死亡会刷新敌人()
        {
            yield return DemoFlowTestHelper.LoadStartedSevenRoomSlice();

            GameObject player = GameObject.Find("Player");
            HealthSystem playerHealth = player.GetComponent<HealthSystem>();
            EnemyBrain guard = GameObject.Find("R1_Guard").GetComponent<EnemyBrain>();
            HealthSystem guardHealth = guard.GetComponent<HealthSystem>();
            HealthSystem otherAreaEnemy = GameObject.Find("R5_ClockArcher")
                .GetComponent<HealthSystem>();
            StageFiveCheckpoint bench = GameObject.Find("CP_R1_Start")
                .GetComponent<StageFiveCheckpoint>();

            guardHealth.TryTakeDamage(new DamageInfo(
                player, guardHealth.MaxHealth, Vector2.zero, Vector2.zero));
            otherAreaEnemy.TryTakeDamage(new DamageInfo(
                player, otherAreaEnemy.MaxHealth, Vector2.zero, Vector2.zero));
            Assert.That(guardHealth.IsDead, Is.True);
            Assert.That(bench.TryActivate(player), Is.True);
            Assert.That(guardHealth.IsFullHealth, Is.True);
            Assert.That(otherAreaEnemy.IsDead, Is.True);

            guardHealth.TryTakeDamage(new DamageInfo(
                player, guardHealth.MaxHealth, Vector2.zero, Vector2.zero));
            Assert.That(guardHealth.IsDead, Is.True);
            playerHealth.TryTakeDamage(new DamageInfo(
                guard.gameObject, playerHealth.MaxHealth, Vector2.zero, Vector2.zero));
            Assert.That(guardHealth.IsFullHealth, Is.True);
            Assert.That(otherAreaEnemy.IsDead, Is.True);
            yield return new WaitForSeconds(1.35f);
            Assert.That(playerHealth.IsFullHealth, Is.True);
            Assert.That(guard.GetComponent<Collider2D>().enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator 时停期间刷新敌人会清除待结算伤害()
        {
            yield return DemoFlowTestHelper.LoadStartedSevenRoomSlice();

            GameObject player = GameObject.Find("Player");
            WorldTimeService world = Object.FindFirstObjectByType<WorldTimeService>();
            TimeGauge gauge = player.GetComponent<TimeGauge>();
            world.UnlockTimeStop();
            gauge.Gain(60f);
            Assert.That(world.TryStartTimeStop(gauge), Is.True);

            GameObject guard = GameObject.Find("R1_Guard");
            TimeAwareDamageReceiver receiver =
                guard.GetComponent<TimeAwareDamageReceiver>();
            Assert.That(receiver.TryTakeDamage(new DamageInfo(
                player, 2, Vector2.zero, Vector2.zero)), Is.True);
            Assert.That(receiver.PendingDamage, Is.EqualTo(2));

            guard.GetComponentInParent<StageFiveRoom>().RefreshEnemies();
            Assert.That(receiver.PendingDamage, Is.Zero);
            world.ResetToNormal();
            Assert.That(guard.GetComponent<HealthSystem>().IsFullHealth, Is.True);
        }

        [UnityTest]
        public IEnumerator 检查点按W激活且门前储能只补到一次时停成本()
        {
            yield return DemoFlowTestHelper.LoadStartedSevenRoomSlice();

            GameObject player = GameObject.Find("Player");
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            StageFiveLevelController level =
                Object.FindFirstObjectByType<StageFiveLevelController>();
            StageFiveCheckpoint checkpoint =
                GameObject.Find("CP_R4_Return").GetComponent<StageFiveCheckpoint>();
            Vector3 checkpointPosition = new(
                checkpoint.transform.position.x, -1.15f, 0f);
            body.position = checkpointPosition;
            player.transform.position = checkpointPosition;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null;

            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                yield return null;
                Assert.That(level.ActiveCheckpointId, Is.EqualTo("CP_R4_Return"));

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;

                WorldTimeService world = Object.FindFirstObjectByType<WorldTimeService>();
                TimeGauge gauge = player.GetComponent<TimeGauge>();
                world.UnlockTimeStop();
                Vector3 gatePosition = GameObject.Find("TimeStopGate_R1_R4")
                    .transform.position;
                body.position = gatePosition;
                player.transform.position = gatePosition;
                Physics2D.SyncTransforms();
                yield return new WaitForFixedUpdate();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                yield return null;
                Assert.That(gauge.CurrentValue, Is.EqualTo(60f));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [UnityTest]
        public IEnumerator 时停解锁后可回访开门隐藏升级不阻挡Boss入口()
        {
            yield return DemoFlowTestHelper.LoadStartedSevenRoomSlice();

            GameObject player = GameObject.Find("Player");
            WorldTimeService world = Object.FindFirstObjectByType<WorldTimeService>();
            StageFiveLevelController level =
                Object.FindFirstObjectByType<StageFiveLevelController>();
            TimeGauge gauge = player.GetComponent<TimeGauge>();
            Assert.That(world.IsTimeStopUnlocked, Is.False);

            StageFivePickup unlock =
                GameObject.Find("Pickup_TimeStop").GetComponent<StageFivePickup>();
            Assert.That(unlock.TryCollect(player), Is.True);
            Assert.That(world.IsTimeStopUnlocked, Is.True);
            Assert.That(gauge.CurrentValue, Is.GreaterThanOrEqualTo(60f));

            StageFiveDoor returnDoor =
                GameObject.Find("Door_R3_Return").GetComponent<StageFiveDoor>();
            player.transform.position = returnDoor.transform.position;
            Assert.That(returnDoor.TryTravel(player), Is.True);
            Assert.That(level.CurrentRoomId, Is.EqualTo("R1"));

            StageFiveTimeGate gate =
                GameObject.Find("TimeStopGate_R1_R4").GetComponent<StageFiveTimeGate>();
            player.transform.position = gate.transform.position;
            Assert.That(world.TryStartTimeStop(gauge), Is.True);
            Assert.That(gate.IsOpen, Is.True);

            StageFiveDoor gatedDoor =
                GameObject.Find("Door_R1_R4_Gated").GetComponent<StageFiveDoor>();
            player.transform.position = gatedDoor.transform.position;
            Assert.That(gatedDoor.TryTravel(player), Is.True);
            Assert.That(level.CurrentRoomId, Is.EqualTo("R4"));

            StageFiveDoor bossDoor =
                GameObject.Find("Door_R5_R7").GetComponent<StageFiveDoor>();
            player.transform.position = bossDoor.transform.position;
            Assert.That(bossDoor.TryTravel(player), Is.True);
            Assert.That(level.CurrentRoomId, Is.EqualTo("R7"));
            Assert.That(world.HasDurationUpgrade, Is.False);

            StageFivePickup upgrade = GameObject.Find("Pickup_SevenSecondUpgrade")
                .GetComponent<StageFivePickup>();
            Assert.That(upgrade.TryCollect(player), Is.True);
            Assert.That(world.HasDurationUpgrade, Is.True);
        }
    }
}
