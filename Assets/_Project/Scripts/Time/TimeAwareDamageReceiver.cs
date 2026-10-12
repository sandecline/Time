using System;
using TimeMetroidvania.Boss;
using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.GameTime
{
    /// <summary>
    /// 时停期间缓存世界对象受到的伤害，并在时间恢复时合并结算。
    /// </summary>
    [RequireComponent(typeof(HealthSystem), typeof(LocalTimeController))]
    public sealed class TimeAwareDamageReceiver : MonoBehaviour, IDamageable
    {
        [SerializeField] private WorldTimeService worldTime;
        [SerializeField, Min(0)] private int maxPendingDamage;
        [SerializeField, Range(0f, 1f)] private float bossPendingDamageFraction = 0.25f;

        private HealthSystem health;
        private LocalTimeController localTime;
        private int pendingDamage;
        private GameObject pendingSource;
        private Vector2 pendingHitPoint;
        private Vector2 pendingKnockback;
        private LineRenderer fractureVisual;
        private Material fractureMaterial;

        public event Action<int, int> PendingDamageChanged;

        public int PendingDamage => pendingDamage;
        public int MaxPendingDamage => ResolvePendingDamageLimit();
        public bool IsPendingDamageLimitReached => MaxPendingDamage > 0
            && pendingDamage >= MaxPendingDamage;

        public void ClearPendingDamage()
        {
            pendingDamage = 0;
            pendingSource = null;
            pendingHitPoint = Vector2.zero;
            pendingKnockback = Vector2.zero;
            UpdateFractureVisual();
            PendingDamageChanged?.Invoke(0, MaxPendingDamage);
        }

        private void Awake()
        {
            health = GetComponent<HealthSystem>();
            localTime = GetComponent<LocalTimeController>();
        }

        private void OnEnable()
        {
            worldTime ??= FindFirstObjectByType<WorldTimeService>();
            if (worldTime != null)
            {
                worldTime.ModeChanged += HandleModeChanged;
            }
        }

        private void OnDisable()
        {
            if (worldTime != null)
            {
                worldTime.ModeChanged -= HandleModeChanged;
            }
        }

        private void OnDestroy()
        {
            if (fractureMaterial != null)
            {
                Destroy(fractureMaterial);
            }
        }

        public bool TryTakeDamage(DamageInfo damageInfo)
        {
            if (damageInfo.Amount <= 0 || health.IsDead)
            {
                return false;
            }

            if (worldTime != null
                && worldTime.CurrentMode == WorldTimeMode.Stopped
                && !localTime.IsTimeResistant)
            {
                int limit = ResolvePendingDamageLimit();
                int acceptedDamage = limit > 0
                    ? Mathf.Min(damageInfo.Amount, limit - pendingDamage)
                    : damageInfo.Amount;
                if (acceptedDamage <= 0)
                {
                    return false;
                }

                pendingDamage += acceptedDamage;
                pendingSource = damageInfo.Source;
                pendingHitPoint = damageInfo.HitPoint;
                pendingKnockback += damageInfo.Knockback;
                UpdateFractureVisual();
                PendingDamageChanged?.Invoke(pendingDamage, limit);
                return true;
            }

            return health.TryTakeDamage(damageInfo);
        }

        private int ResolvePendingDamageLimit()
        {
            health ??= GetComponent<HealthSystem>();
            if (GetComponent<BossBrain>() != null && health != null)
            {
                return Mathf.Max(1,
                    Mathf.CeilToInt(health.MaxHealth * bossPendingDamageFraction));
            }

            return maxPendingDamage;
        }

        private void HandleModeChanged(WorldTimeMode previousMode, WorldTimeMode nextMode)
        {
            if (previousMode == WorldTimeMode.Stopped && nextMode != WorldTimeMode.Stopped)
            {
                FlushBufferedDamage();
            }
        }

        private void FlushBufferedDamage()
        {
            if (pendingDamage <= 0)
            {
                return;
            }

            DamageInfo combinedDamage = new(
                pendingSource,
                pendingDamage,
                pendingHitPoint,
                pendingKnockback);
            ClearPendingDamage();
            health.TryTakeDamage(combinedDamage);
        }

        private void UpdateFractureVisual()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (pendingDamage <= 0)
            {
                if (fractureVisual != null)
                {
                    fractureVisual.enabled = false;
                }
                return;
            }

            EnsureFractureVisual();
            if (fractureVisual == null)
            {
                return;
            }

            int limit = ResolvePendingDamageLimit();
            float ratio = limit > 0
                ? Mathf.Clamp01((float)pendingDamage / limit)
                : Mathf.Clamp01(pendingDamage / 5f);
            Color color = Color.Lerp(
                new Color(0.2f, 0.85f, 1f, 0.9f),
                new Color(0.85f, 0.2f, 1f, 1f),
                ratio);
            fractureVisual.startColor = color;
            fractureVisual.endColor = color;
            fractureVisual.widthMultiplier = Mathf.Lerp(0.045f, 0.085f, ratio);
            fractureVisual.enabled = true;
        }

        private void EnsureFractureVisual()
        {
            if (fractureVisual != null)
            {
                return;
            }

            Shader shader = Shader.Find("Sprites/Default")
                ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
            {
                return;
            }

            GameObject visualObject = new("TimeFractureVisual");
            visualObject.transform.SetParent(transform, false);
            fractureVisual = visualObject.AddComponent<LineRenderer>();
            fractureMaterial = new Material(shader);
            fractureVisual.material = fractureMaterial;
            fractureVisual.useWorldSpace = false;
            fractureVisual.loop = false;
            fractureVisual.positionCount = 6;
            fractureVisual.SetPositions(new[]
            {
                new Vector3(-0.35f, 0.55f, -0.1f),
                new Vector3(-0.08f, 0.22f, -0.1f),
                new Vector3(-0.2f, 0.04f, -0.1f),
                new Vector3(0.12f, -0.12f, -0.1f),
                new Vector3(0.02f, -0.34f, -0.1f),
                new Vector3(0.36f, -0.58f, -0.1f)
            });
            SpriteRenderer sprite = transform.Find("ArtVisual")?.GetComponent<SpriteRenderer>()
                ?? GetComponent<SpriteRenderer>();
            if (sprite != null)
            {
                fractureVisual.sortingLayerID = sprite.sortingLayerID;
                fractureVisual.sortingOrder = sprite.sortingOrder + 5;
            }
            fractureVisual.enabled = false;
        }
    }
}