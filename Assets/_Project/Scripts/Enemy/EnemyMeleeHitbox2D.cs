using System.Collections.Generic;
using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Enemy
{
    /// <summary>守卫刀光使用独立世界尺寸攻击框，表现与判定共享有效期，每刀只伤害一次。</summary>
    public sealed class EnemyMeleeHitbox2D : MonoBehaviour
    {
        private BoxCollider2D hitbox;
        private LineRenderer slash;
        private Material material;
        private readonly Collider2D[] overlaps = new Collider2D[12];
        private readonly HashSet<HealthSystem> struck = new();
        private GameObject owner;
        private int facing;
        public Vector2 Size { get; private set; }

        public static EnemyMeleeHitbox2D Create(GameObject owner)
        {
            GameObject go = new("EnemySlashHitbox");
            go.transform.SetParent(owner.transform, false);
            EnemyMeleeHitbox2D attack = go.AddComponent<EnemyMeleeHitbox2D>();
            attack.owner = owner;
            attack.hitbox = go.AddComponent<BoxCollider2D>();
            attack.hitbox.isTrigger = true;
            attack.hitbox.enabled = false;
            attack.slash = go.AddComponent<LineRenderer>();
            attack.material = new Material(Shader.Find("Sprites/Default"));
            attack.slash.sharedMaterial = attack.material;
            attack.slash.positionCount = 9;
            attack.slash.useWorldSpace = true;
            attack.slash.widthMultiplier = 0.12f;
            attack.slash.sortingOrder = 35;
            attack.slash.startColor = attack.slash.endColor = new Color(1f, 0.25f, 0.12f);
            attack.slash.enabled = false;
            return attack;
        }

        public void Begin(int direction, Vector2 size)
        {
            struck.Clear(); facing = direction; Size = size;
            hitbox.enabled = slash.enabled = true;
            Tick();
        }

        public void Tick()
        {
            if (!hitbox.enabled) return;
            Vector3 scale = owner.transform.lossyScale;
            transform.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
            transform.position = owner.transform.position + Vector3.right * facing * (Size.x * 0.5f + 0.15f);
            hitbox.size = Size;
            Vector2 center = transform.position;
            // 挥砍线条向判定框内收半个线宽，避免视觉刀光越过命中边界。
            float inset = slash.widthMultiplier * 0.5f;
            float radiusX = Mathf.Max(0f, Size.x * 0.5f - inset);
            float radiusY = Mathf.Max(0f, Size.y * 0.5f - inset);
            for (int i = 0; i < 9; i++)
            {
                float angle = Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, i / 8f);
                slash.SetPosition(i, center + new Vector2(facing * Mathf.Cos(angle) * radiusX,
                    Mathf.Sin(angle) * radiusY));
            }
            int count = Physics2D.OverlapBox(center, Size, 0f,
                new ContactFilter2D { useTriggers = true }, overlaps);
            for (int i = 0; i < count; i++)
            {
                HealthSystem player = overlaps[i].GetComponentInParent<HealthSystem>();
                if (player == null || !player.CompareTag("Player") || struck.Contains(player)) continue;
                if (WorldCollision2D.Blocks(owner.transform.position, player.transform.position)) continue;
                if (player.TryTakeDamage(new DamageInfo(owner, 1, player.transform.position, new Vector2(facing * 6f, 3f))))
                    struck.Add(player);
            }
        }

        public void End() { if (hitbox != null) hitbox.enabled = false; if (slash != null) slash.enabled = false; }
        private void OnDisable() => End();
        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}
