using System;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Level;
using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Boss
{
    public enum BossAttackStage { None, Telegraph, Travel, Active, Recovery }

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
        [SerializeField, Min(0f)] private float firstPhaseCooldown = 0.85f;
        [SerializeField, Min(0f)] private float secondPhaseCooldown = 0.60f;
        [Header("钟面扇形")]
        [SerializeField, Min(1)] private int firstPhaseWaveCount = 5;
        [SerializeField, Min(1)] private int secondPhaseWaveCount = 7;
        [SerializeField, Range(1, 36)] private int firstPhaseShotsPerWave = 28;
        [SerializeField, Range(1, 36)] private int secondPhaseShotsPerWave = 36;
        [SerializeField, Min(0.01f)] private float firstPhaseWaveInterval = 0.4f;
        [SerializeField, Min(0.01f)] private float secondPhaseWaveInterval = 0.32f;
        [SerializeField, Min(0.1f)] private float firstPhaseProjectileSpeed = 3.2f;
        [SerializeField, Min(0.1f)] private float secondPhaseProjectileSpeed = 3.6f;
        [Header("落雨弹幕")]
        [SerializeField, Min(1)] private int firstPhaseRainRows = 6;
        [SerializeField, Min(1)] private int secondPhaseRainRows = 8;
        [SerializeField, Min(1)] private int firstPhaseRainShots = 30;
        [SerializeField, Min(1)] private int secondPhaseRainShots = 38;
        [SerializeField, Min(0.01f)] private float firstPhaseRainInterval = 0.55f;
        [SerializeField, Min(0.01f)] private float secondPhaseRainInterval = 0.45f;
        [SerializeField, Min(0.1f)] private float firstPhaseRainSpeed = 3.4f;
        [SerializeField, Min(0.1f)] private float secondPhaseRainSpeed = 4.2f;
        [Header("交叉弹幕")]
        [SerializeField, Min(1)] private int firstPhaseCrossGroups = 8;
        [SerializeField, Min(1)] private int secondPhaseCrossGroups = 12;
        [SerializeField, Min(1)] private int firstPhaseCrossShots = 14;
        [SerializeField, Min(1)] private int secondPhaseCrossShots = 18;
        [SerializeField, Min(0.01f)] private float firstPhaseCrossInterval = 0.38f;
        [SerializeField, Min(0.01f)] private float secondPhaseCrossInterval = 0.30f;
        [SerializeField, Min(0.1f)] private float firstPhaseCrossSpeed = 3.6f;
        [SerializeField, Min(0.1f)] private float secondPhaseCrossSpeed = 4.2f;
        [Header("近战判定")]
        [SerializeField] private Vector2 sweepHitRange = new(3.5f, 1.25f);
        [SerializeField] private Vector2 slamHitRange = new(2.35f, 1.6f);
        [SerializeField] private bool showLegacyHud = true;
        [SerializeField] private Sprite bossHudFrameSprite;
        [SerializeField] private Sprite bossHudFillSprite;

        private static readonly BossAttackType[] FirstPhaseSequence =
        {
            BossAttackType.ClockFan,
            BossAttackType.Sweep,
            BossAttackType.Rain,
            BossAttackType.LeapSlam,
            BossAttackType.Crossfire
        };

        private static readonly BossAttackType[] SecondPhaseSequence =
        {
            BossAttackType.ClockFan,
            BossAttackType.Rain,
            BossAttackType.Sweep,
            BossAttackType.Crossfire,
            BossAttackType.LeapSlam,
            BossAttackType.Rain
        };
        private static readonly int[] RainGapDrift = { 0, 1, 2, 1, 0, -1, -2, -1 };
        private static readonly float[] AnchorDelays = { 0.9f, 1.8f, 2.7f };

        private HealthSystem health;
        private HealthSystem targetHealth;
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
        private BossAttackHitbox2D sweepHitbox;
        private BossAttackHitbox2D slamHitbox;
        private Vector2 lockedAim;
        private bool aimLocked;
        private bool mirrorNextVolley;
        private bool mirrorCurrentVolley;
        private int volleyWavesFired;
        private float nextVolleyAt;
        private float lastVolleyAt;
        private int gapStart;
        private int patternSerial;
        private bool patternCleared;
        private bool patternFading;
        private bool anchorOneFired;
        private bool anchorTwoFired;
        private bool anchorThreeFired;
        private CameraFollow2D fightCamera;
        private RoomCameraBounds2D arenaRoom;
        private readonly Vector2[] crossfireAims = new Vector2[64];
        private readonly bool[] crossfireAimLocked = new bool[64];
        private readonly Vector2[] anchorAims = new Vector2[3];
        private readonly bool[] anchorAimLocked = new bool[3];
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
        public Vector2 TelegraphAimPosition => aimLocked ? lockedAim : TargetPosition;
        public bool IsPhaseTransitioning => phaseTransitionRemaining > 0f;
        public bool HasResolvedCurrentAttack => attackResolved;
        public int VolleyWavesFired => volleyWavesFired;
        public int VolleyShotCount => Phase == BossPhase.One
            ? firstPhaseShotsPerWave : secondPhaseShotsPerWave;
        public int VolleyWaveCount => Phase == BossPhase.One
            ? firstPhaseWaveCount : secondPhaseWaveCount;
        public int PatternWaveCount => CurrentAttack switch
        {
            BossAttackType.Rain => Phase == BossPhase.One ? firstPhaseRainRows : secondPhaseRainRows,
            BossAttackType.Crossfire => Phase == BossPhase.One ? firstPhaseCrossGroups : secondPhaseCrossGroups,
            _ => VolleyWaveCount
        };
        public int PatternShotCount => CurrentAttack switch
        {
            BossAttackType.Rain => Phase == BossPhase.One ? firstPhaseRainShots : secondPhaseRainShots,
            BossAttackType.Crossfire => Phase == BossPhase.One ? firstPhaseCrossShots : secondPhaseCrossShots,
            _ => VolleyShotCount
        };
        public bool IsPatternAttack => CurrentAttack == BossAttackType.ClockFan
            || CurrentAttack == BossAttackType.Rain || CurrentAttack == BossAttackType.Crossfire;
        public float VolleyWaveInterval => Phase == BossPhase.One
            ? firstPhaseWaveInterval : secondPhaseWaveInterval;
        public float PatternInterval => CurrentAttack switch
        {
            BossAttackType.Rain => Phase == BossPhase.One ? firstPhaseRainInterval : secondPhaseRainInterval,
            BossAttackType.Crossfire => Phase == BossPhase.One ? firstPhaseCrossInterval : secondPhaseCrossInterval,
            _ => VolleyWaveInterval
        };
        public int PatternSerial => patternSerial;
        public int PatternGapStart => gapStart;
        public bool IsAttackActive => sweepHitbox != null && sweepHitbox.IsActive
            || slamHitbox != null && slamHitbox.IsActive;
        public float AttackProgress => CurrentAttack == BossAttackType.None
            ? 0f : Mathf.Clamp01(attackElapsed / Mathf.Max(0.01f, AttackTotalDuration()));
        public BossAttackStage AttackStage
        {
            get
            {
                if (CurrentAttack == BossAttackType.None) return BossAttackStage.None;
                if (IsAttackActive) return BossAttackStage.Active;
                if (attackElapsed < TelegraphDuration()) return BossAttackStage.Telegraph;
                if (CurrentAttack == BossAttackType.ProjectileVolley
                    && volleyWavesFired < VolleyWaveCount) return BossAttackStage.Active;
                if (IsPatternAttack && !patternCleared) return BossAttackStage.Active;
                if (CurrentAttack == BossAttackType.LeapSlam && !attackResolved)
                    return BossAttackStage.Travel;
                return BossAttackStage.Recovery;
            }
        }
        public bool IsAttackTelegraphing => CurrentAttack != BossAttackType.None
            && (attackElapsed < TelegraphDuration()
                || CurrentAttack == BossAttackType.LeapSlam && !attackResolved);

        public void Configure(
            Transform player, BossProjectilePool pool,
            StageFiveLevelController controller, Vector2 center)
        {
            target = player;
            targetHealth = target != null ? target.GetComponent<HealthSystem>() : null;
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
            sweepHitbox = EnsureAttackHitbox("SweepHitbox");
            slamHitbox = EnsureAttackHitbox("SlamHitbox");
            fightCamera = FindFirstObjectByType<CameraFollow2D>();
            arenaRoom = GetComponentInParent<RoomCameraBounds2D>();
            startPosition = transform.position;
            target ??= GameObject.FindGameObjectWithTag("Player")?.transform;
            targetHealth = target != null ? target.GetComponent<HealthSystem>() : null;
            initialized = true;
        }

        private BossAttackHitbox2D EnsureAttackHitbox(string objectName)
        {
            Transform child = transform.Find(objectName);
            if (child == null)
            {
                GameObject go = new(objectName);
                go.transform.SetParent(transform, false);
                child = go.transform;
            }
            return child.GetComponent<BossAttackHitbox2D>()
                ?? child.gameObject.AddComponent<BossAttackHitbox2D>();
        }

        private float TelegraphDuration() => CurrentAttack switch
        {
            BossAttackType.Sweep => Phase == BossPhase.One ? 0.70f : 0.55f,
            BossAttackType.LeapSlam => Phase == BossPhase.One ? 0.80f : 0.65f,
            BossAttackType.ProjectileVolley => Phase == BossPhase.One ? 0.65f : 0.50f,
            BossAttackType.ClockFan => Phase == BossPhase.One ? 1f : 0.85f,
            BossAttackType.Rain => Phase == BossPhase.One ? 0.9f : 0.75f,
            BossAttackType.Crossfire => Phase == BossPhase.One ? 0.9f : 0.75f,
            BossAttackType.TimeAnchor => 0.70f,
            _ => 0f
        };

        private float AttackTotalDuration()
        {
            float telegraph = TelegraphDuration();
            return CurrentAttack switch
            {
                BossAttackType.Sweep => telegraph + 0.16f + 0.30f,
                BossAttackType.LeapSlam => telegraph
                    + (Phase == BossPhase.One ? 0.62f : 0.48f) + 0.12f + 0.40f,
                BossAttackType.ProjectileVolley => telegraph
                    + (VolleyWaveCount - 1) * VolleyWaveInterval + 0.35f,
                BossAttackType.ClockFan or BossAttackType.Rain or BossAttackType.Crossfire =>
                    telegraph + (PatternWaveCount - 1) * PatternInterval + 3.45f,
                BossAttackType.TimeAnchor => telegraph + 0.42f,
                _ => 0f
            };
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
            sweepHitbox?.End();
            slamHitbox?.End();
            ClearVolleyState();
            fightCamera?.SetPatternZoom(false);
            CurrentAttack = BossAttackType.None;
            attackElapsed = 0f;
            attackResolved = false;
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
            if (target == null || health.IsDead || Phase == BossPhase.Defeated)
            {
                return;
            }
            if (targetHealth != null && targetHealth.IsDead
                || level != null && !level.IsBossRoom(level.CurrentRoomId))
            {
                if (IsEngaged || CurrentAttack != BossAttackType.None
                    || level != null && level.BossBattleActive)
                {
                    ResetEncounter();
                }
                return;
            }
            if (delta <= 0f)
            {
                // 时停保持现有有效段危险；玩家仍可走进冻结的刀光。
                sweepHitbox?.Tick();
                slamHitbox?.Tick();
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

            if (CurrentAttack == BossAttackType.None) FaceTarget();
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
            LockPatternAims();
            AdvanceCurrentAttack();
            sweepHitbox?.Tick();
            slamHitbox?.Tick();
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
            sweepHitbox?.End();
            slamHitbox?.End();
            ClearVolleyState();
            aimLocked = false;
            lockedAim = target.position;
            leapStart = transform.position;
            float targetX = Mathf.Clamp(
                target.position.x, arenaCenter.x - 6f, arenaCenter.x + 6f);
            leapTarget = new Vector3(targetX, startPosition.y, transform.position.z);
            if (CurrentAttack == BossAttackType.ProjectileVolley)
            {
                mirrorCurrentVolley = mirrorNextVolley;
                mirrorNextVolley = !mirrorNextVolley;
            }
            if (IsPatternAttack)
            {
                mirrorCurrentVolley = mirrorNextVolley;
                mirrorNextVolley = !mirrorNextVolley;
                gapStart = 0;
                patternSerial++;
                fightCamera?.SetPatternZoom(true);
            }

            if (sprite != null)
            {
                sprite.color = CurrentAttack switch
                {
                    BossAttackType.Sweep => new Color(1f, 0.38f, 0.2f),
                    BossAttackType.LeapSlam => new Color(1f, 0.85f, 0.18f),
                    BossAttackType.ProjectileVolley => new Color(0.2f, 0.85f, 1f),
                    BossAttackType.ClockFan or BossAttackType.Rain or BossAttackType.Crossfire
                        => new Color(0.2f, 0.85f, 1f),
                    BossAttackType.TimeAnchor => new Color(0.78f, 0.28f, 1f),
                    _ => Color.white
                };
            }
        }

        private void AdvanceCurrentAttack()
        {
            float telegraph = TelegraphDuration();
            if (!aimLocked && attackElapsed >= telegraph - 0.15f)
            {
                lockedAim = target.position;
                aimLocked = true;
                if (IsPatternAttack) gapStart = CalculateGapStart(lockedAim);
            }

            switch (CurrentAttack)
            {
                case BossAttackType.Sweep:
                    if (!attackResolved && attackElapsed >= telegraph)
                    {
                        attackResolved = true;
                        ApplySweepDamage();
                    }
                    if (attackElapsed >= telegraph + 0.16f) sweepHitbox?.End();
                    if (attackElapsed >= telegraph + 0.46f)
                    {
                        EndAttack();
                    }
                    break;
                case BossAttackType.LeapSlam:
                    AdvanceLeapSlam(telegraph);
                    break;
                case BossAttackType.ProjectileVolley:
                    AdvanceProjectileVolley(telegraph);
                    break;
                case BossAttackType.ClockFan:
                case BossAttackType.Rain:
                case BossAttackType.Crossfire:
                    AdvancePattern(telegraph);
                    break;
                case BossAttackType.TimeAnchor:
                    if (!attackResolved && attackElapsed >= telegraph)
                    {
                        attackResolved = true;
                        FireTimeAnchorVolley();
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
            if (attackElapsed >= telegraph + leapDuration + 0.12f) slamHitbox?.End();
            if (attackElapsed >= telegraph + leapDuration + 0.52f)
            {
                EndAttack();
            }
        }

        private void ApplySweepDamage()
        {
            sweepHitbox?.Begin((Vector2)transform.position
                + Vector2.right * facing * sweepHitRange.x * 0.5f,
                new Vector2(sweepHitRange.x, sweepHitRange.y * 2f),
                1, new Vector2(facing * 8f, 3f));
        }

        private void ApplySlamDamage()
        {
            float direction = Mathf.Sign(target.position.x - transform.position.x);
            slamHitbox?.Begin(transform.position,
                new Vector2(slamHitRange.x * 2f, slamHitRange.y * 2f),
                2, new Vector2(direction * 9f, 4.5f));
        }

        private void AdvanceProjectileVolley(float telegraph)
        {
            if (attackElapsed < telegraph)
            {
                return;
            }

            if (volleyWavesFired < VolleyWaveCount && attackElapsed >= nextVolleyAt)
            {
                FireFanWave();
                attackResolved = true;
                volleyWavesFired++;
                lastVolleyAt = attackElapsed;
                nextVolleyAt = attackElapsed + VolleyWaveInterval;
            }

            if (volleyWavesFired >= VolleyWaveCount
                && attackElapsed >= lastVolleyAt + 0.35f)
            {
                EndAttack();
            }
        }

        private void AdvancePattern(float telegraph)
        {
            if (attackElapsed < telegraph) return;
            if (volleyWavesFired < PatternWaveCount && attackElapsed >= nextVolleyAt)
            {
                int wave = volleyWavesFired;
                for (int index = 0; index < PatternShotCount; index++)
                {
                    if (TryGetPatternShot(wave, index, out Vector2 origin,
                        out Vector2 direction, out bool breakable))
                    {
                        projectilePool?.TryFire(origin, direction,
                            PatternSpeed, breakable, false);
                    }
                }
                volleyWavesFired++;
                attackResolved = true;
                lastVolleyAt = attackElapsed;
                nextVolleyAt = attackElapsed + PatternInterval;
            }

            if (Phase == BossPhase.Two && CurrentAttack == BossAttackType.Crossfire)
            {
                FireScheduledAnchor(0, ref anchorOneFired, telegraph);
                FireScheduledAnchor(1, ref anchorTwoFired, telegraph);
                FireScheduledAnchor(2, ref anchorThreeFired, telegraph);
            }

            if (volleyWavesFired < PatternWaveCount) return;
            float elapsedAfterLast = attackElapsed - lastVolleyAt;
            if (!patternFading && elapsedAfterLast >= 2.2f)
            {
                patternFading = true;
                projectilePool?.FadeActive(0.25f);
            }
            if (!patternCleared && elapsedAfterLast >= 2.45f)
            {
                patternCleared = true;
                projectilePool?.ClearActive();
                fightCamera?.SetPatternZoom(false);
                SetBaseColor();
            }
            if (elapsedAfterLast >= (Phase == BossPhase.One ? 3.45f : 3.25f))
                EndAttack();
        }

        private void LockPatternAims()
        {
            if (CurrentAttack != BossAttackType.Crossfire || target == null) return;
            float telegraph = TelegraphDuration();
            for (int wave = 0; wave < Mathf.Min(PatternWaveCount, crossfireAims.Length); wave++)
            {
                if (!crossfireAimLocked[wave]
                    && attackElapsed >= telegraph + wave * PatternInterval - 0.6f)
                {
                    crossfireAims[wave] = target.position;
                    crossfireAimLocked[wave] = true;
                }
            }
            if (Phase != BossPhase.Two) return;
            for (int index = 0; index < AnchorDelays.Length; index++)
            {
                if (!anchorAimLocked[index]
                    && attackElapsed >= telegraph + AnchorDelays[index] - 0.8f)
                {
                    anchorAims[index] = target.position;
                    anchorAimLocked[index] = true;
                }
            }
        }

        public bool TryGetAnchorWarning(int index, out Vector2 origin,
            out Vector2 aim, out float remaining)
        {
            origin = (Vector2)transform.position + Vector2.up * 0.35f;
            aim = Vector2.zero;
            remaining = 0f;
            if (CurrentAttack != BossAttackType.Crossfire || Phase != BossPhase.Two
                || index < 0 || index >= AnchorDelays.Length || !anchorAimLocked[index])
                return false;
            float fireAt = TelegraphDuration() + AnchorDelays[index];
            remaining = fireAt - attackElapsed;
            if (remaining <= 0f || remaining > 0.8f) return false;
            aim = anchorAims[index];
            return true;
        }

        private void FireScheduledAnchor(int index, ref bool fired, float telegraph)
        {
            if (fired || attackElapsed < telegraph + AnchorDelays[index]) return;
            fired = true;
            Vector2 origin = (Vector2)transform.position + Vector2.up * 0.35f;
            Vector2 direction = ((anchorAimLocked[index] ? anchorAims[index] : lockedAim)
                - origin).normalized;
            if (direction.sqrMagnitude < 0.001f) direction = Vector2.left;
            projectilePool?.TryFire(origin, direction, 2f, false, true);
        }

        private float PatternSpeed => CurrentAttack switch
        {
            BossAttackType.Rain => Phase == BossPhase.One ? firstPhaseRainSpeed : secondPhaseRainSpeed,
            BossAttackType.Crossfire => Phase == BossPhase.One ? firstPhaseCrossSpeed : secondPhaseCrossSpeed,
            _ => Phase == BossPhase.One ? firstPhaseProjectileSpeed : secondPhaseProjectileSpeed
        };

        private Bounds ArenaBounds
        {
            get
            {
                return arenaRoom != null ? arenaRoom.WorldBounds
                    : new Bounds(arenaCenter + Vector2.up * 8f, new Vector3(26f, 16f, 1f));
            }
        }

        private int GapWidth => CurrentAttack == BossAttackType.ClockFan
            && Phase == BossPhase.Two ? 3 : 2;

        private int CalculateGapStart(Vector2 playerPosition)
        {
            Bounds room = ArenaBounds;
            int slots = PatternShotCount + GapWidth;
            float preferredSlot;
            if (CurrentAttack == BossAttackType.Rain)
            {
                preferredSlot = Mathf.InverseLerp(room.min.x + 0.8f,
                    room.max.x - 0.8f, playerPosition.x) * (slots - 1);
            }
            else if (CurrentAttack == BossAttackType.ClockFan)
            {
                Vector2 delta = playerPosition - FanOrigin;
                float angle = Mathf.Atan2(delta.x, -delta.y) * Mathf.Rad2Deg;
                preferredSlot = Mathf.InverseLerp(-75f, 75f, angle) * (slots - 1);
            }
            else preferredSlot = slots * 0.5f;
            int offset = mirrorCurrentVolley ? 1 : -1;
            return Mathf.Clamp(Mathf.RoundToInt(preferredSlot - GapWidth * 0.5f) + offset,
                1, slots - GapWidth - 1);
        }

        public Vector2 FanOrigin => new(ArenaBounds.center.x, ArenaBounds.min.y + 6.5f);

        /// <summary>预警与发射共享弹道计算；无效索引不生成弹幕。</summary>
        public bool TryGetPatternShot(int wave, int shotIndex,
            out Vector2 origin, out Vector2 direction, out bool breakable)
        {
            origin = Vector2.zero;
            direction = Vector2.zero;
            breakable = false;
            if (!IsPatternAttack || wave < 0 || wave >= PatternWaveCount
                || shotIndex < 0 || shotIndex >= PatternShotCount) return false;

            Bounds room = ArenaBounds;
            int slots = PatternShotCount + GapWidth;
            int rowGap = aimLocked ? gapStart : CalculateGapStart(TargetPosition);
            if (CurrentAttack == BossAttackType.Rain)
            {
                rowGap = Mathf.Clamp(gapStart + RainGapDrift[wave % RainGapDrift.Length]
                    * (mirrorCurrentVolley ? -1 : 1), 1, slots - GapWidth - 1);
            }
            else if (CurrentAttack == BossAttackType.Crossfire)
                rowGap = (slots - GapWidth) / 2;
            int slot = shotIndex < rowGap ? shotIndex : shotIndex + GapWidth;
            bool gapEdge = slot == rowGap - 1 || slot == rowGap + GapWidth;
            breakable = gapEdge || (Phase == BossPhase.One
                ? shotIndex % 4 != 3 : shotIndex % 2 == 0);

            switch (CurrentAttack)
            {
                case BossAttackType.ClockFan:
                    origin = FanOrigin;
                    float angle = -75f + 150f * slot / (slots - 1);
                    direction = Rotate(Vector2.down, angle);
                    break;
                case BossAttackType.Rain:
                    origin = new Vector2(Mathf.Lerp(room.min.x + 0.8f,
                        room.max.x - 0.8f, slot / (float)(slots - 1)),
                        room.min.y + 7.5f);
                    direction = Vector2.down;
                    break;
                case BossAttackType.Crossfire:
                    bool fromLeft = (wave + (mirrorCurrentVolley ? 1 : 0)) % 2 == 0;
                    float emitterX = fromLeft ? room.min.x + 2f : room.max.x - 2f;
                    Camera view = fightCamera != null ? fightCamera.GetComponent<Camera>() : null;
                    if (view != null && view.orthographic)
                    {
                        float halfWidth = view.orthographicSize * view.aspect;
                        emitterX = Mathf.Clamp(emitterX,
                            view.transform.position.x - halfWidth + 0.5f,
                            view.transform.position.x + halfWidth - 0.5f);
                    }
                    origin = new Vector2(emitterX,
                        room.min.y + 5.5f);
                    Vector2 aimedAt = wave < crossfireAimLocked.Length && crossfireAimLocked[wave]
                        ? crossfireAims[wave] : TargetPosition;
                    Vector2 aim = (aimedAt - origin).normalized;
                    if (aim.sqrMagnitude < 0.001f) aim = fromLeft ? Vector2.right : Vector2.left;
                    float spread = Phase == BossPhase.One ? 55f : 65f;
                    direction = Rotate(aim, -spread * 0.5f + spread * slot / (slots - 1));
                    break;
            }
            return true;
        }

        private void FireFanWave()
        {
            if (projectilePool == null)
            {
                return;
            }

            Vector2 origin = VolleyOrigin;
            float speed = Phase == BossPhase.One
                ? firstPhaseProjectileSpeed : secondPhaseProjectileSpeed;
            for (int index = 0; index < VolleyShotCount; index++)
            {
                if (!TryGetVolleyRay(index, out Vector2 direction, out bool breakable))
                {
                    continue;
                }

                projectilePool.TryFire(origin, direction, speed, breakable, false);
            }
        }

        public Vector2 VolleyOrigin => (Vector2)transform.position + Vector2.up * 0.35f;

        /// <summary>
        /// 预警与实际发射共用同一条扇形射线，中央空出两个角度位置。
        /// </summary>
        public bool TryGetVolleyRay(int shotIndex, out Vector2 direction, out bool breakable)
        {
            direction = Vector2.zero;
            breakable = false;
            int shotCount = VolleyShotCount;
            if (shotIndex < 0 || shotIndex >= shotCount)
            {
                return false;
            }

            int slotCount = shotCount + 2;
            int gapStart = shotCount / 2;
            int slot = shotIndex < gapStart ? shotIndex : shotIndex + 2;
            float step = Phase == BossPhase.One ? 15f : 12f;
            float angle = (slot - (slotCount - 1) * 0.5f) * step;
            if (mirrorCurrentVolley)
            {
                angle = -angle;
            }

            Vector2 baseDirection = (TelegraphAimPosition - VolleyOrigin).normalized;
            if (baseDirection.sqrMagnitude < 0.001f)
            {
                baseDirection = Vector2.right * facing;
            }

            direction = Rotate(baseDirection, angle);
            bool besideGap = slot == gapStart - 1 || slot == gapStart + 2;
            breakable = besideGap || (shotIndex + 1) % 3 != 0;
            return true;
        }

        private void FireTimeAnchorVolley()
        {
            if (projectilePool == null)
            {
                return;
            }

            Vector2 origin = VolleyOrigin;
            Vector2 baseDirection = (lockedAim - origin).normalized;
            if (baseDirection.sqrMagnitude < 0.001f)
            {
                baseDirection = Vector2.right * facing;
            }
            for (int index = 0; index < 3; index++)
            {
                projectilePool.TryFire(
                    origin, Rotate(baseDirection, (index - 1) * 16f),
                    4.3f, false, true);
            }
        }

        private void ClearVolleyState()
        {
            volleyWavesFired = 0;
            nextVolleyAt = 0f;
            lastVolleyAt = 0f;
            mirrorCurrentVolley = false;
            patternCleared = false;
            patternFading = false;
            anchorOneFired = anchorTwoFired = anchorThreeFired = false;
            Array.Clear(crossfireAimLocked, 0, crossfireAimLocked.Length);
            Array.Clear(anchorAimLocked, 0, anchorAimLocked.Length);
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
            fightCamera?.SetPatternZoom(false);
            sweepHitbox?.End();
            slamHitbox?.End();
            CurrentAttack = BossAttackType.None;
            attackElapsed = 0f;
            attackResolved = false;
            ClearVolleyState();
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
            sweepHitbox?.End();
            slamHitbox?.End();
            ClearVolleyState();
            CurrentAttack = BossAttackType.None;
            sequenceIndex = 0;
            phaseTransitionRemaining = 0.9f;
            projectilePool?.ClearActive();
            fightCamera?.SetPatternZoom(false);
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
            sweepHitbox?.End();
            slamHitbox?.End();
            ClearVolleyState();
            CurrentAttack = BossAttackType.None;
            IsDefeatedConfirmed = true;
            projectilePool?.ClearActive();
            fightCamera?.SetPatternZoom(false);
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
            sweepHitbox?.End();
            slamHitbox?.End();
            ClearVolleyState();
            CurrentAttack = BossAttackType.None;
            IsEngaged = false;
            sequenceIndex = 0;
            mirrorNextVolley = false;
            cooldownRemaining = 0f;
            attackElapsed = 0f;
            phaseTransitionRemaining = 0f;
            attackResolved = false;
            transform.position = startPosition;
            level?.EndBossBattle();
            damageReceiver.ClearPendingDamage();
            localTime.ClearStoredMotion();
            projectilePool?.ClearActive();
            fightCamera?.SetPatternZoom(false);
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
            if (bossHudFrameSprite != null)
                TimeMetroidvania.Presentation.V2GuiSprite.Draw(bossHudFrameSprite,
                    new Rect(left, 20f, width, 54f));
            else GUI.Box(new Rect(left, 20f, width, 54f), GUIContent.none);
            GUI.Label(new Rect(left, 22f, width, 24f),
                $"时蚀守卫 · 阶段 {(int)Phase}", bossLabelStyle);
            float normalized = health.MaxHealth <= 0
                ? 0f
                : (float)health.CurrentHealth / health.MaxHealth;
            GUI.color = Phase == BossPhase.Two
                ? new Color(0.75f, 0.28f, 0.95f)
                : new Color(0.95f, 0.25f, 0.25f);
            if (bossHudFillSprite != null)
                TimeMetroidvania.Presentation.V2GuiSprite.DrawFilled(bossHudFillSprite,
                    new Rect(left + 62f, 51f, width - 124f, 11f), normalized);
            else GUI.Box(new Rect(left + 16f, 50f, (width - 32f) * normalized, 14f), GUIContent.none);
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
