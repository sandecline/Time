using NUnit.Framework;
using TimeMetroidvania.Player;

namespace TimeMetroidvania.Tests
{
    public sealed class PlayerMovementTests
    {
        [TestCase(PlayerState.Normal, true, 0f, true)]
        [TestCase(PlayerState.Normal, false, -2f, true)]
        [TestCase(PlayerState.Attack, false, 0f, true)]
        [TestCase(PlayerState.Attack, true, 2f, true)]
        [TestCase(PlayerState.Attack, true, 0f, true)]
        [TestCase(PlayerState.Healing, true, 0f, true)]
        [TestCase(PlayerState.Healing, true, 1f, false)]
        [TestCase(PlayerState.Healing, false, 0f, false)]
        [TestCase(PlayerState.Dash, false, 0f, false)]
        [TestCase(PlayerState.Hurt, false, 0f, false)]
        [TestCase(PlayerState.Dead, false, 0f, false)]
        public void 状态与着地条件决定是否允许移动(
            PlayerState state, bool grounded, float verticalVelocity, bool expected)
        {
            Assert.That(
                PlayerMovement.CanControlMovement(state, grounded, verticalVelocity),
                Is.EqualTo(expected));
        }
    }
}
