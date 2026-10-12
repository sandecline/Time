using TimeMetroidvania.Boss;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Presentation
{
    /// <summary>
    /// 将时蚀守卫状态映射到两阶段 Boss 动画。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossArtAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private bool spriteFacesRight;
        [SerializeField, Min(1f)] private float framesPerSecond = 10f;
        [SerializeField] private Sprite[] idle;
        [SerializeField] private Sprite[] sweepTelegraph;
        [SerializeField] private Sprite[] sweepAttack;
        [SerializeField] private Sprite[] jump;
        [SerializeField] private Sprite[] slam;
        [SerializeField] private Sprite[] shoot;
        [SerializeField] private Sprite[] timeAnchor;
        [SerializeField] private Sprite[] hurt;
        [SerializeField] private Sprite[] phaseChange;
        [SerializeField] private Sprite[] death;
        [Header("受击反馈")]
        [SerializeField, Min(0.05f)] private float damageFlashDuration = 0.34f;
        [SerializeField, Min(0f)] private float damageShakeDistance = 0.075f;

        private readonly SpriteFramePlayer framePlayer = new();
        private BossBrain brain;
        private LocalTimeController localTime;
        private HealthSystem health;
        private float hurtRemaining;
        private Vector3 baseVisualPosition;
        private Vector3 baseVisualScale;

        private void Awake()
        {
            brain = GetComponent<BossBrain>();
            localTime = GetComponent<LocalTimeController>();
            health = GetComponent<HealthSystem>();
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
            if (targetRenderer == null || brain == null)
            {
                return;
            }

            float delta = localTime != null ? localTime.LocalDeltaTime : Time.deltaTime;
            hurtRemaining = Mathf.Max(0f, hurtRemaining - delta);
            (Sprite[] frames, bool loop) = ResolveAnimation();
            // 素材的默认朝向由接入场景指定，旧场景保留原有朝左设置。
            targetRenderer.flipX = spriteFacesRight
                ? brain.FacingDirection < 0 : brain.FacingDirection > 0;
            targetRenderer.color = ResolveColor();
            framePlayer.Play(targetRenderer, frames, framesPerSecond, loop, delta);
            ApplyTransformFeedback();
        }

        private Color ResolveColor()
        {
            if (hurtRemaining > 0f)
            {
                return Mathf.FloorToInt(Time.unscaledTime * 30f) % 2 == 0
                    ? Color.white
                    : new Color(1f, 0.08f, 0.05f, 1f);
            }

            if (!brain.IsAttackTelegraphing)
            {
                return Color.white;
            }

            Color warning = brain.CurrentAttack switch
            {
                BossAttackType.Sweep => new Color(1f, 0.2f, 0.15f),
                BossAttackType.LeapSlam => new Color(1f, 0.82f, 0.12f),
                BossAttackType.ProjectileVolley => new Color(0.15f, 0.78f, 1f),
                BossAttackType.TimeAnchor => new Color(0.78f, 0.25f, 1f),
                _ => Color.white
            };
            float pulse = 0.78f + Mathf.Sin(Time.unscaledTime * 20f) * 0.18f;
            return Color.Lerp(Color.white, warning, pulse);
        }

        private void ApplyTransformFeedback()
        {
            if (hurtRemaining > 0f)
            {
                float strength = Mathf.Clamp01(hurtRemaining / damageFlashDuration);
                float shake = Mathf.Sin(Time.unscaledTime * 105f)
                    * damageShakeDistance * strength;
                targetRenderer.transform.localPosition = baseVisualPosition
                    + new Vector3(shake, 0f, 0f);
                targetRenderer.transform.localScale = new Vector3(
                    baseVisualScale.x * (1f + 0.12f * strength),
                    baseVisualScale.y * (1f - 0.08f * strength),
                    baseVisualScale.z);
                return;
            }

            targetRenderer.transform.localPosition = baseVisualPosition;
            float telegraphPulse = brain.IsAttackTelegraphing
                ? 1f + (0.035f + Mathf.Sin(Time.unscaledTime * 20f) * 0.018f)
                : 1f;
            targetRenderer.transform.localScale = baseVisualScale * telegraphPulse;
        }

        private void RestoreVisualTransform()
        {
            if (targetRenderer == null)
            {
                return;
            }
            targetRenderer.transform.localPosition = baseVisualPosition;
            targetRenderer.transform.localScale = baseVisualScale;
            targetRenderer.color = Color.white;
        }

        private (Sprite[] frames, bool loop) ResolveAnimation()
        {
            if (brain.Phase == BossPhase.Defeated)
            {
                return (death, false);
            }

            if (brain.IsPhaseTransitioning)
            {
                return (phaseChange, false);
            }

            if (hurtRemaining > 0f)
            {
                return (hurt, false);
            }

            return brain.CurrentAttack switch
            {
                BossAttackType.Sweep => (brain.IsAttackTelegraphing
                    ? sweepTelegraph
                    : sweepAttack, false),
                BossAttackType.LeapSlam => (brain.HasResolvedCurrentAttack ? slam : jump, false),
                BossAttackType.ProjectileVolley => (shoot, false),
                BossAttackType.ClockFan => (shoot, false),
                BossAttackType.Rain => (shoot, false),
                BossAttackType.Crossfire => (shoot, false),
                BossAttackType.TimeAnchor => (timeAnchor, false),
                _ => (idle, true)
            };
        }

        private void HandleDamaged(DamageInfo damageInfo)
        {
            hurtRemaining = damageFlashDuration;
        }
    }
}
