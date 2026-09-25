using TimeMetroidvania.GameTime;
using UnityEngine;

namespace TimeMetroidvania.Level
{
    public enum StageFivePickupKind
    {
        TimeStop,
        DurationUpgrade
    }

    /// <summary>
    /// 灰盒能力道具；正式持久化将在阶段 7 接入。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class StageFivePickup : MonoBehaviour
    {
        [SerializeField] private StageFivePickupKind kind;
        [SerializeField] private WorldTimeService worldTime;
        [SerializeField] private TimeGauge timeGauge;
        [SerializeField] private StageFiveLevelController level;
        [SerializeField] private SpriteRenderer artRenderer;
        [SerializeField] private Sprite timeStopSprite;
        [SerializeField] private Sprite durationUpgradeSprite;

        private bool collected;

        public StageFivePickupKind Kind => kind;
        public bool IsCollected => collected;

        private void Awake()
        {
            artRenderer ??= transform.Find("ArtVisual")?.GetComponent<SpriteRenderer>();
            if (artRenderer != null)
            {
                artRenderer.sprite = kind == StageFivePickupKind.TimeStop
                    ? timeStopSprite
                    : durationUpgradeSprite;
                artRenderer.color = Color.white;
            }
        }

        public void Configure(
            StageFivePickupKind pickupKind, WorldTimeService time,
            TimeGauge gauge, StageFiveLevelController controller)
        {
            kind = pickupKind;
            worldTime = time;
            timeGauge = gauge;
            level = controller;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryCollect(other.gameObject);
        }

        public bool TryCollect(GameObject actor)
        {
            if (collected || actor == null || !actor.CompareTag("Player") || worldTime == null)
            {
                return false;
            }

            collected = true;
            if (kind == StageFivePickupKind.TimeStop)
            {
                worldTime.UnlockTimeStop();
                timeGauge.Gain(60f);
                level.ShowMessage("已获得时停！返回房间 1，用 I 开启旧区域封闭路线。");
            }
            else
            {
                worldTime.UnlockDurationUpgrade();
                level.ShowMessage("隐藏升级已取得：时停最长时间 5 秒 → 7 秒。");
            }

            GetComponent<Collider2D>().enabled = false;
            GetComponent<SpriteRenderer>().enabled = false;
            if (artRenderer != null)
            {
                artRenderer.enabled = false;
            }
            return true;
        }
    }
}
