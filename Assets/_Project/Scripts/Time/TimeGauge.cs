using System;
using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.GameTime
{
    /// <summary>
    /// 管理时间槽的战斗获取、持续消耗、自动上弦和死亡清空。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TimeGauge : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxValue = 100f;
        [SerializeField, Min(0f)] private float initialValue;

        [Header("自动上弦")]
        [SerializeField, Min(0f)] private float automaticRecoveryCap = 20f;
        [SerializeField, Min(0f)] private float automaticRecoveryDelay = 1f;
        [SerializeField, Min(0f)] private float automaticRecoveryRate = 5f;

        private HealthSystem health;
        private PlayerHealing healing;
        private WorldTimeService worldTime;
        private float currentValue;
        private float lastSuccessfulHitUnscaledTime;
        private bool initialized;

        public event Action<float, float> ValueChanged;

        public float CurrentValue
        {
            get
            {
                EnsureInitialized();
                return currentValue;
            }
        }

        public float MaxValue => maxValue;
        public float NormalizedValue => MaxValue <= 0f ? 0f : CurrentValue / MaxValue;
        public float AutomaticRecoveryCap => Mathf.Min(automaticRecoveryCap, maxValue);
        public float AutomaticRecoveryDelay => automaticRecoveryDelay;
        public float AutomaticRecoveryRate => automaticRecoveryRate;

        private void Awake()
        {
            EnsureInitialized();
            ResolveDependencies();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            ResolveDependencies();
            if (health != null)
            {
                health.Died += Clear;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= Clear;
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0f)
            {
                return;
            }

            ResolveDependencies();
            bool recoveryBlocked = worldTime != null
                && worldTime.CurrentMode == WorldTimeMode.Stopped;
            recoveryBlocked |= healing != null && healing.IsHealing;
            AdvanceAutomaticRecovery(
                Time.unscaledDeltaTime,
                Time.unscaledTime,
                recoveryBlocked);
        }

        public float Gain(float amount)
        {
            EnsureInitialized();
            if (amount <= 0f)
            {
                return 0f;
            }

            float previousValue = currentValue;
            currentValue = Mathf.Min(maxValue, currentValue + amount);
            NotifyIfChanged(previousValue);
            return currentValue - previousValue;
        }

        public float GainFromSuccessfulHit(float amount)
        {
            NotifySuccessfulHit();
            return Gain(amount);
        }

        public void NotifySuccessfulHit()
        {
            EnsureInitialized();
            lastSuccessfulHitUnscaledTime = Time.unscaledTime;
        }

        public bool TrySpend(float amount)
        {
            EnsureInitialized();
            if (amount < 0f || currentValue + Mathf.Epsilon < amount)
            {
                return false;
            }

            float previousValue = currentValue;
            currentValue = Mathf.Max(0f, currentValue - amount);
            NotifyIfChanged(previousValue);
            return true;
        }

        public float SpendUpTo(float amount)
        {
            EnsureInitialized();
            if (amount <= 0f)
            {
                return 0f;
            }

            float previousValue = currentValue;
            float spent = Mathf.Min(currentValue, amount);
            currentValue -= spent;
            NotifyIfChanged(previousValue);
            return spent;
        }

        public void Fill()
        {
            Gain(maxValue - CurrentValue);
        }

        public void Clear()
        {
            EnsureInitialized();
            float previousValue = currentValue;
            currentValue = 0f;
            lastSuccessfulHitUnscaledTime = Time.unscaledTime;
            NotifyIfChanged(previousValue);
        }

        internal void AdvanceAutomaticRecovery(
            float unscaledDeltaTime,
            float currentUnscaledTime,
            bool recoveryBlocked)
        {
            EnsureInitialized();
            if (recoveryBlocked || unscaledDeltaTime <= 0f
                || currentValue >= AutomaticRecoveryCap
                || currentUnscaledTime - lastSuccessfulHitUnscaledTime
                    < automaticRecoveryDelay)
            {
                return;
            }

            float previousValue = currentValue;
            currentValue = Mathf.Min(
                AutomaticRecoveryCap,
                currentValue + automaticRecoveryRate * unscaledDeltaTime);
            NotifyIfChanged(previousValue);
        }

        private void ResolveDependencies()
        {
            health ??= GetComponent<HealthSystem>();
            healing ??= GetComponent<PlayerHealing>();
            worldTime ??= FindFirstObjectByType<WorldTimeService>();
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            health = GetComponent<HealthSystem>();
            currentValue = Mathf.Clamp(initialValue, 0f, maxValue);
            lastSuccessfulHitUnscaledTime = Time.unscaledTime;
            initialized = true;
        }

        private void NotifyIfChanged(float previousValue)
        {
            if (!Mathf.Approximately(previousValue, currentValue))
            {
                ValueChanged?.Invoke(currentValue, maxValue);
            }
        }

        private void OnValidate()
        {
            maxValue = Mathf.Max(1f, maxValue);
            initialValue = Mathf.Clamp(initialValue, 0f, maxValue);
            automaticRecoveryCap = Mathf.Clamp(automaticRecoveryCap, 0f, maxValue);
            automaticRecoveryDelay = Mathf.Max(0f, automaticRecoveryDelay);
            automaticRecoveryRate = Mathf.Max(0f, automaticRecoveryRate);
            if (initialized)
            {
                currentValue = Mathf.Clamp(currentValue, 0f, maxValue);
            }
        }
    }
}