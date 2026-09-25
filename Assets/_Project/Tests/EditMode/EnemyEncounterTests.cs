using System.Reflection;
using NUnit.Framework;
using TimeMetroidvania.Enemy;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TimeMetroidvania.Tests
{
    public sealed class EnemyEncounterTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Prototype/EnemyEncounter.unity";

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [Test]
        public void 两段遭遇包含三类敌人及有限弹药的射手()
        {
            Transform first = GameObject.Find("Encounter_1_Guard_Bug").transform;
            Transform second = GameObject.Find("Encounter_2_Bug_Archer_Guard").transform;
            Assert.That(first.childCount, Is.EqualTo(2));
            Assert.That(second.childCount, Is.EqualTo(3));
            Assert.That(GameObject.Find("TrainingDummy"), Is.Null);
            Assert.That(GameObject.Find("EncounterGround"), Is.Not.Null);

            EnemyBrain[] enemies = Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None);
            Assert.That(enemies.Length, Is.EqualTo(5));
            Assert.That(GameObject.Find("Guard_A").GetComponent<EnemyBrain>().Kind,
                Is.EqualTo(EnemyKind.Guard));
            Assert.That(GameObject.Find("ClockworkBug_A").GetComponent<EnemyBrain>().Kind,
                Is.EqualTo(EnemyKind.ClockworkBug));
            EnemyBrain archer = GameObject.Find("ClockArcher_B").GetComponent<EnemyBrain>();
            Assert.That(archer.Kind, Is.EqualTo(EnemyKind.ClockArcher));
            Assert.That(archer.ShotsRemaining, Is.EqualTo(8));
            ProjectilePool pool = Object.FindFirstObjectByType<ProjectilePool>();
            WorldTimeService world = Object.FindFirstObjectByType<WorldTimeService>();
            Assert.That(pool, Is.Not.Null);
            Assert.That(world, Is.Not.Null);
            Assert.That(new SerializedObject(pool).FindProperty("worldTime").objectReferenceValue,
                Is.SameAs(world));
            PlayerCombat combat = GameObject.Find("Player").GetComponent<PlayerCombat>();
            Assert.That(new SerializedObject(combat).FindProperty("worldTime").objectReferenceValue,
                Is.SameAs(world));
            foreach (EnemyBrain enemy in enemies)
            {
                Assert.That(enemy.GetComponent<TimeAwareDamageReceiver>(), Is.Not.Null);
                Assert.That(enemy.GetComponent<LocalTimeController>(), Is.Not.Null);
            }
        }

        [Test]
        public void 守卫经历巡逻警戒攻击受击与死亡()
        {
            GameObject guard = GameObject.Find("Guard_A");
            EnemyBrain brain = guard.GetComponent<EnemyBrain>();
            HealthSystem health = guard.GetComponent<HealthSystem>();
            SerializedObject serializedHealth = new(health);
            serializedHealth.FindProperty("damageProtectionDuration").floatValue = 0f;
            serializedHealth.ApplyModifiedPropertiesWithoutUndo();
            Invoke(brain, "Awake");
            Invoke(brain, "OnEnable");

            Assert.That(brain.CurrentState, Is.EqualTo(EnemyState.Patrol));
            brain.Advance(0.02f);
            Assert.That(brain.CurrentState, Is.EqualTo(EnemyState.Alert));

            GameObject player = GameObject.Find("Player");
            player.transform.position = guard.transform.position + Vector3.left * 0.6f;
            brain.Advance(0.02f);
            Assert.That(brain.CurrentState, Is.EqualTo(EnemyState.Attack));

            Assert.That(health.TryTakeDamage(new DamageInfo(
                player, 1, Vector2.zero, Vector2.zero)), Is.True);
            Assert.That(brain.CurrentState, Is.EqualTo(EnemyState.Hurt));
            Assert.That(health.TryTakeDamage(new DamageInfo(
                player, health.MaxHealth, Vector2.zero, Vector2.zero)), Is.True);
            Assert.That(brain.CurrentState, Is.EqualTo(EnemyState.Dead));
            Assert.That(guard.GetComponent<Collider2D>().enabled, Is.False);
        }

        [Test]
        public void 敌人响应时缓与时停且恢复原速()
        {
            WorldTimeService world = Object.FindFirstObjectByType<WorldTimeService>();
            LocalTimeController local = GameObject.Find("ClockworkBug_A")
                .GetComponent<LocalTimeController>();
            Invoke(local, "Awake");
            Invoke(local, "OnEnable");

            Assert.That(world.TryStartSlow(), Is.True);
            Assert.That(local.CurrentTimeScale, Is.EqualTo(0.35f).Within(0.001f));

            TimeGauge gauge = GameObject.Find("Player").GetComponent<TimeGauge>();
            gauge.Gain(60f);
            Assert.That(world.TryStartTimeStop(gauge), Is.True);
            Assert.That(local.CurrentTimeScale, Is.Zero);
            Assert.That(world.EndTimeStopEarly(), Is.True);
            Assert.That(local.CurrentTimeScale, Is.EqualTo(1f));
        }

        [Test]
        public void 投射物回收后复用同一个实例()
        {
            ProjectilePool pool = Object.FindFirstObjectByType<ProjectilePool>();
            SerializedObject serializedPool = new(pool);
            serializedPool.FindProperty("capacity").intValue = 2;
            serializedPool.ApplyModifiedPropertiesWithoutUndo();
            Invoke(pool, "Awake");

            GameObject attacker = GameObject.Find("Player");
            Assert.That(pool.TryFire(Vector2.zero, Vector2.right), Is.True);
            PooledProjectile first = pool.transform.GetChild(0).GetComponent<PooledProjectile>();
            Assert.That(first.gameObject.activeSelf, Is.True);
            Assert.That(pool.TryFire(Vector2.zero, Vector2.right), Is.True);
            Assert.That(pool.TryFire(Vector2.zero, Vector2.right), Is.False);
            Assert.That(pool.ActiveCount, Is.EqualTo(2));

            Assert.That(first.TryDestroyByAttack(attacker), Is.True);
            Assert.That(pool.ActiveCount, Is.EqualTo(1));
            Assert.That(pool.TryFire(Vector2.one, Vector2.left), Is.True);
            Assert.That(pool.transform.GetChild(0).GetComponent<PooledProjectile>(), Is.SameAs(first));
            Assert.That(pool.ActiveCount, Is.EqualTo(2));
        }

        [Test]
        public void 钟弩手耗尽弹药后不会持续产出可斩弹幕()
        {
            ProjectilePool pool = Object.FindFirstObjectByType<ProjectilePool>();
            Invoke(pool, "Awake");
            EnemyBrain archer = GameObject.Find("ClockArcher_B").GetComponent<EnemyBrain>();
            Invoke(archer, "Awake");
            GameObject.Find("Player").transform.position =
                archer.transform.position + Vector3.left * 2f;

            archer.Advance(0.01f);
            archer.Advance(0.01f);
            for (int i = 0; i < 8; i++)
            {
                Assert.That(archer.CurrentState, Is.EqualTo(EnemyState.Attack));
                archer.Advance(0.61f);
                archer.Advance(0.16f);
                archer.Advance(1.6f);
            }

            Assert.That(archer.ShotsRemaining, Is.Zero);
            Assert.That(pool.ActiveCount, Is.EqualTo(8));
            Assert.That(archer.CurrentState, Is.EqualTo(EnemyState.Alert));
            archer.Advance(20f);
            Assert.That(pool.ActiveCount, Is.EqualTo(8));
        }

        private static void Invoke(Object target, string method)
        {
            target.GetType()
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(target, null);
        }
    }
}
