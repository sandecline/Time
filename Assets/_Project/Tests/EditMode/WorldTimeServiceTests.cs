using System.Reflection;
using NUnit.Framework;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Player;
using UnityEditor;
using UnityEngine;

namespace TimeMetroidvania.Tests
{
    public sealed class WorldTimeServiceTests
    {
        private GameObject serviceObject;
        private GameObject gaugeObject;
        private WorldTimeService worldTime;
        private TimeGauge gauge;

        [SetUp]
        public void SetUp()
        {
            serviceObject = new GameObject("WorldTimeServiceTests");
            worldTime = serviceObject.AddComponent<WorldTimeService>();
            gaugeObject = new GameObject("TimeGaugeTests");
            gauge = gaugeObject.AddComponent<TimeGauge>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gaugeObject);
            Object.DestroyImmediate(serviceObject);
        }

        [Test]
        public void 时缓使用未缩放时间并在三秒后恢复()
        {
            float originalTimeScale = Time.timeScale;

            Assert.That(worldTime.TryStartSlow(), Is.True);
            Assert.That(worldTime.CurrentMode, Is.EqualTo(WorldTimeMode.Slowed));
            Assert.That(worldTime.CurrentWorldScale, Is.EqualTo(0.35f).Within(0.001f));

            worldTime.AdvanceUnscaledTime(2.99f);
            Assert.That(worldTime.CurrentMode, Is.EqualTo(WorldTimeMode.Slowed));
            worldTime.AdvanceUnscaledTime(0.01f);

            Assert.That(worldTime.CurrentMode, Is.EqualTo(WorldTimeMode.Normal));
            Assert.That(Time.timeScale, Is.EqualTo(originalTimeScale));
        }

        [Test]
        public void 时停扣除资源且攻击与提前结束按比例退款()
        {
            worldTime.UnlockTimeStop();
            gauge.Gain(60f);

            Assert.That(worldTime.TryStartTimeStop(gauge), Is.True);
            Assert.That(worldTime.CurrentMode, Is.EqualTo(WorldTimeMode.Stopped));
            Assert.That(worldTime.TimeStopMaximum, Is.EqualTo(5f));
            Assert.That(gauge.CurrentValue, Is.Zero);

            worldTime.ConsumeTimeStopAttack(0.6f);
            worldTime.AdvanceUnscaledTime(2f);
            Assert.That(worldTime.TimeStopRemaining, Is.EqualTo(2.4f).Within(0.001f));
            Assert.That(worldTime.EndTimeStopEarly(), Is.True);

            Assert.That(worldTime.CurrentMode, Is.EqualTo(WorldTimeMode.Normal));
            Assert.That(gauge.CurrentValue, Is.EqualTo(28f));
        }

        [Test]
        public void 时停升级将上限延长到七秒()
        {
            worldTime.UnlockTimeStop();
            worldTime.UnlockDurationUpgrade();
            gauge.Gain(60f);

            Assert.That(worldTime.TryStartTimeStop(gauge), Is.True);
            Assert.That(worldTime.TimeStopMaximum, Is.EqualTo(7f));
            Assert.That(worldTime.TimeStopRemaining, Is.EqualTo(7f));
        }

        [Test]
        public void 时停启动失败时保留正在生效的时缓()
        {
            worldTime.UnlockTimeStop();
            Assert.That(worldTime.TryStartSlow(), Is.True);

            Assert.That(worldTime.TryStartTimeStop(gauge), Is.False);
            Assert.That(worldTime.CurrentMode, Is.EqualTo(WorldTimeMode.Slowed));
            Assert.That(worldTime.SlowRemaining, Is.EqualTo(3f));
        }

        [TestCase(1f, 1f, true, WorldTimeMode.Normal, true)]
        [TestCase(0.99f, 1f, true, WorldTimeMode.Normal, false)]
        [TestCase(1f, 1f, false, WorldTimeMode.Normal, false)]
        [TestCase(1f, 1f, true, WorldTimeMode.Slowed, false)]
        [TestCase(1f, 1f, true, WorldTimeMode.Stopped, false)]
        public void 时缓只在正常时间且完成蓄力后触发(
            float heldDuration,
            float threshold,
            bool wasCharging,
            WorldTimeMode mode,
            bool expected)
        {
            Assert.That(
                PlayerTimeAbilities.ShouldTriggerSlow(heldDuration, threshold, wasCharging, mode),
                Is.EqualTo(expected));
        }

        [TestCase(WorldTimeMode.Normal, 1f)]
        [TestCase(WorldTimeMode.Slowed, 1f)]
        [TestCase(WorldTimeMode.Stopped, 0.25f)]
        public void 时停期间攻击补槽仅为正常值四分之一(
            WorldTimeMode mode,
            float expectedMultiplier)
        {
            Assert.That(
                PlayerCombat.GetGaugeGainMultiplier(mode),
                Is.EqualTo(expectedMultiplier));
        }

        [Test]
        public void 时停期间伤害缓存并在恢复后合并结算()
        {
            GameObject target = new("TimeAwareTarget");
            GameObject source = new("DamageSource");
            target.SetActive(false);

            try
            {
                HealthSystem health = target.AddComponent<HealthSystem>();
                ConfigureFloat(health, "damageProtectionDuration", 0f);
                LocalTimeController localTime = target.AddComponent<LocalTimeController>();
                TimeAwareDamageReceiver receiver = target.AddComponent<TimeAwareDamageReceiver>();
                ConfigureObjectReference(localTime, "worldTime", worldTime);
                ConfigureObjectReference(receiver, "worldTime", worldTime);
                InvokeLifecycle(health, "Awake");
                InvokeLifecycle(localTime, "Awake");
                InvokeLifecycle(receiver, "Awake");
                InvokeLifecycle(localTime, "OnEnable");
                InvokeLifecycle(receiver, "OnEnable");

                worldTime.UnlockTimeStop();
                gauge.Gain(60f);
                Assert.That(worldTime.TryStartTimeStop(gauge), Is.True);

                Assert.That(receiver.TryTakeDamage(
                    new DamageInfo(source, 1, Vector2.zero, Vector2.right)), Is.True);
                Assert.That(receiver.TryTakeDamage(
                    new DamageInfo(source, 1, Vector2.zero, Vector2.left)), Is.True);
                Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth));
                Assert.That(receiver.PendingDamage, Is.EqualTo(2));

                worldTime.EndTimeStopEarly();

                Assert.That(receiver.PendingDamage, Is.Zero);
                Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth - 2));
            }
            finally
            {
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(target);
            }
        }

        private static void ConfigureObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new(target);
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureFloat(Object target, string propertyName, float value)
        {
            SerializedObject serializedObject = new(target);
            serializedObject.FindProperty(propertyName).floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void InvokeLifecycle(Object target, string methodName)
        {
            target.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(target, null);
        }
    }
}
