using TimeMetroidvania.GameTime;
using TimeMetroidvania.Presentation;
using UnityEngine;

namespace TimeMetroidvania.Boss
{
    /// <summary>
    /// 为 Boss 预热普通、可斩和时间锚弹幕，战斗中不创建新对象。
    /// </summary>
    public sealed class BossProjectilePool : MonoBehaviour
    {
        [SerializeField, Min(1)] private int capacity = 512;
        [SerializeField] private Sprite projectileSprite;
        [SerializeField] private Sprite breakableProjectileSprite;
        [SerializeField] private Sprite unbreakableProjectileSprite;
        [SerializeField] private Sprite timeAnchorProjectileSprite;
        [SerializeField] private WorldTimeService worldTime;

        private BossProjectile[] projectiles;
        private Sprite[] generatedSprites;
        private int activeCount;
        private int nextSearchIndex;

        public int Capacity => capacity;
        public int ActiveCount => activeCount;
        public int TotalShotsFired { get; private set; }
        public int FailedShots { get; private set; }

        private void Awake()
        {
            generatedSprites = new Sprite[3];
            Sprite blue = breakableProjectileSprite;
            Sprite orange = unbreakableProjectileSprite;
            Sprite purple = timeAnchorProjectileSprite;
            if (blue == null) blue = generatedSprites[0] = CreateShapeSprite(0);
            if (orange == null) orange = generatedSprites[1] = CreateShapeSprite(1);
            if (purple == null) purple = generatedSprites[2] = CreateShapeSprite(2);
            projectiles = new BossProjectile[capacity];
            for (int index = 0; index < capacity; index++)
            {
                GameObject projectile = new($"BossProjectile_{index}");
                projectile.transform.SetParent(transform, false);
                projectile.transform.localScale = Vector3.one;

                GameObject visual = new("Visual");
                visual.transform.SetParent(projectile.transform, false);
                visual.transform.localScale = Vector3.one * 0.055f;
                SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = blue;
                renderer.sortingOrder = 20;
                CircleCollider2D collider = projectile.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.radius = 0.22f;
                Rigidbody2D body = projectile.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                LocalTimeController localTime = projectile.AddComponent<LocalTimeController>();
                localTime.SetWorldTime(worldTime);
                projectiles[index] = projectile.AddComponent<BossProjectile>();
                projectiles[index].ConfigureVisuals(
                    blue, orange, purple);
                projectile.SetActive(false);
            }
            TimeStopProjectileShatter.Prewarm(64, transform);
        }

        private static Sprite CreateShapeSprite(int kind)
        {
            const int side = 16;
            Texture2D texture = new(side, side, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = kind == 0 ? "BossBlueCircle" : kind == 1
                    ? "BossOrangeDiamond" : "BossPurpleTriangle"
            };
            Color32[] pixels = new Color32[side * side];
            for (int y = 0; y < side; y++)
            for (int x = 0; x < side; x++)
            {
                float dx = x - 7.5f;
                float dy = y - 7.5f;
                bool filled = kind switch
                {
                    0 => dx * dx + dy * dy <= 42.25f,
                    1 => Mathf.Abs(dx) + Mathf.Abs(dy) <= 7f,
                    _ => dy >= -6f && dy <= 6f
                        && Mathf.Abs(dx) <= (6f - dy) * 0.55f
                };
                pixels[y * side + x] = filled
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(0, 0, 0, 0);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite shape = Sprite.Create(texture, new Rect(0f, 0f, side, side),
                new Vector2(0.5f, 0.5f), 32f);
            shape.name = texture.name;
            return shape;
        }

        private void OnDestroy()
        {
            if (generatedSprites == null) return;
            foreach (Sprite shape in generatedSprites)
            {
                if (shape == null) continue;
                Texture2D texture = shape.texture;
                Destroy(shape);
                if (texture != null) Destroy(texture);
            }
        }

        public bool TryFire(
            Vector2 origin, Vector2 direction, float speed,
            bool breakable, bool timeAnchored)
        {
            if (projectiles == null || direction.sqrMagnitude < 0.01f)
            {
                return false;
            }

            for (int offset = 0; offset < projectiles.Length; offset++)
            {
                int index = (nextSearchIndex + offset) % projectiles.Length;
                BossProjectile projectile = projectiles[index];
                if (projectile.gameObject.activeSelf)
                {
                    continue;
                }

                projectile.Launch(
                    this, origin, direction, speed, breakable, timeAnchored);
                activeCount++;
                nextSearchIndex = (index + 1) % projectiles.Length;
                TotalShotsFired++;
                return true;
            }

            FailedShots++;
            return false;
        }

        internal void Recycle(BossProjectile projectile)
        {
            if (projectile == null || !projectile.gameObject.activeSelf)
            {
                return;
            }

            projectile.gameObject.SetActive(false);
            activeCount = Mathf.Max(0, activeCount - 1);
        }

        public void ClearActive()
        {
            if (projectiles == null)
            {
                return;
            }

            foreach (BossProjectile projectile in projectiles)
            {
                Recycle(projectile);
            }
        }

        public void FadeActive(float duration)
        {
            if (projectiles == null) return;
            foreach (BossProjectile projectile in projectiles)
            {
                if (projectile.gameObject.activeSelf) projectile.BeginFade(duration);
            }
        }
    }
}
