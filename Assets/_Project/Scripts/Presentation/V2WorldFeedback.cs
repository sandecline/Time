using TimeMetroidvania.GameTime;
using UnityEngine;

namespace TimeMetroidvania.Presentation
{
    /// <summary>预热火花池、时间钟环与轻量屏幕边缘，不依赖最终美术、不改变玩法计时。</summary>
    [DefaultExecutionOrder(900)]
    public sealed class V2WorldFeedback : MonoBehaviour
    {
        [SerializeField] private Sprite blueImpactSprite;
        [SerializeField] private Sprite orangeImpactSprite;
        [SerializeField] private Sprite timeRingSprite;
        private static V2WorldFeedback active;
        private WorldTimeService world;
        private Transform player;
        private Material material;
        private LineRenderer ring;
        private LineRenderer clockRing;
        private readonly LineRenderer[] impacts = new LineRenderer[16];
        private readonly SpriteRenderer[] impactArt = new SpriteRenderer[16];
        private SpriteRenderer clockArt;
        private readonly float[] ages = new float[16];
        private readonly Vector3[] origins = new Vector3[16];
        private readonly Color[] colors = new Color[16];
        private int next;
        private float pulse, hurt;
        private Vector3 pulseCenter;
        private Color pulseColor;
        private WorldTimeMode mode;
        private void Awake()
        {
            active = this;
            world = FindFirstObjectByType<WorldTimeService>();
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            material = new Material(Shader.Find("Sprites/Default"));
            ring = Create("时间扩散钟环", 65, 0.065f);
            clockRing = Create("时间状态钟环", 49, 0.035f);
            for (int i = 0; i < impacts.Length; i++) impacts[i] = Create("命中火花_" + i, 24, 0.045f);
            if (blueImpactSprite != null && orangeImpactSprite != null)
                for (int i = 0; i < impactArt.Length; i++)
                    impactArt[i] = CreateSprite("命中精灵_" + i, 71);
            if (timeRingSprite != null)
            {
                clockArt = CreateSprite("时间状态精灵", 65);
                clockArt.sprite = timeRingSprite;
                clockArt.transform.localScale = Vector3.one * (1.72f / timeRingSprite.bounds.size.x);
            }
        }
        private void OnEnable() { if (world != null) world.ModeChanged += ModeChanged; }
        private void OnDisable() { if (world != null) world.ModeChanged -= ModeChanged; if (active == this) active = null; }
        private void OnDestroy() { if (material != null) Destroy(material); }
        private LineRenderer Create(string label, int points, float width)
        {
            GameObject go = new(label); go.transform.SetParent(transform, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material; line.useWorldSpace = true;
            line.positionCount = points; line.widthMultiplier = width; line.sortingOrder = 70; line.enabled = false;
            return line;
        }
        private SpriteRenderer CreateSprite(string label, int order)
        {
            GameObject go = new(label); go.transform.SetParent(transform, false);
            SpriteRenderer result = go.AddComponent<SpriteRenderer>();
            result.sortingOrder = order;
            result.enabled = false;
            return result;
        }
        private void ModeChanged(WorldTimeMode previous, WorldTimeMode current)
        {
            mode = current; pulse = 0.6f; pulseCenter = player != null ? player.position : Vector3.zero;
            pulseColor = current == WorldTimeMode.Stopped ? new Color(1f, 0.91f, 0.63f) : new Color(0.28f, 0.85f, 1f);
        }
        public static void Emit(Vector3 position, Color color)
        {
            if (active == null) return;
            int index = active.next++ % active.impacts.Length;
            active.origins[index] = position; active.colors[index] = color; active.ages[index] = 0.18f;
        }
        public static void PlayerHurt() { if (active != null) active.hurt = 0.22f; }
        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            hurt = Mathf.Max(0f, hurt - dt);
            pulse = Mathf.Max(0f, pulse - dt);
            ring.enabled = pulse > 0f;
            if (ring.enabled) Circle(ring, pulseCenter, Mathf.Lerp(7f, 0.6f, pulse / 0.6f),
                WithAlpha(pulseColor, pulse / 0.6f * 0.6f), 0f);
            clockRing.enabled = clockArt == null && mode != WorldTimeMode.Normal && player != null;
            if (clockRing.enabled) Circle(clockRing, player.position, 0.86f,
                WithAlpha(pulseColor, 0.3f), mode == WorldTimeMode.Stopped ? 0f : Time.time * 1.5f);
            if (clockArt != null)
            {
                clockArt.enabled = mode != WorldTimeMode.Normal && player != null;
                if (clockArt.enabled)
                {
                    clockArt.transform.position = player.position;
                    clockArt.color = WithAlpha(Color.white, 0.42f);
                }
            }
            for (int i = 0; i < impacts.Length; i++)
            {
                ages[i] = Mathf.Max(0f, ages[i] - dt);
                LineRenderer line = impacts[i]; line.enabled = impactArt[i] == null && ages[i] > 0f;
                if (impactArt[i] != null)
                {
                    SpriteRenderer visual = impactArt[i];
                    visual.enabled = ages[i] > 0f;
                    if (visual.enabled)
                    {
                        float artProgress = 1f - ages[i] / 0.18f;
                        visual.sprite = colors[i].r > colors[i].g * 1.1f
                            ? orangeImpactSprite : blueImpactSprite;
                        visual.transform.position = origins[i];
                        visual.transform.localScale = Vector3.one *
                            (0.62f / visual.sprite.bounds.size.x * (0.7f + artProgress * 0.3f));
                        visual.color = WithAlpha(Color.white, 1f - artProgress);
                    }
                }
                if (!line.enabled) continue;
                float progress = 1f - ages[i] / 0.18f;
                Color c = WithAlpha(colors[i], 1f - progress); line.startColor = line.endColor = c;
                for (int ray = 0; ray < 8; ray++)
                {
                    float angle = ray * Mathf.PI / 4f;
                    Vector3 direction = new(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                    line.SetPosition(ray * 3, origins[i] + direction * progress * 0.35f);
                    line.SetPosition(ray * 3 + 1, origins[i] + direction * (0.3f + progress * 0.65f));
                    line.SetPosition(ray * 3 + 2, origins[i] + direction * progress * 0.35f);
                }
            }
        }
        private static Color WithAlpha(Color c, float a) { c.a = a; return c; }
        private static void Circle(LineRenderer line, Vector3 center, float radius, Color color, float phase)
        {
            line.startColor = line.endColor = color;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / (line.positionCount - 1) + phase;
                float r = radius * (i % 4 == 0 ? 0.93f : 1f);
                line.SetPosition(i, center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * r);
            }
        }
        private void OnGUI()
        {
            if (mode == WorldTimeMode.Normal && hurt <= 0f) return;
            Color old = GUI.color;
            GUI.color = hurt > 0f ? new Color(1f, 0.18f, 0.12f, hurt * 1.6f) : WithAlpha(pulseColor, 0.12f);
            float border = Mathf.Max(5f, Screen.height * 0.015f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, border), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, Screen.height - border, Screen.width, border), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, 0, border, Screen.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(Screen.width - border, 0, border, Screen.height), Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
