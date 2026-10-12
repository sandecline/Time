using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimeMetroidvania.GameTime
{
    /// <summary>
    /// 管理正常、时缓和可即时开关的时停，并向世界对象分发独立时间倍率。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldTimeService : MonoBehaviour
    {
        [Header("时缓")]
        [SerializeField, Range(0.01f, 1f)] private float slowScale = 0.35f;
        [SerializeField, Min(0.01f)] private float slowDuration = 3f;

        [Header("时停")]
        [SerializeField, Min(0.01f)] private float timeStopDrainPerSecond = 10f;
        [SerializeField, Min(0.01f)] private float maximumTimeStopDuration = 10f;
        [SerializeField, Range(0f, 1f)] private float timeEchoRatio = 0.25f;
        [SerializeField, Range(0f, 1f)] private float maximumEchoSpentRatio = 0.5f;
        [SerializeField] private bool timeStopUnlocked;

        private readonly HashSet<ITimeControllable> controllables = new();
        private TimeGauge activeGauge;
        private TimeStopSession activeSession;
        private float slowRemaining;

        public event Action<WorldTimeMode, WorldTimeMode> ModeChanged;
        public event Action<float, float> TimeStopRemainingChanged;
        public event Action<float, float> TimeEchoChanged;
        public event Action<float> SlowRemainingChanged;

        public WorldTimeMode CurrentMode { get; private set; } = WorldTimeMode.Normal;
        public float CurrentWorldScale => CurrentMode switch
        {
            WorldTimeMode.Slowed => slowScale,
            WorldTimeMode.Stopped => 0f,
            _ => 1f
        };
        public float SlowRemaining => slowRemaining;
        public float TimeStopRemaining => CurrentMode == WorldTimeMode.Stopped
            && activeGauge != null
            ? activeGauge.CurrentValue / timeStopDrainPerSecond
            : 0f;
        public float TimeStopMaximum => maximumTimeStopDuration;
        public float TimeStopDrainPerSecond => timeStopDrainPerSecond;
        public float PendingTimeEcho => activeSession?.PendingEcho ?? 0f;
        public float SettleableTimeEcho => activeSession?.CalculateEcho(maximumEchoSpentRatio) ?? 0f;
        public float TimeStopEnergySpent => activeSession?.EnergySpent ?? 0f;
        public bool IsTimeStopUnlocked => timeStopUnlocked;
        public bool HasDurationUpgrade => false;

        private void Update()
        {
            if (Time.timeScale > 0f)
            {
                AdvanceUnscaledTime(Time.unscaledDeltaTime);
            }
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

        public bool ToggleTimeStop(TimeGauge gauge)
        {
            return CurrentMode == WorldTimeMode.Stopped
                ? EndTimeStopEarly()
                : TryStartTimeStop(gauge);
        }

        public bool TryStartTimeStop(TimeGauge gauge)
        {
            if (!timeStopUnlocked || gauge == null
                || gauge.CurrentValue <= Mathf.Epsilon
                || CurrentMode == WorldTimeMode.Stopped)
            {
                return false;
            }

            activeGauge = gauge;
            activeSession = new TimeStopSession();
            slowRemaining = 0f;
            SetMode(WorldTimeMode.Stopped);
            TimeStopRemainingChanged?.Invoke(TimeStopRemaining, TimeStopMaximum);
            TimeEchoChanged?.Invoke(0f, 0f);
            return true;
        }

        public bool EndTimeStopEarly()
        {
            if (CurrentMode != WorldTimeMode.Stopped || activeSession == null)
            {
                return false;
            }

            FinishTimeStop(true);
            return true;
        }

        public void EndTimeStopForTransition()
        {
            if (CurrentMode == WorldTimeMode.Stopped)
            {
                FinishTimeStop(true);
            }
        }

        public void ResetToNormal()
        {
            activeSession = null;
            activeGauge = null;
            slowRemaining = 0f;
            SetMode(WorldTimeMode.Normal);
            TimeStopRemainingChanged?.Invoke(0f, TimeStopMaximum);
            TimeEchoChanged?.Invoke(0f, 0f);
            SlowRemainingChanged?.Invoke(0f);
        }

        public bool RegisterTimeEcho(float normalGaugeGain)
        {
            if (CurrentMode != WorldTimeMode.Stopped || activeSession == null
                || normalGaugeGain <= 0f)
            {
                return false;
            }

            activeSession.AddEcho(normalGaugeGain, timeEchoRatio);
            TimeEchoChanged?.Invoke(
                activeSession.PendingEcho,
                activeSession.EnergySpent * maximumEchoSpentRatio);
            return true;
        }

        public void UnlockTimeStop()
        {
            timeStopUnlocked = true;
        }

        // 保留旧接口以兼容历史场景和测试工具；V2 不再提供时停时长升级。
        public void UnlockDurationUpgrade()
        {
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

            if (CurrentMode != WorldTimeMode.Stopped
                || activeSession == null
                || activeGauge == null)
            {
                return;
            }

            float spent = activeGauge.SpendUpTo(
                timeStopDrainPerSecond * unscaledDeltaTime);
            activeSession.RecordEnergySpent(spent);
            TimeStopRemainingChanged?.Invoke(TimeStopRemaining, TimeStopMaximum);
            TimeEchoChanged?.Invoke(
                activeSession.PendingEcho,
                activeSession.EnergySpent * maximumEchoSpentRatio);

            if (activeGauge.CurrentValue <= Mathf.Epsilon)
            {
                FinishTimeStop(true);
            }
        }

        private void FinishTimeStop(bool settleEcho)
        {
            TimeStopSession finishedSession = activeSession;
            TimeGauge finishedGauge = activeGauge;
            activeSession = null;
            activeGauge = null;
            SetMode(WorldTimeMode.Normal);

            if (settleEcho && finishedSession != null && finishedGauge != null)
            {
                finishedGauge.Gain(
                    finishedSession.CalculateEcho(maximumEchoSpentRatio));
            }

            TimeStopRemainingChanged?.Invoke(0f, TimeStopMaximum);
            TimeEchoChanged?.Invoke(0f, 0f);
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

        private void OnValidate()
        {
            timeStopDrainPerSecond = Mathf.Max(0.01f, timeStopDrainPerSecond);
            maximumTimeStopDuration = Mathf.Max(0.01f, maximumTimeStopDuration);
            timeEchoRatio = Mathf.Clamp01(timeEchoRatio);
            maximumEchoSpentRatio = Mathf.Clamp01(maximumEchoSpentRatio);
        }
    }
}