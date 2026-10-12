using TimeMetroidvania.Boss;
using UnityEngine;

namespace TimeMetroidvania.Presentation
{
    /// <summary>灰盒 Boss 的武器蓄力、横扫与能力提示；读取状态机进度，不参与伤害。</summary>
    [RequireComponent(typeof(BossBrain))]
    public sealed class BossGreyboxPoseVisual : MonoBehaviour
    {
        private BossBrain brain;
        private LineRenderer weapon;
        private Material material;

        private void Awake()
        {
            brain = GetComponent<BossBrain>();
            Transform existing = transform.Find("BossGreyboxWeapon");
            GameObject visual = existing != null ? existing.gameObject : new GameObject("BossGreyboxWeapon");
            visual.transform.SetParent(transform, false);
            weapon = visual.GetComponent<LineRenderer>();
            if (weapon == null) weapon = visual.AddComponent<LineRenderer>();
            if (weapon == null)
            {
                Debug.LogError("Boss 灰盒武器缺少 LineRenderer，无法显示攻击动作。", visual);
                return;
            }
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                material = new Material(shader);
                weapon.sharedMaterial = material;
            }
            weapon.useWorldSpace = true;
            weapon.positionCount = 2;
            weapon.sortingOrder = 47;
            weapon.enabled = false;
        }

        private void LateUpdate()
        {
            if (weapon == null || brain == null) return;
            weapon.enabled = brain.IsEngaged && !brain.IsPhaseTransitioning
                && brain.CurrentAttack != BossAttackType.None && brain.Phase != BossPhase.Defeated;
            if (!weapon.enabled) return;

            int facing = brain.FacingDirection;
            float t = brain.AttackProgress;
            Vector2 origin = (Vector2)transform.position + new Vector2(facing * 0.35f, 0.65f);
            Vector2 end;
            Color color;
            switch (brain.CurrentAttack)
            {
                case BossAttackType.Sweep:
                    color = new Color(1f, 0.18f, 0.09f);
                    end = brain.IsAttackTelegraphing
                        ? origin + new Vector2(-facing * 0.8f, 1.25f + t * 0.35f)
                        : brain.IsAttackActive
                            ? origin + new Vector2(facing * brain.SweepHitRange.x, 0f)
                            : origin + new Vector2(facing * 0.85f, 0.9f);
                    break;
                case BossAttackType.LeapSlam:
                    color = new Color(1f, 0.82f, 0.1f);
                    end = brain.IsAttackActive
                        ? origin + new Vector2(0f, -brain.SlamHitRange.y)
                        : origin + new Vector2(-facing * 0.55f, 1.3f);
                    break;
                case BossAttackType.TimeAnchor:
                    color = new Color(0.78f, 0.2f, 1f);
                    end = origin + new Vector2(facing * (0.9f + t * 0.65f), 0.75f);
                    break;
                default:
                    color = new Color(0.15f, 0.83f, 1f);
                    end = origin + new Vector2(facing * (0.8f + t * 0.5f), 0.4f);
                    break;
            }
            weapon.startColor = weapon.endColor = color;
            weapon.widthMultiplier = brain.IsAttackActive ? 0.22f : 0.16f;
            if (brain.IsAttackActive && brain.CurrentAttack == BossAttackType.Sweep)
            {
                Vector2 halfRange = brain.SweepHitRange;
                Vector2 center = (Vector2)transform.position
                    + Vector2.right * facing * halfRange.x * 0.5f;
                // 有效攻击线向碰撞框内收半个线宽，端点不越过横扫边界。
                float inset = weapon.widthMultiplier * 0.5f;
                weapon.positionCount = 3;
                weapon.SetPosition(0, center + new Vector2(-facing * (halfRange.x * 0.5f - inset), -halfRange.y + inset));
                weapon.SetPosition(1, center + Vector2.up * (halfRange.y - inset));
                weapon.SetPosition(2, center + new Vector2(facing * (halfRange.x * 0.5f - inset), -halfRange.y + inset));
                return;
            }
            if (brain.IsAttackActive && brain.CurrentAttack == BossAttackType.LeapSlam)
            {
                Vector2 halfRange = brain.SlamHitRange;
                Vector2 center = transform.position;
                float inset = weapon.widthMultiplier * 0.5f;
                weapon.positionCount = 3;
                weapon.SetPosition(0, center + new Vector2(-halfRange.x + inset, -halfRange.y + inset));
                weapon.SetPosition(1, center + Vector2.up * (halfRange.y - inset));
                weapon.SetPosition(2, center + new Vector2(halfRange.x - inset, -halfRange.y + inset));
                return;
            }
            weapon.positionCount = 2;
            weapon.SetPosition(0, origin);
            weapon.SetPosition(1, end);
        }

        private void OnDisable()
        {
            if (weapon != null) weapon.enabled = false;
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
