using NUnit.Framework;
using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Tests
{
    public sealed class HealthSystemTests
    {
        private GameObject player;
        private GameObject source;
        private PlayerStateMachine stateMachine;
        private HealthSystem health;

        [SetUp]
        public void SetUp()
        {
            player = new GameObject("HealthSystemTests");
            player.AddComponent<Rigidbody2D>();
            stateMachine = player.AddComponent<PlayerStateMachine>();
            health = player.AddComponent<HealthSystem>();
            source = new GameObject("DamageSource");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(player);
        }

        [Test]
        public void 受伤保护会拒绝紧接着的第二次伤害()
        {
            DamageInfo damage = new(source, 1, Vector2.zero, Vector2.right);

            Assert.That(health.TryTakeDamage(damage), Is.True);
            Assert.That(health.TryTakeDamage(damage), Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth - 1));
        }

        [Test]
        public void Dash不提供无敌且受伤会中断Dash()
        {
            Assert.That(stateMachine.TryEnter(PlayerState.Dash), Is.True);

            bool applied = health.TryTakeDamage(new DamageInfo(
                source,
                1,
                Vector2.zero,
                new Vector2(-4f, 2f)));

            Assert.That(applied, Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth - 1));
            Assert.That(stateMachine.CurrentState, Is.EqualTo(PlayerState.Hurt));
        }

        [Test]
        public void 致命伤害进入死亡状态()
        {
            bool applied = health.TryTakeDamage(new DamageInfo(
                source,
                health.MaxHealth,
                Vector2.zero,
                Vector2.zero));

            Assert.That(applied, Is.True);
            Assert.That(health.IsDead, Is.True);
            Assert.That(stateMachine.CurrentState, Is.EqualTo(PlayerState.Dead));
        }
    }
}
