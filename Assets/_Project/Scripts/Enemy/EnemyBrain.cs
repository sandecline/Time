using TimeMetroidvania.GameTime;
using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Enemy
{
    /// <summary>
    /// 灰盒敌人状态机：守卫近战、机械虫高速突进、钟弩手有限弹药射击。
    /// </summary>
    [RequireComponent(typeof(HealthSystem), typeof(LocalTimeController), typeof(TimeAwareDamageReceiver))]
    public sealed class EnemyBrain : MonoBehaviour
    {
        [SerializeField] private EnemyKind kind;
        [SerializeField] private Transform target;
        [SerializeField] private ProjectilePool projectilePool;
        [SerializeField, Min(0f)] private float patrolHalfDistance = 1.4f;
        [SerializeField, Min(0.1f)] private float detectionRange = 5f;
        [SerializeField, Min(0.1f)] private float loseRange = 7f;
        [SerializeField, Min(0.1f)] private float attackRange = 1.25f;
        [SerializeField] private Vector2 meleeHitRange = new(1.5f, 1.4f);
        [SerializeField, Min(0f)] private float patrolSpeed = 1.1f;
        [SerializeField, Min(0f)] private float chaseSpeed = 2f;
        [SerializeField, Min(0f)] private float attackWindup = 0.65f;
        [SerializeField, Min(0f)] private float attackCooldown = 1.2f;
        [SerializeField, Min(0f)] private float hurtDuration = 0.32f;
        [SerializeField, Min(0)] private int maxShots = 8;

        private HealthSystem health;
        private LocalTimeController localTime;
        private SpriteRenderer sprite;
        private Vector3 patrolCenter;
        private float stateElapsed;
        private float cooldownRemaining;
        private int facing = 1;
        private int shotsFired;
        private bool attackResolved;
        private bool initialized;

        public EnemyKind Kind => kind;
        public EnemyState CurrentState { get; private set; } = EnemyState.Patrol;
        public int ShotsRemaining => Mathf.Max(0, maxShots - shotsFired);
        public int FacingDirection => facing;
        public float AttackRange => attackRange;
        public Vector2 MeleeHitRange => meleeHitRange;
        public Vector2 TargetPosition => target != null
            ? target.position
            : transform.position;
        public bool IsAttackTelegraphing =>
            CurrentState == EnemyState.Attack && !attackResolved;
        public float AttackTelegraphProgress => IsAttackTelegraphing
            ? Mathf.Clamp01(stateElapsed / Mathf.Max(0.01f, attackWindup))
            : 0f;
        public float AttackRecoveryProgress =>
            CurrentState == EnemyState.Attack && attackResolved
                ? Mathf.Clamp01((stateElapsed - attackWindup) / 0.16f)
                : 0f;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }
            health = GetComponent<HealthSystem>();
            localTime = GetComponent<LocalTimeController>();
            sprite = transform.Find("ArtVisual")?.GetComponent<SpriteRenderer>()
                ?? GetComponent<SpriteRenderer>();
            patrolCenter = transform.position;
            target ??= GameObject.FindGameObjectWithTag("Player")?.transform;
            initialized = true;
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

        private void Update()
        {
            Advance(localTime.LocalDeltaTime);
        }

        internal void Advance(float delta)
        {
            if (CurrentState == EnemyState.Dead || target == null)
            {
                return;
            }
            if (delta <= 0f)
            {
                return;
            }

            stateElapsed += delta;
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - delta);
            float distance = Mathf.Abs(target.position.x - transform.position.x);

            switch (CurrentState)
            {
                case EnemyState.Patrol:
                    Patrol(delta);
                    if (distance <= detectionRange)
                    {
                        SetState(EnemyState.Alert);
                    }
                    break;
                case EnemyState.Alert:
                    if (distance > loseRange)
                    {
                        SetState(EnemyState.Patrol);
                        break;
                    }
                    FaceTarget();
                    if (CanAttack(distance))
                    {
                        SetState(EnemyState.Attack);
                    }
                    else if (kind != EnemyKind.ClockArcher)
                    {
                        MoveHorizontal(facing * chaseSpeed * delta);
                    }
                    break;
                case EnemyState.Attack:
                    if (!attackResolved && stateElapsed >= attackWindup)
                    {
                        attackResolved = true;
                        PerformAttack();
                    }
                    if (stateElapsed >= attackWindup + 0.16f)
                    {
                        cooldownRemaining = attackCooldown;
                        SetState(EnemyState.Alert);
                    }
                    break;
                case EnemyState.Hurt:
                    if (stateElapsed >= hurtDuration)
                    {
                        SetState(distance <= loseRange ? EnemyState.Alert : EnemyState.Patrol);
                    }
                    break;
            }
        }

        private bool CanAttack(float distance)
        {
            if (cooldownRemaining > 0f)
            {
                return false;
            }

            return kind == EnemyKind.ClockArcher
                ? shotsFired < maxShots && projectilePool != null && distance <= attackRange
                : distance <= attackRange;
        }

        private void Patrol(float delta)
        {
            if (patrolHalfDistance <= 0f)
            {
                return;
            }
            if (transform.position.x >= patrolCenter.x + patrolHalfDistance)
            {
                facing = -1;
            }
            else if (transform.position.x <= patrolCenter.x - patrolHalfDistance)
            {
                facing = 1;
            }
            MoveHorizontal(facing * patrolSpeed * delta);
        }

        private void FaceTarget()
        {
            facing = target.position.x >= transform.position.x ? 1 : -1;
        }

        private void MoveHorizontal(float distance)
        {
            Vector3 position = transform.position;
            position.x += distance;
            transform.position = position;
        }

        private void PerformAttack()
        {
            if (kind == EnemyKind.ClockArcher)
            {
                Vector2 origin = (Vector2)transform.position + new Vector2(facing * 0.7f, 0.15f);
                Vector2 direction = ((Vector2)target.position - origin).normalized;
                if (projectilePool.TryFire(origin, direction))
                {
                    shotsFired++;
                }
                return;
            }

            Vector2 targetDelta = target.position - transform.position;
            bool targetInFront = Mathf.Sign(targetDelta.x) == facing
                || Mathf.Abs(targetDelta.x) < 0.05f;
            if (!targetInFront
                || Mathf.Abs(targetDelta.x) > meleeHitRange.x
                || Mathf.Abs(targetDelta.y) > meleeHitRange.y)
            {
                return;
            }

            HealthSystem playerHealth = target.GetComponent<HealthSystem>();
            if (playerHealth == null)
            {
                return;
            }
            playerHealth.TryTakeDamage(new DamageInfo(
                gameObject, 1, target.position, new Vector2(facing * 6f, 3f)));
        }

        private void HandleDamaged(DamageInfo damageInfo)
        {
            if (!health.IsDead)
            {
                SetState(EnemyState.Hurt);
            }
        }

        private void HandleDied()
        {
            SetState(EnemyState.Dead);
            Collider2D hitbox = GetComponent<Collider2D>();
            if (hitbox != null)
            {
                hitbox.enabled = false;
            }
        }

        public void ResetEncounter()
        {
            EnsureInitialized();
            GetComponent<TimeAwareDamageReceiver>()?.ClearPendingDamage();
            localTime?.ClearStoredMotion();
            transform.position = patrolCenter;
            health.ResetForEncounter();
            Collider2D hitbox = GetComponent<Collider2D>();
            if (hitbox != null)
            {
                hitbox.enabled = true;
            }
            stateElapsed = 0f;
            cooldownRemaining = 0f;
            facing = 1;
            shotsFired = 0;
            CurrentState = EnemyState.Dead;
            SetState(EnemyState.Patrol);
        }

        private void SetState(EnemyState next)
        {
            if (CurrentState == next)
            {
                return;
            }
            CurrentState = next;
            stateElapsed = 0f;
            attackResolved = false;
            if (sprite != null)
            {
                sprite.color = next switch
                {
                    EnemyState.Hurt => Color.white,
                    EnemyState.Dead => new Color(0.25f, 0.25f, 0.25f),
                    EnemyState.Attack => new Color(1f, 0.85f, 0.2f),
                    _ => kind switch
                    {
                        EnemyKind.Guard => new Color(0.85f, 0.32f, 0.25f),
                        EnemyKind.ClockworkBug => new Color(0.7f, 0.28f, 0.85f),
                        _ => new Color(0.25f, 0.7f, 0.85f)
                    }
                };
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.75f, 0.1f, 0.75f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
            if (kind == EnemyKind.ClockArcher)
            {
                return;
            }

            int previewFacing = facing == 0 ? 1 : facing;
            Vector2 center = (Vector2)transform.position
                + Vector2.right * previewFacing * meleeHitRange.x * 0.5f;
            Gizmos.color = kind == EnemyKind.ClockworkBug
                ? new Color(1f, 0.82f, 0.08f, 0.9f)
                : new Color(1f, 0.12f, 0.06f, 0.9f);
            Gizmos.DrawWireCube(
                center,
                new Vector2(meleeHitRange.x, meleeHitRange.y * 2f));
        }
    }
}
