using UnityEngine;

namespace TimeMetroidvania.Presentation
{
    /// <summary>
    /// 轻量逐帧播放器，由现有玩法状态驱动，不参与碰撞与位移。
    /// </summary>
    internal sealed class SpriteFramePlayer
    {
        private Sprite[] currentFrames;
        private float elapsed;

        public void Play(
            SpriteRenderer target,
            Sprite[] frames,
            float framesPerSecond,
            bool loop,
            float deltaTime)
        {
            if (target == null || frames == null || frames.Length == 0)
            {
                return;
            }

            if (!ReferenceEquals(currentFrames, frames))
            {
                currentFrames = frames;
                elapsed = 0f;
            }
            else
            {
                elapsed += Mathf.Max(0f, deltaTime);
            }

            int frameIndex = Mathf.FloorToInt(elapsed * Mathf.Max(0.01f, framesPerSecond));
            frameIndex = loop
                ? frameIndex % frames.Length
                : Mathf.Min(frameIndex, frames.Length - 1);
            target.sprite = frames[frameIndex];
        }

        public void PlayNormalized(
            SpriteRenderer target,
            Sprite[] frames,
            float normalizedTime)
        {
            if (target == null || frames == null || frames.Length == 0)
            {
                return;
            }

            currentFrames = frames;
            elapsed = 0f;
            int frameIndex = Mathf.Min(
                Mathf.FloorToInt(Mathf.Clamp01(normalizedTime) * frames.Length),
                frames.Length - 1);
            target.sprite = frames[frameIndex];
        }

        public void Reset()
        {
            currentFrames = null;
            elapsed = 0f;
        }
    }
}
