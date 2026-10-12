using TimeMetroidvania.GameTime;
using TimeMetroidvania.Player;
using TimeMetroidvania.Presentation;
using UnityEngine;
using TimeMetroidvania.Enemy;

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
        private WorldTimeService worldTime;
        private Transform visual;
        private CircleCollider2D hitCircle;
        private Vector2 direction;
        private float speed;
        private float remaining;
        private bool breakable;
        private bool fading;
        private float fadeRemaining;
        private float fadeDuration;
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
            hitCircle = GetComponent<CircleCollider2D>();
            worldTime = FindFirstObjectByType<WorldTimeService>();
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
            remaining = timeAnchored ? 4f : 7.5f;
            breakable = canBreak;
            fading = false;
            fadeRemaining = 0f;
            if (hitCircle != null) hitCircle.enabled = true;
            IsTimeAnchored = timeAnchored;
            transform.position = origin;
            body.position = origin;
            body.linearVelocity = Vector2.zero;
            transform.right = direction;
            localTime.SetTimeResistance(timeAnchored);
            sprite.sprite = timeAnchored
                ? timeAnchorSprite
                : canBreak
                    ? breakableSprite
                    : unbreakableSprite;
            sprite.color = Color.white;
            // 实体像素收在对应圆形碰撞体内；三角弹的外接框还需留出角点余量。
            float diameter = timeAnchored ? 0.25f : canBreak ? 0.18f : 0.23f;
            if (hitCircle != null)
                hitCircle.radius = timeAnchored ? 0.18f : canBreak ? 0.10f : 0.13f;
            if (visual != null)
            {
                float spriteWidth = sprite.sprite != null ? sprite.sprite.bounds.size.x : 1f;
                visual.localScale = Vector3.one * (diameter / Mathf.Max(0.01f, spriteWidth));
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

            if (fading)
            {
                fadeRemaining -= delta;
                Color fadeColor = sprite.color;
                fadeColor.a = Mathf.Clamp01(fadeRemaining / fadeDuration);
                sprite.color = fadeColor;
                if (fadeRemaining <= 0f) pool.Recycle(this);
                return;
            }

            Vector2 next = body.position + direction * (speed * delta);
            if (WorldCollision2D.Blocks(body.position, next,
                hitCircle != null ? hitCircle.radius : 0.1f))
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

        internal void BeginFade(float duration)
        {
            if (fading || !gameObject.activeSelf) return;
            fading = true;
            fadeDuration = Mathf.Max(0.01f, duration);
            fadeRemaining = fadeDuration;
            if (hitCircle != null) hitCircle.enabled = false;
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

            TimeStopProjectileShatter.TryCreate(sprite, worldTime);
            pool.Recycle(this);
            return true;
        }
    }
}
