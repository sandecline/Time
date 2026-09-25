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

        private HealthSystem health;
        private LocalTimeController localTime;
        private int pendingDamage;
        private GameObject pendingSource;
        private Vector2 pendingHitPoint;
        private Vector2 pendingKnockback;

        public int PendingDamage => pendingDamage;
        public int MaxPendingDamage => maxPendingDamage;

        public void ClearPendingDamage()
        {
            pendingDamage = 0;
            pendingSource = null;
            pendingHitPoint = Vector2.zero;
            pendingKnockback = Vector2.zero;
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
                int acceptedDamage = maxPendingDamage > 0
                    ? Mathf.Min(damageInfo.Amount, maxPendingDamage - pendingDamage)
                    : damageInfo.Amount;
                if (acceptedDamage <= 0)
                {
                    return false;
                }

                pendingDamage += acceptedDamage;
                pendingSource = damageInfo.Source;
                pendingHitPoint = damageInfo.HitPoint;
                pendingKnockback += damageInfo.Knockback;
                return true;
            }

            return health.TryTakeDamage(damageInfo);
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
            pendingDamage = 0;
            pendingSource = null;
            pendingHitPoint = Vector2.zero;
            pendingKnockback = Vector2.zero;
            health.TryTakeDamage(combinedDamage);
        }
    }
}
