using UnityEngine;

namespace TimeMetroidvania.GameTime
{
    /// <summary>
    /// 记录单次时停的成本、最大时长、剩余时长与提前结束返还。
    /// </summary>
    public sealed class TimeStopSession
    {
        public TimeStopSession(float startCost, float maxDuration)
        {
            StartCost = Mathf.Max(0f, startCost);
            MaxDuration = Mathf.Max(0.01f, maxDuration);
            RemainingDuration = MaxDuration;
        }

        public float StartCost { get; }
        public float MaxDuration { get; }
        public float RemainingDuration { get; private set; }
        public bool IsExpired => RemainingDuration <= 0f;

        public void Advance(float unscaledDeltaTime)
        {
            if (unscaledDeltaTime <= 0f)
            {
                return;
            }

            RemainingDuration = Mathf.Max(0f, RemainingDuration - unscaledDeltaTime);
        }

        public void ConsumeAttackTime(float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            RemainingDuration = Mathf.Max(0f, RemainingDuration - duration);
        }

        public int CalculateEarlyEndRefund()
        {
            float ratio = RemainingDuration / MaxDuration;
            return Mathf.FloorToInt(StartCost * Mathf.Clamp01(ratio));
        }
    }
}
