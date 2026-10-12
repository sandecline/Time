using UnityEngine;
using UnityEngine.InputSystem;

namespace TimeMetroidvania.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerStateMachine))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Header("输入")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;

        [Header("水平移动")]
        [SerializeField, Min(0f)] private float moveSpeed = 7f;
        [SerializeField, Min(0f)] private float groundAcceleration = 90f;
        [SerializeField, Min(0f)] private float groundDeceleration = 110f;
        [SerializeField, Min(0f)] private float airAcceleration = 55f;
        [SerializeField, Min(0f)] private float airDeceleration = 45f;
        [SerializeField, Range(0.1f, 1f)] private float attackMoveMultiplier = 0.88f;
        [SerializeField, Range(0.1f, 1f)] private float healingMoveMultiplier = 0.65f;

        [Header("跳跃")]
        [SerializeField, Min(0f)] private float jumpSpeed = 12f;
        [SerializeField, Min(0f)] private float coyoteTime = 0.1f;
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;
        [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.42f;
        [SerializeField, Min(1f)] private float fallGravityMultiplier = 2.1f;
        [SerializeField, Min(0f)] private float maxFallSpeed = 24f;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private Vector2 groundCheckSize = new(0.8f, 0.12f);
        [SerializeField] private LayerMask groundLayer;

        private Rigidbody2D body;
        private PlayerStateMachine stateMachine;
        private float horizontalInput;
        private float coyoteTimeCounter;
        private float jumpBufferCounter;
        private bool jumpReleased;
        private bool isGrounded;
        private bool pogoAscending;

        public bool IsGrounded => isGrounded;
        public float VerticalInput { get; private set; }
        public int FacingDirection { get; private set; } = 1;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            stateMachine = GetComponent<PlayerStateMachine>();
            // 玩家与墙面接触时不能被默认摩擦力挂住。
            PhysicsMaterial2D noFriction = new("PlayerNoWallFriction")
            {
                friction = 0f,
                bounciness = 0f
            };
            foreach (Collider2D hitbox in GetComponents<Collider2D>())
            {
                hitbox.sharedMaterial = noFriction;
            }
        }

        private void OnEnable()
        {
            if (moveAction == null || jumpAction == null)
            {
                Debug.LogError("PlayerMovement 未完整配置 Move 或 Jump 输入动作。", this);
                enabled = false;
                return;
            }

            if (groundCheck == null)
            {
                Debug.LogError("PlayerMovement 未配置 GroundCheck。", this);
                enabled = false;
                return;
            }

            moveAction.action.Enable();
            jumpAction.action.Enable();
        }

        private void OnDisable()
        {
            moveAction?.action.Disable();
            jumpAction?.action.Disable();
        }

        private void Update()
        {
            if (!CanControlMovement(stateMachine.CurrentState, isGrounded, body.linearVelocity.y))
            {
                horizontalInput = 0f;
                VerticalInput = 0f;
                jumpBufferCounter = 0f;
                jumpReleased = false;
                return;
            }

            Vector2 moveInput = moveAction.action.ReadValue<Vector2>();
            horizontalInput = moveInput.x;
            VerticalInput = moveInput.y;

            if (Mathf.Abs(horizontalInput) > 0.01f)
            {
                FacingDirection = horizontalInput > 0f ? 1 : -1;
            }

            if (stateMachine.CurrentState != PlayerState.Healing
                && jumpAction.action.WasPressedThisFrame())
            {
                jumpBufferCounter = jumpBufferTime;
            }
            else
            {
                jumpBufferCounter -= Time.deltaTime;
            }

            if (jumpAction.action.WasReleasedThisFrame())
            {
                jumpReleased = true;
            }
        }

        private void FixedUpdate()
        {
            if (TimeMetroidvania.Level.V2TimeStopWater.IsPlayerFrozen(gameObject))
            {
                pogoAscending = false;
                jumpBufferCounter = 0f;
                body.linearVelocity = Vector2.zero;
                return;
            }
            isGrounded = Physics2D.OverlapBox(
                groundCheck.position,
                groundCheckSize,
                0f,
                groundLayer);
            if (isGrounded || body.linearVelocity.y <= 0f
                || stateMachine.CurrentState is PlayerState.Dash or PlayerState.Hurt
                    or PlayerState.Dead or PlayerState.Healing)
                pogoAscending = false;

            if (!CanControlMovement(stateMachine.CurrentState, isGrounded, body.linearVelocity.y))
            {
                if (stateMachine.CurrentState == PlayerState.Healing)
                {
                    Vector2 lockedVelocity = body.linearVelocity;
                    lockedVelocity.x = Mathf.MoveTowards(
                        lockedVelocity.x,
                        0f,
                        groundDeceleration * Time.fixedDeltaTime);
                    body.linearVelocity = lockedVelocity;
                }

                return;
            }

            Vector2 velocity = body.linearVelocity;

            if (isGrounded && velocity.y <= 0.1f)
            {
                coyoteTimeCounter = coyoteTime;
            }
            else
            {
                coyoteTimeCounter -= Time.fixedDeltaTime;
            }

            float movementMultiplier = stateMachine.CurrentState switch
            {
                PlayerState.Attack => attackMoveMultiplier,
                PlayerState.Healing => healingMoveMultiplier,
                _ => 1f
            };
            float targetSpeed = horizontalInput * moveSpeed * movementMultiplier;
            float acceleration = isGrounded
                ? Mathf.Abs(targetSpeed) > 0.01f
                    ? groundAcceleration
                    : groundDeceleration
                : Mathf.Abs(targetSpeed) > 0.01f
                    ? airAcceleration
                    : airDeceleration;
            velocity.x = Mathf.MoveTowards(
                velocity.x,
                targetSpeed,
                acceleration * Time.fixedDeltaTime);

            if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
            {
                pogoAscending = false;
                velocity.y = jumpSpeed;
                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
            }

            if (jumpReleased && velocity.y > 0f && !pogoAscending)
            {
                velocity.y *= jumpCutMultiplier;
            }

            if (velocity.y < 0f)
            {
                float extraGravity = Physics2D.gravity.y
                    * body.gravityScale
                    * (fallGravityMultiplier - 1f)
                    * Time.fixedDeltaTime;

                velocity.y = Mathf.Max(velocity.y + extraGravity, -maxFallSpeed);
            }

            jumpReleased = false;
            body.linearVelocity = velocity;
        }

        /// <summary>
        /// 下劈命中有效目标后施加稳定的向上反弹，不改变水平速度。
        /// </summary>
        public void ApplyPogoBounce(float upwardSpeed)
        {
            if (body == null || upwardSpeed <= 0f)
            {
                return;
            }

            Vector2 velocity = body.linearVelocity;
            velocity.y = Mathf.Max(velocity.y, upwardSpeed);
            body.linearVelocity = velocity;
            isGrounded = false;
            pogoAscending = true;
            GetComponent<PlayerDash>()?.ResetAirDash();
            coyoteTimeCounter = 0f;
            jumpBufferCounter = 0f;
            jumpReleased = false;
        }

        internal static bool CanControlMovement(
            PlayerState state, bool grounded, float verticalVelocity)
        {
            // 攻击保留完整方向控制；治疗只能在地面以较低速度水平移动。
            return state switch
            {
                PlayerState.Normal or PlayerState.Attack => true,
                PlayerState.Healing => grounded && verticalVelocity <= 0.1f,
                _ => false
            };
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null)
            {
                return;
            }

            Gizmos.color = isGrounded ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }
}
