using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.GameTime
{
    /// <summary>
    /// 为单个世界对象提供局部 DeltaTime，并同步 Animator 与刚体的时停状态。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalTimeController : MonoBehaviour, ITimeControllable
    {
        [SerializeField] private WorldTimeService worldTime;
        [SerializeField] private bool timeResistant;
        [SerializeField] private bool controlAnimator = true;
        [SerializeField] private bool freezeRigidbodyDuringStop = true;

        private Animator animator;
        private Rigidbody2D body;
        private RigidbodyConstraints2D savedConstraints;
        private Vector2 savedVelocity;
        private float savedAngularVelocity;
        private bool rigidbodyFrozen;
        private HitStopController hitStop;

        public bool IsTimeResistant => timeResistant;
        public float CurrentTimeScale { get; private set; } = 1f;
        public bool IsHitStopped => ResolveHitStop() != null && hitStop.IsPaused;
        public float LocalDeltaTime => IsHitStopped
            ? 0f
            : UnityEngine.Time.deltaTime * CurrentTimeScale;
        public float LocalFixedDeltaTime => IsHitStopped
            ? 0f
            : UnityEngine.Time.fixedDeltaTime * CurrentTimeScale;

        public void SetTimeResistance(bool resistant)
        {
            if (timeResistant == resistant)
            {
                return;
            }

            timeResistant = resistant;
            if (worldTime != null && isActiveAndEnabled)
            {
                worldTime.Unregister(this);
                worldTime.Register(this);
            }
        }

        public void ClearStoredMotion()
        {
            savedVelocity = Vector2.zero;
            savedAngularVelocity = 0f;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
        }

        public void SetWorldTime(WorldTimeService service)
        {
            if (worldTime == service)
            {
                return;
            }
            if (isActiveAndEnabled)
            {
                worldTime?.Unregister(this);
            }
            worldTime = service;
            if (isActiveAndEnabled)
            {
                worldTime?.Register(this);
            }
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            body = GetComponent<Rigidbody2D>();
            hitStop = GetComponent<HitStopController>();
        }

        private void OnEnable()
        {
            worldTime ??= FindFirstObjectByType<WorldTimeService>();
            worldTime?.Register(this);
        }

        private void Update()
        {
            UpdateAnimatorSpeed();
        }

        private void OnDisable()
        {
            worldTime?.Unregister(this);
            RestoreRigidbody();
        }

        public void ApplyWorldTime(WorldTimeMode mode, float timeScale)
        {
            CurrentTimeScale = timeResistant ? 1f : Mathf.Clamp01(timeScale);

            UpdateAnimatorSpeed();

            if (!freezeRigidbodyDuringStop || body == null)
            {
                return;
            }

            if (mode == WorldTimeMode.Stopped && !timeResistant)
            {
                FreezeRigidbody();
            }
            else
            {
                RestoreRigidbody();
            }
        }

        private HitStopController ResolveHitStop()
        {
            hitStop ??= GetComponent<HitStopController>();
            return hitStop;
        }

        private void UpdateAnimatorSpeed()
        {
            if (controlAnimator && animator != null)
            {
                animator.speed = IsHitStopped ? 0f : CurrentTimeScale;
            }
        }

        private void FreezeRigidbody()
        {
            if (rigidbodyFrozen)
            {
                return;
            }

            savedConstraints = body.constraints;
            savedVelocity = body.linearVelocity;
            savedAngularVelocity = body.angularVelocity;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
            rigidbodyFrozen = true;
        }

        private void RestoreRigidbody()
        {
            if (!rigidbodyFrozen || body == null)
            {
                return;
            }

            body.constraints = savedConstraints;
            body.linearVelocity = savedVelocity;
            body.angularVelocity = savedAngularVelocity;
            rigidbodyFrozen = false;
        }
    }
}
