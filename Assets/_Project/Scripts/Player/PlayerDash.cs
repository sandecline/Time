using UnityEngine;
using UnityEngine.InputSystem;
using TimeMetroidvania.Level;

namespace TimeMetroidvania.Player
{
    /// <summary>
    /// 提供无无敌帧的水平 Dash；碰撞和受伤规则仍由物理与生命系统正常处理。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerStateMachine), typeof(PlayerMovement))]
    [DefaultExecutionOrder(50)]
    public sealed class PlayerDash : MonoBehaviour
    {
        [Header("输入")]
        [SerializeField] private InputActionReference dashAction;

        [Header("Dash")]
        [SerializeField, Min(0f)] private float dashSpeed = 15f;
        [SerializeField, Min(0.01f)] private float dashDuration = 0.16f;
        [SerializeField, Min(0f)] private float dashCooldown = 0.2f;
        [SerializeField, Min(0f)] private float dashInputBuffer = 0.15f;

        private Rigidbody2D body;
        private PlayerStateMachine stateMachine;
        private PlayerMovement movement;
        private PlayerCombat combat;
        private float defaultGravityScale;
        private float dashRemaining;
        private float cooldownRemaining;
        private int dashDirection;
        private bool airDashUsed;
        private float dashRequestRemaining;
        private HitStopController hitStop;
        private StageFiveLevelController level;
        public float BufferedDashRemaining => dashRequestRemaining;
        public float NormalGravityScale => defaultGravityScale;
        public void SetEnvironmentGravity(float value) { defaultGravityScale = value; if (!IsDashing) body.gravityScale = value; }
        public void ResetAirDash() => airDashUsed = false;
        public void CancelDash() { if (IsDashing) EndDash(); }

        public bool IsDashing => stateMachine.CurrentState == PlayerState.Dash;
        public float DashProgress => IsDashing ? Mathf.Clamp01(1f - dashRemaining / dashDuration) : 0f;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            stateMachine = GetComponent<PlayerStateMachine>();
            movement = GetComponent<PlayerMovement>();
            combat = GetComponent<PlayerCombat>();
            hitStop = GetComponent<HitStopController>();
            defaultGravityScale = body.gravityScale;
        }

        private void OnEnable()
        {
            if (dashAction == null)
            {
                Debug.LogError("PlayerDash 未配置 Dash 输入动作。", this);
                enabled = false;
                return;
            }

            stateMachine.StateChanged += HandleStateChanged;
            dashAction.action.Enable();
        }

        private void Start()
        {
            level = FindFirstObjectByType<StageFiveLevelController>();
            if (level != null) level.RoomEntered += HandleRoomEntered;
        }

        private void OnDisable()
        {
            stateMachine.StateChanged -= HandleStateChanged;
            if (level != null) level.RoomEntered -= HandleRoomEntered;
            ClearBufferedDash();
            dashAction?.action.Disable();
            RestoreGravity();
        }

        private void Update()
        {
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);
            if (Time.timeScale <= 0f || V2TimeStopWater.IsPlayerFrozen(gameObject))
            {
                ClearBufferedDash();
                return;
            }
            if (dashAction.action.WasPressedThisFrame()) RequestDash();
            if (dashRequestRemaining > 0f)
            {
                if (TryConsumeBufferedDash()) dashRequestRemaining = 0f;
                else if (hitStop == null || !hitStop.IsPaused)
                    dashRequestRemaining = Mathf.Max(0f, dashRequestRemaining - Time.deltaTime);
            }
            if (!IsDashing)
            {
                return;
            }

            dashRemaining -= Time.deltaTime;
            if (dashRemaining <= 0f)
            {
                EndDash();
            }
        }

        private void FixedUpdate()
        {
            if (movement.IsGrounded && !IsDashing) airDashUsed = false;
            if (IsDashing)
            {
                body.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
            }
        }

        public bool BeginDash()
        {
            if (TimeMetroidvania.Level.V2TimeStopWater.IsPlayerFrozen(gameObject)
                || !movement.IsGrounded && airDashUsed
                || cooldownRemaining > 0f
                || stateMachine.CurrentState == PlayerState.Attack
                    && (combat == null || !combat.CanCancelIntoDash)
                || !stateMachine.TryEnter(PlayerState.Dash))
            {
                return false;
            }

            if (!movement.IsGrounded) airDashUsed = true;
            ClearBufferedDash();
            dashDirection = movement.FacingDirection;
            dashRemaining = dashDuration;
            cooldownRemaining = dashDuration + dashCooldown;
            body.gravityScale = 0f;
            body.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
            return true;
        }

        public bool RequestDash()
        {
            if (stateMachine.CurrentState is PlayerState.Dead or PlayerState.Hurt or PlayerState.Healing
                || Time.timeScale <= 0f || V2TimeStopWater.IsPlayerFrozen(gameObject)) return false;
            dashRequestRemaining = Mathf.Max(dashInputBuffer, Time.deltaTime);
            if (TryConsumeBufferedDash()) dashRequestRemaining = 0f;
            return true;
        }

        public void ClearBufferedDash() => dashRequestRemaining = 0f;

        private bool TryConsumeBufferedDash()
        {
            if (dashRequestRemaining <= 0f || hitStop != null && hitStop.IsPaused) return false;
            return BeginDash();
        }

        private void HandleRoomEntered(string roomId) => ClearBufferedDash();

        private void EndDash()
        {
            RestoreGravity();
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            stateMachine.TryReturnToNormal(PlayerState.Dash);
        }

        private void HandleStateChanged(PlayerState previousState, PlayerState nextState)
        {
            if (nextState is PlayerState.Hurt or PlayerState.Dead or PlayerState.Healing)
                ClearBufferedDash();
            if (previousState == PlayerState.Dash && nextState != PlayerState.Dash)
            {
                RestoreGravity();
            }
        }

        private void RestoreGravity()
        {
            if (body != null)
            {
                body.gravityScale = defaultGravityScale;
            }
        }
    }
}
