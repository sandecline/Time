using NUnit.Framework;
using TimeMetroidvania.Level;
using UnityEngine;

namespace TimeMetroidvania.Tests
{
    public sealed class PortalBoundaryTests
    {
        [TestCase(1f, 0f)] [TestCase(-1f, 0f)] [TestCase(0f, 1f)] [TestCase(0f, -1f)]
        public void 身体尾缘完全穿过才算越界(float x, float y)
        {
            Vector2 normal = new(x, y);
            Bounds actor = new(Vector3.zero, Vector3.one);
            Assert.That(StageFiveDoor.TrailingDistance(actor, Vector2.zero, normal), Is.LessThan(0f));
            actor.center = normal * 0.51f;
            Assert.That(StageFiveDoor.TrailingDistance(actor, Vector2.zero, normal), Is.GreaterThan(0f));
        }
        [Test]
        public void 擦到门外墙体不满足入口宽度()
        {
            Bounds door = new(Vector3.zero, new Vector3(1.4f, 3f, 1f));
            Bounds player = new(new Vector3(1f, 1.7f, 0f), new Vector3(0.6f, 0.8f, 1f));
            Assert.That(StageFiveDoor.FitsOpening(player, door, Vector2.right), Is.False);
        }
    }
}
