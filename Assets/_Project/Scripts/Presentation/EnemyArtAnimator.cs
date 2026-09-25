using TimeMetroidvania.Enemy;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Presentation
{
    /// <summary>
    /// 将普通敌人状态映射到对应的原创逐帧动画。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyArtAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField, Min(1f)] private float framesPerSecond = 10f;
        [SerializeField] private Sprite[] idle;
        [SerializeField] private Sprite[] patrol;
        [SerializeField] private Sprite[] prepareAttack;
        [SerializeField] private Sprite[] attack;
        [SerializeField] private Sprite[] hurt;
        [SerializeField] private Sprite[] death;
        [Header("受击反馈")]
        [SerializeField, Min(0.05f)] private float damageFlashDuration = 0.32f;
        [SerializeField, Min(0f)] private float damageShakeDistance = 0.065f;

        private readonly SpriteFramePlayer framePlayer = new();
        private EnemyBrain brain;
        private LocalTimeController localTime;
        private HealthSystem health;
        private Vector3 baseVisualPosition;
        private Vector3 baseVisualScale;
        private float damageFeedbackRemaining;

        private void Awake()
        {
            brain = GetComponent<EnemyBrain>();
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
            if (targetRenderer == null || brain == null)
            {
                return;
            }

            float delta = localTime != null ? localTime.LocalDeltaTime : Time.deltaTime;
            damageFeedbackRemaining = Mathf.Max(
                0f,
                damageFeedbackRemaining - Time.unscaledDeltaTime);
            if (brain.CurrentState == EnemyState.Attack)
            {
                PlayAttack(delta);
            }
            else
            {
                (Sprite[] frames, bool loop) = brain.CurrentState switch
                {
                    EnemyState.Dead => (death, false),
                    EnemyState.Hurt => (hurt, false),
                    EnemyState.Alert => (idle != null && idle.Length > 0 ? idle : patrol, true),
                    _ => (patrol != null && patrol.Length > 0 ? patrol : idle, true)
                };

                targetRenderer.flipX = brain.FacingDirection < 0;
                targetRenderer.color = Color.white;
                framePlayer.Play(targetRenderer, frames, framesPerSecond, loop, delta);
            }
            ApplyDamageFeedback();
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
            bool whiteFlash = Mathf.FloorToInt(Time.unscaledTime * 30f) % 2 == 0;
            targetRenderer.color = whiteFlash
                ? Color.white
                : new Color(1f, 0.1f, 0.06f, 1f);
            float shake = Mathf.Sin(Time.unscaledTime * 100f)
                * damageShakeDistance * strength;
            targetRenderer.transform.localPosition = baseVisualPosition
                + new Vector3(shake, 0f, 0f);
            targetRenderer.transform.localScale = new Vector3(
                baseVisualScale.x * (1f + 0.16f * strength),
                baseVisualScale.y * (1f - 0.12f * strength),
                baseVisualScale.z);
        }

        private void RestoreVisualTransform()
        {
            if (targetRenderer == null)
            {
                return;
            }
            targetRenderer.transform.localPosition = baseVisualPosition;
            targetRenderer.transform.localScale = baseVisualScale;
        }

        private void PlayAttack(float delta)
        {
            targetRenderer.flipX = brain.FacingDirection < 0;
            if (brain.IsAttackTelegraphing)
            {
                float pulse = 0.65f + Mathf.Sin(Time.unscaledTime * 24f) * 0.2f;
                Color warning = brain.Kind switch
                {
                    EnemyKind.Guard => new Color(1f, 0.22f, 0.15f),
                    EnemyKind.ClockworkBug => new Color(1f, 0.82f, 0.12f),
                    _ => new Color(0.18f, 0.78f, 1f)
                };
                targetRenderer.color = Color.Lerp(Color.white, warning, pulse);
                Sprite[] telegraph = prepareAttack != null && prepareAttack.Length > 0
                    ? prepareAttack
                    : attack;
                framePlayer.PlayNormalized(
                    targetRenderer,
                    telegraph,
                    brain.AttackTelegraphProgress);
                return;
            }

            targetRenderer.color = Color.white;
            if (brain.Kind == EnemyKind.ClockArcher)
            {
                framePlayer.PlayNormalized(
                    targetRenderer,
                    attack,
                    brain.AttackRecoveryProgress);
                return;
            }

            Sprite[] impact = prepareAttack != null && prepareAttack.Length > 0
                ? prepareAttack
                : attack;
            framePlayer.PlayNormalized(targetRenderer, impact, 1f);
        }
    }
}
