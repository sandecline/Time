using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Level
{
    /// <summary>
    /// 玩家走入入口后切换房间，清除速度并对齐镜头。
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class StageFiveDoor : MonoBehaviour
    {
        [SerializeField] private Transform destination;
        [SerializeField] private string destinationRoomId;
        [SerializeField] private string sourceRoomId;
        [SerializeField] private string destinationName;
        [SerializeField] private StageFiveLevelController level;
        [SerializeField] private StageFiveTimeGate gate;
        [SerializeField] private bool requiresTimeStopUnlock;
        [SerializeField] private Collider2D lockedBlocker;
        [SerializeField] private SpriteRenderer lockedBlockerRenderer;
        [SerializeField] private Collider2D bossBattleBlocker;
        [SerializeField] private SpriteRenderer bossBattleBlockerRenderer;
        [SerializeField] private Vector2 destinationVelocity;
        [SerializeField] private bool preserveHorizontalDirection;

        [SerializeField] private Vector2 outwardNormal = Vector2.right;
        [SerializeField] private StageFiveDoor pairedDoor;
        [SerializeField, Min(0f)] private float crossingMargin = 0.025f;
        private GameObject nearbyPlayer;
        private Collider2D playerCollider;
        private float previousTrailing;
        private bool sampled;
        public Vector2 OutwardNormal => outwardNormal;
        public StageFiveDoor PairedDoor => pairedDoor;
        public Transform Destination => destination;
        public void ConfigureBoundary(Vector2 normal, StageFiveDoor pair)
        {
            outwardNormal = normal.normalized;
            pairedDoor = pair;
        }

        private void FixedUpdate()
        {
            if (level == null || level.CurrentRoomId != sourceRoomId) { sampled = false; return; }
            if (playerCollider == null)
                playerCollider = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Collider2D>();
            if (playerCollider == null) return;
            Bounds bounds = playerCollider.bounds;
            float trailing = TrailingDistance(bounds, transform.position, outwardNormal);
            if (sampled && previousTrailing <= crossingMargin && trailing > crossingMargin)
                TryTravel(playerCollider.gameObject);
            previousTrailing = trailing;
            sampled = true;
        }

        internal static float TrailingDistance(Bounds bounds, Vector2 center, Vector2 normal)
            => Vector2.Dot((Vector2)bounds.center - center, normal)
                - Mathf.Abs(normal.x) * bounds.extents.x - Mathf.Abs(normal.y) * bounds.extents.y;

        internal static bool FitsOpening(Bounds actor, Bounds opening, Vector2 normal)
        {
            bool horizontal = Mathf.Abs(normal.x) > 0.5f;
            return horizontal
                ? actor.min.y >= opening.min.y - 0.03f && actor.max.y <= opening.max.y + 0.03f
                : actor.min.x >= opening.min.x - 0.03f && actor.max.x <= opening.max.x + 0.03f;
        }

        public string DestinationRoomId => destinationRoomId;
        public string SourceRoomId => sourceRoomId;
        public bool RequiresTimeStopUnlock => requiresTimeStopUnlock;
        public bool IsBossBattleBlocked => IsBossDoor
            && level != null
            && level.BossBattleActive;
        private bool IsBossDoor => level != null
            && (level.IsBossRoom(sourceRoomId) || level.IsBossRoom(destinationRoomId));

        private void Awake()
        {
            CacheBlockerReferences();
        }

        public void Configure(
            Transform target, string roomId, string label,
            StageFiveLevelController controller, StageFiveTimeGate requiredGate = null,
            bool requireUnlock = false)
        {
            destination = target;
            sourceRoomId = GetComponentInParent<StageFiveRoom>()?.RoomId;
            destinationRoomId = roomId;
            destinationName = label;
            level = controller;
            gate = requiredGate;
            requiresTimeStopUnlock = requireUnlock;
            Transform blocker = transform.Find("LockedBlocker");
            lockedBlocker = blocker?.GetComponent<Collider2D>();
            lockedBlockerRenderer = blocker?.GetComponent<SpriteRenderer>();
            CacheBlockerReferences();
            UpdateBlocker();
        }

        public void ConfigureExitMotion(Vector2 velocity, bool preserveDirection)
        {
            destinationVelocity = velocity;
            preserveHorizontalDirection = preserveDirection;
        }

        public void ConfigureBossBattleBlocker(
            Collider2D blocker, SpriteRenderer blockerRenderer)
        {
            bossBattleBlocker = blocker;
            bossBattleBlockerRenderer = blockerRenderer;
            UpdateBlocker();
        }

        private void Update()
        {
            UpdateBlocker();
            if (nearbyPlayer == null)
            {
                return;
            }

            if (!IsBossBattleBlocked)
            {
                level.OfferPrompt($"走入入口：{destinationName}", 1);
            }
        }

        private void UpdateBlocker()
        {
            bool shouldBlock = requiresTimeStopUnlock
                && (level == null || level.WorldTime == null
                    || !level.WorldTime.IsTimeStopUnlocked)
                || gate != null && !gate.IsOpen;
            if (lockedBlocker != null)
            {
                lockedBlocker.enabled = shouldBlock;
            }
            if (lockedBlockerRenderer != null)
            {
                Transform art = lockedBlockerRenderer.transform.Find("EnvironmentArt");
                lockedBlockerRenderer.enabled = art == null && shouldBlock;
                SpriteRenderer artRenderer = art?.GetComponent<SpriteRenderer>();
                if (artRenderer != null)
                {
                    artRenderer.enabled = shouldBlock;
                }
            }
            bool shouldBlockBossDoor = IsBossBattleBlocked;
            if (bossBattleBlocker != null)
            {
                bossBattleBlocker.enabled = shouldBlockBossDoor;
            }
            if (bossBattleBlockerRenderer != null)
            {
                bossBattleBlockerRenderer.enabled = shouldBlockBossDoor;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                nearbyPlayer = other.gameObject;
                // 仅记录接近状态，身体越过边界由 FixedUpdate 判断。
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                nearbyPlayer = other.gameObject;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.gameObject == nearbyPlayer)
            {
                nearbyPlayer = null;
            }
        }

        public bool TryTravel(GameObject actor)
        {
            if (actor == null || !actor.CompareTag("Player") || destination == null
                || level == null || level.CurrentRoomId != sourceRoomId
                || actor.TryGetComponent(out HealthSystem health) && health.IsDead)
            {
                return false;
            }

            Collider2D actorShape = actor.GetComponent<Collider2D>();
            if (actorShape == null) return false;
            float trailing = TrailingDistance(actorShape.bounds, transform.position, outwardNormal);
            if (trailing <= crossingMargin || trailing > 2f
                || !FitsOpening(actorShape.bounds, GetComponent<BoxCollider2D>().bounds, outwardNormal))
                return false;

            if (requiresTimeStopUnlock && !level.WorldTime.IsTimeStopUnlocked)
            {
                level.ShowMessage("先取得时停能力，再返回旧区域。");
                return false;
            }

            if (IsBossBattleBlocked)
            {
                return false;
            }

            if (gate != null && !gate.IsOpen)
            {
                level.ShowMessage("先在附近发动时停，打开封闭路线。");
                return false;
            }

            Rigidbody2D body = actor.GetComponent<Rigidbody2D>();
            float horizontalSign = actor.GetComponent<PlayerMovement>()?.FacingDirection ?? 1;
            Vector2 arrival = destination.position;
            if (pairedDoor != null)
            {
                // 保存穿口时的相对高度或横向位置，避免每次被吸到入口正中心。
                Vector2 tangent = Mathf.Abs(outwardNormal.x) > 0.5f ? Vector2.up : Vector2.right;
                Bounds opening = pairedDoor.GetComponent<BoxCollider2D>().bounds;
                float actorHalf = Mathf.Abs(outwardNormal.x) > 0.5f ? actorShape.bounds.extents.y : actorShape.bounds.extents.x;
                float openingHalf = Mathf.Abs(outwardNormal.x) > 0.5f ? opening.extents.y : opening.extents.x;
                float offset = Vector2.Dot((Vector2)actorShape.bounds.center - (Vector2)transform.position, tangent);
                arrival += tangent * Mathf.Clamp(offset, -Mathf.Max(0f, openingHalf - actorHalf - 0.04f),
                    Mathf.Max(0f, openingHalf - actorHalf - 0.04f));
            }
            Vector2 exitVelocity = Mathf.Abs(outwardNormal.x) > 0.5f
                ? body.linearVelocity : destinationVelocity;
            if (preserveHorizontalDirection && Mathf.Abs(exitVelocity.x) > 0f)
            {
                exitVelocity.x = Mathf.Abs(exitVelocity.x)
                    * (Mathf.Abs(horizontalSign) > 0f ? horizontalSign : 1f);
            }
            body.linearVelocity = Vector2.zero;
            V2TimeStopWater.ReleasePlayer(actor);
            body.position = arrival;
            actor.transform.position = arrival;
            body.linearVelocity = exitVelocity;
            nearbyPlayer = null;
            level.TransitionRoom(sourceRoomId, destinationRoomId);
            level.SnapCamera();
            return true;
        }

        private void CacheBlockerReferences()
        {
            Transform blocker = transform.Find("BossBattleBlocker");
            bossBattleBlocker ??= blocker?.GetComponent<Collider2D>();
            bossBattleBlockerRenderer ??= blocker?.GetComponent<SpriteRenderer>();
        }
    }
}
