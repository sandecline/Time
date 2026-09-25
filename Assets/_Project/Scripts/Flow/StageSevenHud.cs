using TimeMetroidvania.GameTime;
using TimeMetroidvania.Level;
using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Flow
{
    /// <summary>
    /// 阶段七统一运行 HUD，替代早期原型诊断面板。
    /// </summary>
    public sealed class StageSevenHud : MonoBehaviour
    {
        [SerializeField] private HealthSystem health;
        [SerializeField] private PlayerStateMachine playerState;
        [SerializeField] private TimeGauge gauge;
        [SerializeField] private PlayerHealing healing;
        [SerializeField] private PlayerTimeAbilities timeAbilities;
        [SerializeField] private WorldTimeService worldTime;
        [SerializeField] private StageFiveLevelController level;
        [SerializeField] private StageSevenFlowController flow;

        private GUIStyle labelStyle;
        private GUIStyle smallStyle;

        public void Configure(
            GameObject player, WorldTimeService time,
            StageFiveLevelController controller, StageSevenFlowController flowController)
        {
            health = player.GetComponent<HealthSystem>();
            playerState = player.GetComponent<PlayerStateMachine>();
            gauge = player.GetComponent<TimeGauge>();
            healing = player.GetComponent<PlayerHealing>();
            timeAbilities = player.GetComponent<PlayerTimeAbilities>();
            worldTime = time;
            level = controller;
            flow = flowController;
        }

        private void OnGUI()
        {
            if (flow == null || flow.State != GameFlowState.Playing)
            {
                return;
            }

            labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 19,
                normal = { textColor = Color.white }
            };
            smallStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                normal = { textColor = new Color(0.86f, 0.9f, 0.95f) }
            };

            DrawPlayerPanel();
            DrawObjectivePanel();
            GUI.Label(new Rect(22f, Screen.height - 35f, 560f, 25f),
                "A/D 移动 · K 跳跃 · J 攻击/长按时缓 · L Dash · I 时停/治疗 · M 地图 · Esc 暂停",
                smallStyle);
        }

        private void DrawPlayerPanel()
        {
            GUI.Box(new Rect(18f, 18f, 430f, 178f), GUIContent.none);
            string hearts = health == null
                ? "--"
                : new string('■', health.CurrentHealth)
                    + new string('□', health.MaxHealth - health.CurrentHealth);
            GUI.Label(new Rect(34f, 30f, 390f, 27f), $"生命  {hearts}", labelStyle);
            GUI.Label(new Rect(34f, 58f, 390f, 27f),
                $"时间槽  {(gauge == null ? 0f : gauge.CurrentValue):0}/{(gauge == null ? 0f : gauge.MaxValue):0}",
                labelStyle);
            GUI.Label(new Rect(34f, 86f, 390f, 27f), BuildTimeText(), labelStyle);
            GUI.Label(new Rect(34f, 114f, 390f, 27f),
                healing != null && healing.IsHealing
                    ? $"治疗进度  {healing.HealingProgress:P0}"
                    : timeAbilities != null && timeAbilities.SlowChargeProgress > 0f
                        ? $"时缓蓄力  {timeAbilities.SlowChargeProgress:P0}"
                        : $"状态  {(playerState == null ? "--" : playerState.CurrentState)}",
                labelStyle);
            GUI.Label(new Rect(34f, 146f, 390f, 24f),
                "测试模式：不读取或写入存档", smallStyle);
        }

        private void DrawObjectivePanel()
        {
            float left = Mathf.Max(470f, Screen.width - 510f);
            GUI.Box(new Rect(left, 18f, 490f, 150f), GUIContent.none);
            GUI.Label(new Rect(left + 14f, 30f, 460f, 27f),
                $"房间 {level.CurrentRoomId[1..]}/7 · {level.ActiveCheckpointId}", labelStyle);
            GUI.Label(new Rect(left + 14f, 58f, 460f, 27f),
                level.ObjectiveText, labelStyle);
            string interaction = level.CurrentPrompt ?? level.CurrentFeedback;
            if (!string.IsNullOrEmpty(interaction))
            {
                GUI.Label(new Rect(left + 14f, 92f, 460f, 48f), interaction, smallStyle);
            }
        }

        private string BuildTimeText()
        {
            if (worldTime == null)
            {
                return "世界时间  --";
            }
            return worldTime.CurrentMode switch
            {
                WorldTimeMode.Slowed => $"时缓  {worldTime.SlowRemaining:0.0}s",
                WorldTimeMode.Stopped =>
                    $"时停  {worldTime.TimeStopRemaining:0.0}/{worldTime.TimeStopMaximum:0.0}s",
                _ => "世界时间  正常"
            };
        }
    }
}
