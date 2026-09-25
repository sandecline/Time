using TimeMetroidvania.GameTime;
using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Boss
{
    /// <summary>
    /// Boss 预热弹幕；普通弹幕可斩，时间锚弹幕在时停中继续移动且不可斩。
    /// </summary>
    [RequireComponent(typeof(LocalTimeController), typeof(Collider2D), typeof(Rigidbody2D))]
    public sealed class BossProjectile : MonoBehaviour, IHittableProjectile
    {
        private BossProjectilePool pool;
        private LocalTimeController localTime;
        private Rigidbody2D body;
        private SpriteRenderer sprite;
        private Transform visual;
        private Vector2 direction;
        private float speed;
        private float remaining;
        private bool breakable;
        private Sprite breakableSprite;
        private Sprite unbreakableSprite;
        private Sprite timeAnchorSprite;

        public bool IsTimeAnchored { get; private set; }
        public bool IsBreakable => breakable;

        internal void ConfigureVisuals(
            Sprite blueBreakable,
            Sprite orangeUnbreakable,
            Sprite purpleTimeAnchor)
        {
            breakableSprite = blueBreakable;
            unbreakableSprite = orangeUnbreakable;
            timeAnchorSprite = purpleTimeAnchor;
        }

        private void Awake()
        {
            localTime = GetComponent<LocalTimeController>();
            body = GetComponent<Rigidbody2D>();
            visual = transform.Find("Visual");
            sprite = visual != null
                ? visual.GetComponent<SpriteRenderer>()
                : GetComponent<SpriteRenderer>();
        }

        internal void Launch(
            BossProjectilePool owner, Vector2 origin, Vector2 travelDirection,
            float travelSpeed, bool canBreak, bool timeAnchored)
        {
            pool = owner;
            direction = travelDirection.normalized;
            speed = travelSpeed;
            remaining = 5f;
            breakable = canBreak;
            IsTimeAnchored = timeAnchored;
            transform.position = origin;
            transform.right = direction;
            localTime.SetTimeResistance(timeAnchored);
            sprite.sprite = timeAnchored
                ? timeAnchorSprite
                : canBreak
                    ? breakableSprite
                    : unbreakableSprite;
            sprite.color = Color.white;
            if (visual != null)
            {
                visual.localScale = Vector3.one * (timeAnchored ? 0.065f : 0.055f);
            }
            gameObject.SetActive(true);
        }

        private void FixedUpdate()
        {
            float delta = localTime.LocalFixedDeltaTime;
            if (delta <= 0f)
            {
                return;
            }

            body.MovePosition(body.position + direction * (speed * delta));
            remaining -= delta;
            if (remaining <= 0f)
            {
                pool.Recycle(this);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            HealthSystem health = other.GetComponentInParent<HealthSystem>();
            health?.TryTakeDamage(new DamageInfo(
                gameObject, 1, transform.position, direction * 6f + Vector2.up * 2f));
            pool.Recycle(this);
        }

        public bool TryDestroyByAttack(GameObject attacker)
        {
            if (!breakable || !gameObject.activeSelf || attacker == null
                || !attacker.CompareTag("Player"))
            {
                return false;
            }

            pool.Recycle(this);
            return true;
        }
    }
}
