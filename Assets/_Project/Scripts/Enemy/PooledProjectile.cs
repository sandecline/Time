using TimeMetroidvania.GameTime;
using TimeMetroidvania.Player;
using TimeMetroidvania.Presentation;
using UnityEngine;
using TimeMetroidvania.Enemy;

namespace TimeMetroidvania.Enemy
{
    /// <summary>
    /// 受局部时间控制的可斩弹幕；击中、斩除或寿命结束时归还对象池。
    /// </summary>
    [RequireComponent(typeof(LocalTimeController), typeof(Collider2D), typeof(Rigidbody2D))]
    public sealed class PooledProjectile : MonoBehaviour, IHittableProjectile
    {
        [SerializeField, Min(0f)] private float speed = 7f;
        [SerializeField, Min(0.1f)] private float lifetime = 4f;

        private ProjectilePool pool;
        private LocalTimeController localTime;
        private Rigidbody2D body;
        private SpriteRenderer sprite;
        private WorldTimeService worldTime;
        private Vector2 direction;
        private float remaining;

        private void Awake()
        {
            localTime = GetComponent<LocalTimeController>();
            body = GetComponent<Rigidbody2D>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            worldTime = FindFirstObjectByType<WorldTimeService>();
        }

        internal void Launch(ProjectilePool owner, Vector2 origin, Vector2 travelDirection)
        {
            pool = owner;
            direction = travelDirection.normalized;
            remaining = lifetime;
            transform.position = origin;
            transform.right = direction;
            gameObject.SetActive(true);
        }

        private void FixedUpdate()
        {
            float delta = localTime.LocalFixedDeltaTime;
            if (delta <= 0f)
            {
                return;
            }
            Vector2 next = body.position + direction * (speed * delta);
            if (WorldCollision2D.Blocks(body.position, next, 0.2f))
            {
                pool.Recycle(this);
                return;
            }
            body.MovePosition(next);
            remaining -= delta;
            if (remaining <= 0f)
            {
                pool.Recycle(this);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.isTrigger && other.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                pool.Recycle(this);
                return;
            }
            if (!other.CompareTag("Player"))
            {
                return;
            }
            HealthSystem health = other.GetComponentInParent<HealthSystem>();
            health?.TryTakeDamage(new DamageInfo(
                gameObject, 1, transform.position, direction * 5f + Vector2.up * 2f));
            pool.Recycle(this);
        }

        public bool TryDestroyByAttack(GameObject attacker)
        {
            if (!gameObject.activeSelf || attacker == null || !attacker.CompareTag("Player"))
            {
                return false;
            }
            TimeStopProjectileShatter.TryCreate(sprite, worldTime);
            pool.Recycle(this);
            return true;
        }
    }
}
