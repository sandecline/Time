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

        [TestCase(0f, 0.3f, false, true)]
        [TestCase(0.299f, 0.3f, false, true)]
        [TestCase(0.3f, 0.3f, false, false)]
        [TestCase(0.1f, 0.3f, true, false)]
        public void 短按判定严格小于阈值(
            float heldDuration,
            float threshold,
            bool longPressResolved,
            bool expected)
        {
            Assert.That(
                PlayerHealing.ShouldRequestShortPress(heldDuration, threshold, longPressResolved),
                Is.EqualTo(expected));
        }
    }
}
