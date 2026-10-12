using TimeMetroidvania.GameTime;
using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Enemy
{
    /// <summary>按局部时间积分重力并扫掠地形，地面与飞行敌人共用墙体阻挡和击退。</summary>
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class EnemyMotor2D : MonoBehaviour
    {
        public bool Flying;
        public Vector2 DesiredVelocity;
        public bool Grounded { get; private set; }
        public bool BlockedHorizontally { get; private set; }
        public Collider2D LastBlockingCollider { get; private set; }
        private readonly RaycastHit2D[] hits = new RaycastHit2D[24];
        private Rigidbody2D body;
        private Collider2D shape;
        private LocalTimeController clock;
        private HealthSystem health;
        private Vector2 velocity;
        private float knockbackRemaining;
        private const float Skin = 0.025f;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<Collider2D>();
            clock = GetComponent<LocalTimeController>();
            health = GetComponent<HealthSystem>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void OnEnable() { if (health != null) health.Damaged += OnDamaged; }
        private void OnDisable() { if (health != null) health.Damaged -= OnDamaged; }
        private void OnDamaged(DamageInfo hit) { velocity = hit.Knockback; knockbackRemaining = 0.22f; }

        private void FixedUpdate()
        {
            body.linearVelocity = Vector2.zero;
            if (health != null && health.IsDead) return;
            float dt = clock != null ? clock.LocalFixedDeltaTime : Time.fixedDeltaTime;
            if (dt <= 0f) return;
            Vector2 position = body.position;
            LastBlockingCollider = null;
            Grounded = Sweep(position, Vector2.down, 0.09f, false) < 0.09f;
            knockbackRemaining = Mathf.Max(0f, knockbackRemaining - dt);
            if (knockbackRemaining <= 0f)
            {
                velocity.x = DesiredVelocity.x;
                if (Flying) velocity.y = DesiredVelocity.y;
            }
            if (!Flying)
                velocity.y = Grounded && velocity.y <= 0f ? 0f : Mathf.Max(-20f, velocity.y - 28f * dt);
            Vector2 motion = velocity * dt;
            float x = Sweep(position, Vector2.right * Mathf.Sign(motion.x), Mathf.Abs(motion.x), true);
            BlockedHorizontally = x + 0.001f < Mathf.Abs(motion.x);
            position.x += Mathf.Sign(motion.x) * x;
            float y = Sweep(position, Vector2.up * Mathf.Sign(motion.y), Mathf.Abs(motion.y), motion.y > 0f);
            position.y += Mathf.Sign(motion.y) * y;
            if (y + 0.001f < Mathf.Abs(motion.y)) velocity.y = 0f;
            if (motion.sqrMagnitude > 0f) body.MovePosition(position);
        }

        private float Sweep(Vector2 position, Vector2 direction, float distance, bool ignoreOneWay)
        {
            if (distance <= 0f) return 0f;
            Bounds bounds = shape.bounds;
            Vector2 origin = (Vector2)bounds.center + position - body.position;
            int count = Physics2D.BoxCast(origin, bounds.size, transform.eulerAngles.z,
                direction, WorldCollision2D.TerrainFilter, hits, distance + Skin);
            float allowed = distance;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider == null || hits[i].collider == shape) continue;
                if (ignoreOneWay && hits[i].collider.usedByEffector) continue;
                if (Vector2.Dot(hits[i].normal, direction) > -0.1f) continue;
                float candidate = Mathf.Max(0f, hits[i].distance - Skin);
                if (candidate < allowed)
                {
                    allowed = candidate;
                    LastBlockingCollider = hits[i].collider;
                }
            }
            return allowed;
        }

        public bool HasFloorAhead(int facing)
        {
            Bounds b = shape.bounds;
            Vector2 start = new(b.center.x + facing * (b.extents.x + 0.18f), b.min.y + 0.12f);
            return Physics2D.Raycast(start, Vector2.down, WorldCollision2D.TerrainFilter, hits, 0.65f) > 0;
        }

        public void ResetMotion()
        {
            velocity = DesiredVelocity = Vector2.zero;
            knockbackRemaining = 0f;
            LastBlockingCollider = null;
            if (body != null) body.linearVelocity = Vector2.zero;
        }
    }
}
