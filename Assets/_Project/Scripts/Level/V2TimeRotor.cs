using TimeMetroidvania.GameTime;
using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Level
{
    /// <summary>逆时针实体转闸，碰触退回进入侧安全点，保护时间不能绕过机械阻挡。</summary>
    public sealed class V2TimeRotor : MonoBehaviour, ITimeControllable
    {
        [SerializeField] private WorldTimeService worldTime;
        [SerializeField, Min(0f)] private float degreesPerSecond = 300f;
        [SerializeField] private Transform leftSafeAnchor;
        [SerializeField] private Transform rightSafeAnchor;
        private float currentScale = 1f;
        private Rigidbody2D body;
        private Transform player;
        private int approachSide = -1;
        private float contactCooldown;
        private Collider2D blade;
        private readonly Collider2D[] overlaps = new Collider2D[12];
        public bool IsTimeResistant => false;
        public float CurrentScale => currentScale;
        public void Configure(WorldTimeService service, float speed) { worldTime = service; degreesPerSecond = Mathf.Abs(speed); }
        public void ConfigureSafety(Transform left, Transform right) { leftSafeAnchor = left; rightSafeAnchor = right; }
        private void OnEnable()
        {
            worldTime ??= FindFirstObjectByType<WorldTimeService>();
            body = GetComponent<Rigidbody2D>(); blade = GetComponent<Collider2D>();
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            worldTime?.Register(this);
        }
        private void OnDisable() => worldTime?.Unregister(this);
        private void FixedUpdate()
        {
            contactCooldown = Mathf.Max(0f, contactCooldown - Time.fixedDeltaTime);
            if (player != null)
            {
                float x = player.position.x - transform.position.x;
                if (Mathf.Abs(x) > 3.8f) approachSide = x < 0f ? -1 : 1;
            }
            if (body != null)
                body.MoveRotation(body.rotation + degreesPerSecond * currentScale * Time.fixedDeltaTime);
            if (blade != null)
            {
                int count = blade.Overlap(new ContactFilter2D { useTriggers = true }, overlaps);
                for (int i = 0; i < count; i++) RejectContact(overlaps[i]);
            }
        }
        private void OnCollisionStay2D(Collision2D collision) => RejectContact(collision.collider);
        private void OnCollisionEnter2D(Collision2D collision) => RejectContact(collision.collider);

        private void RejectContact(Collider2D other)
        {
            if (contactCooldown > 0f || !other.CompareTag("Player")) return;
            Rigidbody2D actor = other.attachedRigidbody;
            HealthSystem health = other.GetComponent<HealthSystem>();
            if (actor == null || health == null || health.IsDead) return;
            Transform safe = approachSide < 0 ? leftSafeAnchor : rightSafeAnchor;
            if (safe == null) return;
            // 保护时间只免除重复扣血，不能让玩家越过实体机械。
            health.TryTakeDamage(new DamageInfo(gameObject, 1, other.bounds.center, new Vector2(approachSide * 5f, 3f)));
            other.GetComponent<PlayerDash>()?.CancelDash();
            actor.position = safe.position;
            other.transform.position = safe.position;
            actor.linearVelocity = Vector2.zero;
            contactCooldown = 0.2f;
        }
        public void ApplyWorldTime(WorldTimeMode mode, float timeScale) => currentScale = Mathf.Clamp01(timeScale);
    }
}
