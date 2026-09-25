using System;
using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.GameTime
{
    /// <summary>
    /// 管理时间槽的获取、消耗、上限和死亡清空，不进行自然恢复。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TimeGauge : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxValue = 100f;
        [SerializeField, Min(0f)] private float initialValue;

        private HealthSystem health;
        private float currentValue;
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

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
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

        public void Clear()
        {
            EnsureInitialized();
            float previousValue = currentValue;
            currentValue = 0f;
            NotifyIfChanged(previousValue);
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            health = GetComponent<HealthSystem>();
            currentValue = Mathf.Clamp(initialValue, 0f, maxValue);
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
            if (initialized)
            {
                currentValue = Mathf.Clamp(currentValue, 0f, maxValue);
            }
        }
    }
}
