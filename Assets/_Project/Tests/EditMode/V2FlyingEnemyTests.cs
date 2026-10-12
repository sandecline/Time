using NUnit.Framework;
using TimeMetroidvania.Enemy;

namespace TimeMetroidvania.Tests.EditMode
{
    public sealed class V2FlyingEnemyTests
    {
        [TestCase(false, 8.9f, true)]
        [TestCase(false, 9.1f, false)]
        [TestCase(true, 11.9f, true)]
        [TestCase(true, 12.1f, false)]
        public void 仇恨使用进入与脱离双阈值(
            bool wasChasing, float distance, bool expected)
        {
            Assert.That(V2FlyingEnemy.ShouldChase(
                wasChasing, distance, 9f, 12f), Is.EqualTo(expected));
        }

        [Test]
        public void 脱离范围不会小于进入范围()
        {
            Assert.That(V2FlyingEnemy.ShouldChase(
                true, 8f, 9f, 5f), Is.True);
            Assert.That(V2FlyingEnemy.ShouldChase(
                true, 9.1f, 9f, 5f), Is.False);
        }
    }
}