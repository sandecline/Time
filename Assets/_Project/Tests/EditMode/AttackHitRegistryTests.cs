using NUnit.Framework;
using TimeMetroidvania.Player;

namespace TimeMetroidvania.Tests
{
    public sealed class AttackHitRegistryTests
    {
        [Test]
        public void 同一次挥剑只接受同一目标一次()
        {
            AttackHitRegistry registry = new();
            DamageableStub target = new();

            Assert.That(registry.TryRegister(target), Is.True);
            Assert.That(registry.TryRegister(target), Is.False);

            registry.Clear();
            Assert.That(registry.TryRegister(target), Is.True);
        }

        private sealed class DamageableStub : IDamageable
        {
            public bool TryTakeDamage(DamageInfo damageInfo)
            {
                return true;
            }
        }
    }
}
