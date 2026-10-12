#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TimeMetroidvania.Level;
using TimeMetroidvania.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TimeMetroidvania.Editor
{
    /// <summary>把场景补图和已有移动帧接入独立试玩场景，不改动碰撞与关卡布局。</summary>
    internal static class V2ArtSupplementBuilder
    {
        private const string Root = "Assets/_Project/Art/V2/Preview/";
        private const string ScenePath = "Assets/_Project/Scenes/V2/EightRoomArtPreview.unity";
        private static readonly string[] Names = {
            "MossBlock", "StoneBlock", "BrassLedge", "Column", "Grass", "Rubble", "Stones", "Gear",
            "Ivy", "Root", "Banner", "BrokenClock", "Fern", "Flowers", "Pedestal", "Fragments"
        };

        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            if (GameObject.Find("V2_Supplement_20260928") != null)
                throw new InvalidOperationException("本轮补图已接入，避免重复创建。");
            string before = CollisionSignature();
            Copy("ArtSource/V2/05_Environment/Environment_Decor_Atlas_v01.png", "Environment_Decor_Atlas_v01.png");
            Copy("ArtSource/V2/06_Props/Checkpoint_Clocks_v01.png", "Checkpoint_Clocks_v01.png");
            Copy("ArtSource/V2/01_Player/AnimationDrafts/Player_Idle_Run_FrameDraft_v01.png", "Player_Idle_Run_FrameDraft_v01.png");
            AssetDatabase.Refresh();
            var regions = new List<Region>();
            int[] xs = { 0, 314, 628, 942, 1254 };
            int[] ys = { 0, 390, 620, 970, 1254 };
            for (int row = 0; row < 4; row++)
                for (int col = 0; col < 4; col++)
                    regions.Add(new Region(Names[row * 4 + col], new RectInt(xs[col], ys[row], xs[col + 1] - xs[col], ys[row + 1] - ys[row]), true, row > 0));
            // 首排横梁跨越常规列边界，使用实际留白分隔线隔开横梁和立柱。
            regions[2] = new Region("BrassLedge", new RectInt(635, 210, 330, 135), true, false);
            regions[3] = new Region("Column", new RectInt(1000, 0, 220, 385), true, false);
            regions.Add(new Region("StoneFill", new RectInt(356, 145, 236, 144), false, false));
            regions.Add(new Region("MossEdge", new RectInt(38, 122, 250, 38), false, false));
            var atlas = Import("Environment_Decor_Atlas_v01.png", regions);
            var clocks = Import("Checkpoint_Clocks_v01.png", new List<Region> {
                new Region("ClockInactive", new RectInt(0, 0, 627, 836), true, true),
                new Region("ClockActive", new RectInt(627, 0, 627, 836), true, true),
                new Region("ClockComplete", new RectInt(1254, 0, 627, 836), true, true)
            });
            var frames = new List<Region>();
            int[] idleX = { 290, 630, 990, 1350, 1670 };
            int[] runX = { 0, 330, 650, 1000, 1300, 1670, 1983 };
            for (int i = 0; i < 4; i++)
                frames.Add(new Region("Idle" + i, new RectInt(idleX[i], 0, idleX[i + 1] - idleX[i], 417), true, true));
            for (int i = 0; i < 6; i++)
                frames.Add(new Region("Run" + i, new RectInt(runX[i], 420, runX[i + 1] - runX[i], 373), true, true));
            var playerFrames = Import("Player_Idle_Run_FrameDraft_v01.png", frames);
            int groundCount = DressTerrain(atlas);
            int checkpointCount = DressCheckpoints(clocks);
            DressPlayer(playerFrames);
            DressBackground();
            if (before != CollisionSignature()) throw new InvalidOperationException("碰撞数据发生变化，停止保存。");
            new GameObject("V2_Supplement_20260928");
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Capture();
            Debug.Log($"V2_SUPPLEMENT_READY terrain={groundCount} clocks={checkpointCount} sprites={atlas.Count + clocks.Count + playerFrames.Count}; COLLIDERS_UNCHANGED");
        }

        private static int DressTerrain(Dictionary<string, Sprite> art)
        {
            int count = 0;
            var colliders = UnityEngine.Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None);
            foreach (var collider in colliders)
            {
                if (!collider.enabled || collider.isTrigger || collider.gameObject.layer != LayerMask.NameToLayer("Ground")
                    || collider.GetComponentInParent<StageFiveRoom>() == null) continue;
                string name = collider.name.ToLowerInvariant();
                if (name.Contains("water") || name.Contains("gate") || name.Contains("door") || name.Contains("rotor")) continue;
                var old = collider.GetComponent<SpriteRenderer>();
                if (old == null) continue;
                Bounds bounds = collider.bounds;
                old.enabled = false;
                var fill = Visual(collider.transform, "V2_StoneSurface", art["StoneFill"], bounds.center, Vector2.one, 0);
                SetWorldScale(fill.transform, Vector3.one * 0.2f);
                fill.drawMode = SpriteDrawMode.Tiled;
                fill.tileMode = SpriteTileMode.Continuous;
                fill.size = new Vector2(bounds.size.x, bounds.size.y) / 0.2f;
                if (!name.Contains("wall") && !name.Contains("ceiling"))
                {
                    var edge = Visual(collider.transform, "V2_MossContactEdge", art["MossEdge"],
                        new Vector3(bounds.center.x, bounds.max.y - 0.10f, 0), Vector2.one, 1);
                    SetWorldScale(edge.transform, new Vector3(0.2f, 0.2f / art["MossEdge"].bounds.size.y, 1f));
                    edge.drawMode = SpriteDrawMode.Tiled;
                    edge.size = new Vector2(bounds.size.x / 0.2f, art["MossEdge"].bounds.size.y);
                    if (bounds.size.x >= 3f)
                    {
                        Visual(collider.transform, "V2_Grass", art["Grass"], new Vector3(bounds.min.x + 0.65f, bounds.max.y, 0), new Vector2(0.7f, 0.35f), 2);
                        Visual(collider.transform, "V2_Rubble", art["Rubble"], new Vector3(bounds.max.x - 0.55f, bounds.max.y, 0), new Vector2(0.85f, 0.4f), 2);
                    }
                    if (bounds.size.y > 2f && bounds.size.x > 3f)
                    {
                        Visual(collider.transform, "V2_HangingIvy", art["Ivy"], new Vector3(bounds.min.x + 0.3f, bounds.max.y - 1.4f, 0), new Vector2(0.55f, 1.4f), 2);
                    }
                }
                count++;
            }
            return count;
        }

        private static int DressCheckpoints(Dictionary<string, Sprite> clocks)
        {
            int count = 0;
            foreach (var transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (transform.Find("ClockTop") == null || transform.Find("ClockBottom") == null) continue;
                foreach (var renderer in transform.GetComponentsInChildren<SpriteRenderer>()) renderer.enabled = false;
                var rendererNew = Visual(transform, "ArtVisual", clocks["ClockInactive"], transform.position + Vector3.down * 0.95f, new Vector2(1.8f, 2.25f), 3);
                if (transform.TryGetComponent<StageFiveCheckpoint>(out var checkpoint))
                {
                    var serialized = new SerializedObject(checkpoint);
                    serialized.FindProperty("artRenderer").objectReferenceValue = rendererNew;
                    serialized.FindProperty("inactiveSprite").objectReferenceValue = clocks["ClockInactive"];
                    serialized.FindProperty("activeSprite").objectReferenceValue = clocks["ClockActive"];
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                else rendererNew.sprite = clocks["ClockActive"];
                count++;
            }
            return count;
        }

        public static void FinalizePreview()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            string before = CollisionSignature();
            int removed = 0;
            foreach (var collider in UnityEngine.Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
            {
                // 冻结水面和战斗封门由各自机关控制，不套用常驻石材显示。
                if (collider.enabled) continue;
                foreach (Transform child in collider.transform.Cast<Transform>().ToArray())
                {
                    if (!child.name.StartsWith("V2_", StringComparison.Ordinal)) continue;
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                    removed++;
                }
            }
            var surfaces = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                .Where(r => r.name == "V2_StoneSurface").ToArray();
            foreach (var surface in surfaces)
            {
                var collider = surface.transform.parent.GetComponent<BoxCollider2D>();
                if (Vector3.Distance(surface.bounds.center, collider.bounds.center) > 0.01f
                    || Vector2.Distance(surface.bounds.size, collider.bounds.size) > 0.01f)
                    throw new InvalidOperationException("地形显示与碰撞不齐：" + collider.name);
            }
            if (before != CollisionSignature()) throw new InvalidOperationException("碰撞数据发生变化，停止保存。");
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"V2_SURFACE_ALIGNMENT_PASS count={surfaces.Length}; removedDynamicOverlays={removed}; COLLIDERS_UNCHANGED");
        }

        private static void DressPlayer(Dictionary<string, Sprite> sprites)
        {
            var player = GameObject.FindWithTag("Player");
            var renderer = player.transform.Find("ArtVisual").GetComponent<SpriteRenderer>();
            var bounds = player.GetComponent<Collider2D>().bounds;
            float scale = bounds.size.y / sprites["Idle0"].bounds.size.y;
            SetWorldScale(renderer.transform, Vector3.one * scale);
            renderer.transform.position = new Vector3(player.transform.position.x, bounds.min.y, 0f);
            renderer.sprite = sprites["Idle0"];
            renderer.sortingOrder = 5;
            var animator = new SerializedObject(player.GetComponent<PlayerArtAnimator>());
            Assign(animator, "idle", Enumerable.Range(0, 4).Select(i => sprites["Idle" + i]).ToArray());
            Assign(animator, "run", Enumerable.Range(0, 6).Select(i => sprites["Run" + i]).ToArray());
            foreach (string field in new[] { "jumpRise", "fall", "attackGround", "attackAir", "dash", "hurt", "death", "heal", "slowCharge" })
                Assign(animator, field, new[] { sprites["Idle0"] });
            animator.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Assign(SerializedObject owner, string name, Sprite[] sprites)
        {
            var array = owner.FindProperty(name);
            array.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }

        private static void DressBackground()
        {
            var reference = GameObject.Find("V2_Background_Reference_Preview").GetComponent<SpriteRenderer>();
            var rooms = UnityEngine.Object.FindObjectsByType<StageFiveRoom>(FindObjectsSortMode.None);
            var regions = new List<Region>();
            foreach (var room in rooms)
            {
                Bounds bounds = room.transform.Find("GreyboxBackdrop").GetComponent<SpriteRenderer>().bounds;
                int width = Mathf.Min(2079, Mathf.RoundToInt(374f * bounds.size.x / bounds.size.y));
                int x = Mathf.Clamp((int.Parse(room.RoomId.Substring(1)) - 1) * 180, 0, 2079 - width);
                regions.Add(new Region(room.RoomId, new RectInt(x, 0, width, 374), false, false));
            }
            var backgrounds = Import("Background_Reference.png", regions);
            // 背景限定到房间内，避免横幅越界遮住邻室；各房间只调整色调。
            foreach (var room in rooms)
            {
                var target = room.transform.Find("GreyboxBackdrop").GetComponent<SpriteRenderer>();
                var bounds = target.bounds;
                target.sprite = backgrounds[room.RoomId];
                SetWorldScale(target.transform, new Vector3(bounds.size.x / target.sprite.bounds.size.x, bounds.size.y / target.sprite.bounds.size.y, 1f));
                target.color = room.RoomId == "R8" ? new Color(0.34f, 0.43f, 0.52f) : new Color(0.7f, 0.8f, 0.77f);
            }
            reference.enabled = false;
        }

        private static SpriteRenderer Visual(Transform parent, string name, Sprite sprite, Vector3 position, Vector2 size, int order)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.position = position;
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            SetWorldScale(child.transform, new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1f));
            return renderer;
        }

        private static void SetWorldScale(Transform target, Vector3 worldScale)
        {
            Vector3 parent = target.parent == null ? Vector3.one : target.parent.lossyScale;
            target.localScale = new Vector3(worldScale.x / parent.x, worldScale.y / parent.y, worldScale.z / parent.z);
        }

        internal static string CollisionSignature()
        {
            return string.Join("\n", UnityEngine.Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.InstanceID)
                .Select(c => c.GetInstanceID() + ":" + EditorJsonUtility.ToJson(c) + ":" + c.transform.position.ToString("R") + ":" + c.transform.lossyScale.ToString("R")));
        }

        internal static void Copy(string source, string name)
        {
            if (!File.Exists(Root + name)) File.Copy(source, Root + name);
        }

        internal readonly struct Region
        {
            public readonly string Name;
            public readonly RectInt Rect;
            public readonly bool Trim;
            public readonly bool BottomPivot;
            public Region(string name, RectInt rect, bool trim, bool bottomPivot)
            { Name = name; Rect = rect; Trim = trim; BottomPivot = bottomPivot; }
        }

        /// <summary>仅读取透明像素定位切片边界，源 PNG 不做重采样或像素修改。</summary>
        internal static Dictionary<string, Sprite> Import(string name, List<Region> regions)
        {
            string path = Root + name;
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(path));
            var slices = new List<SpriteMetaData>();
            foreach (var region in regions)
            {
                int xMin = region.Rect.xMin, xMax = region.Rect.xMax;
                int yMin = texture.height - region.Rect.yMax, yMax = texture.height - region.Rect.yMin;
                if (region.Trim)
                {
                    int left = xMax, right = xMin, bottom = yMax, top = yMin;
                    for (int y = yMin; y < yMax; y++)
                        for (int x = xMin; x < xMax; x++)
                            if (texture.GetPixel(x, y).a > 0.125f)
                            { left = Math.Min(left, x); right = Math.Max(right, x + 1); bottom = Math.Min(bottom, y); top = Math.Max(top, y + 1); }
                    if (right <= left || top <= bottom) throw new InvalidOperationException("切片为空：" + region.Name);
                    xMin = left; xMax = right; yMin = bottom; yMax = top;
                }
                slices.Add(new SpriteMetaData { name = region.Name, rect = new Rect(xMin, yMin, xMax - xMin, yMax - yMin), alignment = (int)SpriteAlignment.Custom, pivot = new Vector2(0.5f, region.BottomPivot ? 0f : 0.5f) });
            }
            UnityEngine.Object.DestroyImmediate(texture);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.maxTextureSize = 4096;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
