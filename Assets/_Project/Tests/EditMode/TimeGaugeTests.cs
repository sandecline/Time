using System.Reflection;
using NUnit.Framework;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Tests
{
    public sealed class TimeGaugeTests
    {
        private GameObject player;

        [SetUp]
        public void SetUp()
        {
            player = new GameObject("TimeGaugeTests");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(player);
        }

        [Test]
        public void 获取与消耗始终限制在零到一百()
        {
            TimeGauge gauge = player.AddComponent<TimeGauge>();

            Assert.That(gauge.Gain(120f), Is.EqualTo(100f));
            Assert.That(gauge.CurrentValue, Is.EqualTo(100f));
            Assert.That(gauge.TrySpend(60f), Is.True);
            Assert.That(gauge.CurrentValue, Is.EqualTo(40f));
            Assert.That(gauge.TrySpend(41f), Is.False);
            Assert.That(gauge.CurrentValue, Is.EqualTo(40f));
            Assert.That(gauge.SpendUpTo(100f), Is.EqualTo(40f));
            Assert.That(gauge.CurrentValue, Is.Zero);
        }

        [Test]
        public void 一秒未命中后以每秒五点恢复至二十()
        {
            TimeGauge gauge = player.AddComponent<TimeGauge>();
            gauge.Clear();
            float start = Time.unscaledTime;

            gauge.AdvanceAutomaticRecovery(0.5f, start + 0.5f, false);
            Assert.That(gauge.CurrentValue, Is.Zero);

            gauge.AdvanceAutomaticRecovery(1f, start + 1f, true);
            Assert.That(gauge.CurrentValue, Is.Zero);

            gauge.AdvanceAutomaticRecovery(1f, start + 1.01f, false);
            Assert.That(gauge.CurrentValue, Is.EqualTo(5f));
            gauge.AdvanceAutomaticRecovery(3f, start + 4f, false);
            Assert.That(gauge.CurrentValue, Is.EqualTo(20f));
            gauge.AdvanceAutomaticRecovery(10f, start + 14f, false);
            Assert.That(gauge.CurrentValue, Is.EqualTo(20f));
        }

        [Test]
        public void 有效命中会立即加槽并重新开始恢复延迟()
        {
            TimeGauge gauge = player.AddComponent<TimeGauge>();
            gauge.Clear();
            gauge.GainFromSuccessfulHit(10f);
            float hitTime = Time.unscaledTime;

            gauge.AdvanceAutomaticRecovery(1f, hitTime + 0.99f, false);
            Assert.That(gauge.CurrentValue, Is.EqualTo(10f));
            gauge.AdvanceAutomaticRecovery(1f, hitTime + 1.01f, false);
            Assert.That(gauge.CurrentValue, Is.EqualTo(15f));
        }

        [Test]
        public void 玩家死亡会清空时间槽()
        {
            player.AddComponent<Rigidbody2D>();
            PlayerStateMachine stateMachine = player.AddComponent<PlayerStateMachine>();
            HealthSystem health = player.AddComponent<HealthSystem>();
            TimeGauge gauge = player.AddComponent<TimeGauge>();
            GameObject source = new("DamageSource");
            InvokeLifecycle(gauge, "OnEnable");

            gauge.Gain(72f);
            health.TryTakeDamage(new DamageInfo(
                source,
                health.MaxHealth,
                Vector2.zero,
                Vector2.zero));

            Assert.That(stateMachine.CurrentState, Is.EqualTo(PlayerState.Dead));
            Assert.That(gauge.CurrentValue, Is.Zero);
            Object.DestroyImmediate(source);
        }

        private static void InvokeLifecycle(MonoBehaviour component, string methodName)
        {
            MethodInfo method = component.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(component, null);
        }
    }
}