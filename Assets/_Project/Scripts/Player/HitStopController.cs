using UnityEngine;

namespace TimeMetroidvania.Player
{
    /// <summary>
    /// 使用真实时间管理单个对象的短暂停顿，不修改全局 Time.timeScale。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitStopController : MonoBehaviour
    {
        private float remaining;

        public bool IsPaused => remaining > 0f;
        public float Remaining => remaining;

        private void Update()
        {
            Advance(Time.unscaledDeltaTime);
        }

        public void Request(float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            remaining = Mathf.Max(remaining, duration);
        }

        public float ScaleDeltaTime(float deltaTime)
        {
            return IsPaused ? 0f : deltaTime;
        }

        internal void Advance(float unscaledDeltaTime)
        {
            remaining = Mathf.Max(0f, remaining - Mathf.Max(0f, unscaledDeltaTime));
        }

        private void OnDisable()
        {
            remaining = 0f;
        }
    }
}
