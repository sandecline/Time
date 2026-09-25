using UnityEngine;
using UnityEngine.InputSystem;

namespace TimeMetroidvania.Level
{
    /// <summary>
    /// 阶段 5 的运行期复活点；阶段 7 再接入退出后继续的存档。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class StageFiveCheckpoint : MonoBehaviour
    {
        [SerializeField] private string checkpointId;
        [SerializeField] private string roomId;
        [SerializeField] private Transform respawnAnchor;
        [SerializeField] private StageFiveLevelController level;
        [SerializeField] private SpriteRenderer artRenderer;
        [SerializeField] private Sprite inactiveSprite;
        [SerializeField] private Sprite activeSprite;

        private GameObject nearbyPlayer;

        public string CheckpointId => checkpointId;
        public string RoomId => roomId;

        private void Awake()
        {
            artRenderer ??= transform.Find("ArtVisual")?.GetComponent<SpriteRenderer>();
            if (artRenderer != null && inactiveSprite != null)
            {
                artRenderer.sprite = inactiveSprite;
                artRenderer.color = Color.white;
            }
        }

        public void Configure(
            string id, string room, Transform anchor, StageFiveLevelController controller)
        {
            checkpointId = id;
            roomId = room;
            respawnAnchor = anchor;
            level = controller;
        }

        private void Update()
        {
            if (nearbyPlayer == null)
            {
                return;
            }

            level.OfferPrompt($"W：坐下休息并激活检查点 {checkpointId}", 2);
            if (Keyboard.current?.wKey.wasPressedThisFrame == true)
            {
                TryActivate(nearbyPlayer);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
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

        public bool TryActivate(GameObject actor)
        {
            if (actor == null || !actor.CompareTag("Player") || respawnAnchor == null
                || Vector2.Distance(actor.transform.position, transform.position) > 2f
                || actor.TryGetComponent(out TimeMetroidvania.Player.HealthSystem health)
                    && health.IsDead)
            {
                return false;
            }

            level.ActivateCheckpoint(checkpointId, roomId, respawnAnchor.position);
            if (artRenderer != null)
            {
                artRenderer.sprite = activeSprite != null ? activeSprite : inactiveSprite;
                artRenderer.color = Color.white;
            }
            else
            {
                GetComponent<SpriteRenderer>().color = new Color(0.25f, 0.9f, 0.65f);
            }
            return true;
        }
    }
}
