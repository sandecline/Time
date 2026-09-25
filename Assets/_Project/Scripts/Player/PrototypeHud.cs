using UnityEngine;
using TimeMetroidvania.GameTime;

namespace TimeMetroidvania.Player
{
    /// <summary>
    /// 灰盒阶段的诊断界面，用于直接观察生命与状态切换。
    /// </summary>
    public sealed class PrototypeHud : MonoBehaviour
    {
        [SerializeField] private HealthSystem playerHealth;
        [SerializeField] private PlayerStateMachine playerState;
        [SerializeField] private TimeGauge timeGauge;
        [SerializeField] private PlayerHealing playerHealing;
        [SerializeField] private PlayerTimeAbilities playerTimeAbilities;
        [SerializeField] private WorldTimeService worldTime;

        private GUIStyle labelStyle;

        private void OnGUI()
        {
            labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                normal = { textColor = Color.white }
            };

            string healthText = playerHealth == null
                ? "生命：未连接"
                : $"生命：{playerHealth.CurrentHealth}/{playerHealth.MaxHealth}";
            string stateText = playerState == null
                ? "状态：未连接"
                : $"状态：{playerState.CurrentState}";
            string gaugeText = timeGauge == null
                ? "时间槽：未连接"
                : $"时间槽：{timeGauge.CurrentValue:0.#}/{timeGauge.MaxValue:0}";
            string healingText = playerHealing != null && playerHealing.IsHealing
                ? $"治疗：{playerHealing.HealingProgress:P0}"
                : "治疗：长按 I";
            string worldTimeText = BuildWorldTimeText();
            string chargeText = playerTimeAbilities != null && playerTimeAbilities.SlowChargeProgress > 0f
                ? $"时缓蓄力：{playerTimeAbilities.SlowChargeProgress:P0}"
                : "时缓：长按 J 1 秒后松开";

            GUI.Box(new Rect(18f, 18f, 420f, 220f), GUIContent.none);
            GUI.Label(new Rect(34f, 30f, 280f, 28f), healthText, labelStyle);
            GUI.Label(new Rect(34f, 56f, 280f, 28f), stateText, labelStyle);
            GUI.Label(new Rect(34f, 82f, 310f, 28f), gaugeText, labelStyle);
            GUI.Label(new Rect(34f, 108f, 310f, 28f), healingText, labelStyle);
            GUI.Label(new Rect(34f, 134f, 370f, 28f), worldTimeText, labelStyle);
            GUI.Label(new Rect(34f, 160f, 370f, 28f), chargeText, labelStyle);
            GUI.Label(new Rect(34f, 198f, 390f, 24f), "A/D 移动  K 跳跃  J 攻击/时缓  L Dash  I 时停/治疗");
        }

        private string BuildWorldTimeText()
        {
            if (worldTime == null)
            {
                return "世界时间：未连接";
            }

            return worldTime.CurrentMode switch
            {
                WorldTimeMode.Slowed => $"世界时间：时缓 ×0.35（{worldTime.SlowRemaining:0.0}s）",
                WorldTimeMode.Stopped =>
                    $"世界时间：时停（{worldTime.TimeStopRemaining:0.0}/{worldTime.TimeStopMaximum:0.0}s）",
                _ => "世界时间：正常"
            };
        }
    }
}
