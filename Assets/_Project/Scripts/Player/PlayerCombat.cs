using System;
using TimeMetroidvania.Boss;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Level;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TimeMetroidvania.Player
{
    /// <summary>
    /// 管理横砍、上砍与下劈的输入、攻击阶段、独立判定和命中反馈。
    /// </summary>
    [RequireComponent(typeof(PlayerStateMachine), typeof(PlayerMovement), typeof(TimeGauge))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [Header("输入")]
        [SerializeField] private InputActionReference attackAction;

        [Header("攻击判定")]
        [SerializeField] private Transform attackOrigin;
        [SerializeField] private PlayerAttackHitbox2D attackHitbox;
        [SerializeField] private Vector2 attackOffset = new(0.9f, 0f);
        [SerializeField] private Vector2 airAttackOffset = new(0.9f, 0f);
        [SerializeField] private Vector2 attackSize = new(1.65f, 1.15f);
        [SerializeField] private Vector2 upAttackOffset = new(0f, 0.85f);
        [SerializeField] private Vector2 upAttackSize = new(1.15f, 1.35f);
        [SerializeField] private Vector2 downAttackOffset = new(0f, -0.85f);
        [SerializeField] private Vector2 downAttackSize = new(1.15f, 1.35f);
        [SerializeField] private LayerMask damageMask = ~0;

        [Header("攻击节奏")]
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField, Min(0.01f)] private float attackDuration = 0.28f;
        [SerializeField, Min(0.01f)] private float attackInterval = 0.41f;
        [SerializeField, Min(0f)] private float attackBufferDuration = 0.12f;
        [SerializeField, Range(0f, 1f)] private float activeStartNormalized = 0.12f;
        [SerializeField, Range(0f, 1f)] private float activeEndNormalized = 0.62f;

        [Header("命中反馈")]
        [SerializeField, Min(0f)] private float horizontalKnockback = 4f;
        [SerializeField, Min(0f)] private float verticalKnockback = 2f;
        [SerializeField, Min(0f)] private float pogoBounceSpeed = 11f;
        [SerializeField, Min(0f)] private float hitStopDuration = 0.05f;

        [Header("时间槽")]
        [SerializeField, Min(0f)] private float enemyHitGaugeGain = 10f;
        [SerializeField, Min(0f)] private float projectileHitGaugeGain = 8f;
        [SerializeField] private WorldTimeService worldTime;

        private readonly Collider2D[] hitBuffer = new Collider2D[512];
        private ContactFilter2D damageFilter;
        private PlayerStateMachine stateMachine;
        private PlayerMovement movement;
        private TimeGauge timeGauge;
        private HitStopController hitStop;
        private float attackElapsed;
        private float attackCooldownRemaining;
        private float attackBufferRemaining;
        private int attackFacing = 1;
        private bool attackLifecycleActive;
        private bool activeWindowOpen;
        private bool pogoAppliedThisSwing;
        private float bossProjectileGaugeThisSwing;
        private StageFiveLevelController level;

        public event Action<AttackDirection> AttackStarted;
        public event Action<AttackDirection> AttackActiveStarted;
        public event Action<AttackDirection, GameObject> AttackLanded;
        public event Action<AttackDirection> AttackEnded;
        public event Action AttackCompleted;

        public bool IsAttacking => stateMachine.CurrentState == PlayerState.Attack;
        public float AttackCooldownRemaining => attackCooldownRemaining;
        public float AttackInterval => attackInterval;
        public float AttackBufferDuration => attackBufferDuration;
        public float PogoBounceSpeed => pogoBounceSpeed;
        public AttackDirection CurrentAttackDirection { get; private set; }
        public int CurrentAttackFacing => attackFacing;
        public PlayerAttackHitbox2D AttackHitbox => attackHitbox;
        public float AnimationDeltaTime => hitStop != null && hitStop.IsPaused
            ? 0f
            : Time.deltaTime;
        public bool CanCancelIntoDash => IsAttacking
            && AttackProgress >= activeEndNormalized;
        public float AttackProgress => IsAttacking
            ? Mathf.Clamp01(attackElapsed / attackDuration)
            : 0f;
        public bool IsAttackActive => IsAttacking
            && AttackProgress >= activeStartNormalized
            && AttackProgress <= activeEndNormalized;
        // 四帧美术依次对应前摇、两帧有效攻击、收招，按实际有效窗口分配时间。
        public float AttackAnimationProgress => AttackProgress < activeStartNormalized
            ? 0f
            : AttackProgress <= activeEndNormalized
                ? Mathf.Lerp(0.25f, 0.749f, Mathf.InverseLerp(activeStartNormalized, activeEndNormalized, AttackProgress))
                : 0.75f;
        public bool IsSlashVisualVisible => IsAttacking
            && AttackProgress >= 0.06f
            && AttackProgress <= 0.94f;
        public Vector2 AttackSize => attackHitbox != null
            ? attackHitbox.WorldSize
            : GetAttackSize(CurrentAttackDirection);
        public Vector2 CurrentAttackCenter
        {
            get
            {
                if (attackHitbox != null)
                {
                    return attackHitbox.WorldCenter;
                }

                Vector2 basePosition =
                    attackOrigin == null ? transform.position : attackOrigin.position;
                Vector2 offset = GetAttackOffset(CurrentAttackDirection);
                if (CurrentAttackDirection == AttackDirection.Horizontal)
                {
                    offset.x *= attackFacing;
                }

                return basePosition + offset;
            }
        }

        private void Awake()
        {
            stateMachine = GetComponent<PlayerStateMachine>();
            movement = GetComponent<PlayerMovement>();
            timeGauge = GetComponent<TimeGauge>();
            level = FindFirstObjectByType<StageFiveLevelController>();
            hitStop = GetComponent<HitStopController>();
            if (hitStop == null)
            {
                hitStop = gameObject.AddComponent<HitStopController>();
            }

            damageFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = damageMask,
                useTriggers = true
            };

            EnsureAttackHitbox();
            ConfigureAttackHitbox(AttackDirection.Horizontal);
            attackHitbox.SetActive(false);
        }

        private void OnEnable()
        {
            stateMachine.StateChanged += HandleStateChanged;
            if (attackAction == null)
            {
                Debug.LogError("PlayerCombat 未配置 Attack 输入动作。", this);
                enabled = false;
                return;
            }

            attackAction.action.Enable();
        }

        private void OnDisable()
        {
            if (stateMachine != null)
            {
                stateMachine.StateChanged -= HandleStateChanged;
            }

            attackAction?.action.Disable();
            attackHitbox?.SetActive(false);
            attackLifecycleActive = false;
            activeWindowOpen = false;
        }

        private void OnValidate()
        {
            activeEndNormalized = Mathf.Max(activeStartNormalized, activeEndNormalized);
        }

        private void Update()
        {
            if (attackAction.action.WasPressedThisFrame())
            {
                if (!BeginAttack())
                {
                    attackBufferRemaining = attackBufferDuration;
                }
            }

            attackCooldownRemaining = Mathf.Max(
                0f,
                attackCooldownRemaining - Time.deltaTime);

            if (hitStop != null && hitStop.IsPaused)
            {
                return;
            }

            attackBufferRemaining = Mathf.Max(
                0f,
                attackBufferRemaining - Time.deltaTime);

            if (!IsAttacking)
            {
                attackHitbox.SetActive(false);
                if (attackBufferRemaining > 0f && BeginAttack())
                {
                    attackBufferRemaining = 0f;
                }
                return;
            }

            attackElapsed += Time.deltaTime;
            UpdateActiveWindow();
            if (IsAttackActive)
            {
                ApplyAttackHits();
            }

            if (attackElapsed < attackDuration)
            {
                return;
            }

            bool bufferedAttack = attackBufferRemaining > 0f;
            if (stateMachine.TryReturnToNormal(PlayerState.Attack)
                && bufferedAttack
                && BeginAttack())
            {
                attackBufferRemaining = 0f;
            }
        }

        public bool BeginAttack()
        {
            return BeginAttack(ResolveAttackDirection(
                movement.VerticalInput,
                movement.IsGrounded));
        }

        public bool BeginAttack(AttackDirection direction)
        {
            if (TimeMetroidvania.Level.V2TimeStopWater.IsPlayerFrozen(gameObject)
                || attackCooldownRemaining > 0f
                || hitStop != null && hitStop.IsPaused)
            {
                return false;
            }

            if (direction == AttackDirection.Down && movement.IsGrounded)
            {
                direction = AttackDirection.Horizontal;
            }

            CurrentAttackDirection = direction;
            attackFacing = movement.FacingDirection;
            if (!stateMachine.TryEnter(PlayerState.Attack))
            {
                return false;
            }

            attackElapsed = 0f;
            attackCooldownRemaining = attackInterval;
            attackLifecycleActive = true;
            activeWindowOpen = false;
            pogoAppliedThisSwing = false;
            bossProjectileGaugeThisSwing = 0f;
            ConfigureAttackHitbox(direction);
            attackHitbox.BeginSwing();
            AttackStarted?.Invoke(direction);
            return true;
        }

        internal static AttackDirection ResolveAttackDirection(
            float verticalInput,
            bool grounded)
        {
            if (verticalInput > 0.25f)
            {
                return AttackDirection.Up;
            }

            if (verticalInput < -0.25f && !grounded)
            {
                return AttackDirection.Down;
            }

            return AttackDirection.Horizontal;
        }

        private void UpdateActiveWindow()
        {
            bool shouldBeActive = IsAttackActive;
            if (shouldBeActive == activeWindowOpen)
            {
                return;
            }

            activeWindowOpen = shouldBeActive;
            attackHitbox.SetActive(shouldBeActive);
            if (shouldBeActive)
            {
                AttackActiveStarted?.Invoke(CurrentAttackDirection);
            }
        }

        private void ApplyAttackHits()
        {
            int hitCount = attackHitbox.CollectOverlaps(damageFilter, hitBuffer);
            Vector2 center = CurrentAttackCenter;

            for (int index = 0; index < hitCount; index++)
            {
                Collider2D hitCollider = hitBuffer[index];
                if (hitCollider == null || hitCollider.transform.root == transform.root)
                {
                    continue;
                }

                StageFiveRoom targetRoom = hitCollider.GetComponentInParent<StageFiveRoom>();
                if (targetRoom != null && level != null && targetRoom.RoomId != level.CurrentRoomId)
                    continue;

                IHittableProjectile projectile =
                    hitCollider.GetComponentInParent(typeof(IHittableProjectile)) as IHittableProjectile;
                if (projectile != null)
                {
                    if (attackHitbox.TryRegister(projectile)
                        && projectile.TryDestroyByAttack(gameObject))
                    {
                        if (projectile is BossProjectile)
                        {
                            float granted = Mathf.Min(projectileHitGaugeGain,
                                Mathf.Max(0f, 16f - bossProjectileGaugeThisSwing));
                            bossProjectileGaugeThisSwing += granted;
                            if (granted > 0f) RewardTimeGauge(granted);
                        }
                        else RewardTimeGauge(projectileHitGaugeGain);
                        HandleSuccessfulHit(hitCollider.gameObject, false);
                    }

                    continue;
                }

                TimeAwareDamageReceiver timeAwareReceiver =
                    hitCollider.GetComponentInParent<TimeAwareDamageReceiver>();
                IDamageable damageable = timeAwareReceiver != null
                    ? timeAwareReceiver
                    : hitCollider.GetComponentInParent(typeof(IDamageable)) as IDamageable;
                if (damageable != null && attackHitbox.TryRegister(damageable))
                {
                    DamageInfo damageInfo = new(
                        gameObject,
                        damage,
                        hitCollider.ClosestPoint(center),
                        GetTargetKnockback());
                    if (damageable.TryTakeDamage(damageInfo))
                    {
                        RewardTimeGauge(enemyHitGaugeGain);
                        Component target = damageable as Component;
                        HandleSuccessfulHit(
                            target != null ? target.gameObject : hitCollider.gameObject,
                            true);
                    }
                    else
                    {
                        TryApplyRejectedDamagePogo(hitCollider);
                    }

                    continue;
                }

                IPogoable pogoable =
                    hitCollider.GetComponentInParent(typeof(IPogoable)) as IPogoable;
                if (pogoable != null
                    && attackHitbox.TryRegister(pogoable)
                    && pogoable.CanPogo(gameObject))
                {
                    HandleSuccessfulHit(hitCollider.gameObject, false);
                }
            }
        }

        private void HandleSuccessfulHit(GameObject target, bool applyHitStop)
        {
            if (CurrentAttackDirection == AttackDirection.Down && !pogoAppliedThisSwing)
            {
                movement.ApplyPogoBounce(pogoBounceSpeed);
                pogoAppliedThisSwing = true;
            }

            if (applyHitStop)
            {
                hitStop.Request(hitStopDuration);
                HitStopController targetHitStop = target.GetComponent<HitStopController>();
                if (targetHitStop == null)
                {
                    targetHitStop = target.AddComponent<HitStopController>();
                }
                targetHitStop.Request(hitStopDuration);
            }

            AttackLanded?.Invoke(CurrentAttackDirection, target);
        }

        /// <summary>
        /// 敌人因受伤保护或 Boss 时停缓存上限拒绝伤害时，仍允许新的一刀下劈反弹。
        /// 此路径不回能、不触发命中停顿，也不派发命中事件。
        /// </summary>
        private void TryApplyRejectedDamagePogo(Collider2D hitCollider)
        {
            if (CurrentAttackDirection != AttackDirection.Down
                || pogoAppliedThisSwing
                || !IsLivePogoTarget(hitCollider))
            {
                return;
            }

            movement.ApplyPogoBounce(pogoBounceSpeed);
            pogoAppliedThisSwing = true;
        }

        internal static bool IsLivePogoTarget(Collider2D hitCollider)
        {
            if (hitCollider == null
                || !hitCollider.enabled
                || !hitCollider.gameObject.activeInHierarchy)
            {
                return false;
            }

            HealthSystem health = hitCollider.GetComponentInParent<HealthSystem>();
            return health != null && health.isActiveAndEnabled && !health.IsDead;
        }

        private Vector2 GetTargetKnockback()
        {
            return CurrentAttackDirection switch
            {
                AttackDirection.Up => new Vector2(0f, Mathf.Max(4f, verticalKnockback)),
                AttackDirection.Down => new Vector2(0f, -verticalKnockback),
                _ => new Vector2(attackFacing * horizontalKnockback, verticalKnockback)
            };
        }

        private void ConfigureAttackHitbox(AttackDirection direction)
        {
            attackHitbox.SetShape(
                direction,
                GetAttackOffset(direction),
                GetAttackSize(direction),
                attackFacing);
        }

        private Vector2 GetAttackOffset(AttackDirection direction)
        {
            return direction switch
            {
                AttackDirection.Up => upAttackOffset,
                AttackDirection.Down => downAttackOffset,
                _ => movement != null && !movement.IsGrounded
                    ? airAttackOffset
                    : attackOffset
            };
        }

        private Vector2 GetAttackSize(AttackDirection direction)
        {
            return direction switch
            {
                AttackDirection.Up => upAttackSize,
                AttackDirection.Down => downAttackSize,
                _ => attackSize
            };
        }

        private void EnsureAttackHitbox()
        {
            attackHitbox ??= GetComponentInChildren<PlayerAttackHitbox2D>(true);
            if (attackHitbox == null)
            {
                Transform parent = attackOrigin != null ? attackOrigin : transform;
                GameObject hitboxObject = new("AttackHitbox");
                hitboxObject.transform.SetParent(parent, false);
                attackHitbox = hitboxObject.AddComponent<PlayerAttackHitbox2D>();
            }

            attackHitbox.ConfigureLayer("PlayerAttack");
        }

        private void HandleStateChanged(PlayerState previousState, PlayerState nextState)
        {
            if (previousState != PlayerState.Attack || nextState == PlayerState.Attack)
            {
                return;
            }

            bool completed = nextState is PlayerState.Normal or PlayerState.Dash;
            EndAttack(completed);
        }

        private void EndAttack(bool completed)
        {
            if (!attackLifecycleActive)
            {
                return;
            }

            attackHitbox.SetActive(false);
            attackLifecycleActive = false;
            activeWindowOpen = false;
            AttackEnded?.Invoke(CurrentAttackDirection);
            if (completed)
            {
                AttackCompleted?.Invoke();
            }
        }

        private void RewardTimeGauge(float normalGain)
        {
            timeGauge.NotifySuccessfulHit();
            if (worldTime != null && worldTime.RegisterTimeEcho(normalGain))
            {
                return;
            }

            timeGauge.Gain(normalGain);
        }

        private float GetGaugeGainMultiplier()
        {
            return GetGaugeGainMultiplier(worldTime != null
                ? worldTime.CurrentMode
                : WorldTimeMode.Normal);
        }

        internal static float GetGaugeGainMultiplier(WorldTimeMode mode)
        {
            return mode == WorldTimeMode.Stopped
                ? 0.25f
                : 1f;
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin = attackOrigin != null ? attackOrigin : transform;
            int facing = movement == null ? 1 : movement.FacingDirection;
            AttackDirection direction = Application.isPlaying
                ? CurrentAttackDirection
                : AttackDirection.Horizontal;
            Vector2 offset = GetAttackOffset(direction);
            if (direction == AttackDirection.Horizontal)
            {
                offset.x *= facing;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube((Vector2)origin.position + offset, GetAttackSize(direction));
        }
    }
}
