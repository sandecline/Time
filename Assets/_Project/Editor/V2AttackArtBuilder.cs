#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TimeMetroidvania.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static TimeMetroidvania.Editor.V2ArtSupplementBuilder;

namespace TimeMetroidvania.Editor
{
    /// <summary>切片三向攻击身体并绑定同一根完整指针，保存可试玩场景。</summary>
    internal static class V2AttackArtBuilder
    {
        private const string Name = "Player_Attack_BodyLayers_v04.png";
        private const string Path = "Assets/_Project/Art/V2/Preview/" + Name;

        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/V2/EightRoomArtPreview.unity");
            string before = CollisionSignature();
            Copy("ArtSource/V2/01_Player/AnimationDrafts/" + Name, Name);
            AssetDatabase.Refresh();
            int[] xs = { 0, 380, 755, 1120, 1448 };
            int[] ys = { 0, 400, 758, 1086 };
            string[] prefixes = { "AttackHorizontal", "AttackUp", "AttackDown" };
            var regions = new List<Region>();
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 4; col++)
                    regions.Add(new Region(prefixes[row] + col, new RectInt(xs[col], ys[row], xs[col + 1] - xs[col], ys[row + 1] - ys[row]), true, true));
            Import(Name, regions);
            // 延伸手臂不改变身体中心；空中收腿保留头部高度，不把脚底强行落回地面。
            float[] centers = { 210, 560, 925, 1280, 215, 565, 930, 1280, 215, 565, 925, 1280 };
            float[] baseline = { 371, 750, 1100 };
            var importer = (TextureImporter)AssetImporter.GetAtPath(Path);
#pragma warning disable CS0618
            var slices = importer.spritesheet;
            for (int i = 0; i < slices.Length; i++)
            {
                Rect rect = slices[i].rect;
                slices[i].pivot = new Vector2((centers[i] - rect.x) / rect.width, (1086 - baseline[i / 4] - rect.y) / rect.height);
                slices[i].alignment = (int)SpriteAlignment.Custom;
            }
            importer.spritesheet = slices;
#pragma warning restore CS0618
            importer.SaveAndReimport();
            var frames = AssetDatabase.LoadAllAssetsAtPath(Path).OfType<Sprite>().ToDictionary(s => s.name);
            var player = GameObject.FindWithTag("Player");
            var body = player.transform.Find("ArtVisual").GetComponent<SpriteRenderer>();
            var animator = new SerializedObject(player.GetComponent<PlayerArtAnimator>());
            Assign(animator, "attackGround", Enumerable.Range(0, 4).Select(i => frames[prefixes[0] + i]).ToArray());
            Assign(animator, "attackAir", Enumerable.Range(0, 4).Select(i => frames[prefixes[0] + i]).ToArray());
            Assign(animator, "attackUp", Enumerable.Range(0, 4).Select(i => frames[prefixes[1] + i]).ToArray());
            Assign(animator, "attackDown", Enumerable.Range(0, 4).Select(i => frames[prefixes[2] + i]).ToArray());
            animator.FindProperty("attackVisualScale").floatValue = 1.10f;
            animator.ApplyModifiedPropertiesWithoutUndo();
            Transform pivot = player.transform.Find("AttackWeaponPivot");
            if (pivot == null) { pivot = new GameObject("AttackWeaponPivot").transform; pivot.SetParent(player.transform, false); }
            pivot.localScale = new Vector3(1f / player.transform.lossyScale.x, 1f / player.transform.lossyScale.y, 1f);
            Transform visual = pivot.Find("StraightPointer");
            if (visual == null) { visual = new GameObject("StraightPointer").transform; visual.SetParent(pivot, false); }
            var weapon = visual.GetComponent<SpriteRenderer>();
            if (weapon == null) weapon = visual.gameObject.AddComponent<SpriteRenderer>();
            weapon.sprite = body.transform.Find("CarriedStraightPointer").GetComponent<SpriteRenderer>().sprite;
            weapon.enabled = false;
            var controller = player.GetComponent<PlayerAttackWeaponVisual>();
            if (controller == null) controller = player.AddComponent<PlayerAttackWeaponVisual>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("bodyRenderer").objectReferenceValue = body;
            serialized.FindProperty("weaponRenderer").objectReferenceValue = weapon;
            serialized.FindProperty("weaponLength").floatValue = 1.16f;
            Vector2[] grips = { new(216,237), new(684,213), new(1064,210), new(1280,229),
                new(216,640), new(580,614), new(932,610), new(1280,627),
                new(223,952), new(575,966), new(930,1000), new(1310,952) };
            float[] angles = { -35,-90,-90,-35, 0,0,0,25, 155,180,180,145 };
            var poses = serialized.FindProperty("poses");
            poses.arraySize = 12;
            for (int i = 0; i < 12; i++)
            {
                var frame = frames[prefixes[i / 4] + i % 4];
                var pose = poses.GetArrayElementAtIndex(i);
                pose.FindPropertyRelative("bodySprite").objectReferenceValue = frame;
                pose.FindPropertyRelative("grip").vector2Value = new Vector2((grips[i].x - centers[i]) / 32f, (baseline[i / 4] - grips[i].y) / 32f);
                pose.FindPropertyRelative("angle").floatValue = angles[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (before != CollisionSignature()) throw new InvalidOperationException("碰撞数据变化，停止保存。");
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Capture(player, frames);
            Debug.Log("V2_ATTACK_ART_READY: 12身体帧、完整指针、三向独立动画；碰撞数据不变。");
        }

        internal static void Assign(SerializedObject target, string field, Sprite[] frames)
        {
            var array = target.FindProperty(field);
            array.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }

        private static void Capture(GameObject player, Dictionary<string, Sprite> frames)
        {
            var body = player.transform.Find("ArtVisual").GetComponent<SpriteRenderer>();
            var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            var carried = player.GetComponent<PlayerCarriedWeaponVisual>();
            var attack = player.GetComponent<PlayerAttackWeaponVisual>();
            Sprite previous = body.sprite;
            Vector3 scale = body.transform.localScale;
            body.transform.localScale = scale * 1.1f;
            camera.orthographicSize = 1.7f;
            camera.transform.position = new Vector3(player.transform.position.x + 0.25f, player.transform.position.y, camera.transform.position.z);
            foreach (string name in new[] { "AttackHorizontal1", "AttackUp1", "AttackDown1" })
            {
                body.sprite = frames[name];
                carried.RefreshVisual();
                attack.RefreshVisual();
                V2ArtPreviewSmoke.Capture(camera, "ArtSource/V2/Previews/Player_" + name + "_v04.png");
            }
            body.sprite = previous;
            body.transform.localScale = scale;
            carried.RefreshVisual();
            attack.RefreshVisual();
        }
    }
}
#endif
