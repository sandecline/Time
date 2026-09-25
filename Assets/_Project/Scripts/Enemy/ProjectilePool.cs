using TimeMetroidvania.GameTime;
using UnityEngine;

namespace TimeMetroidvania.Enemy
{
    /// <summary>
    /// 开场预热固定数量的投射物；射击与回收只切换已有实例。
    /// </summary>
    public sealed class ProjectilePool : MonoBehaviour
    {
        [SerializeField, Min(1)] private int capacity = 12;
        [SerializeField] private Sprite projectileSprite;
        [SerializeField] private WorldTimeService worldTime;

        private PooledProjectile[] shots;
        private int activeCount;

        public int Capacity => capacity;
        public int ActiveCount => activeCount;

        private void Awake()
        {
            shots = new PooledProjectile[capacity];
            for (int i = 0; i < capacity; i++)
            {
                GameObject shot = new($"ClockBolt_{i}");
                shot.transform.SetParent(transform, false);
                shot.transform.localScale = Vector3.one;

                GameObject visual = new("Visual");
                visual.transform.SetParent(shot.transform, false);
                visual.transform.localScale = Vector3.one * 0.06f;
                SpriteRenderer sprite = visual.AddComponent<SpriteRenderer>();
                sprite.sprite = projectileSprite;
                sprite.color = Color.white;
                sprite.sortingOrder = 20;

                CircleCollider2D collider = shot.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.radius = 0.2f;
                Rigidbody2D body = shot.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                LocalTimeController localTime = shot.AddComponent<LocalTimeController>();
                localTime.SetWorldTime(worldTime);
                shots[i] = shot.AddComponent<PooledProjectile>();
                shot.SetActive(false);
            }
        }

        public bool TryFire(Vector2 origin, Vector2 direction)
        {
            if (shots == null || direction.sqrMagnitude < 0.01f)
            {
                return false;
            }

            for (int i = 0; i < shots.Length; i++)
            {
                if (shots[i].gameObject.activeSelf)
                {
                    continue;
                }

                shots[i].Launch(this, origin, direction);
                activeCount++;
                return true;
            }
            return false;
        }

        internal void Recycle(PooledProjectile shot)
        {
            if (!shot.gameObject.activeSelf)
            {
                return;
            }
            shot.gameObject.SetActive(false);
            activeCount = Mathf.Max(0, activeCount - 1);
        }

        public void ClearActive()
        {
            if (shots == null)
            {
                return;
            }
            foreach (PooledProjectile shot in shots)
            {
                Recycle(shot);
            }
        }
    }
}
