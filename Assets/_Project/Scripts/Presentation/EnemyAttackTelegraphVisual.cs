using TimeMetroidvania.Enemy;
using UnityEngine;

namespace TimeMetroidvania.Presentation
{
    /// <summary>
    /// 用高对比轮廓直观显示普通敌人的攻击前摇与实际威胁方向。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyBrain))]
    public sealed class EnemyAttackTelegraphVisual : MonoBehaviour
    {
        private const int PolygonPointCount = 4;

        private EnemyBrain brain;
        private LineRenderer warningLine;
        private Material warningMaterial;

        private void Awake()
        {
            brain = GetComponent<EnemyBrain>();
            CreateRenderer();
        }

        private void LateUpdate()
        {
            if (warningLine == null || brain == null)
            {
                return;
            }

            warningLine.enabled = brain.IsAttackTelegraphing;
            if (!warningLine.enabled)
            {
                return;
            }

            float progress = brain.AttackTelegraphProgress;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 26f);
            Color color = brain.Kind switch
            {
                EnemyKind.Guard => new Color(1f, 0.12f, 0.06f, 0.95f),
                EnemyKind.ClockworkBug => new Color(1f, 0.82f, 0.08f, 0.95f),
                _ => new Color(0.1f, 0.78f, 1f, 0.95f)
            };
            color.a = Mathf.Lerp(0.5f, 1f, Mathf.Max(progress, pulse));
            warningLine.startColor = color;
            warningLine.endColor = color;
            warningLine.widthMultiplier = Mathf.Lerp(0.07f, 0.16f, pulse);

            if (brain.Kind == EnemyKind.ClockArcher)
            {
                DrawAimLane();
            }
            else if (brain.Kind == EnemyKind.ClockworkBug)
            {
                DrawChargeWedge();
            }
            else
            {
                DrawMeleeBox();
            }
        }

        private void DrawMeleeBox()
        {
            Vector2 origin = transform.position;
            Vector2 range = brain.MeleeHitRange;
            float endX = origin.x + brain.FacingDirection * range.x;
            warningLine.positionCount = PolygonPointCount;
            warningLine.loop = true;
            warningLine.SetPosition(0, new Vector2(origin.x, origin.y - range.y));
            warningLine.SetPosition(1, new Vector2(endX, origin.y - range.y));
            warningLine.SetPosition(2, new Vector2(endX, origin.y + range.y));
            warningLine.SetPosition(3, new Vector2(origin.x, origin.y + range.y));
        }

        private void DrawChargeWedge()
        {
            Vector2 origin = transform.position;
            float endX = origin.x + brain.FacingDirection * brain.MeleeHitRange.x;
            warningLine.positionCount = PolygonPointCount;
            warningLine.loop = true;
            warningLine.SetPosition(0, origin + Vector2.up * 0.38f);
            warningLine.SetPosition(1, new Vector2(endX, origin.y));
            warningLine.SetPosition(2, origin + Vector2.down * 0.38f);
            warningLine.SetPosition(3, origin + new Vector2(brain.FacingDirection * 0.28f, 0f));
        }

        private void DrawAimLane()
        {
            Vector2 origin = (Vector2)transform.position
                + new Vector2(brain.FacingDirection * 0.45f, 0.15f);
            Vector2 target = brain.TargetPosition;
            Vector2 direction = (target - origin).normalized;
            if (direction.sqrMagnitude <= 0.001f)
            {
                direction = Vector2.right * brain.FacingDirection;
            }
            Vector2 side = new(-direction.y, direction.x);
            float halfWidth = 0.09f;
            warningLine.positionCount = PolygonPointCount;
            warningLine.loop = true;
            warningLine.SetPosition(0, origin + side * halfWidth);
            warningLine.SetPosition(1, target + side * halfWidth);
            warningLine.SetPosition(2, target - side * halfWidth);
            warningLine.SetPosition(3, origin - side * halfWidth);
        }

        private void CreateRenderer()
        {
            Transform existing = transform.Find("AttackTelegraphVisual");
            GameObject visual = existing != null
                ? existing.gameObject
                : new GameObject("AttackTelegraphVisual");
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
                    name = "EnemyAttackTelegraph_Runtime"
                };
                warningLine.material = warningMaterial;
            }
            warningLine.useWorldSpace = true;
            warningLine.numCapVertices = 0;
            warningLine.numCornerVertices = 0;
            warningLine.sortingOrder = 45;
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
