using TimeMetroidvania.Boss;
using UnityEngine;

namespace TimeMetroidvania.Presentation
{
    /// <summary>
    /// 在 Boss 攻击前摇阶段绘制与近战范围或弹道方向一致的危险轮廓。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BossBrain))]
    public sealed class BossAttackTelegraphVisual : MonoBehaviour
    {
        private BossBrain brain;
        private LineRenderer warningLine;
        private readonly LineRenderer[] volleyLines = new LineRenderer[80];
        private readonly LineRenderer[] anchorLines = new LineRenderer[3];
        private Material warningMaterial;

        private void Awake()
        {
            brain = GetComponent<BossBrain>();
            CreateRenderer();
        }

        private void LateUpdate()
        {
            if (warningLine == null || brain == null)
            {
                return;
            }

            bool showVolley = brain.IsAttackTelegraphing
                && brain.CurrentAttack == BossAttackType.ProjectileVolley;
            bool showPattern = brain.IsPatternAttack
                && brain.VolleyWavesFired < brain.PatternWaveCount;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 22f);
            UpdateVolleyWarnings(showVolley || showPattern, pulse);
            UpdateAnchorWarnings(pulse);
            warningLine.enabled = brain.IsAttackTelegraphing && !showVolley && !showPattern;
            if (!warningLine.enabled)
            {
                return;
            }

            Color warning = brain.CurrentAttack switch
            {
                BossAttackType.Sweep => new Color(1f, 0.1f, 0.05f, 0.95f),
                BossAttackType.LeapSlam => new Color(1f, 0.82f, 0.08f, 0.95f),
                BossAttackType.ProjectileVolley => new Color(0.08f, 0.76f, 1f, 0.95f),
                BossAttackType.TimeAnchor => new Color(0.78f, 0.18f, 1f, 0.95f),
                _ => Color.white
            };
            warning.a = Mathf.Lerp(0.58f, 1f, pulse);
            warningLine.startColor = warning;
            warningLine.endColor = warning;
            warningLine.widthMultiplier = Mathf.Lerp(0.1f, 0.2f, pulse);

            switch (brain.CurrentAttack)
            {
                case BossAttackType.Sweep:
                    DrawSweepRange();
                    break;
                case BossAttackType.LeapSlam:
                    DrawSlamRange();
                    break;
                default:
                    DrawAimLane();
                    break;
            }
        }

        private void DrawSweepRange()
        {
            Vector2 origin = transform.position;
            Vector2 range = brain.SweepHitRange;
            float endX = origin.x + brain.FacingDirection * range.x;
            DrawBox(
                new Vector2((origin.x + endX) * 0.5f, origin.y),
                new Vector2(Mathf.Abs(endX - origin.x) * 0.5f, range.y));
        }

        private void DrawSlamRange()
        {
            DrawBox(brain.TelegraphTargetPosition, brain.SlamHitRange);
        }

        private void DrawAimLane()
        {
            Vector2 origin = (Vector2)transform.position + Vector2.up * 0.35f;
            Vector2 target = brain.TelegraphAimPosition;
            Vector2 direction = (target - origin).normalized;
            if (direction.sqrMagnitude <= 0.001f)
            {
                direction = Vector2.right * brain.FacingDirection;
            }
            Vector2 side = new(-direction.y, direction.x);
            const float HalfWidth = 0.13f;
            warningLine.positionCount = 4;
            warningLine.loop = true;
            warningLine.SetPosition(0, origin + side * HalfWidth);
            warningLine.SetPosition(1, target + side * HalfWidth);
            warningLine.SetPosition(2, target - side * HalfWidth);
            warningLine.SetPosition(3, origin - side * HalfWidth);
        }

