using System;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Level;
using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Boss
{
    /// <summary>
    /// 两阶段 Boss 状态机，组合近战、落地、弹幕和抗时停的时间锚攻击。
    /// </summary>
    [RequireComponent(typeof(HealthSystem), typeof(LocalTimeController),
        typeof(TimeAwareDamageReceiver))]
    public sealed class BossBrain : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private BossProjectilePool projectilePool;
        [SerializeField] private StageFiveLevelController level;
        [SerializeField] private Vector2 arenaCenter;
        [SerializeField, Min(1f)] private float activationRange = 7f;
        [SerializeField, Min(0f)] private float firstPhaseCooldown = 1.15f;
        [SerializeField, Min(0f)] private float secondPhaseCooldown = 0.78f;
        [SerializeField, Min(0.1f)] private float firstPhaseTelegraph = 0.58f;
        [SerializeField, Min(0.1f)] private float secondPhaseTelegraph = 0.4f;
        [Header("近战判定")]
        [SerializeField] private Vector2 sweepHitRange = new(3.5f, 1.25f);
        [SerializeField] private Vector2 slamHitRange = new(2.35f, 1.6f);
        [SerializeField] private bool showLegacyHud = true;

        private static readonly BossAttackType[] FirstPhaseSequence =
        {
            BossAttackType.Sweep,
            BossAttackType.LeapSlam,
            BossAttackType.ProjectileVolley
        };

        private static readonly BossAttackType[] SecondPhaseSequence =
        {
            BossAttackType.Sweep,
            BossAttackType.ProjectileVolley,
            BossAttackType.LeapSlam,
            BossAttackType.TimeAnchor
        };

        private HealthSystem health;
        private LocalTimeController localTime;
        private TimeAwareDamageReceiver damageReceiver;
        private SpriteRenderer sprite;
        private Collider2D hitbox;
        private Vector3 startPosition;
        private Vector3 leapStart;
        private Vector3 leapTarget;
        private float cooldownRemaining;
        private float attackElapsed;
        private float phaseTransitionRemaining;
        private int sequenceIndex;
        private int facing = -1;
        private bool attackResolved;
        private bool initialized;
        private GUIStyle bossLabelStyle;

        public event Action Defeated;

        public BossPhase Phase { get; private set; } = BossPhase.One;
        public BossAttackType CurrentAttack { get; private set; }
        public bool IsEngaged { get; private set; }
        public bool IsDefeatedConfirmed { get; private set; }
        public int CurrentHealth => health?.CurrentHealth ?? 0;
        public int MaxHealth => health?.MaxHealth ?? 0;
        public int FacingDirection => facing;
        public Vector2 SweepHitRange => sweepHitRange;
        public Vector2 SlamHitRange => slamHitRange;
        public Vector2 TargetPosition => target != null
            ? target.position
            : transform.position;
        public Vector2 TelegraphTargetPosition => CurrentAttack == BossAttackType.LeapSlam
            ? (Vector2)leapTarget
            : TargetPosition;
        public bool IsPhaseTransitioning => phaseTransitionRemaining > 0f;
        public bool HasResolvedCurrentAttack => attackResolved;
        public bool IsAttackTelegraphing => CurrentAttack != BossAttackType.None
            && attackElapsed < (Phase == BossPhase.One
                ? firstPhaseTelegraph
                : secondPhaseTelegraph);

        public void Configure(
            Transform player, BossProjectilePool pool,
            StageFiveLevelController controller, Vector2 center)
        {
            target = player;
            projectilePool = pool;
            level = controller;
            arenaCenter = center;
        }

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
            damageReceiver = GetComponent<TimeAwareDamageReceiver>();
            sprite = transform.Find("ArtVisual")?.GetComponent<SpriteRenderer>()
                ?? GetComponent<SpriteRenderer>();
            hitbox = GetComponent<Collider2D>();
            startPosition = transform.position;
            target ??= GameObject.FindGameObjectWithTag("Player")?.transform;
            initialized = true;
        }

        private void OnEnable()
        {
            EnsureInitialized();
            health.HealthChanged -= HandleHealthChanged;
            health.Died -= HandleDied;
            health.HealthChanged += HandleHealthChanged;
            health.Died += HandleDied;
        }

        private void OnDisable()
        {
            if (health == null)
            {
                return;
            }

            health.HealthChanged -= HandleHealthChanged;
            health.Died -= HandleDied;
        }

        private void Update()
        {
            Advance(localTime.LocalDeltaTime);
        }

        public void Advance(float delta)
        {
            EnsureInitialized();
            if (delta <= 0f || target == null || health.IsDead
                || Phase == BossPhase.Defeated)
            {
                return;
            }

            if (!IsEngaged)
            {
                if (Vector2.Distance(target.position, transform.position) > activationRange)
                {
                    return;
                }

                IsEngaged = true;
                cooldownRemaining = 0.7f;
                level?.BeginBossBattle();
            }

            FaceTarget();
            if (phaseTransitionRemaining > 0f)
            {
                phaseTransitionRemaining = Mathf.Max(0f, phaseTransitionRemaining - delta);
                if (phaseTransitionRemaining <= 0f)
                {
                    SetBaseColor();
                }
                return;
            }

            if (CurrentAttack == BossAttackType.None)
            {
                cooldownRemaining -= delta;
                if (cooldownRemaining <= 0f)
                {
                    BeginNextAttack();
                }
                return;
            }

            attackElapsed += delta;
            AdvanceCurrentAttack();
        }

        private void BeginNextAttack()
        {
            BossAttackType[] sequence = Phase == BossPhase.One
                ? FirstPhaseSequence
                : SecondPhaseSequence;
            CurrentAttack = sequence[sequenceIndex % sequence.Length];
            sequenceIndex++;
            attackElapsed = 0f;
            attackResolved = false;
            leapStart = transform.position;
            float targetX = Mathf.Clamp(
                target.position.x, arenaCenter.x - 6f, arenaCenter.x + 6f);
            leapTarget = new Vector3(targetX, startPosition.y, transform.position.z);

            if (sprite != null)
            {
                sprite.color = CurrentAttack switch
                {
                    BossAttackType.Sweep => new Color(1f, 0.38f, 0.2f),
                    BossAttackType.LeapSlam => new Color(1f, 0.85f, 0.18f),
                    BossAttackType.ProjectileVolley => new Color(0.2f, 0.85f, 1f),
                    BossAttackType.TimeAnchor => new Color(0.78f, 0.28f, 1f),
                    _ => Color.white
                };
            }
        }

        private void AdvanceCurrentAttack()
        {
            float telegraph = Phase == BossPhase.One
                ? firstPhaseTelegraph
                : secondPhaseTelegraph;

            switch (CurrentAttack)
            {
                case BossAttackType.Sweep:
                    if (!attackResolved && attackElapsed >= telegraph)
                    {
                        attackResolved = true;
                        ApplySweepDamage();
                    }
                    if (attackElapsed >= telegraph + 0.28f)
                    {
                        EndAttack();
                    }
                    break;
                case BossAttackType.LeapSlam:
                    AdvanceLeapSlam(telegraph);
                    break;
                case BossAttackType.ProjectileVolley:
                    if (!attackResolved && attackElapsed >= telegraph)
                    {
                        attackResolved = true;
                        FireVolley(Phase == BossPhase.Two, false);
                    }
                    if (attackElapsed >= telegraph + 0.35f)
                    {
                        EndAttack();
                    }
                    break;
                case BossAttackType.TimeAnchor:
                    if (!attackResolved && attackElapsed >= telegraph)
                    {
                        attackResolved = true;
                        FireVolley(false, true);
                    }
                    if (attackElapsed >= telegraph + 0.42f)
                    {
                        EndAttack();
                    }
                    break;
            }
        }

        private void AdvanceLeapSlam(float telegraph)
        {
            if (attackElapsed < telegraph)
            {
                return;
            }

            float leapDuration = Phase == BossPhase.One ? 0.62f : 0.48f;
            float progress = Mathf.Clamp01((attackElapsed - telegraph) / leapDuration);
            Vector3 position = Vector3.Lerp(leapStart, leapTarget, progress);
            position.y += Mathf.Sin(progress * Mathf.PI) * 3.8f;
            transform.position = position;

            if (!attackResolved && progress >= 1f)
            {
                attackResolved = true;
                ApplySlamDamage();
            }

            if (attackElapsed >= telegraph + leapDuration + 0.3f)
            {
                EndAttack();
            }
        }

        private void ApplySweepDamage()
        {
            Vector2 delta = target.position - transform.position;
            bool inFront = Mathf.Sign(delta.x) == facing || Mathf.Abs(delta.x) < 0.15f;
            if (inFront
                && Mathf.Abs(delta.x) <= sweepHitRange.x
                && Mathf.Abs(delta.y) <= sweepHitRange.y)
            {
                DamagePlayer(1, new Vector2(facing * 8f, 3f));
            }
        }

        private void ApplySlamDamage()
        {
            Vector2 delta = target.position - transform.position;
            if (Mathf.Abs(delta.x) <= slamHitRange.x
                && Mathf.Abs(delta.y) <= slamHitRange.y)
            {
                float direction = Mathf.Sign(delta.x);
                DamagePlayer(2, new Vector2(direction * 9f, 4.5f));
            }
        }

        private void DamagePlayer(int amount, Vector2 knockback)
        {
            target.GetComponent<HealthSystem>()?.TryTakeDamage(new DamageInfo(
                gameObject, amount, target.position, knockback));
        }

        private void FireVolley(bool mixedBreakability, bool timeAnchored)
        {
            if (projectilePool == null)
            {
                return;
            }

            int count = timeAnchored ? 3 : Phase == BossPhase.One ? 3 : 5;
            float spread = timeAnchored ? 16f : 12f;
            float speed = timeAnchored ? 4.3f : Phase == BossPhase.One ? 5.5f : 6.5f;
            Vector2 origin = (Vector2)transform.position + Vector2.up * 0.35f;
            Vector2 baseDirection = ((Vector2)target.position - origin).normalized;
            for (int index = 0; index < count; index++)
            {
                float offset = (index - (count - 1) * 0.5f) * spread;
                // 二阶段普通弹幕交替为蓝色可斩与橙色不可斩，不能用一次平砍全清。
                bool breakable = !timeAnchored
                    && mixedBreakability
                    && index % 2 == 0;
                projectilePool.TryFire(
                    origin, Rotate(baseDirection, offset), speed, breakable, timeAnchored);
            }
        }

        private static Vector2 Rotate(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos);
        }

        private void EndAttack()
        {
            CurrentAttack = BossAttackType.None;
            attackElapsed = 0f;
            attackResolved = false;
            cooldownRemaining = Phase == BossPhase.One
                ? firstPhaseCooldown
                : secondPhaseCooldown;
            SetBaseColor();
        }

        private void FaceTarget()
        {
            facing = target.position.x >= transform.position.x ? 1 : -1;
        }

        private void HandleHealthChanged(int current, int maximum)
        {
            if (current <= 0 || Phase != BossPhase.One || current * 2 > maximum)
            {
                return;
            }

            Phase = BossPhase.Two;
            CurrentAttack = BossAttackType.None;
            sequenceIndex = 0;
            phaseTransitionRemaining = 0.9f;
            projectilePool?.ClearActive();
            if (sprite != null)
            {
                sprite.color = new Color(0.78f, 0.28f, 1f);
            }
            level?.ShowMessage(
                "Boss 进入第二阶段：蓝弹可斩，橙弹不可斩；紫色时间锚不受时停影响！");
        }

        private void HandleDied()
        {
            Phase = BossPhase.Defeated;
            CurrentAttack = BossAttackType.None;
            IsDefeatedConfirmed = true;
            projectilePool?.ClearActive();
            if (hitbox != null)
            {
                hitbox.enabled = false;
            }
            if (sprite != null)
            {
                sprite.color = new Color(0.2f, 0.2f, 0.24f);
            }
            level?.ConfirmBossDefeated();
            Defeated?.Invoke();
        }

        public void ResetEncounter()
        {
            EnsureInitialized();
            if (IsDefeatedConfirmed)
            {
                return;
            }

            Phase = BossPhase.One;
            CurrentAttack = BossAttackType.None;
            IsEngaged = false;
            sequenceIndex = 0;
            cooldownRemaining = 0f;
            attackElapsed = 0f;
            phaseTransitionRemaining = 0f;
            attackResolved = false;
            transform.position = startPosition;
            level?.EndBossBattle();
            damageReceiver.ClearPendingDamage();
            localTime.ClearStoredMotion();
            projectilePool?.ClearActive();
            health.ResetForEncounter();
            if (hitbox != null)
            {
                hitbox.enabled = true;
            }
            SetBaseColor();
        }

        private void SetBaseColor()
        {
            if (sprite != null)
            {
                sprite.color = Phase == BossPhase.Two
                    ? new Color(0.62f, 0.22f, 0.78f)
                    : new Color(0.82f, 0.24f, 0.28f);
            }
        }

        private void OnGUI()
        {
            if (!showLegacyHud || !IsEngaged || health == null || health.IsDead)
            {
                return;
            }

            bossLabelStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                normal = { textColor = Color.white }
            };
            float width = Mathf.Min(620f, Screen.width * 0.55f);
            float left = (Screen.width - width) * 0.5f;
            GUI.Box(new Rect(left, 20f, width, 54f), GUIContent.none);
            GUI.Label(new Rect(left, 22f, width, 24f),
                $"时蚀守卫 · 阶段 {(int)Phase}", bossLabelStyle);
            float normalized = health.MaxHealth <= 0
                ? 0f
                : (float)health.CurrentHealth / health.MaxHealth;
            GUI.color = Phase == BossPhase.Two
                ? new Color(0.75f, 0.28f, 0.95f)
                : new Color(0.95f, 0.25f, 0.25f);
            GUI.Box(new Rect(left + 16f, 50f, (width - 32f) * normalized, 14f), GUIContent.none);
            GUI.color = Color.white;
        }

        private void OnDrawGizmosSelected()
        {
            int previewFacing = facing == 0 ? -1 : facing;
            Vector2 sweepCenter = (Vector2)transform.position
                + Vector2.right * previewFacing * sweepHitRange.x * 0.5f;
            Gizmos.color = new Color(1f, 0.12f, 0.06f, 0.9f);
            Gizmos.DrawWireCube(
                sweepCenter,
                new Vector2(sweepHitRange.x, sweepHitRange.y * 2f));

            Gizmos.color = new Color(1f, 0.82f, 0.08f, 0.9f);
            Gizmos.DrawWireCube(
                transform.position,
                new Vector2(slamHitRange.x * 2f, slamHitRange.y * 2f));
        }
    }
}
