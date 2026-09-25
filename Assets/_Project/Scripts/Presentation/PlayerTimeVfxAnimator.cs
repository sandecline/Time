using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Presentation
{
    /// <summary>
    /// 播放玩家时缓蓄力、治疗、冲刺和死亡倒流的时间特效。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerTimeVfxAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField, Min(1f)] private float framesPerSecond = 12f;
        [SerializeField] private Sprite[] slowCharge;
        [SerializeField] private Sprite[] slowReady;
        [SerializeField] private Sprite[] heal;
        [SerializeField] private Sprite[] dash;
        [SerializeField] private Sprite[] deathRewind;

        private readonly SpriteFramePlayer framePlayer = new();
        private PlayerStateMachine stateMachine;
        private PlayerTimeAbilities timeAbilities;

        private void Awake()
        {
            stateMachine = GetComponent<PlayerStateMachine>();
            timeAbilities = GetComponent<PlayerTimeAbilities>();
            targetRenderer ??= transform.Find("TimeVfx")?.GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            framePlayer.Reset();
        }

        private void LateUpdate()
        {
            if (targetRenderer == null || stateMachine == null)
            {
                return;
            }

            Sprite[] frames = null;
            bool loop = true;
            switch (stateMachine.CurrentState)
            {
                case PlayerState.Dead:
                    frames = deathRewind;
                    loop = false;
                    break;
                case PlayerState.Dash:
                    frames = dash;
                    break;
                case PlayerState.Healing:
                    frames = heal;
                    break;
                default:
                    if (timeAbilities != null && timeAbilities.SlowChargeProgress > 0f)
                    {
                        frames = timeAbilities.IsSlowReady ? slowReady : slowCharge;
                    }
                    break;
            }

            targetRenderer.enabled = frames != null && frames.Length > 0;
            if (!targetRenderer.enabled)
            {
                framePlayer.Reset();
                return;
            }

            targetRenderer.color = Color.white;
            framePlayer.Play(
                targetRenderer,
                frames,
                framesPerSecond,
                loop,
                Time.unscaledDeltaTime);
        }
    }
}
