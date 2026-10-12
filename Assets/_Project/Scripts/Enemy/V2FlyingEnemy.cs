using TimeMetroidvania.GameTime;
using TimeMetroidvania.Level;
using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Enemy
{
    public enum FlyingEnemyState { Hover, Chase, Hurt, Return, Dead }

    /// <summary>
    /// 蚊子型飞行敌人：平时在出生点附近悬停，玩家进入仇恨范围后主动追撞玩家。
    /// </summary>
    [RequireComponent(typeof(HealthSystem), typeof(LocalTimeController),
        typeof(TimeAwareDamageReceiver))]
    public sealed class V2FlyingEnemy : MonoBehaviour
    {
        [SerializeField] private Transform target;

        [Header("待机悬停")]
        [SerializeField, Min(0f)] private float patrolRadius = 0.55f;
        [SerializeField, Min(0f)] private float patrolSpeed = 2f;

        [Header("仇恨追踪")]
        [SerializeField, Min(0f)] private float chaseSpeed = 4.2f;
        [SerializeField, Min(0f)] private float detectionRange = 9f;
        [SerializeField, Min(0f)] private float loseRange = 12f;
        [SerializeField, Min(1)] private int contactDamage = 1;

        private HealthSystem health;
        private LocalTimeController localTime;
        private Collider2D hitbox;
        private SpriteRenderer sprite;
        private Vector3 startPosition;
        private float patrolPhase;
        private EnemyMotor2D motor;
        private Rigidbody2D body;
        private StageFiveLevelController level;
        private StageFiveRoom room;
        private Vector2 desired;
        private Vector2 lastPosition;
        private float targetSearchRemaining;
        private float hurtRemaining;
        private float flashRemaining;
        private float blockedRemaining;
        private bool warnedMissingTarget;

        public FlyingEnemyState State { get; private set; } = FlyingEnemyState.Hover;
        public bool IsChasing => State == FlyingEnemyState.Chase;
        public bool HasTarget => target != null;
        public Vector2 DesiredVelocity => motor != null ? motor.DesiredVelocity : Vector2.zero;
        public Collider2D BlockingCollider => motor != null ? motor.LastBlockingCollider : null;
        public float CurrentTimeScale => localTime != null ? localTime.CurrentTimeScale : 1f;

        private void Awake()
        {
            health = GetComponent<HealthSystem>();
            localTime = GetComponent<LocalTimeController>();
            hitbox = GetComponent<Collider2D>();
            sprite = transform.Find("ArtVisual")?.GetComponent<SpriteRenderer>()
                ?? GetComponent<SpriteRenderer>();
            body = GetComponent<Rigidbody2D>();
            startPosition = transform.position;
            lastPosition = startPosition;
            level = FindFirstObjectByType<StageFiveLevelController>();
            room = GetComponentInParent<StageFiveRoom>();
            motor = GetComponent<EnemyMotor2D>() ?? gameObject.AddComponent<EnemyMotor2D>();
            motor.Flying = true;
            BindTarget(level != null ? level.PlayerTransform : null);
        }

        private void OnEnable()
        {
            health.Damaged += HandleDamaged;
            health.Died += HandleDied;
        }

        private void OnDisable()
        {
            health.Damaged -= HandleDamaged;
            health.Died -= HandleDied;
        }

        public void BindTarget(Transform player)
        {
            if (player == null || !player.CompareTag("Player")) return;
            target = player;
            warnedMissingTarget = false;
        }

        private void FixedUpdate()
        {
            if (health.IsDead)
            {
                State = FlyingEnemyState.Dead;
                return;
            }

            float delta = localTime.LocalFixedDeltaTime;
            if (delta <= 0f)
            {
                return;
            }

            targetSearchRemaining -= delta;
            if (target == null && targetSearchRemaining <= 0f)
            {
                targetSearchRemaining = 0.25f;
                BindTarget(level != null ? level.PlayerTransform
                    : GameObject.FindGameObjectWithTag("Player")?.transform);
                if (target == null && !warnedMissingTarget)
                {
                    Debug.LogWarning($"{name} 尚未绑定玩家，继续待机悬停。", this);
                    warnedMissingTarget = true;
                }
            }
            flashRemaining = Mathf.Max(0f, flashRemaining - delta);
            if (sprite != null)
                sprite.color = flashRemaining > 0f || sprite.transform != transform
                    ? Color.white : new Color(0.2f, 0.5f, 1f);
            if (State == FlyingEnemyState.Hurt)
            {
                hurtRemaining -= delta;
                if (hurtRemaining > 0f) return;
                State = CanTarget() ? FlyingEnemyState.Chase : FlyingEnemyState.Return;
                desired = Vector2.zero;
            }

            Vector2 position = body.position;
            bool available = CanTarget();
            Vector2 targetPosition = available ? TargetPhysicsPosition() : Vector2.zero;
            float distance = available ? Vector2.Distance(position, targetPosition) : float.PositiveInfinity;
            bool visible = available && !WorldCollision2D.Blocks(position, targetPosition);
            if (State == FlyingEnemyState.Chase && (!available || distance > loseRange))
                State = FlyingEnemyState.Return;
            if (State != FlyingEnemyState.Chase && available && distance <= detectionRange && visible)
                State = FlyingEnemyState.Chase;
            if (State == FlyingEnemyState.Chase)
            {
                ChaseTarget(delta);
                if (distance > 1.2f && (position - lastPosition).sqrMagnitude < 0.000225f
                    && motor.LastBlockingCollider != null)
                    blockedRemaining += delta;
                else blockedRemaining = 0f;
                if (blockedRemaining >= 1f)
                {
                    State = FlyingEnemyState.Return;
                    blockedRemaining = 0f;
                }
            }
            if (State == FlyingEnemyState.Return)
            {
                SteerTo(startPosition, patrolSpeed, delta);
                if (Vector2.Distance(position, startPosition) <= 0.15f)
                    State = FlyingEnemyState.Hover;
            }
            if (State == FlyingEnemyState.Hover)
            {
                HoverNearSpawn(delta);
            }

            lastPosition = position;
            if (available) UpdateFacing();
        }

        private void ChaseTarget(float delta)
        {
            Vector2 aim = target.GetComponent<Collider2D>()?.bounds.center ?? TargetPhysicsPosition();
            SteerTo(aim, chaseSpeed, delta);
        }

        private Vector2 TargetPhysicsPosition()
        {
            Rigidbody2D targetBody = target.GetComponent<Rigidbody2D>();
            return targetBody != null ? targetBody.position : target.position;
        }

        private void HoverNearSpawn(float delta)
        {
            patrolPhase += delta * patrolSpeed;
            Vector3 hoverTarget = startPosition + new Vector3(
                Mathf.Sin(patrolPhase) * patrolRadius,
                Mathf.Cos(patrolPhase * 0.7f) * patrolRadius * 0.45f,
                0f);
            SteerTo(hoverTarget, patrolSpeed, delta);
        }

        private void SteerTo(Vector2 goal, float speed, float delta)
        {
            Vector2 request = Vector2.ClampMagnitude((goal - body.position) / delta, speed);
            desired = Vector2.MoveTowards(desired, request, 18f * delta);
            motor.DesiredVelocity = desired;
        }

        private bool CanTarget()
        {
            if (target == null || !target.gameObject.activeInHierarchy) return false;
            if (target.GetComponent<HealthSystem>() is { IsDead: true }) return false;
            return level == null || room == null || level.CurrentRoomId == room.RoomId;
        }

        private void HandleDamaged(DamageInfo hit)
        {
            if (health.IsDead) return;
            if (hit.Source != null && hit.Source.CompareTag("Player"))
                BindTarget(hit.Source.transform);
            State = FlyingEnemyState.Hurt;
            hurtRemaining = 0.22f;
            flashRemaining = 0.1f;
            if (sprite != null) sprite.color = Color.white;
            desired = motor.DesiredVelocity = Vector2.zero;
        }

        private void UpdateFacing()
        {
            if (sprite == null || target == null)
            {
                return;
            }

            float horizontalDifference = target.position.x - transform.position.x;
            if (Mathf.Abs(horizontalDifference) > 0.02f)
            {
                sprite.flipX = horizontalDifference < 0f;
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (health.IsDead || !other.CompareTag("Player")
                || !other.TryGetComponent(out HealthSystem playerHealth))
            {
                return;
            }

            float horizontalDifference = other.transform.position.x - transform.position.x;
            float direction = Mathf.Abs(horizontalDifference) > 0.01f
                ? Mathf.Sign(horizontalDifference)
                : 1f;
            playerHealth.TryTakeDamage(new DamageInfo(
                gameObject, contactDamage, other.transform.position,
                new Vector2(direction * 5f, 3f)));
        }

        private void HandleDied()
        {
            if (hitbox != null)
            {
                hitbox.enabled = false;
            }
            if (sprite != null)
            {
                sprite.color = new Color(0.2f, 0.2f, 0.25f, 0.45f);
            }
            State = FlyingEnemyState.Dead;
            motor.ResetMotion();
        }

        public void ResetEncounter()
        {
            GetComponent<TimeAwareDamageReceiver>()?.ClearPendingDamage();
            localTime?.ClearStoredMotion();
            transform.position = startPosition;
            body.position = startPosition;
            health.ResetForEncounter();
            motor.ResetMotion();
            patrolPhase = 0f;
            State = FlyingEnemyState.Hover;
            hurtRemaining = flashRemaining = blockedRemaining = 0f;
            lastPosition = startPosition;
            desired = Vector2.zero;
            if (hitbox != null)
            {
                hitbox.enabled = true;
            }
            if (sprite != null)
            {
                sprite.color = sprite.transform != transform
                    ? Color.white : new Color(0.2f, 0.5f, 1f);
            }
        }

        internal static bool ShouldChase(
            bool wasChasing, float distance, float detectionRange, float loseRange)
        {
            float enterDistance = Mathf.Max(0f, detectionRange);
            float exitDistance = Mathf.Max(enterDistance, loseRange);
            return wasChasing
                ? distance <= exitDistance
                : distance <= enterDistance;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.25f, 0.2f, 0.75f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = new Color(1f, 0.75f, 0.2f, 0.45f);
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(detectionRange, loseRange));
        }
#endif
    }
}
