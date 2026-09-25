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

            warningLine.enabled = brain.IsAttackTelegraphing;
            if (!warningLine.enabled)
            {
                return;
            }

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 22f);
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
            Vector2 target = brain.TargetPosition;
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

        private void DrawBox(Vector2 center, Vector2 halfSize)
        {
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
