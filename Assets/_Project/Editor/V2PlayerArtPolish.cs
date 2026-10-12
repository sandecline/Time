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
    /// <summary>接入站姿分层武器，修正身体两侧露出的武器不共线的问题。</summary>
    internal static class V2PlayerArtPolish
    {
        public static void AddAirFrames()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/V2/EightRoomArtPreview.unity");
            string before = CollisionSignature();
            Copy("ArtSource/V2/01_Player/AnimationDrafts/Player_Air_Dash_Clean_v02.png", "Player_Air_Dash_Clean_v02.png");
            AssetDatabase.Refresh();
            var regions = new List<Region>();
            int[] xs = { 0, 445, 885, 1325, 1774 };
            string[] names = { "JumpRise", "JumpApex", "Fall", "LandingDraft" };
            for (int i = 0; i < 4; i++)
            {
                regions.Add(new Region(names[i], new RectInt(xs[i], 0, xs[i + 1] - xs[i], 510), true, true));
                regions.Add(new Region("Dash" + i, new RectInt(xs[i], 510, xs[i + 1] - xs[i], 377), true, true));
            }
            var sprites = Import("Player_Air_Dash_Clean_v02.png", regions);
            var player = GameObject.FindWithTag("Player");
            var animator = new SerializedObject(player.GetComponent<PlayerArtAnimator>());
            Assign(animator, "jumpRise", new[] { sprites["JumpRise"] });
            Assign(animator, "jumpApex", new[] { sprites["JumpApex"] });
            Assign(animator, "fall", new[] { sprites["Fall"] });
            Assign(animator, "dash", Enumerable.Range(0, 4).Select(i => sprites["Dash" + i]).ToArray());
            animator.ApplyModifiedPropertiesWithoutUndo();
            if (before != CollisionSignature()) throw new InvalidOperationException("碰撞数据发生变化，停止保存。");
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("V2_AIR_FRAMES_READY: 起跳、顶点、下落及四帧冲刺已接入；碰撞数据不变。");
        }

        private static void Assign(SerializedObject target, string field, Sprite[] sprites)
        {
            var array = target.FindProperty(field);
            array.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }

        public static void FixIdle()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/V2/EightRoomArtPreview.unity");
            string before = CollisionSignature();
            Copy("ArtSource/V2/01_Player/AnimationDrafts/Player_Idle_Layered_v03.png", "Player_Idle_Layered_v03.png");
            AssetDatabase.Refresh();
            var regions = new List<Region>();
            int[] xs = { 290, 630, 990, 1350, 1670 };
            for (int i = 0; i < 4; i++)
                regions.Add(new Region("IdleBody" + i, new RectInt(xs[i], 0, xs[i + 1] - xs[i], 417), true, true));
            regions.Add(new Region("StraightPointer", new RectInt(0, 0, 250, 417), true, true));
            var sprites = Import("Player_Idle_Layered_v03.png", regions);
            var player = GameObject.FindWithTag("Player");
            var animator = new SerializedObject(player.GetComponent<PlayerArtAnimator>());
            var oldIdle = animator.FindProperty("idle");
            var oldSprites = Enumerable.Range(0, oldIdle.arraySize).Select(i => oldIdle.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            Sprite[] bodies = Enumerable.Range(0, 4).Select(i => sprites["IdleBody" + i]).ToArray();
            oldIdle.arraySize = 4;
            for (int i = 0; i < 4; i++) oldIdle.GetArrayElementAtIndex(i).objectReferenceValue = bodies[i];
            // 仅替换仍指向旧待机的占位状态，保留已有跑动与后续动作配置。
            foreach (string field in new[] { "jumpRise", "fall", "attackGround", "attackAir", "dash", "hurt", "death", "heal", "slowCharge" })
            {
                var array = animator.FindProperty(field);
                for (int i = 0; i < array.arraySize; i++)
                    if (oldSprites.Contains(array.GetArrayElementAtIndex(i).objectReferenceValue))
                        array.GetArrayElementAtIndex(i).objectReferenceValue = bodies[0];
            }
            animator.ApplyModifiedPropertiesWithoutUndo();
            var body = player.transform.Find("ArtVisual").GetComponent<SpriteRenderer>();
            body.sprite = bodies[0];
            Transform existing = body.transform.Find("CarriedStraightPointer");
            var weaponObject = existing == null ? new GameObject("CarriedStraightPointer") : existing.gameObject;
            weaponObject.transform.SetParent(body.transform, false);
            // 精灵以底端为轴；圆配重中心距底端约 0.7 个素材单位。
            weaponObject.transform.localScale = Vector3.one * 1.16f;
            weaponObject.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            weaponObject.transform.localPosition = new Vector3(3.18f, 1.20f, 0f);
            var weapon = weaponObject.GetComponent<SpriteRenderer>();
            if (weapon == null) weapon = weaponObject.AddComponent<SpriteRenderer>();
            weapon.sprite = sprites["StraightPointer"];
            weapon.sortingOrder = body.sortingOrder - 1;
            var controller = player.GetComponent<PlayerCarriedWeaponVisual>();
            if (controller == null) controller = player.AddComponent<PlayerCarriedWeaponVisual>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("bodyRenderer").objectReferenceValue = body;
            serialized.FindProperty("weaponRenderer").objectReferenceValue = weapon;
            serialized.FindProperty("rightHandOffset").vector2Value = new Vector2(3.18f, 1.20f);
            var frameArray = serialized.FindProperty("bodyOnlySprites");
            frameArray.arraySize = bodies.Length;
            for (int i = 0; i < bodies.Length; i++) frameArray.GetArrayElementAtIndex(i).objectReferenceValue = bodies[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (before != CollisionSignature()) throw new InvalidOperationException("碰撞数据发生变化，停止保存。");
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("V2_IDLE_WEAPON_FIXED: 单一完整武器、左右镜像随身体朝向、碰撞数据不变。");
        }
    }
}
#endif
