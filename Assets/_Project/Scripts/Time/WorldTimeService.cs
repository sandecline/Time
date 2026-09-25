using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimeMetroidvania.GameTime
{
    /// <summary>
    /// 管理正常、时缓和时停状态，并向已注册世界对象分发独立时间倍率。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldTimeService : MonoBehaviour
    {
        [Header("时缓")]
        [SerializeField, Range(0.01f, 1f)] private float slowScale = 0.35f;
        [SerializeField, Min(0.01f)] private float slowDuration = 3f;

        [Header("时停")]
        [SerializeField, Min(0f)] private float timeStopCost = 60f;
        [SerializeField, Min(0.01f)] private float baseTimeStopDuration = 5f;
        [SerializeField, Min(0.01f)] private float upgradedTimeStopDuration = 7f;
        [SerializeField] private bool timeStopUnlocked;
        [SerializeField] private bool durationUpgradeUnlocked;

        private readonly HashSet<ITimeControllable> controllables = new();
        private TimeGauge activeGauge;
        private TimeStopSession activeSession;
        private float slowRemaining;

        public event Action<WorldTimeMode, WorldTimeMode> ModeChanged;
        public event Action<float, float> TimeStopRemainingChanged;
        public event Action<float> SlowRemainingChanged;

        public WorldTimeMode CurrentMode { get; private set; } = WorldTimeMode.Normal;
        public float CurrentWorldScale => CurrentMode switch
        {
            WorldTimeMode.Slowed => slowScale,
            WorldTimeMode.Stopped => 0f,
            _ => 1f
        };
        public float SlowRemaining => slowRemaining;
        public float TimeStopRemaining => activeSession?.RemainingDuration ?? 0f;
        public float TimeStopMaximum => activeSession?.MaxDuration
            ?? (durationUpgradeUnlocked ? upgradedTimeStopDuration : baseTimeStopDuration);
        public bool IsTimeStopUnlocked => timeStopUnlocked;
        public bool HasDurationUpgrade => durationUpgradeUnlocked;

        private void Update()
        {
            AdvanceUnscaledTime(Time.unscaledDeltaTime);
        }

        public void Register(ITimeControllable controllable)
        {
            if (controllable == null || !controllables.Add(controllable))
            {
                return;
            }

            float scale = controllable.IsTimeResistant ? 1f : CurrentWorldScale;
            controllable.ApplyWorldTime(CurrentMode, scale);
        }

        public void Unregister(ITimeControllable controllable)
        {
            if (controllable != null)
            {
                controllables.Remove(controllable);
            }
        }

        public bool TryStartSlow()
        {
            if (CurrentMode == WorldTimeMode.Stopped)
            {
                return false;
            }

            slowRemaining = slowDuration;
            SetMode(WorldTimeMode.Slowed);
            SlowRemainingChanged?.Invoke(slowRemaining);
            return true;
        }

        public bool TryStartTimeStop(TimeGauge gauge)
        {
            if (!timeStopUnlocked || gauge == null || CurrentMode == WorldTimeMode.Stopped)
            {
                return false;
            }

            if (!gauge.TrySpend(timeStopCost))
            {
                return false;
            }

            float maximum = durationUpgradeUnlocked
                ? upgradedTimeStopDuration
                : baseTimeStopDuration;
            activeGauge = gauge;
            activeSession = new TimeStopSession(timeStopCost, maximum);
            slowRemaining = 0f;
            SetMode(WorldTimeMode.Stopped);
            TimeStopRemainingChanged?.Invoke(activeSession.RemainingDuration, activeSession.MaxDuration);
            return true;
        }

        public bool EndTimeStopEarly()
        {
            if (CurrentMode != WorldTimeMode.Stopped || activeSession == null)
            {
                return false;
            }

            int refund = activeSession.CalculateEarlyEndRefund();
            activeGauge?.Gain(refund);
            FinishTimeStop();
            return true;
        }

        public void ResetToNormal()
        {
            activeSession = null;
            activeGauge = null;
            slowRemaining = 0f;
            SetMode(WorldTimeMode.Normal);
            TimeStopRemainingChanged?.Invoke(0f, TimeStopMaximum);
            SlowRemainingChanged?.Invoke(0f);
        }

        public void ConsumeTimeStopAttack(float duration)
        {
            if (CurrentMode != WorldTimeMode.Stopped || activeSession == null)
            {
                return;
            }

            activeSession.ConsumeAttackTime(duration);
            TimeStopRemainingChanged?.Invoke(activeSession.RemainingDuration, activeSession.MaxDuration);
            if (activeSession.IsExpired)
            {
                FinishTimeStop();
            }
        }

        public void UnlockTimeStop()
        {
            timeStopUnlocked = true;
        }

        public void UnlockDurationUpgrade()
        {
            durationUpgradeUnlocked = true;
        }

        internal void AdvanceUnscaledTime(float unscaledDeltaTime)
        {
            if (unscaledDeltaTime <= 0f)
            {
                return;
            }

            if (CurrentMode == WorldTimeMode.Slowed)
            {
                slowRemaining = Mathf.Max(0f, slowRemaining - unscaledDeltaTime);
                SlowRemainingChanged?.Invoke(slowRemaining);
                if (slowRemaining <= 0f)
                {
                    SetMode(WorldTimeMode.Normal);
                }

                return;
            }

            if (CurrentMode != WorldTimeMode.Stopped || activeSession == null)
            {
                return;
            }

            activeSession.Advance(unscaledDeltaTime);
            TimeStopRemainingChanged?.Invoke(activeSession.RemainingDuration, activeSession.MaxDuration);
            if (activeSession.IsExpired)
            {
                FinishTimeStop();
            }
        }

        private void FinishTimeStop()
        {
            activeSession = null;
            activeGauge = null;
            SetMode(WorldTimeMode.Normal);
            TimeStopRemainingChanged?.Invoke(0f, TimeStopMaximum);
        }

        private void SetMode(WorldTimeMode nextMode)
        {
            if (CurrentMode == nextMode)
            {
                ApplyCurrentMode();
                return;
            }

            WorldTimeMode previousMode = CurrentMode;
            CurrentMode = nextMode;
            ApplyCurrentMode();
            ModeChanged?.Invoke(previousMode, nextMode);
        }

        private void ApplyCurrentMode()
        {
            foreach (ITimeControllable controllable in controllables)
            {
                if (controllable == null)
                {
                    continue;
                }

                float scale = controllable.IsTimeResistant ? 1f : CurrentWorldScale;
                controllable.ApplyWorldTime(CurrentMode, scale);
            }
        }
    }
}
