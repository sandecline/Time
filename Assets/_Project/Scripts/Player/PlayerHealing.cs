using System;
using TimeMetroidvania.GameTime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TimeMetroidvania.Player
{
    /// <summary>
    /// 处理 I 键短按/长按分流，以及可被松键、受伤和离地打断的持续治疗。
    /// </summary>
    [RequireComponent(typeof(PlayerStateMachine), typeof(PlayerMovement), typeof(HealthSystem))]
    [RequireComponent(typeof(TimeGauge))]
    public sealed class PlayerHealing : MonoBehaviour
    {
        [Header("输入")]
        [SerializeField] private InputActionReference timeAbilityAction;

        [Header("长按判定")]
        [SerializeField, Min(0.01f)] private float holdThreshold = 0.3f;

        [Header("治疗")]
        [SerializeField, Min(0.01f)] private float healingDuration = 1.5f;
        [SerializeField, Min(0f)] private float healingCost = 60f;
        [SerializeField] private WorldTimeService worldTime;

        private PlayerStateMachine stateMachine;
        private PlayerMovement movement;
        private HealthSystem health;
        private TimeGauge timeGauge;
        private bool inputHeld;
        private bool longPressResolved;
        private float holdElapsed;
        private float healingElapsed;
        private float healingSpent;
        private bool suppressUntilRelease;

        public event Action ShortPressRequested;
        public event Action<float> HealingProgressChanged;
        public event Action<string> HealingUnavailable;

        public bool IsHealing => stateMachine != null && stateMachine.CurrentState == PlayerState.Healing;
        public float HealingProgress => IsHealing
            ? Mathf.Clamp01(healingElapsed / healingDuration)
            : 0f;

        private void Awake()
        {
            stateMachine = GetComponent<PlayerStateMachine>();
            movement = GetComponent<PlayerMovement>();
            health = GetComponent<HealthSystem>();
            timeGauge = GetComponent<TimeGauge>();
        }

        private void OnEnable()
        {
            if (timeAbilityAction == null)
            {
                Debug.LogError("PlayerHealing 未配置 TimeAbility 输入动作。", this);
                enabled = false;
                return;
            }

            health.Damaged += HandleDamaged;
            timeAbilityAction.action.Enable();
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= HandleDamaged;
            }

            timeAbilityAction?.action.Disable();
            CancelHealing();
            ResetPress();
        }

        private void Update()
        {
            if (suppressUntilRelease)
            {
                if (timeAbilityAction.action.WasReleasedThisFrame())
                {
                    suppressUntilRelease = false;
                    ResetPress();
                }

                return;
            }

            if (timeAbilityAction.action.WasPressedThisFrame())
            {
                if (worldTime != null && worldTime.CurrentMode == WorldTimeMode.Stopped)
                {
                    worldTime.EndTimeStopEarly();
                    suppressUntilRelease = true;
                    ResetPress();
                    return;
                }

                BeginPress();
            }

            if (!inputHeld)
            {
                return;
            }

            if (IsHealing)
            {
                UpdateHealing();
            }
            else if (timeAbilityAction.action.IsPressed() && !longPressResolved)
            {
                holdElapsed += Time.deltaTime;
                if (holdElapsed >= holdThreshold)
                {
                    longPressResolved = true;
                    TryBeginHealing();
                }
            }

            if (timeAbilityAction.action.WasReleasedThisFrame())
            {
                ResolveRelease();
            }
        }

        public bool TryBeginHealing()
        {
            if (worldTime != null && worldTime.CurrentMode != WorldTimeMode.Normal)
            {
                ReportUnavailable("时间能力生效期间不能治疗");
                return false;
            }

            if (health.IsFullHealth)
            {
                ReportUnavailable("生命已满");
                return false;
            }

            if (timeGauge.CurrentValue + Mathf.Epsilon < healingCost)
            {
                ReportUnavailable("时间槽不足");
                return false;
            }

            if (!movement.IsGrounded)
            {
                ReportUnavailable("必须站在地面治疗");
                return false;
            }

            if (!stateMachine.TryEnter(PlayerState.Healing))
            {
                ReportUnavailable("当前状态无法治疗");
                return false;
            }

            healingElapsed = 0f;
            healingSpent = 0f;
            HealingProgressChanged?.Invoke(0f);
            return true;
        }

        private void BeginPress()
        {
            inputHeld = true;
            longPressResolved = false;
            holdElapsed = 0f;
        }

        private void ResolveRelease()
        {
            if (IsHealing)
            {
                CancelHealing();
            }
            else if (ShouldRequestShortPress(holdElapsed, holdThreshold, longPressResolved))
            {
                ShortPressRequested?.Invoke();
            }

            ResetPress();
        }

        private void UpdateHealing()
        {
            if (!timeAbilityAction.action.IsPressed() || !movement.IsGrounded)
            {
                CancelHealing();
                return;
            }

            AdvanceHealing(Time.deltaTime);
        }

        internal void AdvanceHealing(float deltaTime)
        {
            if (!IsHealing || deltaTime <= 0f)
            {
                return;
            }

            if (!movement.IsGrounded)
            {
                CancelHealing();
                return;
            }

            healingElapsed = Mathf.Min(healingDuration, healingElapsed + deltaTime);
            float targetSpent = healingCost * (healingElapsed / healingDuration);
            float spendThisFrame = targetSpent - healingSpent;
            if (!timeGauge.TrySpend(spendThisFrame))
            {
                CancelHealing();
                return;
            }

            healingSpent = targetSpent;
            HealingProgressChanged?.Invoke(HealingProgress);

            if (healingElapsed < healingDuration)
            {
                return;
            }

            health.RestoreToFull();
            stateMachine.TryReturnToNormal(PlayerState.Healing);
            HealingProgressChanged?.Invoke(0f);
            ResetPress();
        }

        internal static bool ShouldRequestShortPress(
            float heldDuration,
            float threshold,
            bool longPressResolved)
        {
            return !longPressResolved && heldDuration < threshold;
        }

        private void HandleDamaged(DamageInfo damageInfo)
        {
            if (IsHealing)
            {
                CancelHealing();
            }
        }

        private void CancelHealing()
        {
            if (stateMachine != null)
            {
                stateMachine.TryReturnToNormal(PlayerState.Healing);
            }

            healingElapsed = 0f;
            healingSpent = 0f;
            HealingProgressChanged?.Invoke(0f);
        }

        private void ResetPress()
        {
            inputHeld = false;
            longPressResolved = false;
            holdElapsed = 0f;
        }

        private void ReportUnavailable(string reason)
        {
            HealingUnavailable?.Invoke(reason);
            Debug.Log($"无法治疗：{reason}", this);
        }
    }
}
