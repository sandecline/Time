using TimeMetroidvania.GameTime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TimeMetroidvania.Level
{
    /// <summary>
    /// 时停开启的回访关卡门；门前储能只补到一次时停所需的 60 点，避免资源耗尽卡关。
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

            if (!worldTime.IsTimeStopUnlocked)
            {
                level.OfferPrompt("旧区域封闭路线：先取得时停能力", 3);
                return;
            }

            level.OfferPrompt("W：补足一次时停所需时间槽；短按 I 开门", 3);
            if (Keyboard.current?.wKey.wasPressedThisFrame == true)
            {
                float missing = Mathf.Max(0f, 60f - timeGauge.CurrentValue);
                if (missing > 0f)
                {
                    timeGauge.Gain(missing);
                }
                level.ShowMessage("时间槽已备妥；在门前短按 I 发动时停。");
            }
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
