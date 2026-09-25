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

        private GameObject nearbyPlayer;

        public string DestinationRoomId => destinationRoomId;
        public string SourceRoomId => sourceRoomId;
        public bool RequiresTimeStopUnlock => requiresTimeStopUnlock;
        public bool IsBossBattleBlocked => IsBossDoor
            && level != null
            && level.BossBattleActive;
        private bool IsBossDoor => sourceRoomId == "R7" || destinationRoomId == "R7";

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
                TryTravel(nearbyPlayer);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                TryTravel(other.gameObject);
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
                || Vector2.Distance(actor.transform.position, transform.position) > 2f
                || actor.TryGetComponent(out HealthSystem health) && health.IsDead)
            {
                return false;
            }

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
            body.linearVelocity = Vector2.zero;
            body.position = destination.position;
            actor.transform.position = destination.position;
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
