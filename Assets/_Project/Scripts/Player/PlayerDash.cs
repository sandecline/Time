using UnityEngine;
using UnityEngine.InputSystem;

namespace TimeMetroidvania.Player
{
    /// <summary>
    /// 提供无无敌帧的水平 Dash；碰撞和受伤规则仍由物理与生命系统正常处理。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerStateMachine), typeof(PlayerMovement))]
    public sealed class PlayerDash : MonoBehaviour
    {
        [Header("输入")]
        [SerializeField] private InputActionReference dashAction;

        [Header("Dash")]
        [SerializeField, Min(0f)] private float dashSpeed = 15f;
        [SerializeField, Min(0.01f)] private float dashDuration = 0.16f;
        [SerializeField, Min(0f)] private float dashCooldown = 0.2f;

        private Rigidbody2D body;
        private PlayerStateMachine stateMachine;
        private PlayerMovement movement;
        private PlayerCombat combat;
        private float defaultGravityScale;
        private float dashRemaining;
        private float cooldownRemaining;
        private int dashDirection;

        public bool IsDashing => stateMachine.CurrentState == PlayerState.Dash;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            stateMachine = GetComponent<PlayerStateMachine>();
            movement = GetComponent<PlayerMovement>();
            combat = GetComponent<PlayerCombat>();
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

        private void OnDisable()
        {
            stateMachine.StateChanged -= HandleStateChanged;
            dashAction?.action.Disable();
            RestoreGravity();
        }

        private void Update()
        {
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);

            if (dashAction.action.WasPressedThisFrame())
            {
                BeginDash();
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
            if (IsDashing)
            {
                body.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
            }
        }

        public bool BeginDash()
        {
            if (cooldownRemaining > 0f
                || stateMachine.CurrentState == PlayerState.Attack
                    && (combat == null || !combat.CanCancelIntoDash)
                || !stateMachine.TryEnter(PlayerState.Dash))
            {
                return false;
            }

            dashDirection = movement.FacingDirection;
            dashRemaining = dashDuration;
            cooldownRemaining = dashDuration + dashCooldown;
            body.gravityScale = 0f;
            body.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
            return true;
        }

        private void EndDash()
        {
            RestoreGravity();
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            stateMachine.TryReturnToNormal(PlayerState.Dash);
        }

        private void HandleStateChanged(PlayerState previousState, PlayerState nextState)
        {
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
