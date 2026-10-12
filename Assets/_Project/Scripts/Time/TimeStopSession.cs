using UnityEngine;

namespace TimeMetroidvania.GameTime
{
    /// <summary>
    /// 记录单次时停实际消耗的能量，以及恢复时间时待结算的时间回声。
    /// </summary>
    public sealed class TimeStopSession
    {
        public float EnergySpent { get; private set; }
        public float PendingEcho { get; private set; }

        public void RecordEnergySpent(float amount)
        {
            if (amount > 0f)
            {
                EnergySpent += amount;
            }
        }

        public void AddEcho(float normalGain, float echoRatio)
        {
            if (normalGain > 0f && echoRatio > 0f)
            {
                PendingEcho += normalGain * echoRatio;
            }
        }

        public float CalculateEcho(float maximumSpentRatio)
        {
            float maximum = EnergySpent * Mathf.Clamp01(maximumSpentRatio);
            return Mathf.Min(PendingEcho, maximum);
        }
    }
}