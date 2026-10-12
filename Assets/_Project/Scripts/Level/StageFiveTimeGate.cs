using TimeMetroidvania.GameTime;
using UnityEngine;

namespace TimeMetroidvania.Level
{
    /// <summary>
    /// 时停开启的历史回访关卡门；V2 依赖时间槽自动上弦避免资源软锁。
    /// </summary>
    public sealed class StageFiveTimeGate : MonoBehaviour
    {
        [SerializeField] private WorldTimeService worldTime;
        [SerializeField] private TimeGauge timeGauge;
        [SerializeField] private Transform player;
        [SerializeField] private StageFiveLevelController level;
        [SerializeField, Min(0.1f)] private float activationDistance = 2.4f;

        private SpriteRenderer gateRenderer;

        public bool IsOpen { get; private set; }

        public void Configure(
            WorldTimeService time, TimeGauge gauge, Transform target,
            StageFiveLevelController controller)
        {
            worldTime = time;
            timeGauge = gauge;
            player = target;
            level = controller;
        }

        private void Awake()
        {
            gateRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (worldTime != null)
            {
                worldTime.ModeChanged += HandleModeChanged;
            }
        }

        private void OnDisable()
        {
            if (worldTime != null)
            {
                worldTime.ModeChanged -= HandleModeChanged;
            }
        }

        private void Update()
        {
            if (IsOpen || player == null
                || Vector2.Distance(player.position, transform.position) > activationDistance)
            {
                return;
            }

            if (TryOpen())
            {
                return;
            }

            level.OfferPrompt(worldTime.IsTimeStopUnlocked
                ? "按 I 开启时停，打开封闭路线"
                : "旧区域封闭路线：先取得时停能力", 3);
        }

        public bool TryOpen()
        {
            if (IsOpen || player == null || worldTime == null
                || worldTime.CurrentMode != WorldTimeMode.Stopped
                || Vector2.Distance(player.position, transform.position) > activationDistance)
            {
                return false;
            }

            IsOpen = true;
            gateRenderer ??= GetComponent<SpriteRenderer>();
            SpriteRenderer artRenderer =
                transform.Find("EnvironmentArt")?.GetComponent<SpriteRenderer>();
            if (artRenderer != null)
            {
                artRenderer.color = new Color(0.55f, 1f, 0.85f);
            }
            else
            {
                gateRenderer.color = new Color(0.25f, 0.9f, 0.65f);
            }
            level.ShowMessage("封闭路线已开启！从左侧入口前往房间 4。");
            return true;
        }

        private void HandleModeChanged(WorldTimeMode previousMode, WorldTimeMode nextMode)
        {
            if (nextMode == WorldTimeMode.Stopped)
            {
                TryOpen();
            }
        }
    }
}