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
    /// <summary>六帧跑步统一背负待机长指针，沿用原帧基线并随背部轻微摆动。</summary>
    internal static class V2RunWeaponPolish
    {
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/V2/EightRoomArtPreview.unity");
            string before = CollisionSignature();
            Copy("ArtSource/V2/01_Player/AnimationDrafts/Player_BodyLayers_v05.png", "Player_BodyLayers_v05.png");
            AssetDatabase.Refresh();
            var original = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/V2/Preview/Player_Idle_Run_FrameDraft_v01.png")
                .OfType<Sprite>().ToDictionary(s => s.name);
            var regions = new List<Region>();
            for (int i = 0; i < 6; i++)
            {
                Rect rect = original["Run" + i].rect;
                // 原有裁边与脚底锚点保留，避免去掉武器后身体横向跳位。
                regions.Add(new Region("RunBody" + i, new RectInt(Mathf.RoundToInt(rect.x), 793 - Mathf.RoundToInt(rect.yMax),
                    Mathf.RoundToInt(rect.width), Mathf.RoundToInt(rect.height)), false, true));
            }
            var sprites = Import("Player_BodyLayers_v05.png", regions);
            var player = GameObject.FindWithTag("Player");
            var animator = new SerializedObject(player.GetComponent<PlayerArtAnimator>());
            var run = animator.FindProperty("run");
            run.arraySize = 6;
            var carried = player.GetComponent<PlayerCarriedWeaponVisual>();
            var serialized = new SerializedObject(carried);
            var weapon = (SpriteRenderer)serialized.FindProperty("weaponRenderer").objectReferenceValue;
            var poses = serialized.FindProperty("framePoses");
            poses.arraySize = 6;
            Vector2[] backOffsets = { new(2.2f, 2.5f), new(2.1f, 2.6f), new(2.2f, 2.7f), new(2.2f, 2.6f), new(2.1f, 2.4f), new(2.2f, 2.5f) };
            float[] angles = { 45f, 48f, 46f, 44f, 48f, 46f };
            for (int i = 0; i < 6; i++)
            {
                Sprite frame = sprites["RunBody" + i];
                run.GetArrayElementAtIndex(i).objectReferenceValue = frame;
                // 武器随背部摆动，不跟随手套；全部帧复用同一缩放和完整轴线。
                var pose = poses.GetArrayElementAtIndex(i);
                pose.FindPropertyRelative("bodySprite").objectReferenceValue = frame;
                pose.FindPropertyRelative("offset").vector2Value = backOffsets[i];
                pose.FindPropertyRelative("angle").floatValue = angles[i];
            }
            animator.ApplyModifiedPropertiesWithoutUndo();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (before != CollisionSignature()) throw new InvalidOperationException("碰撞数据发生变化，停止保存。");
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Capture(player, sprites, carried);
            Debug.Log("V2_RUN_WEAPON_FIXED: 六帧跑步背负待机完整武器，长度与粗细固定，背部小幅摆动；碰撞数据不变。");
        }

        private static void Capture(GameObject player, Dictionary<string, Sprite> frames, PlayerCarriedWeaponVisual carried)
        {
            var renderer = player.transform.Find("ArtVisual").GetComponent<SpriteRenderer>();
            var weapon = renderer.transform.Find("CarriedStraightPointer").GetComponent<SpriteRenderer>();
            var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            Sprite original = renderer.sprite;
            Vector3 cameraPosition = camera.transform.position;
            float cameraSize = camera.orthographicSize;
            Vector3 playerPosition = player.transform.position;
            bool flip = renderer.flipX;
            try
            {
                var floor = GameObject.Find("Room_R1").transform.Find("FloorLow").GetComponent<Collider2D>();
                player.transform.position += Vector3.up * (floor.bounds.max.y - player.GetComponent<Collider2D>().bounds.min.y);
                camera.orthographicSize = 1.25f;
                camera.transform.position = new Vector3(player.transform.position.x - 0.3f, renderer.bounds.center.y + 0.15f, cameraPosition.z);
                renderer.flipX = false;
                carried.RefreshVisual();
                V2ArtPreviewSmoke.Capture(camera, "ArtSource/V2/Previews/Player_Idle_Aligned_v06.png");
                renderer.color = new Color(1f, 1f, 1f, 0.25f);
                // 仅检查镜头临时淡化身体，完整武器轴线可直接检查，不保存到游戏。
                V2ArtPreviewSmoke.Capture(camera, "ArtSource/V2/Previews/Player_Weapon_AxisCheck_v06.png");
                renderer.color = Color.white;
                for (int i = 0; i < 6; i++)
                {
                    renderer.sprite = frames["RunBody" + i];
                    carried.RefreshVisual();
                    V2ArtPreviewSmoke.Capture(camera, $"ArtSource/V2/Previews/Player_Run_BackWeapon_Frame{i}_v06.png");
                }
                renderer.sprite = frames["RunBody1"];
                renderer.flipX = true;
                carried.RefreshVisual();
                V2ArtPreviewSmoke.Capture(camera, "ArtSource/V2/Previews/Player_Run_BackWeapon_Left_v06.png");
            }
            finally
            {
                renderer.sprite = original;
                renderer.flipX = flip;
                renderer.color = Color.white;
                carried.RefreshVisual();
                camera.transform.position = cameraPosition;
                camera.orthographicSize = cameraSize;
                player.transform.position = playerPosition;
            }
        }
    }
}
#endif
