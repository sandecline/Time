using TimeMetroidvania.GameTime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TimeMetroidvania.Player
{
    /// <summary>
    /// 处理 J 长按时缓，以及 I 按下瞬间开启或关闭时停。
    /// </summary>
    [RequireComponent(typeof(PlayerCombat), typeof(PlayerHealing), typeof(TimeGauge))]
    [RequireComponent(typeof(PlayerStateMachine), typeof(HealthSystem))]
    public sealed class PlayerTimeAbilities : MonoBehaviour
    {
        [Header("输入")]
        [SerializeField] private InputActionReference attackAction;
        [SerializeField] private InputActionReference timeAbilityAction;

        [Header("服务")]
        [SerializeField] private WorldTimeService worldTime;

        [Header("时缓")]
        [SerializeField, Min(0.01f)] private float slowChargeDuration = 1f;

        private PlayerStateMachine stateMachine;
        private HealthSystem health;
        private TimeGauge timeGauge;
        private bool slowCharging;
        private float slowChargeElapsed;

        public float SlowChargeProgress => slowCharging
            ? Mathf.Clamp01(slowChargeElapsed / slowChargeDuration)
            : 0f;
        public bool IsSlowReady => slowCharging && slowChargeElapsed >= slowChargeDuration;

        private void Awake()
        {
            stateMachine = GetComponent<PlayerStateMachine>();
            health = GetComponent<HealthSystem>();
            timeGauge = GetComponent<TimeGauge>();
        }

        private void OnEnable()
        {
            if (attackAction == null || timeAbilityAction == null || worldTime == null)
            {
                Debug.LogError("PlayerTimeAbilities 未完整配置 Attack、TimeAbility 或 WorldTimeService。", this);
                enabled = false;
                return;
            }

            attackAction.action.Enable();
            timeAbilityAction.action.Enable();
            health.Damaged += HandleDamaged;
            worldTime.ModeChanged += HandleWorldTimeChanged;
        }

        private void OnDisable()
        {
            attackAction?.action.Disable();
            timeAbilityAction?.action.Disable();
            if (health != null)
            {
                health.Damaged -= HandleDamaged;
            }

            if (worldTime != null)
            {
                worldTime.ModeChanged -= HandleWorldTimeChanged;
            }

            CancelSlowCharge();
        }

        private void Update()
        {
            if (Time.timeScale <= 0f)
            {
                return;
            }

            if (timeAbilityAction.action.WasPressedThisFrame())
            {
                ToggleTimeStop();
            }

            if (attackAction.action.WasPressedThisFrame())
            {
                BeginSlowCharge();
            }

            if (slowCharging && attackAction.action.IsPressed())
            {
                slowChargeElapsed += Time.unscaledDeltaTime;
            }

            if (!attackAction.action.WasReleasedThisFrame())
            {
                return;
            }

            if (ShouldTriggerSlow(
                    slowChargeElapsed,
                    slowChargeDuration,
                    slowCharging,
                    worldTime.CurrentMode))
            {
                worldTime.TryStartSlow();
            }

            CancelSlowCharge();
        }

        internal static bool ShouldTriggerSlow(
            float heldDuration,
            float threshold,
            bool wasCharging,
            WorldTimeMode mode)
        {
            return wasCharging
                && heldDuration >= threshold
                && mode == WorldTimeMode.Normal;
        }

        private void ToggleTimeStop()
        {
            if (stateMachine.CurrentState is PlayerState.Healing
                or PlayerState.Hurt
                or PlayerState.Dead)
            {
                return;
            }

            if (!worldTime.ToggleTimeStop(timeGauge))
            {
                Debug.Log("无法切换时停：能力未解锁或时间槽为空。", this);
            }
        }

        private void BeginSlowCharge()
        {
            if (worldTime.CurrentMode != WorldTimeMode.Normal
                || stateMachine.CurrentState is not (PlayerState.Normal or PlayerState.Attack))
            {
                CancelSlowCharge();
                return;
            }

            slowCharging = true;
            slowChargeElapsed = 0f;
        }

        private void HandleDamaged(DamageInfo damageInfo)
        {
            if (worldTime.CurrentMode == WorldTimeMode.Stopped)
            {
                worldTime.EndTimeStopEarly();
            }
        }

        private void HandleWorldTimeChanged(WorldTimeMode previousMode, WorldTimeMode nextMode)
        {
            if (nextMode != WorldTimeMode.Normal)
            {
                CancelSlowCharge();
            }
        }

        private void CancelSlowCharge()
        {
            slowCharging = false;
            slowChargeElapsed = 0f;
        }
    }
}