        private void UpdateVolleyWarnings(bool show, float pulse)
        {
            bool pattern = brain.IsPatternAttack;
            int firstWave = pattern ? brain.VolleyWavesFired : 0;
            int previewCount = pattern && brain.CurrentAttack != BossAttackType.ClockFan
                ? Mathf.Min(2, brain.PatternWaveCount - firstWave) : 1;
            int shots = pattern ? brain.PatternShotCount : brain.VolleyShotCount;
            for (int index = 0; index < volleyLines.Length; index++)
            {
                if (!show || index >= shots * previewCount)
                {
                    if (volleyLines[index] != null)
                    {
                        volleyLines[index].enabled = false;
                    }
                    continue;
                }

                int wave = firstWave + index / shots;
                Vector2 origin = Vector2.zero;
                Vector2 direction;
                bool breakable;
                bool valid = pattern
                    ? brain.TryGetPatternShot(wave, index % shots,
                        out origin, out direction, out breakable)
                    : brain.TryGetVolleyRay(index, out direction, out breakable);
                if (!valid)
                {
                    continue;
                }
                if (!pattern) origin = brain.VolleyOrigin;

                LineRenderer line = EnsureVolleyLine(index);
                Color color = breakable
                    ? new Color(0.08f, 0.76f, 1f)
                    : new Color(1f, 0.48f, 0.12f);
                color.a = Mathf.Lerp(0.45f, 0.85f, pulse);
                if (pattern && wave > firstWave) color.a *= 0.52f;
                line.startColor = color;
                line.endColor = color;
                line.widthMultiplier = Mathf.Lerp(0.055f, 0.095f, pulse);
                line.SetPosition(0, origin);
                line.SetPosition(1, origin + direction
                    * (pattern ? brain.CurrentAttack == BossAttackType.Crossfire ? 9f : 8f : 5.5f));
                line.enabled = true;
            }
        }

        private LineRenderer EnsureVolleyLine(int index)
        {
            if (volleyLines[index] != null)
            {
                return volleyLines[index];
            }

            GameObject ray = new($"VolleyWarning_{index}");
            ray.transform.SetParent(warningLine.transform, false);
            LineRenderer line = ray.AddComponent<LineRenderer>();
            line.material = warningMaterial;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.sortingOrder = 46;
            line.numCapVertices = 0;
            line.numCornerVertices = 0;
            volleyLines[index] = line;
            return line;
        }

        private void UpdateAnchorWarnings(float pulse)
        {
            for (int index = 0; index < anchorLines.Length; index++)
            {
                bool visible = brain.TryGetAnchorWarning(index,
                    out Vector2 origin, out Vector2 aim, out float remaining);
                if (!visible)
                {
                    if (anchorLines[index] != null) anchorLines[index].enabled = false;
                    continue;
                }
                if (anchorLines[index] == null)
                {
                    GameObject warning = new($"AnchorWarning_{index}");
                    warning.transform.SetParent(warningLine.transform, false);
                    anchorLines[index] = warning.AddComponent<LineRenderer>();
                    anchorLines[index].material = warningMaterial;
                    anchorLines[index].useWorldSpace = true;
                    anchorLines[index].positionCount = 2;
                    anchorLines[index].sortingOrder = 47;
                }
                LineRenderer line = anchorLines[index];
                Color purple = new(0.78f, 0.18f, 1f,
                    Mathf.Lerp(0.4f, 1f, pulse) * (1f - remaining * 0.35f));
                line.startColor = line.endColor = purple;
                line.widthMultiplier = Mathf.Lerp(0.1f, 0.18f, pulse);
                line.SetPosition(0, origin);
                line.SetPosition(1, aim);
                line.enabled = true;
            }
        }

        private void DrawBox(Vector2 center, Vector2 halfSize)
        {
            // 预警轮廓向实际判定框内收半个线宽，外缘与命中边界对齐。
            float inset = warningLine.widthMultiplier * 0.5f;
            halfSize.x = Mathf.Max(0f, halfSize.x - inset);
            halfSize.y = Mathf.Max(0f, halfSize.y - inset);
            warningLine.positionCount = 4;
            warningLine.loop = true;
            warningLine.SetPosition(0, center + new Vector2(-halfSize.x, -halfSize.y));
            warningLine.SetPosition(1, center + new Vector2(halfSize.x, -halfSize.y));
            warningLine.SetPosition(2, center + new Vector2(halfSize.x, halfSize.y));
            warningLine.SetPosition(3, center + new Vector2(-halfSize.x, halfSize.y));
        }

        private void CreateRenderer()
        {
            Transform existing = transform.Find("BossAttackTelegraphVisual");
            GameObject visual = existing != null
                ? existing.gameObject
                : new GameObject("BossAttackTelegraphVisual");
            visual.transform.SetParent(transform, false);
            warningLine = visual.GetComponent<LineRenderer>();
            if (warningLine == null)
            {
                warningLine = visual.AddComponent<LineRenderer>();
            }

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                warningMaterial = new Material(shader)
                {
                    name = "BossAttackTelegraph_Runtime"
                };
                warningLine.material = warningMaterial;
            }
            warningLine.useWorldSpace = true;
            warningLine.numCapVertices = 0;
            warningLine.numCornerVertices = 0;
            warningLine.sortingOrder = 46;
            warningLine.enabled = false;
        }

        private void OnDestroy()
        {
            if (warningMaterial != null)
            {
                Destroy(warningMaterial);
            }
        }
    }
}
