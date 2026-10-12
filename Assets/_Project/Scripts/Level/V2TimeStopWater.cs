using System.Collections.Generic;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Level
{
    /// <summary>水中可以活动；只在身体浸入后减重力，时停冻结水中玩家，离水、换房与死亡恢复。</summary>
    [DefaultExecutionOrder(300)]
    [RequireComponent(typeof(Collider2D))]
    public sealed class V2TimeStopWater : MonoBehaviour, ITimeControllable
    {
        [SerializeField] private WorldTimeService worldTime;
        [SerializeField] private Collider2D frozenSurface;
        [SerializeField] private SpriteRenderer waterRenderer;
        [SerializeField] private SpriteRenderer surfaceRenderer;
        [SerializeField] private SpriteRenderer normalSurfaceRenderer;
        [SerializeField] private Sprite normalWaterSprite;
        [SerializeField] private Sprite frozenWaterSprite;
        [SerializeField] private Sprite frozenSurfaceSprite;
        [SerializeField, Range(0.05f, 1f)] private float underwaterGravityScale = 0.3f;
        [SerializeField, Min(0.1f)] private float maximumUnderwaterRiseSpeed = 8.5f;
        [SerializeField, Min(0.1f)] private float maximumUnderwaterFallSpeed = 5f;
        [SerializeField, Min(0f)] private float underwaterDamping = 0.35f;
        [SerializeField, Min(0.1f)] private float underwaterMoveSpeed = 4.2f;
        private static readonly HashSet<V2TimeStopWater> Active = new();
        private Collider2D volume;
        private Rigidbody2D playerBody;
        private HealthSystem playerHealth;
        private PlayerDash dash;
        private float savedGravity, savedDamping;
        private RigidbodyConstraints2D savedConstraints;
        private Vector2 savedVelocity;
        private bool playerFrozen;
        private WorldTimeMode currentMode;
        public bool IsTimeResistant => true;
        public bool IsFrozen => currentMode == WorldTimeMode.Stopped;
        public bool HasSubmergedPlayer => playerBody != null;

        public void Configure(WorldTimeService service, Collider2D surface,
            SpriteRenderer waterVisual, SpriteRenderer surfaceVisual)
        {
            worldTime = service; frozenSurface = surface;
            waterRenderer = waterVisual; surfaceRenderer = surfaceVisual;
            UpdateFrozenSurface();
        }

        private void OnEnable()
        {
            volume = GetComponent<Collider2D>();
            Active.Add(this);
            worldTime ??= FindFirstObjectByType<WorldTimeService>();
            worldTime?.Register(this);
        }

        private void OnDisable()
        {
            worldTime?.Unregister(this);
            RestorePlayer();
            Active.Remove(this);
        }

        public static bool IsPlayerFrozen(GameObject player)
        {
            foreach (V2TimeStopWater water in Active)
                if (water.playerBody != null && water.playerBody.gameObject == player && water.playerFrozen) return true;
            return false;
        }

        public static void ReleasePlayer(GameObject player)
        {
            foreach (V2TimeStopWater water in Active)
                if (water.playerBody != null && water.playerBody.gameObject == player) water.RestorePlayer();
        }

        public void ApplyWorldTime(WorldTimeMode mode, float timeScale)
        {
            currentMode = mode;
            UpdateFrozenSurface();
            if (playerBody == null) return;
            if (IsFrozen) FreezePlayer(); else ThawPlayer();
        }

        private bool IsSubmerged(Collider2D shape)
        {
            Bounds w = volume.bounds, p = shape.bounds;
            return p.center.y < w.max.y - 0.18f && p.max.y > w.min.y
                && p.center.x > w.min.x && p.center.x < w.max.x;
        }

        private void OnTriggerStay2D(Collider2D other) => ConsiderPlayer(other);
        private void OnTriggerEnter2D(Collider2D other) => ConsiderPlayer(other);

        private void ConsiderPlayer(Collider2D other)
        {
            if (!other.CompareTag("Player") || other.attachedRigidbody == null
                || other != other.attachedRigidbody.GetComponent<Collider2D>()) return;
            if (!IsSubmerged(other)) { if (playerBody == other.attachedRigidbody) RestorePlayer(); return; }
            if (playerBody != null) return;
            playerBody = other.attachedRigidbody;
            dash = playerBody.GetComponent<PlayerDash>();
            savedGravity = dash != null ? dash.NormalGravityScale : playerBody.gravityScale;
            savedDamping = playerBody.linearDamping;
            savedConstraints = playerBody.constraints;
            playerHealth = playerBody.GetComponent<HealthSystem>();
            if (playerHealth != null) playerHealth.Died += RestorePlayer;
            if (dash != null) dash.SetEnvironmentGravity(savedGravity * underwaterGravityScale);
            else playerBody.gravityScale = savedGravity * underwaterGravityScale;
            playerBody.linearDamping = underwaterDamping;
            if (IsFrozen) FreezePlayer();
        }

        private void FixedUpdate()
        {
            if (playerBody == null) return;
            if (!IsSubmerged(playerBody.GetComponent<Collider2D>())) { RestorePlayer(); return; }
            if (playerFrozen) { playerBody.linearVelocity = Vector2.zero; return; }
            Vector2 v = playerBody.linearVelocity;
            v.x = Mathf.Clamp(v.x, -underwaterMoveSpeed, underwaterMoveSpeed);
            v.y = Mathf.Clamp(v.y, -maximumUnderwaterFallSpeed, maximumUnderwaterRiseSpeed);
            playerBody.linearVelocity = v;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (playerBody != null && other == playerBody.GetComponent<Collider2D>()) RestorePlayer();
        }

        private void FreezePlayer()
        {
            if (playerFrozen) return;
            savedVelocity = playerBody.linearVelocity;
            savedConstraints = playerBody.constraints;
            playerBody.linearVelocity = Vector2.zero;
            playerBody.constraints = RigidbodyConstraints2D.FreezeAll;
            playerFrozen = true;
        }

        private void ThawPlayer()
        {
            if (!playerFrozen || playerBody == null) return;
            playerBody.constraints = savedConstraints;
            playerBody.linearVelocity = savedVelocity;
            playerFrozen = false;
        }

        private void RestorePlayer()
        {
            if (playerBody == null) return;
            ThawPlayer();
            if (dash != null) dash.SetEnvironmentGravity(savedGravity);
            else playerBody.gravityScale = savedGravity;
            playerBody.linearDamping = savedDamping;
            if (playerHealth != null) playerHealth.Died -= RestorePlayer;
            playerBody = null; playerHealth = null; dash = null;
        }

        private void UpdateFrozenSurface()
        {
            if (frozenSurface != null) frozenSurface.enabled = IsFrozen;
            if (surfaceRenderer != null) surfaceRenderer.enabled = IsFrozen;
            if (normalSurfaceRenderer != null) normalSurfaceRenderer.enabled = !IsFrozen;
            if (surfaceRenderer != null && frozenSurfaceSprite != null)
                surfaceRenderer.sprite = frozenSurfaceSprite;
            if (waterRenderer != null)
            {
                if (normalWaterSprite != null && frozenWaterSprite != null)
                {
                    waterRenderer.sprite = IsFrozen ? frozenWaterSprite : normalWaterSprite;
                    waterRenderer.color = new Color(1f, 1f, 1f, IsFrozen ? 0.72f : 0.62f);
                }
                else waterRenderer.color = IsFrozen
                    ? new Color(0.55f, 0.9f, 1f, 0.62f) : new Color(0.1f, 0.62f, 0.82f, 0.38f);
            }
        }
    }
}
