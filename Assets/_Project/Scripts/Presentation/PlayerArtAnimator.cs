using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Presentation
{
    /// <summary>
    /// 将玩家现有状态映射到像素逐帧动画，视觉不会反向影响物理判定。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerArtAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField, Min(1f)] private float framesPerSecond = 12f;
        [SerializeField] private Sprite[] idle;
        [SerializeField] private Sprite[] run;
        [SerializeField] private Sprite[] jumpRise;
        [SerializeField] private Sprite[] jumpApex;
        [SerializeField] private Sprite[] fall;
        [SerializeField] private Sprite[] attackGround;
        [SerializeField] private Sprite[] attackAir;
        [SerializeField] private Sprite[] attackUp;
        [SerializeField] private Sprite[] attackDown;
        [SerializeField, Min(0.1f)] private float attackVisualScale = 1f;
        [SerializeField] private Sprite[] dash;
        [SerializeField] private Sprite[] hurt;
        [SerializeField] private Sprite[] death;
        [SerializeField] private Sprite[] heal;
        [SerializeField] private Sprite[] slowCharge;
        [Header("受击反馈")]
        [SerializeField, Min(0.05f)] private float damageFlashDuration = 0.32f;
        [SerializeField, Min(0f)] private float damageShakeDistance = 0.075f;

        private readonly SpriteFramePlayer framePlayer = new();
        private PlayerStateMachine stateMachine;
        private PlayerMovement movement;
        private PlayerCombat combat;
        private PlayerDash dashAbility;
        private PlayerTimeAbilities timeAbilities;
        private HealthSystem health;
        private Rigidbody2D body;
        private Vector3 baseVisualPosition;
        private Vector3 baseVisualScale;
        private float damageFeedbackRemaining;

        private void Awake()
        {
            stateMachine = GetComponent<PlayerStateMachine>();
            movement = GetComponent<PlayerMovement>();
            combat = GetComponent<PlayerCombat>();
            dashAbility = GetComponent<PlayerDash>();
            timeAbilities = GetComponent<PlayerTimeAbilities>();
            health = GetComponent<HealthSystem>();
            body = GetComponent<Rigidbody2D>();
            targetRenderer ??= transform.Find("ArtVisual")?.GetComponent<SpriteRenderer>();
            if (targetRenderer != null)
            {
                baseVisualPosition = targetRenderer.transform.localPosition;
                baseVisualScale = targetRenderer.transform.localScale;
            }
        }

        private void OnEnable()
        {
            framePlayer.Reset();
            if (health != null)
            {
                health.Damaged -= HandleDamaged;
                health.Damaged += HandleDamaged;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= HandleDamaged;
            }
            RestoreVisualTransform();
        }

        private void LateUpdate()
        {
            if (targetRenderer == null || stateMachine == null || movement == null)
            {
                return;
            }

            (Sprite[] frames, bool loop) = ResolveAnimation();
            targetRenderer.flipX = stateMachine.CurrentState == PlayerState.Attack && combat != null
                ? combat.CurrentAttackFacing < 0 : movement.FacingDirection < 0;
            targetRenderer.color = Color.white;
            damageFeedbackRemaining = Mathf.Max(
                0f,
                damageFeedbackRemaining - Time.unscaledDeltaTime);
            if (stateMachine.CurrentState == PlayerState.Attack && combat != null)
            {
                framePlayer.PlayNormalized(
                    targetRenderer,
                    frames,
                    combat.AttackAnimationProgress);
            }
            else if (stateMachine.CurrentState == PlayerState.Dash && dashAbility != null)
            {
                framePlayer.PlayNormalized(targetRenderer, frames, dashAbility.DashProgress);
            }
            else
            {
                framePlayer.Play(targetRenderer, frames, framesPerSecond, loop, Time.deltaTime);
            }
            ApplyDamageFeedback();
            if (stateMachine.CurrentState == PlayerState.Attack)
                targetRenderer.transform.localScale *= attackVisualScale;
        }

        private void HandleDamaged(DamageInfo damageInfo)
        {
            damageFeedbackRemaining = damageFlashDuration;
        }

        private void ApplyDamageFeedback()
        {
            if (damageFeedbackRemaining <= 0f)
            {
                RestoreVisualTransform();
                return;
            }

            float strength = Mathf.Clamp01(damageFeedbackRemaining / damageFlashDuration);
            bool whiteFlash = Mathf.FloorToInt(Time.unscaledTime * 28f) % 2 == 0;
            targetRenderer.color = whiteFlash
                ? Color.white
                : new Color(1f, 0.12f, 0.08f, 1f);
            float shakeX = Mathf.Sin(Time.unscaledTime * 95f)
                * damageShakeDistance * strength;
            float shakeY = Mathf.Cos(Time.unscaledTime * 73f)
                * damageShakeDistance * 0.55f * strength;
            targetRenderer.transform.localPosition = baseVisualPosition
                + new Vector3(shakeX, shakeY, 0f);
            targetRenderer.transform.localScale = new Vector3(
                baseVisualScale.x * (1f + 0.14f * strength),
                baseVisualScale.y * (1f - 0.1f * strength),
                baseVisualScale.z);
        }

        private void RestoreVisualTransform()
        {
            if (targetRenderer == null)
            {
                return;
            }
            targetRenderer.color = Color.white;
            targetRenderer.transform.localPosition = baseVisualPosition;
            targetRenderer.transform.localScale = baseVisualScale;
        }

        private (Sprite[] frames, bool loop) ResolveAnimation()
        {
            switch (stateMachine.CurrentState)
            {
                case PlayerState.Dead:
                    return (death, false);
                case PlayerState.Hurt:
                    return (hurt, false);
                case PlayerState.Healing:
                    return (heal, true);
                case PlayerState.Dash:
                    return (dash, true);
                case PlayerState.Attack:
                    if (combat != null && combat.CurrentAttackDirection == AttackDirection.Up
                        && attackUp != null && attackUp.Length > 0) return (attackUp, false);
                    if (combat != null && combat.CurrentAttackDirection == AttackDirection.Down
                        && attackDown != null && attackDown.Length > 0) return (attackDown, false);
                    return (movement.IsGrounded ? attackGround : attackAir, false);
            }

            if (timeAbilities != null && timeAbilities.SlowChargeProgress > 0f)
            {
                return (slowCharge, true);
            }

            if (!movement.IsGrounded)
            {
                if (body != null && Mathf.Abs(body.linearVelocity.y) < 1f
                    && jumpApex != null && jumpApex.Length > 0)
                    return (jumpApex, true);
                return (body != null && body.linearVelocity.y > 0.05f ? jumpRise : fall, true);
            }

            return body != null && Mathf.Abs(body.linearVelocity.x) > 0.05f
                ? (run, true)
                : (idle, true);
        }
    }
}
