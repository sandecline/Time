#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TimeMetroidvania.Boss;
using TimeMetroidvania.Enemy;
using TimeMetroidvania.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static TimeMetroidvania.Editor.V2ArtSupplementBuilder;

namespace TimeMetroidvania.Editor
{
    /// <summary>将四类敌人和 Boss 的动作图接入独立试玩场景，保持现有碰撞体不变。</summary>
    internal static class V2ActorArtBuilder
    {
        private const string PreviewRoot = "Assets/_Project/Art/V2/Preview/";

        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/V2/EightRoomArtPreview.unity");
            string before = CollisionSignature();
            var guard = Load("ArtSource/V2/02_Enemies/Enemy_Guard_Animation_v03.png", "Enemy_Guard_Animation_v03.png", 1536, 1024,
                new[] { 0, 384, 768, 1152, 1536 }, new[] { 0, 335, 670, 1024 }, "Guard");
            var bug = Load("ArtSource/V2/02_Enemies/Enemy_Bug_Animation_v02.png", "Enemy_Bug_Animation_v02.png", 1536, 1024,
                new[] { 0, 384, 768, 1152, 1536 }, new[] { 0, 335, 670, 1024 }, "Bug");
            var flying = Load("ArtSource/V2/02_Enemies/Enemy_Flying_Animation_v02.png", "Enemy_Flying_Animation_v02.png", 1448, 1086,
                new[] { 0, 402, 707, 1067, 1448 }, new[] { 0, 360, 715, 1086 }, "Flying");
            var ranged = Load("ArtSource/V2/02_Enemies/Enemy_Ranged_Animation_v02.png", "Enemy_Ranged_Animation_v02.png", 1448, 1086,
                new[] { 0, 362, 724, 1086, 1448 }, new[] { 0, 350, 705, 1086 }, "Ranged");
            var boss = Load("ArtSource/V2/03_Boss/Boss_TimeWarden_BodyActions_v03.png", "Boss_TimeWarden_BodyActions_v03.png", 1448, 1086,
                new[] { 0, 362, 724, 1086, 1448 }, new[] { 0, 398, 745, 1086 }, "BossAction");
            var bossStatus = Load("ArtSource/V2/03_Boss/Boss_TimeWarden_Status_v03.png", "Boss_TimeWarden_Status_v03.png", 1774, 887,
                new[] { 0, 444, 887, 1330, 1774 }, new[] { 0, 450, 887 }, "BossStatus");
            Reanchor("Enemy_Guard_Animation_v03.png", 1024, new[] { 190f, 575f, 960f, 1345f }, new[] { 310f, 630f, 960f });
            Reanchor("Enemy_Bug_Animation_v02.png", 1024, new[] { 190f, 575f, 960f, 1345f }, new[] { 295f, 625f, 958f });
            Reanchor("Enemy_Ranged_Animation_v02.png", 1086, new[] { 180f, 542f, 904f, 1266f }, new[] { 340f, 670f, 1030f });
            Reanchor("Boss_TimeWarden_BodyActions_v03.png", 1086, new[] { 180f, 542f, 904f, 1266f }, new[] { 388f, 740f, 1072f });
            Reanchor("Boss_TimeWarden_Status_v03.png", 887, new[] { 220f, 665f, 1108f, 1548f }, new[] { 444f, 855f });
            guard = Sprites("Enemy_Guard_Animation_v03.png");
            bug = Sprites("Enemy_Bug_Animation_v02.png");
            ranged = Sprites("Enemy_Ranged_Animation_v02.png");
            boss = Sprites("Boss_TimeWarden_BodyActions_v03.png");
            bossStatus = Sprites("Boss_TimeWarden_Status_v03.png");
            int groundCount = 0;
            foreach (var enemy in UnityEngine.Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))
            {
                switch (enemy.Kind)
                {
                    case EnemyKind.Guard: ConfigureEnemy(enemy, guard, "Guard", 1.45f); break;
                    case EnemyKind.ClockworkBug: ConfigureEnemy(enemy, bug, "Bug", 0.76f); break;
                    case EnemyKind.ClockArcher: ConfigureEnemy(enemy, ranged, "Ranged", 1.45f); break;
                    default: throw new InvalidOperationException("未知敌人类型：" + enemy.Kind);
                }
                groundCount++;
            }
            int flyingCount = 0;
            foreach (var flyer in UnityEngine.Object.FindObjectsByType<V2FlyingEnemy>(FindObjectsSortMode.None))
            {
                var visual = CreateVisual(flyer.gameObject, flying["Flying0"], 0.96f, false);
                var animator = flyer.GetComponent<FlyingArtAnimator>();
                if (animator == null) animator = flyer.gameObject.AddComponent<FlyingArtAnimator>();
                var settings = new SerializedObject(animator);
                settings.FindProperty("targetRenderer").objectReferenceValue = visual;
                Assign(settings, "hover", Frames(flying, "Flying", 0, 1, 2, 3));
                Assign(settings, "charge", Frames(flying, "Flying", 4, 5, 6, 7));
                Assign(settings, "hurt", Frames(flying, "Flying", 8, 9));
                Assign(settings, "death", Frames(flying, "Flying", 10, 11));
                settings.ApplyModifiedPropertiesWithoutUndo();
                flyingCount++;
            }
            int bossCount = 0;
            foreach (var brain in UnityEngine.Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None))
            {
                var visual = CreateVisual(brain.gameObject, boss["BossAction0"], 2.6f, true);
                var animator = brain.GetComponent<BossArtAnimator>();
                if (animator == null) animator = brain.gameObject.AddComponent<BossArtAnimator>();
                var settings = new SerializedObject(animator);
                settings.FindProperty("targetRenderer").objectReferenceValue = visual;
                settings.FindProperty("spriteFacesRight").boolValue = true;
                Assign(settings, "idle", Frames(boss, "BossAction", 0, 1));
                Assign(settings, "sweepTelegraph", Frames(boss, "BossAction", 2));
                Assign(settings, "sweepAttack", Frames(boss, "BossAction", 3, 4));
                Assign(settings, "jump", Frames(boss, "BossAction", 6, 7));
                Assign(settings, "slam", Frames(boss, "BossAction", 8, 9));
                Assign(settings, "shoot", Frames(boss, "BossAction", 10));
                Assign(settings, "timeAnchor", Frames(boss, "BossAction", 11));
                Assign(settings, "hurt", Frames(bossStatus, "BossStatus", 0, 1));
                Assign(settings, "phaseChange", Frames(bossStatus, "BossStatus", 2, 3));
                Assign(settings, "death", Frames(bossStatus, "BossStatus", 4, 5, 6, 7));
                settings.ApplyModifiedPropertiesWithoutUndo();
                bossCount++;
            }
            if (groundCount != 15 || flyingCount != 7 || bossCount != 1)
                throw new InvalidOperationException($"角色数异常：ground={groundCount}, flying={flyingCount}, boss={bossCount}");
            if (before != CollisionSignature()) throw new InvalidOperationException("碰撞数据变化，停止保存。");
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Capture("R2_Ground_A", "Guard", guard["Guard5"]);
            Capture("R4_Ground_B", "Bug", bug["Bug6"]);
            Capture("R4_Archer", "Ranged", ranged["Ranged5"]);
            Capture("R3_Flying_A", "Flying", flying["Flying0"]);
            Capture("Boss_TimeWarden_R8", "Boss", boss["BossAction3"]);
            Debug.Log($"V2_ACTOR_ART_READY: ground={groundCount}, flying={flyingCount}, boss={bossCount}; colliders unchanged.");
        }

        private static Dictionary<string, Sprite> Load(string source, string name, int width, int height, int[] xs, int[] ys, string prefix)
        {
            Copy(source, name);
            AssetDatabase.Refresh();
            var regions = new List<Region>();
            for (int row = 0; row < ys.Length - 1; row++)
                for (int col = 0; col < xs.Length - 1; col++)
                    regions.Add(new Region(prefix + (row * 4 + col), new RectInt(xs[col], ys[row], xs[col + 1] - xs[col], ys[row + 1] - ys[row]), true, true));
            return Import(name, regions);
        }

        private static Dictionary<string, Sprite> Sprites(string name) =>
            AssetDatabase.LoadAllAssetsAtPath(PreviewRoot + name).OfType<Sprite>().ToDictionary(s => s.name);

        private static void Reanchor(string name, int height, float[] centers, float[] baselines)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(PreviewRoot + name);
