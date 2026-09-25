using TimeMetroidvania.GameTime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TimeMetroidvania.Player
{
    /// <summary>
    /// 处理 J 长按时缓、I 短按时停以及时停攻击扣除剩余时间。
    /// </summary>
    [RequireComponent(typeof(PlayerCombat), typeof(PlayerHealing), typeof(TimeGauge))]
    public sealed class PlayerTimeAbilities : MonoBehaviour
    {
        [Header("输入")]
        [SerializeField] private InputActionReference attackAction;

        [Header("服务")]
        [SerializeField] private WorldTimeService worldTime;

        [Header("时缓")]
        [SerializeField, Min(0.01f)] private float slowChargeDuration = 1f;

        [Header("时停")]
        [SerializeField, Min(0f)] private float attackTimeStopCost = 0.6f;

        private PlayerCombat combat;
        private PlayerHealing healing;
        private TimeGauge timeGauge;
        private bool slowCharging;
        private float slowChargeElapsed;

        public float SlowChargeProgress => slowCharging
            ? Mathf.Clamp01(slowChargeElapsed / slowChargeDuration)
            : 0f;
        public bool IsSlowReady => slowCharging && slowChargeElapsed >= slowChargeDuration;

        private void Awake()
        {
            combat = GetComponent<PlayerCombat>();
            healing = GetComponent<PlayerHealing>();
            timeGauge = GetComponent<TimeGauge>();
        }

        private void OnEnable()
        {
            if (attackAction == null || worldTime == null)
            {
                Debug.LogError("PlayerTimeAbilities 未完整配置 Attack 输入或 WorldTimeService。", this);
                enabled = false;
                return;
            }

            attackAction.action.Enable();
            healing.ShortPressRequested += HandleShortTimePress;
            combat.AttackCompleted += HandleAttackCompleted;
            worldTime.ModeChanged += HandleWorldTimeChanged;
        }

        private void OnDisable()
        {
            attackAction?.action.Disable();
            if (healing != null)
            {
                healing.ShortPressRequested -= HandleShortTimePress;
            }

            if (combat != null)
            {
                combat.AttackCompleted -= HandleAttackCompleted;
            }

            if (worldTime != null)
            {
                worldTime.ModeChanged -= HandleWorldTimeChanged;
            }

            CancelSlowCharge();
        }

        private void Update()
        {
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

        private void BeginSlowCharge()
        {
            if (worldTime.CurrentMode != WorldTimeMode.Normal)
            {
                CancelSlowCharge();
                return;
            }

            slowCharging = true;
            slowChargeElapsed = 0f;
        }

        private void HandleShortTimePress()
        {
            if (!worldTime.TryStartTimeStop(timeGauge))
            {
                Debug.Log("无法开启时停：能力未解锁、资源不足或已在时停中。", this);
            }
        }

        private void HandleAttackCompleted()
        {
            if (worldTime.CurrentMode == WorldTimeMode.Stopped)
            {
                worldTime.ConsumeTimeStopAttack(attackTimeStopCost);
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
