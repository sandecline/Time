using TimeMetroidvania.GameTime;
using UnityEngine;

namespace TimeMetroidvania.Boss
{
    /// <summary>
    /// 为 Boss 预热普通、可斩和时间锚弹幕，战斗中不创建新对象。
    /// </summary>
    public sealed class BossProjectilePool : MonoBehaviour
    {
        [SerializeField, Min(1)] private int capacity = 24;
        [SerializeField] private Sprite projectileSprite;
        [SerializeField] private Sprite breakableProjectileSprite;
        [SerializeField] private Sprite unbreakableProjectileSprite;
        [SerializeField] private Sprite timeAnchorProjectileSprite;
        [SerializeField] private WorldTimeService worldTime;

        private BossProjectile[] projectiles;
        private int activeCount;

        public int Capacity => capacity;
        public int ActiveCount => activeCount;

        private void Awake()
        {
            projectiles = new BossProjectile[capacity];
            for (int index = 0; index < capacity; index++)
            {
                GameObject projectile = new($"BossProjectile_{index}");
                projectile.transform.SetParent(transform, false);
                projectile.transform.localScale = Vector3.one;

                GameObject visual = new("Visual");
                visual.transform.SetParent(projectile.transform, false);
                visual.transform.localScale = Vector3.one * 0.055f;
                SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = breakableProjectileSprite != null
                    ? breakableProjectileSprite
                    : projectileSprite;
                renderer.sortingOrder = 20;
                CircleCollider2D collider = projectile.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.radius = 0.22f;
                Rigidbody2D body = projectile.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                LocalTimeController localTime = projectile.AddComponent<LocalTimeController>();
                localTime.SetWorldTime(worldTime);
                projectiles[index] = projectile.AddComponent<BossProjectile>();
                projectiles[index].ConfigureVisuals(
                    breakableProjectileSprite != null
                        ? breakableProjectileSprite
                        : projectileSprite,
                    unbreakableProjectileSprite != null
                        ? unbreakableProjectileSprite
                        : projectileSprite,
                    timeAnchorProjectileSprite != null
                        ? timeAnchorProjectileSprite
                        : projectileSprite);
                projectile.SetActive(false);
            }
        }

        public bool TryFire(
            Vector2 origin, Vector2 direction, float speed,
            bool breakable, bool timeAnchored)
        {
            if (projectiles == null || direction.sqrMagnitude < 0.01f)
            {
                return false;
            }

            foreach (BossProjectile projectile in projectiles)
            {
                if (projectile.gameObject.activeSelf)
                {
                    continue;
                }

                projectile.Launch(
                    this, origin, direction, speed, breakable, timeAnchored);
                activeCount++;
                return true;
            }

            return false;
        }

        internal void Recycle(BossProjectile projectile)
        {
            if (projectile == null || !projectile.gameObject.activeSelf)
            {
                return;
            }

            projectile.gameObject.SetActive(false);
            activeCount = Mathf.Max(0, activeCount - 1);
        }

        public void ClearActive()
        {
            if (projectiles == null)
            {
                return;
            }

            foreach (BossProjectile projectile in projectiles)
            {
                Recycle(projectile);
            }
        }
    }
}
