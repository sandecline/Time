using TimeMetroidvania.Boss;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Level;
using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Presentation
{
    /// <summary>
    /// 使用阶段八哥特钟表 UI 素材显示生命、时间槽、能力和 Boss 血条。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GothicHudPresenter : MonoBehaviour
    {
        [SerializeField] private HealthSystem playerHealth;
        [SerializeField] private TimeGauge timeGauge;
        [SerializeField] private StageFiveLevelController level;
        [SerializeField] private BossBrain boss;
        [SerializeField] private Sprite healthFrame;
        [SerializeField] private Sprite timeGaugeFrame;
        [SerializeField] private Sprite slowIcon;
        [SerializeField] private Sprite timeStopIcon;
        [SerializeField] private Sprite healIcon;
        [SerializeField] private Sprite checkpointIcon;
        [SerializeField] private Sprite bossBarFrame;

        private GUIStyle valueStyle;

        private void OnGUI()
        {
            if (playerHealth == null || timeGauge == null)
            {
                return;
            }

            DrawPlayerStatus();
            DrawBossStatus();
        }

        private void DrawPlayerStatus()
        {
            Rect heartsRect = new(18f, 16f, 255f, 84f);
            DrawSprite(healthFrame, heartsRect);

            int maximum = Mathf.Max(1, playerHealth.MaxHealth);
            float usableWidth = heartsRect.width * 0.78f;
            float heartWidth = usableWidth / maximum;
            float firstHeart = heartsRect.x + heartsRect.width * 0.11f;
            for (int index = playerHealth.CurrentHealth; index < maximum; index++)
            {
                GUI.color = new Color(0.03f, 0.04f, 0.08f, 0.78f);
                GUI.DrawTexture(
                    new Rect(
                        firstHeart + index * heartWidth,
                        heartsRect.y + heartsRect.height * 0.31f,
                        heartWidth * 0.82f,
                        heartsRect.height * 0.43f),
                    Texture2D.whiteTexture);
            }
            GUI.color = Color.white;

            Rect gaugeRect = new(24f, 101f, 106f, 106f);
            DrawSprite(timeGaugeFrame, gaugeRect);
            GUI.color = new Color(0.12f, 0.82f, 0.95f, 0.9f);
            GUI.DrawTexture(
                new Rect(
                    gaugeRect.x + 25f,
                    gaugeRect.y + 79f,
                    56f * timeGauge.NormalizedValue,
                    7f),
                Texture2D.whiteTexture);
            GUI.color = Color.white;

            valueStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                normal = { textColor = Color.white }
            };
            GUI.Label(
                new Rect(gaugeRect.x + 25f, gaugeRect.y + 43f, 56f, 24f),
                $"{Mathf.RoundToInt(timeGauge.CurrentValue)}",
                valueStyle);

            DrawSprite(slowIcon, new Rect(140f, 112f, 48f, 48f));
            DrawSprite(healIcon, new Rect(194f, 112f, 48f, 48f));
            if (level != null && level.WorldTime != null && level.WorldTime.IsTimeStopUnlocked)
            {
                DrawSprite(timeStopIcon, new Rect(248f, 112f, 48f, 48f));
            }
            DrawSprite(checkpointIcon, new Rect(302f, 112f, 48f, 48f));
        }

        private void DrawBossStatus()
        {
            if (boss == null || !boss.IsEngaged || boss.CurrentHealth <= 0)
            {
                return;
            }

            float width = Mathf.Min(680f, Screen.width * 0.62f);
            Rect frame = new((Screen.width - width) * 0.5f, 18f, width, 86f);
            DrawSprite(bossBarFrame, frame);
            float normalized = boss.MaxHealth <= 0
                ? 0f
                : (float)boss.CurrentHealth / boss.MaxHealth;
            GUI.color = boss.Phase == BossPhase.Two
                ? new Color(0.72f, 0.24f, 0.95f, 0.9f)
                : new Color(0.95f, 0.2f, 0.18f, 0.9f);
            GUI.DrawTexture(
                new Rect(
                    frame.x + width * 0.17f,
                    frame.y + frame.height * 0.44f,
                    width * 0.66f * normalized,
                    11f),
                Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static void DrawSprite(Sprite sprite, Rect destination)
        {
            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            Rect source = sprite.rect;
            Texture2D texture = sprite.texture;
            Rect uv = new(
                source.x / texture.width,
                source.y / texture.height,
                source.width / texture.width,
                source.height / texture.height);
            GUI.DrawTextureWithTexCoords(destination, texture, uv, true);
        }
    }
}
