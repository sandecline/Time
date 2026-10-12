using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Presentation
{
    /// <summary>
    /// 绘制与当前横砍、上砍或下劈判定一致的临时刀光。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerCombat))]
    public sealed class PlayerSlashVisual : MonoBehaviour
    {
        private const int PointCount = 17;
        private const float ActiveSlashWidth = 0.19f;
        private const float LingeringSlashWidth = 0.07f;

        private PlayerCombat combat;
        private LineRenderer slash;
        private Material slashMaterial;
        private float lingerRemaining;

        private void Awake()
        {
            combat = GetComponent<PlayerCombat>();
            CreateSlashRenderer();
        }

        private void LateUpdate()
        {
            if (slash == null || combat == null)
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
                    lingerRemaining - Time.deltaTime);
            }

            slash.enabled = combat.IsSlashVisualVisible || lingerRemaining > 0f;
            float strength = combat.IsAttackActive ? 1f : 0.35f;
            slash.startColor = slash.endColor = new Color(0.65f, 0.95f, 1f, strength);
            slash.widthMultiplier = combat.IsAttackActive
                ? ActiveSlashWidth
                : LingeringSlashWidth;
            if (!slash.enabled)
            {
                return;
            }

            ResolveArc(out Vector3 start, out Vector3 control, out Vector3 end);
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

        private void ResolveArc(
            out Vector3 start,
            out Vector3 control,
            out Vector3 end)
        {
            Vector2 center = combat.CurrentAttackCenter;
            Vector2 halfSize = combat.AttackSize * 0.5f;
            // 刀光线条本身有宽度，中心线向判定框内收，避免视觉效果越界。
            float inset = ActiveSlashWidth * 0.5f;
            halfSize.x = Mathf.Max(0f, halfSize.x - inset);
            halfSize.y = Mathf.Max(0f, halfSize.y - inset);
            float facing = combat.CurrentAttackFacing;

            switch (combat.CurrentAttackDirection)
            {
                case AttackDirection.Up:
                    start = center + new Vector2(-halfSize.x, -halfSize.y * 0.3f);
                    control = center + new Vector2(0f, halfSize.y * 2f);
                    end = center + new Vector2(halfSize.x, -halfSize.y * 0.3f);
                    break;
                case AttackDirection.Down:
                    start = center + new Vector2(-halfSize.x, halfSize.y * 0.3f);
                    control = center + new Vector2(0f, -halfSize.y * 2f);
                    end = center + new Vector2(halfSize.x, halfSize.y * 0.3f);
                    break;
                default:
                    start = center + new Vector2(-facing * halfSize.x, -halfSize.y);
                    control = center + new Vector2(facing * halfSize.x * 3f, 0f);
                    end = center + new Vector2(-facing * halfSize.x, halfSize.y);
                    return;
            }

            if (facing < 0f)
            {
                (start, end) = (end, start);
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
            // 两端收尖，保留判定内的最大宽度，避免刀光看起来像粗折线。
            slash.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(0.2f, 0.75f),
                new Keyframe(0.5f, 1f), new Keyframe(0.8f, 0.75f),
                new Keyframe(1f, 0f));
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
