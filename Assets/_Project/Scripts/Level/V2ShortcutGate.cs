using TimeMetroidvania.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TimeMetroidvania.Level
{
    /// <summary>
    /// R2 与 R4 之间的单向捷径门，只能从 R4 主体一侧按 W 开启。
    /// </summary>
    public sealed class V2ShortcutGate : MonoBehaviour
    {
        [SerializeField] private Collider2D solidBlocker;
        [SerializeField] private SpriteRenderer gateRenderer;
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite openSprite;
        [SerializeField] private SpriteRenderer switchRenderer;
        [SerializeField] private Sprite switchOffSprite;
        [SerializeField] private Sprite switchOnSprite;
        [SerializeField] private StageFiveLevelController level;
        [SerializeField] private bool openFromRight = true;

        private GameObject nearbyPlayer;

        public bool IsOpen { get; private set; }

        public void Configure(
            Collider2D blocker, SpriteRenderer renderer,
            StageFiveLevelController controller, bool rightSide)
        {
            solidBlocker = blocker;
            gateRenderer = renderer;
            level = controller;
            openFromRight = rightSide;
        }

        public void ConfigureArt(SpriteRenderer door, Sprite closed, Sprite open,
            SpriteRenderer lever, Sprite off, Sprite on)
        {
            gateRenderer = door;
            closedSprite = closed;
            openSprite = open;
            switchRenderer = lever;
            switchOffSprite = off;
            switchOnSprite = on;
            if (gateRenderer != null) gateRenderer.sprite = IsOpen ? openSprite : closedSprite;
            if (switchRenderer != null) switchRenderer.sprite = IsOpen ? switchOnSprite : switchOffSprite;
        }

        private void Update()
        {
            if (IsOpen || nearbyPlayer == null || !IsOnSwitchSide(nearbyPlayer.transform.position))
            {
                return;
            }

            level?.OfferPrompt("W：从内侧开启 R2↔R4 捷径机关门", 3);
            if (Keyboard.current?.wKey.wasPressedThisFrame == true)
            {
                Open();
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

        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            IsOpen = true;
            if (solidBlocker != null)
            {
                solidBlocker.enabled = false;
            }
            if (gateRenderer != null)
            {
                if (openSprite != null) gateRenderer.sprite = openSprite;
                gateRenderer.color = openSprite != null ? Color.white : new Color(0.25f, 0.9f, 0.65f, 0.28f);
            }
            if (switchRenderer != null && switchOnSprite != null) switchRenderer.sprite = switchOnSprite;
            level?.ShowMessage("捷径机关门已开启，本次运行中保持开启。");
        }

        private bool IsOnSwitchSide(Vector3 playerPosition)
        {
            return openFromRight
                ? playerPosition.x >= transform.position.x + 0.35f
                : playerPosition.x <= transform.position.x - 0.35f;
        }
    }
}
