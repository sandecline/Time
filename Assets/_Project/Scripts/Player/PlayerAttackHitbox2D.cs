using UnityEngine;

namespace TimeMetroidvania.Player
{
    /// <summary>
    /// 独立于玩家本体碰撞框的挥砍 Trigger，负责攻击窗口内的接触查询。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class PlayerAttackHitbox2D : MonoBehaviour
    {
        [SerializeField] private BoxCollider2D hitbox;

        private readonly AttackHitRegistry hitTargets = new();

        public bool IsActive => hitbox != null && hitbox.enabled;
        public AttackDirection Direction { get; private set; }
        public Vector2 Size => hitbox != null ? hitbox.size : Vector2.zero;
        public Vector2 WorldSize
        {
            get
            {
                Vector2 localSize = Size;
                Vector3 scale = transform.lossyScale;
                return new Vector2(
                    Mathf.Abs(localSize.x * scale.x),
                    Mathf.Abs(localSize.y * scale.y));
            }
        }
        public Vector2 WorldCenter => transform.TransformPoint(
            hitbox != null ? hitbox.offset : Vector2.zero);
        public BoxCollider2D HitboxCollider => hitbox;

        private void Awake()
        {
            EnsureHitbox();
        }

        private void Reset()
        {
            EnsureHitbox();
            SetActive(false);
        }

        public void ConfigureLayer(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
            {
                gameObject.layer = layer;
            }
        }

        public void SetShape(
            AttackDirection direction,
            Vector2 localOffset,
            Vector2 size,
            int facingDirection)
        {
            EnsureHitbox();
            Direction = direction;
            Vector2 orientedOffset = localOffset;
            if (direction == AttackDirection.Horizontal)
            {
                orientedOffset.x *= facingDirection >= 0 ? 1f : -1f;
            }

            Vector3 parentScale = transform.parent != null
                ? transform.parent.lossyScale
                : Vector3.one;
            if (direction != AttackDirection.Horizontal)
            {
                // 纵向刀光使用世界单位，避免角色本体的纵向缩放放大攻击范围。
                float parentScaleX = Mathf.Abs(parentScale.x) < 0.0001f
                    ? 1f
                    : parentScale.x;
                float parentScaleY = Mathf.Abs(parentScale.y) < 0.0001f
                    ? 1f
                    : parentScale.y;
                transform.localPosition = new Vector3(
                    orientedOffset.x / parentScaleX,
                    orientedOffset.y / parentScaleY,
                    0f);
            }
            else
            {
                transform.localPosition = orientedOffset;
            }
            transform.localRotation = Quaternion.identity;
            hitbox.offset = Vector2.zero;
            Vector3 worldScale = transform.lossyScale;
            hitbox.size = direction == AttackDirection.Horizontal
                ? new Vector2(
                    Mathf.Max(0.01f, size.x),
                    Mathf.Max(0.01f, size.y))
                : new Vector2(
                    Mathf.Max(0.01f, size.x / Mathf.Max(0.0001f, Mathf.Abs(worldScale.x))),
                    Mathf.Max(0.01f, size.y / Mathf.Max(0.0001f, Mathf.Abs(worldScale.y))));
        }

        public void BeginSwing()
        {
            hitTargets.Clear();
            SetActive(false);
        }

        public bool TryRegister(object target)
        {
            return hitTargets.TryRegister(target);
        }

        public void SetActive(bool active)
        {
            EnsureHitbox();
            hitbox.enabled = active;
        }

        public int CollectOverlaps(ContactFilter2D filter, Collider2D[] results)
        {
            if (!IsActive || results == null || results.Length == 0)
            {
                return 0;
            }

            return hitbox.Overlap(filter, results);
        }

        private void EnsureHitbox()
        {
            hitbox ??= GetComponent<BoxCollider2D>();
            hitbox.isTrigger = true;
        }
    }
}
