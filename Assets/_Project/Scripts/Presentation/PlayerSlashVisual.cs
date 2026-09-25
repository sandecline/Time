using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Presentation
{
    /// <summary>
    /// 绘制与玩家平砍判定范围一致的短暂刀光，仅负责视觉反馈。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerCombat), typeof(PlayerMovement))]
    public sealed class PlayerSlashVisual : MonoBehaviour
    {
        private const int PointCount = 9;

        private PlayerCombat combat;
        private PlayerMovement movement;
        private LineRenderer slash;
        private Material slashMaterial;
        private float lingerRemaining;

        private void Awake()
        {
            combat = GetComponent<PlayerCombat>();
            movement = GetComponent<PlayerMovement>();
            CreateSlashRenderer();
        }

        private void LateUpdate()
        {
            if (slash == null || combat == null || movement == null)
            {
                return;
            }

            if (combat.IsSlashVisualVisible)
            {
                lingerRemaining = 0.08f;
            }
            else
            {
                lingerRemaining = Mathf.Max(
                    0f,
                    lingerRemaining - Time.unscaledDeltaTime);
            }

            slash.enabled = combat.IsSlashVisualVisible || lingerRemaining > 0f;
            if (!slash.enabled)
            {
                return;
            }

            Vector2 center = combat.CurrentAttackCenter;
            Vector2 halfSize = combat.AttackSize * 0.5f;
            float facing = movement.FacingDirection;
            Vector3 start = center + new Vector2(-facing * halfSize.x, -halfSize.y);
            Vector3 control = center + new Vector2(facing * halfSize.x * 3f, 0f);
            Vector3 end = center + new Vector2(-facing * halfSize.x, halfSize.y);

            for (int index = 0; index < PointCount; index++)
            {
                float time = index / (PointCount - 1f);
                float inverse = 1f - time;
                Vector3 point = inverse * inverse * start
                    + 2f * inverse * time * control
                    + time * time * end;
                slash.SetPosition(index, point);
            }
        }

        private void OnDestroy()
        {
            if (slashMaterial != null)
            {
                Destroy(slashMaterial);
            }
        }

        private void CreateSlashRenderer()
        {
            Transform existing = transform.Find("SlashVisual");
            GameObject visual = existing != null
                ? existing.gameObject
                : new GameObject("SlashVisual");
            visual.transform.SetParent(transform, false);

            slash = visual.GetComponent<LineRenderer>();
            if (slash == null)
            {
                slash = visual.AddComponent<LineRenderer>();
            }

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                slashMaterial = new Material(shader)
                {
                    name = "PlayerSlashVisual_Runtime"
                };
                slash.material = slashMaterial;
            }

            slash.useWorldSpace = true;
            slash.positionCount = PointCount;
            slash.widthMultiplier = 0.16f;
            slash.numCapVertices = 0;
            slash.numCornerVertices = 0;
            slash.textureMode = LineTextureMode.Stretch;
            slash.sortingOrder = 30;
            slash.colorGradient = new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(new Color(0.55f, 0.95f, 1f), 0f),
                    new GradientColorKey(Color.white, 0.5f),
                    new GradientColorKey(new Color(0.2f, 0.8f, 1f), 1f)
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.18f),
                    new GradientAlphaKey(0.9f, 0.78f),
                    new GradientAlphaKey(0f, 1f)
                }
            };
            slash.enabled = false;
        }
    }
}
