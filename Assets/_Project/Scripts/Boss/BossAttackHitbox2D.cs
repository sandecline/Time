using System.Collections.Generic;
using TimeMetroidvania.Enemy;
using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Boss
{
    /// <summary>Boss 近战的独立 Trigger；每次有效段只允许同一玩家受伤一次。</summary>
    public sealed class BossAttackHitbox2D : MonoBehaviour
    {
        [SerializeField] private bool showDebugHitbox;
        private BoxCollider2D hitbox;
        private Transform owner;
        private readonly HashSet<HealthSystem> struck = new();
        private readonly Collider2D[] candidates = new Collider2D[24];
        private int damage;
        private Vector2 knockback;
        public bool IsActive => hitbox != null && hitbox.enabled;
        public Vector2 WorldSize => hitbox != null ? hitbox.bounds.size : Vector2.zero;

        private void Awake()
        {
            owner = transform.parent;
            hitbox = GetComponent<BoxCollider2D>() ?? gameObject.AddComponent<BoxCollider2D>();
            hitbox.isTrigger = true;
            hitbox.enabled = false;
        }

        public void Begin(Vector2 worldCenter, Vector2 worldSize, int amount, Vector2 impulse)
        {
            owner ??= transform.parent;
            hitbox ??= GetComponent<BoxCollider2D>();
            if (hitbox == null) return;
            struck.Clear();
            damage = amount;
            knockback = impulse;
            Vector3 scale = owner != null ? owner.lossyScale : Vector3.one;
            transform.localScale = new Vector3(1f / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
                1f / Mathf.Max(0.001f, Mathf.Abs(scale.y)), 1f);
            transform.position = worldCenter;
            transform.rotation = Quaternion.identity;
            hitbox.size = worldSize;
            hitbox.enabled = true;
            Tick();
        }

        public void Tick()
        {
            if (!IsActive) return;
            int count = Physics2D.OverlapBox(transform.position, hitbox.bounds.size, 0f,
                new ContactFilter2D { useTriggers = true }, candidates);
            for (int i = 0; i < count; i++)
            {
                Collider2D candidate = candidates[i];
                if (candidate == null) continue;
                HealthSystem player = candidate.GetComponentInParent<HealthSystem>();
                if (player == null || !player.CompareTag("Player") || struck.Contains(player)) continue;
                if (owner != null && WorldCollision2D.Blocks(owner.position, player.transform.position))
                    continue;
                if (player.TryTakeDamage(new DamageInfo(owner != null ? owner.gameObject : gameObject,
                    damage, candidate.ClosestPoint(transform.position), knockback)))
                    struck.Add(player);
            }
        }

        public void End()
        {
            if (hitbox != null) hitbox.enabled = false;
        }

        private void OnDisable() => End();

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!showDebugHitbox) return;
            BoxCollider2D box = GetComponent<BoxCollider2D>();
            if (box == null) return;
            Gizmos.color = box.enabled ? new Color(1f, 0.15f, 0.06f)
                : new Color(0.5f, 0.5f, 0.5f, 0.45f);
            Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
        }
#endif
    }
}
