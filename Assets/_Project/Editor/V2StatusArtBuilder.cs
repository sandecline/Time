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
    /// <summary>接入主角的受伤、死亡、治疗和蓄时身体帧。</summary>
    internal static class V2StatusArtBuilder
    {
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/V2/EightRoomArtPreview.unity");
            string before = CollisionSignature();
            const string name = "Player_Status_BodyLayers_v02.png";
            Copy("ArtSource/V2/01_Player/AnimationDrafts/" + name, name);
            AssetDatabase.Refresh();
            int[] xs = { 0, 445, 885, 1285, 1774 };
            int[] ys = { 0, 470, 887 };
            var regions = new List<Region>();
            for (int row = 0; row < 2; row++)
                for (int col = 0; col < 4; col++)
                    regions.Add(new Region("Status" + (row * 4 + col),
                        new RectInt(xs[col], ys[row], xs[col + 1] - xs[col], ys[row + 1] - ys[row]), true, true));
            Import(name, regions);
            var importer = (TextureImporter)AssetImporter.GetAtPath("Assets/_Project/Art/V2/Preview/" + name);
            float[] centers = { 225, 667, 1090, 1520, 225, 644, 1095, 1545 };
#pragma warning disable CS0618
            var metadata = importer.spritesheet;
            for (int i = 0; i < metadata.Length; i++)
            {
                Rect rect = metadata[i].rect;
                float foot = i < 4 ? 454f : 839f;
                metadata[i].alignment = (int)SpriteAlignment.Custom;
                metadata[i].pivot = new Vector2((centers[i] - rect.x) / rect.width,
                    (887f - foot - rect.y) / rect.height);
            }
            importer.spritesheet = metadata;
#pragma warning restore CS0618
            importer.SaveAndReimport();
            var frames = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/V2/Preview/" + name)
                .OfType<Sprite>().ToDictionary(s => s.name);
            var player = GameObject.FindWithTag("Player");
            var body = player.transform.Find("ArtVisual").GetComponent<SpriteRenderer>();
            var animator = new SerializedObject(player.GetComponent<PlayerArtAnimator>());
            V2AttackArtBuilder.Assign(animator, "hurt", new[] { frames["Status0"], frames["Status1"] });
            V2AttackArtBuilder.Assign(animator, "death", new[] { frames["Status2"], frames["Status3"] });
            V2AttackArtBuilder.Assign(animator, "heal", new[] { frames["Status4"], frames["Status5"] });
            V2AttackArtBuilder.Assign(animator, "slowCharge", new[] { frames["Status6"], frames["Status7"] });
            animator.ApplyModifiedPropertiesWithoutUndo();
            var carried = player.GetComponent<PlayerCarriedWeaponVisual>();
            var serialized = new SerializedObject(carried);
            var poses = serialized.FindProperty("framePoses");
            int first = poses.arraySize;
            poses.arraySize = first + 8;
            Vector2[] offsets = { new(2.5f, 2.3f), new(2.2f, 2.1f), new(2.2f, 1.7f), new(3.8f, 0.45f),
                new(2.5f, 2.3f), new(2.5f, 2.3f), new(2.5f, 2.3f), new(2.5f, 2.3f) };
            for (int i = 0; i < 8; i++)
            {
                var pose = poses.GetArrayElementAtIndex(first + i);
                pose.FindPropertyRelative("bodySprite").objectReferenceValue = frames["Status" + i];
                pose.FindPropertyRelative("offset").vector2Value = offsets[i];
                pose.FindPropertyRelative("angle").floatValue = i == 3 ? -90f : 45f;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (before != CollisionSignature()) throw new InvalidOperationException("碰撞数据变化，停止保存。");
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            camera.orthographicSize = 1.7f;
            camera.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, camera.transform.position.z);
            foreach (int i in new[] { 0, 3, 4, 6 })
            {
                body.sprite = frames["Status" + i];
                carried.RefreshVisual();
                V2ArtPreviewSmoke.Capture(camera, $"ArtSource/V2/Previews/Player_Status_{i}_v02.png");
            }
            Debug.Log("V2_STATUS_ART_READY: 受伤、死亡、治疗、蓄时各两帧；背负武器已绑定；碰撞数据不变。");
        }
    }
}
#endif
