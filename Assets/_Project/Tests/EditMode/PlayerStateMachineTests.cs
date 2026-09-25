using NUnit.Framework;
using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Tests
{
    public sealed class PlayerStateMachineTests
    {
        private GameObject player;
        private PlayerStateMachine stateMachine;

        [SetUp]
        public void SetUp()
        {
            player = new GameObject("PlayerStateMachineTests");
            stateMachine = player.AddComponent<PlayerStateMachine>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(player);
        }

        [Test]
        public void Attack后摇允许直接衔接Dash()
        {
            Assert.That(stateMachine.TryEnter(PlayerState.Attack), Is.True);
            Assert.That(stateMachine.TryEnter(PlayerState.Dash), Is.True);
            Assert.That(stateMachine.CurrentState, Is.EqualTo(PlayerState.Dash));
        }

        [Test]
        public void 受伤可以中断Dash且死亡会锁定状态()
        {
            Assert.That(stateMachine.TryEnter(PlayerState.Dash), Is.True);
            Assert.That(stateMachine.TryEnter(PlayerState.Hurt), Is.True);
            Assert.That(stateMachine.CurrentState, Is.EqualTo(PlayerState.Hurt));
            Assert.That(stateMachine.TryEnter(PlayerState.Dead), Is.True);
            Assert.That(stateMachine.TryEnter(PlayerState.Normal), Is.False);
        }
    }
}