#pragma warning disable CS0618
            importer.spritesheet = slices.ToArray();
#pragma warning restore CS0618
            importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
        }

        public static void Capture()
        {
            var player = GameObject.FindWithTag("Player");
            var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            Vector3 playerPosition = player.transform.position;
            Vector3 cameraPosition = camera.transform.position;
            float size = camera.orthographicSize;
            try
            {
                // 截图中将脚底落在现有地面表面，预览场景的出生点保持原样。
                var floor = GameObject.Find("Room_R1").transform.Find("FloorLow").GetComponent<Collider2D>();
                player.transform.position += Vector3.up * (floor.bounds.max.y - player.GetComponent<Collider2D>().bounds.min.y);
                camera.transform.position = new Vector3(player.transform.position.x + 2f, player.transform.position.y + 2.5f, cameraPosition.z);
                camera.orthographicSize = 5f;
                CaptureFrame(camera, "ArtSource/V2/Previews/R1_EnvironmentSupplement_v01.png");
            }
            finally { player.transform.position = playerPosition; camera.transform.position = cameraPosition; camera.orthographicSize = size; }
        }

        private static void CaptureFrame(Camera camera, string path)
        {
            var target = RenderTexture.GetTemporary(1920, 1080, 24);
            var previous = RenderTexture.active;
            var previousTarget = camera.targetTexture;
            var texture = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
#endif
