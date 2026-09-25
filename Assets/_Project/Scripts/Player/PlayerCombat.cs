using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TimeMetroidvania.GameTime;

namespace TimeMetroidvania.Player
{
    /// <summary>
    /// 处理地面与空中平砍，并保证单次挥剑对同一目标只结算一次。
    /// </summary>
    [RequireComponent(typeof(PlayerStateMachine), typeof(PlayerMovement), typeof(TimeGauge))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [Header("输入")]
        [SerializeField] private InputActionReference attackAction;

        [Header("攻击")]
        [SerializeField] private Transform attackOrigin;
        [SerializeField] private Vector2 attackOffset = new(0.85f, 0.05f);
        [SerializeField] private Vector2 airAttackOffset = new(0.8f, -0.05f);
        [SerializeField] private Vector2 attackSize = new(1.15f, 0.9f);
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField, Min(0.01f)] private float attackDuration = 0.32f;
        [SerializeField, Min(0.01f)] private float attackInterval = 0.72f;
        [SerializeField, Min(0f)] private float attackBufferDuration = 0.1f;
        [SerializeField, Range(0f, 1f)] private float activeStartNormalized = 0.2f;
        [SerializeField, Range(0f, 1f)] private float activeEndNormalized = 0.65f;
        [SerializeField, Min(0f)] private float horizontalKnockback = 4f;
        [SerializeField, Min(0f)] private float verticalKnockback = 2f;
        [SerializeField] private LayerMask damageMask = ~0;

        [Header("时间槽")]
        [SerializeField, Min(0f)] private float enemyHitGaugeGain = 8f;
        [SerializeField, Min(0f)] private float projectileHitGaugeGain = 4f;
        [SerializeField] private WorldTimeService worldTime;

        private readonly Collider2D[] hitBuffer = new Collider2D[16];
        private readonly AttackHitRegistry hitTargets = new();
        private ContactFilter2D damageFilter;
        private PlayerStateMachine stateMachine;
        private PlayerMovement movement;
        private TimeGauge timeGauge;
        private float attackElapsed;
        private float attackCooldownRemaining;
        private float attackBufferRemaining;

        public event Action AttackCompleted;

        public bool IsAttacking => stateMachine.CurrentState == PlayerState.Attack;
        public float AttackCooldownRemaining => attackCooldownRemaining;
        public bool CanCancelIntoDash => IsAttacking
            && AttackProgress >= activeEndNormalized;
        public float AttackProgress => IsAttacking
            ? Mathf.Clamp01(attackElapsed / attackDuration)
            : 0f;
        public bool IsAttackActive => IsAttacking
            && AttackProgress >= activeStartNormalized
            && AttackProgress <= activeEndNormalized;
        public bool IsSlashVisualVisible => IsAttacking
            && AttackProgress >= 0.06f
            && AttackProgress <= 0.94f;
        public Vector2 AttackSize => attackSize;
        public Vector2 CurrentAttackCenter
        {
            get
            {
                Vector2 basePosition =
                    attackOrigin == null ? transform.position : attackOrigin.position;
                Vector2 offset = movement.IsGrounded ? attackOffset : airAttackOffset;
                offset.x *= movement.FacingDirection;
                return basePosition + offset;
            }
        }

        private void Awake()
        {
            stateMachine = GetComponent<PlayerStateMachine>();
            movement = GetComponent<PlayerMovement>();
            timeGauge = GetComponent<TimeGauge>();
            damageFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = damageMask,
                useTriggers = true
            };
        }

        private void OnEnable()
        {
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
            attackAction?.action.Disable();
        }

        private void Update()
        {
            attackCooldownRemaining = Mathf.Max(
                0f,
                attackCooldownRemaining - Time.deltaTime);
            attackBufferRemaining = Mathf.Max(
                0f,
                attackBufferRemaining - Time.deltaTime);
            if (attackAction.action.WasPressedThisFrame())
            {
                if (!BeginAttack())
                {
                    attackBufferRemaining = attackBufferDuration;
                }
            }

            if (!IsAttacking)
            {
                if (attackBufferRemaining > 0f && BeginAttack())
                {
                    attackBufferRemaining = 0f;
                }
                return;
            }

            attackElapsed += Time.deltaTime;
            if (IsAttackActive)
            {
                ApplyAttackHits();
            }

            if (attackElapsed >= attackDuration)
            {
                if (stateMachine.TryReturnToNormal(PlayerState.Attack))
                {
                    AttackCompleted?.Invoke();
                    if (attackBufferRemaining > 0f && BeginAttack())
                    {
                        attackBufferRemaining = 0f;
                    }
                }
            }
        }

        public bool BeginAttack()
        {
            if (attackCooldownRemaining > 0f)
            {
                return false;
            }

            if (!stateMachine.TryEnter(PlayerState.Attack))
            {
                return false;
            }

            attackElapsed = 0f;
            attackCooldownRemaining = attackInterval;
            hitTargets.Clear();
            return true;
        }

        private void ApplyAttackHits()
        {
            Vector2 center = CurrentAttackCenter;
            int hitCount = Physics2D.OverlapBox(center, attackSize, 0f, damageFilter, hitBuffer);

            for (int index = 0; index < hitCount; index++)
            {
                Collider2D hitCollider = hitBuffer[index];
                if (hitCollider == null || hitCollider.transform.root == transform.root)
                {
                    continue;
                }

                IHittableProjectile projectile =
                    hitCollider.GetComponentInParent(typeof(IHittableProjectile)) as IHittableProjectile;
                if (projectile != null)
                {
                    if (hitTargets.TryRegister(projectile) && projectile.TryDestroyByAttack(gameObject))
                    {
                        timeGauge.Gain(projectileHitGaugeGain * GetGaugeGainMultiplier());
                    }

                    continue;
                }

                TimeAwareDamageReceiver timeAwareReceiver =
                    hitCollider.GetComponentInParent<TimeAwareDamageReceiver>();
                IDamageable damageable = timeAwareReceiver != null
                    ? timeAwareReceiver
                    : hitCollider.GetComponentInParent(typeof(IDamageable)) as IDamageable;
                if (!hitTargets.TryRegister(damageable))
                {
                    continue;
                }

                Vector2 knockback = new(movement.FacingDirection * horizontalKnockback, verticalKnockback);
                DamageInfo damageInfo = new(gameObject, damage, hitCollider.ClosestPoint(center), knockback);
                if (damageable.TryTakeDamage(damageInfo))
                {
                    timeGauge.Gain(enemyHitGaugeGain * GetGaugeGainMultiplier());
                }
            }
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
            Vector2 basePosition = attackOrigin == null ? transform.position : attackOrigin.position;
            int facing = movement == null ? 1 : movement.FacingDirection;
            Vector2 offset = movement != null && !movement.IsGrounded
                ? airAttackOffset
                : attackOffset;
            offset.x *= facing;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(basePosition + offset, attackSize);
        }
    }
}
