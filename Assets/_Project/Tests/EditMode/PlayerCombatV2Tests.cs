using NUnit.Framework;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Tests
{
    public sealed class PlayerCombatV2Tests
    {
        [TestCase(0f, true, AttackDirection.Horizontal)]
        [TestCase(-1f, true, AttackDirection.Horizontal)]
        [TestCase(1f, true, AttackDirection.Up)]
        [TestCase(1f, false, AttackDirection.Up)]
        [TestCase(-1f, false, AttackDirection.Down)]
        public void 根据纵向输入和落地状态选择攻击方向(
            float verticalInput,
            bool grounded,
            AttackDirection expected)
        {
            Assert.That(
                PlayerCombat.ResolveAttackDirection(verticalInput, grounded),
                Is.EqualTo(expected));
        }

        [Test]
        public void 独立攻击框按方向配置且只在有效段启用()
        {
            GameObject root = new("AttackHitboxTestRoot");
            root.transform.localScale = new Vector3(1f, 1.6f, 1f);
            GameObject hitboxObject = new("AttackHitbox");
            hitboxObject.transform.SetParent(root.transform, false);

            try
            {
                PlayerAttackHitbox2D hitbox =
                    hitboxObject.AddComponent<PlayerAttackHitbox2D>();
                hitbox.SetShape(
                    AttackDirection.Horizontal,
                    new Vector2(0.8f, 0.1f),
                    new Vector2(1.2f, 0.9f),
                    -1);

                Assert.That(hitbox.HitboxCollider.isTrigger, Is.True);
                Assert.That(
                    hitbox.transform.localPosition.x,
                    Is.EqualTo(-0.8f).Within(0.001f));
                Assert.That(hitbox.Size, Is.EqualTo(new Vector2(1.2f, 0.9f)));
                Assert.That(hitbox.WorldSize.y, Is.EqualTo(1.44f).Within(0.001f));

                hitbox.SetShape(
                    AttackDirection.Up,
                    new Vector2(0f, 0.75f),
                    new Vector2(0.95f, 1.1f),
                    1);
                Assert.That(hitbox.WorldCenter.y, Is.EqualTo(0.75f).Within(0.001f));
                Assert.That(hitbox.WorldSize.x, Is.EqualTo(0.95f).Within(0.001f));
                Assert.That(hitbox.WorldSize.y, Is.EqualTo(1.1f).Within(0.001f));

                hitbox.SetShape(
                    AttackDirection.Down,
                    new Vector2(0f, -0.75f),
                    new Vector2(0.95f, 1.1f),
                    1);
                Assert.That(hitbox.WorldCenter.y, Is.EqualTo(-0.75f).Within(0.001f));
                Assert.That(hitbox.WorldSize.y, Is.EqualTo(1.1f).Within(0.001f));

                hitbox.SetActive(true);
                Assert.That(hitbox.IsActive, Is.True);
                hitbox.SetActive(false);
                Assert.That(hitbox.IsActive, Is.False);

                object target = new();
                hitbox.BeginSwing();
                Assert.That(hitbox.TryRegister(target), Is.True);
                Assert.That(hitbox.TryRegister(target), Is.False);
                hitbox.BeginSwing();
                Assert.That(hitbox.TryRegister(target), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HitStop只缩放本地时间并按真实时间恢复()
        {
            GameObject target = new("HitStopTarget");
            float globalTimeScale = Time.timeScale;

            try
            {
                HitStopController hitStop = target.AddComponent<HitStopController>();
                LocalTimeController localTime = target.AddComponent<LocalTimeController>();
                hitStop.Request(0.05f);

                Assert.That(hitStop.IsPaused, Is.True);
                Assert.That(hitStop.ScaleDeltaTime(0.2f), Is.Zero);
                Assert.That(localTime.LocalDeltaTime, Is.Zero);
                Assert.That(Time.timeScale, Is.EqualTo(globalTimeScale));

                hitStop.Advance(0.05f);

                Assert.That(hitStop.IsPaused, Is.False);
                Assert.That(hitStop.ScaleDeltaTime(0.2f), Is.EqualTo(0.2f));
                Assert.That(localTime.IsHitStopped, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
