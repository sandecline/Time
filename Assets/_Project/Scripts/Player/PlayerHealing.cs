using System;
using TimeMetroidvania.GameTime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TimeMetroidvania.Player
{
    /// <summary>
    /// 使用独立 U 键处理可移动、可打断且持续消耗时间槽的治疗。
    /// </summary>
    [RequireComponent(typeof(PlayerStateMachine), typeof(PlayerMovement), typeof(HealthSystem))]
    [RequireComponent(typeof(TimeGauge))]
    public sealed class PlayerHealing : MonoBehaviour
    {
        [Header("输入")]
        [SerializeField] private InputActionReference healAction;

        [Header("治疗")]
        [SerializeField, Min(0.01f)] private float healingDuration = 1.5f;
        [SerializeField, Min(0f)] private float healingCost = 50f;
        [SerializeField, Min(1)] private int healingAmount = 1;
        [SerializeField] private WorldTimeService worldTime;

        private PlayerStateMachine stateMachine;
        private PlayerMovement movement;
        private HealthSystem health;
        private TimeGauge timeGauge;
        private float healingElapsed;
        private float healingSpent;

        public event Action<float> HealingProgressChanged;
        public event Action<string> HealingUnavailable;

        public bool IsHealing => stateMachine != null
            && stateMachine.CurrentState == PlayerState.Healing;
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
            if (healAction == null)
            {
                Debug.LogError("PlayerHealing 未配置 Heal 输入动作。", this);
                enabled = false;
                return;
            }

            health.Damaged += HandleDamaged;
            healAction.action.Enable();
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= HandleDamaged;
            }

            healAction?.action.Disable();
            // 暂停菜单会临时禁用控制组件，保留治疗进度以便恢复后继续判定按键。
            if (Time.timeScale > 0f || !gameObject.activeInHierarchy)
            {
                CancelHealing();
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0f)
            {
                return;
            }

            if (!IsHealing && healAction.action.WasPressedThisFrame())
            {
                TryBeginHealing();
            }

            if (!IsHealing)
            {
                return;
            }

            if (!healAction.action.IsPressed() || !movement.IsGrounded)
            {
                CancelHealing();
                return;
            }

            AdvanceHealing(Time.deltaTime);
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

            health.Restore(healingAmount);
            stateMachine.TryReturnToNormal(PlayerState.Healing);
            healingElapsed = 0f;
            healingSpent = 0f;
            HealingProgressChanged?.Invoke(0f);
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

        private void ReportUnavailable(string reason)
        {
            HealingUnavailable?.Invoke(reason);
            Debug.Log($"无法治疗：{reason}", this);
        }
    }
}