#pragma warning disable CS0618
            var metadata = importer.spritesheet;
            for (int i = 0; i < metadata.Length; i++)
            {
                Rect rect = metadata[i].rect;
                metadata[i].alignment = (int)SpriteAlignment.Custom;
                metadata[i].pivot = new Vector2((centers[i % 4] - rect.x) / rect.width,
                    (height - baselines[i / 4] - rect.y) / rect.height);
            }
            importer.spritesheet = metadata;
#pragma warning restore CS0618
            importer.SaveAndReimport();
        }

        private static Sprite[] Frames(Dictionary<string, Sprite> sprites, string prefix, params int[] indices) =>
            indices.Select(i => sprites[prefix + i]).ToArray();

        private static void Assign(SerializedObject settings, string field, Sprite[] sprites) =>
            V2AttackArtBuilder.Assign(settings, field, sprites);

        private static SpriteRenderer CreateVisual(GameObject owner, Sprite initial, float targetWorldHeight, bool grounded)
        {
            var old = owner.GetComponent<SpriteRenderer>();
            Transform child = owner.transform.Find("ArtVisual");
            if (child == null) { child = new GameObject("ArtVisual").transform; child.SetParent(owner.transform, false); }
            var renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = child.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = initial;
            renderer.color = Color.white;
            if (old != null)
            {
                renderer.sortingLayerID = old.sortingLayerID;
                renderer.sortingOrder = old.sortingOrder;
                old.enabled = false;
            }
            Vector3 rootScale = owner.transform.lossyScale;
            float worldScale = targetWorldHeight / initial.bounds.size.y;
            child.localScale = new Vector3(worldScale / rootScale.x, worldScale / rootScale.y, 1f);
            float floor = grounded && owner.GetComponent<Collider2D>() != null
                ? owner.GetComponent<Collider2D>().bounds.min.y - owner.transform.position.y : 0f;
            child.localPosition = new Vector3(0f, floor / rootScale.y, 0f);
            // 关卡中 Boss 根节点带旋转，视觉子层保持竖直；根节点和碰撞体不变。
            child.rotation = Quaternion.identity;
            return renderer;
        }

        private static void ConfigureEnemy(EnemyBrain enemy, Dictionary<string, Sprite> sprites, string prefix, float height)
        {
            var visual = CreateVisual(enemy.gameObject, sprites[prefix + "0"], height, true);
            var animator = enemy.GetComponent<EnemyArtAnimator>();
            if (animator == null) animator = enemy.gameObject.AddComponent<EnemyArtAnimator>();
            var settings = new SerializedObject(animator);
            settings.FindProperty("targetRenderer").objectReferenceValue = visual;
            Assign(settings, "idle", Frames(sprites, prefix, 0, 3));
            Assign(settings, "patrol", Frames(sprites, prefix, 0, 1, 2, 3));
            Assign(settings, "prepareAttack", Frames(sprites, prefix, 4));
            Assign(settings, "attack", Frames(sprites, prefix, 5, 6, 7));
            Assign(settings, "hurt", Frames(sprites, prefix, 8, 9));
            Assign(settings, "death", Frames(sprites, prefix, 10, 11));
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Capture(string objectName, string label, Sprite frame)
        {
            var owner = GameObject.Find(objectName);
            if (owner == null) return;
            var visual = owner.transform.Find("ArtVisual")?.GetComponent<SpriteRenderer>();
            if (visual == null) return;
            Sprite previous = visual.sprite;
            var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            Vector3 cameraPosition = camera.transform.position;
            float cameraSize = camera.orthographicSize;
            try
            {
                visual.sprite = frame;
                camera.orthographicSize = label == "Boss" ? 3f : 1.5f;
                camera.transform.position = new Vector3(owner.transform.position.x, owner.transform.position.y, cameraPosition.z);
                V2ArtPreviewSmoke.Capture(camera, $"ArtSource/V2/Previews/{label}_RuntimeArt_v02.png");
            }
            finally { visual.sprite = previous; camera.transform.position = cameraPosition; camera.orthographicSize = cameraSize; }
        }
    }
}
#endif
