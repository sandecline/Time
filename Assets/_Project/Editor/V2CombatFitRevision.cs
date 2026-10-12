#if UNITY_EDITOR
using System;
using TimeMetroidvania.Boss;
using TimeMetroidvania.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TimeMetroidvania.Editor
{
    /// <summary>核对上下劈与 Boss 身体贴图的实际世界范围。</summary>
    internal static class V2CombatFitRevision
    {
        private const string ScenePath = "Assets/_Project/Scenes/V2/EightRoomArtPreview.unity";

        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var boss = UnityEngine.Object.FindFirstObjectByType<BossBrain>();
            var body = boss.GetComponent<BoxCollider2D>();
            var visual = boss.transform.Find("ArtVisual").GetComponent<SpriteRenderer>();
            var animator = boss.GetComponent<TimeMetroidvania.Presentation.BossArtAnimator>();
            if (body == null || visual.sprite == null || animator == null)
                throw new InvalidOperationException("Boss 身体贴图或碰撞体缺失。");

            // 旧灰盒将根节点旋转 45 度，导致碰撞框变菱形且竖直贴图横向偏移。
            if (Mathf.Abs(Mathf.DeltaAngle(boss.transform.eulerAngles.z, 45f)) < 0.1f)
                boss.transform.position += Vector3.down * 0.25f;
            boss.transform.rotation = Quaternion.identity;
            visual.transform.rotation = Quaternion.identity;

            var idle = new SerializedObject(animator).FindProperty("idle");
            if (idle == null || idle.arraySize < 2)
                throw new InvalidOperationException("Boss 待机动画帧缺失。");
            Sprite previous = visual.sprite;
            Bounds visible = default;
            for (int i = 0; i < idle.arraySize; i++)
            {
                visual.sprite = idle.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
                if (visual.sprite == null) throw new InvalidOperationException("Boss 待机帧为空。");
                if (i == 0) visible = visual.bounds;
                else visible.Encapsulate(visual.bounds);
            }
            visual.sprite = previous;

            // 身体受击和接触范围以两帧待机的可见像素并集为准；独立攻击框继续管挥击。
            Vector3 scale = boss.transform.lossyScale;
            body.size = new Vector2(visible.size.x / Mathf.Abs(scale.x),
                visible.size.y / Mathf.Abs(scale.y));
            body.offset = boss.transform.InverseTransformPoint(visible.center);
            Physics2D.SyncTransforms();
            if (Vector2.Distance(body.bounds.center, visible.center) > 0.02f
                || Vector2.Distance(body.bounds.size, visible.size) > 0.02f)
                throw new InvalidOperationException("Boss 身体碰撞未与待机贴图对齐。");
            EditorSceneManager.SaveScene(scene);
            CaptureFit(body, visible);
            Debug.Log($"V2_COMBAT_FIT_READY bossVisible={visible}; bodyBounds={body.bounds}; "
                + $"bossRotation={boss.transform.eulerAngles.z:F1}; upDownSize=1.15x1.35; COLLIDER_ALIGNED");
        }

        public static void Report()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var boss = UnityEngine.Object.FindFirstObjectByType<BossBrain>();
            var collider = boss.GetComponent<BoxCollider2D>();
            var sprite = boss.transform.Find("ArtVisual").GetComponent<SpriteRenderer>();
            var player = GameObject.FindWithTag("Player").GetComponent<PlayerCombat>();
            var combat = new SerializedObject(player);
            Debug.Log($"V2_COMBAT_FIT_BEFORE bossRoot={boss.transform.position.ToString("F3")}; "
                + $"rotation={boss.transform.eulerAngles.z:F1}; scale={boss.transform.lossyScale.ToString("F3")}; "
                + $"colliderSize={collider.size.ToString("F3")}; colliderOffset={collider.offset.ToString("F3")}; "
                + $"colliderBounds={collider.bounds}; spriteBounds={sprite.bounds}; "
                + $"spriteName={sprite.sprite.name}; "
                + $"upSize={combat.FindProperty("upAttackSize").vector2Value.ToString("F3")}; "
                + $"downSize={combat.FindProperty("downAttackSize").vector2Value.ToString("F3")}");
            EditorApplication.Exit(0);
        }

        private static void CaptureFit(BoxCollider2D body, Bounds visible)
        {
            var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            Vector3 oldPosition = camera.transform.position;
            float oldSize = camera.orthographicSize;
            var outline = new GameObject("BossColliderCheckOnly");
            var line = outline.AddComponent<LineRenderer>();
            var material = new Material(Shader.Find("Sprites/Default"));
            line.material = material;
            line.useWorldSpace = true;
            line.startColor = line.endColor = new Color(0.3f, 1f, 0.45f);
            line.widthMultiplier = 0.022f;
            line.sortingOrder = 100;
            line.positionCount = 4;
            line.loop = true;
            Bounds bounds = body.bounds;
            line.SetPositions(new[] {
                new Vector3(bounds.min.x, bounds.min.y, 0f),
                new Vector3(bounds.max.x, bounds.min.y, 0f),
                new Vector3(bounds.max.x, bounds.max.y, 0f),
                new Vector3(bounds.min.x, bounds.max.y, 0f)
            });
            try
            {
                camera.transform.position = new Vector3(visible.center.x, visible.center.y, oldPosition.z);
                camera.orthographicSize = 2.3f;
                V2ArtPreviewSmoke.Capture(camera, "ArtSource/V2/Previews/Boss_BodyColliderFit_v01.png");
            }
            finally
            {
                camera.transform.position = oldPosition;
                camera.orthographicSize = oldSize;
                UnityEngine.Object.DestroyImmediate(outline);
                UnityEngine.Object.DestroyImmediate(material);
            }
        }
    }
}
#endif
