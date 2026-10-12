using UnityEngine;
using TimeMetroidvania.GameTime;

namespace TimeMetroidvania.Player
{
    /// <summary>
    /// 将敌人或陷阱碰撞转换为统一伤害；Dash 不会绕过该入口。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class ContactDamage2D : MonoBehaviour
    {
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField, Min(0f)] private float horizontalKnockback = 6f;
        [SerializeField, Min(0f)] private float verticalKnockback = 3f;

        private HealthSystem ownHealth;
        private LocalTimeController localTime;

        private void Awake()
        {
            ownHealth = GetComponentInParent<HealthSystem>();
            localTime = GetComponentInParent<LocalTimeController>();
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryDamage(collision.collider, collision.GetContact(0).point);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDamage(other, other.ClosestPoint(transform.position));
        }

        private void TryDamage(Collider2D other, Vector2 hitPoint)
        {
            if ((ownHealth != null && ownHealth.IsDead) || !other.CompareTag("Player"))
            {
                return;
            }

            IDamageable damageable = other.GetComponentInParent(typeof(IDamageable)) as IDamageable;
            if (damageable == null)
            {
                return;
            }

            float direction = Mathf.Sign(other.transform.position.x - transform.position.x);
            Vector2 knockback = new(direction * horizontalKnockback, verticalKnockback);
            damageable.TryTakeDamage(new DamageInfo(gameObject, damage, hitPoint, knockback));
        }
    }
}
