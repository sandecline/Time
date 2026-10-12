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
        public void 时停即时开启并按每秒十点持续消耗()
        {
            worldTime.UnlockTimeStop();
            gauge.Fill();

            Assert.That(worldTime.TryStartTimeStop(gauge), Is.True);
            Assert.That(worldTime.CurrentMode, Is.EqualTo(WorldTimeMode.Stopped));
            Assert.That(worldTime.TimeStopMaximum, Is.EqualTo(10f));
            Assert.That(gauge.CurrentValue, Is.EqualTo(100f));

            worldTime.AdvanceUnscaledTime(2f);
            Assert.That(gauge.CurrentValue, Is.EqualTo(80f).Within(0.001f));
            Assert.That(worldTime.TimeStopRemaining, Is.EqualTo(8f).Within(0.001f));
            Assert.That(worldTime.EndTimeStopEarly(), Is.True);
            Assert.That(worldTime.CurrentMode, Is.EqualTo(WorldTimeMode.Normal));
            Assert.That(gauge.CurrentValue, Is.EqualTo(80f).Within(0.001f));
        }

        [Test]
        public void 时间槽耗尽会自动结束时停()
        {
            worldTime.UnlockTimeStop();
            gauge.Gain(20f);

            Assert.That(worldTime.TryStartTimeStop(gauge), Is.True);
            worldTime.AdvanceUnscaledTime(2f);

            Assert.That(worldTime.CurrentMode, Is.EqualTo(WorldTimeMode.Normal));
            Assert.That(gauge.CurrentValue, Is.Zero);
        }

        [Test]
        public void 时间回声按四分之一累计且最多返还消耗的一半()
        {
            worldTime.UnlockTimeStop();
            gauge.Fill();
            Assert.That(worldTime.TryStartTimeStop(gauge), Is.True);

            worldTime.AdvanceUnscaledTime(1f);
            for (int index = 0; index < 10; index++)
            {
                Assert.That(worldTime.RegisterTimeEcho(10f), Is.True);
            }

            Assert.That(worldTime.PendingTimeEcho, Is.EqualTo(25f));
            Assert.That(worldTime.SettleableTimeEcho, Is.EqualTo(5f));
            worldTime.EndTimeStopEarly();

            Assert.That(gauge.CurrentValue, Is.EqualTo(95f).Within(0.001f));
        }

        [Test]
        public void 旧时长升级接口不会改变十秒上限()
        {
            worldTime.UnlockTimeStop();
            worldTime.UnlockDurationUpgrade();
            gauge.Fill();

            Assert.That(worldTime.TryStartTimeStop(gauge), Is.True);
            Assert.That(worldTime.HasDurationUpgrade, Is.False);
            Assert.That(worldTime.TimeStopMaximum, Is.EqualTo(10f));
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
                gauge.Fill();
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