#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TimeMetroidvania.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static TimeMetroidvania.Editor.V2ArtSupplementBuilder;

namespace TimeMetroidvania.Editor
{
    /// <summary>将透明时间特效绑定至玩家状态和无伤害的命中反馈。</summary>
    internal static class V2VfxArtBuilder
    {
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/V2/EightRoomArtPreview.unity");
            string before = CollisionSignature();
            const string name = "TimeCombat_RuntimeAtlas_v02.png";
            Copy("ArtSource/V2/08_VFX/" + name, name);
            AssetDatabase.Refresh();
            var regions = new List<Region>();
            string[] labels = { "BlueImpact", "OrangeImpact", "TimeRing",
                "HealGlint", "BlueShatter", "AnchorCrack" };
            for (int i = 0; i < labels.Length; i++)
                regions.Add(new Region(labels[i],
                    new RectInt(i % 3 * 512, i / 3 * 512, 512, 512), true, false));
            var sprites = Import(name, regions);
            var feedback = UnityEngine.Object.FindFirstObjectByType<V2WorldFeedback>();
            var player = GameObject.FindWithTag("Player");
            if (feedback == null || player == null)
                throw new InvalidOperationException("缺少命中特效管理器或玩家。");
            var feedbackSettings = new SerializedObject(feedback);
            feedbackSettings.FindProperty("blueImpactSprite").objectReferenceValue = sprites["BlueImpact"];
            feedbackSettings.FindProperty("orangeImpactSprite").objectReferenceValue = sprites["OrangeImpact"];
            feedbackSettings.FindProperty("timeRingSprite").objectReferenceValue = sprites["TimeRing"];
            feedbackSettings.ApplyModifiedPropertiesWithoutUndo();

            Transform visual = player.transform.Find("TimeVfx");
            if (visual == null) { visual = new GameObject("TimeVfx").transform; visual.SetParent(player.transform, false); }
            var renderer = visual.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = visual.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprites["TimeRing"];
            renderer.enabled = false;
            renderer.sortingOrder = 7;
            visual.localPosition = Vector3.up * 0.12f;
            Vector3 scale = player.transform.lossyScale;
            visual.localScale = new Vector3(0.85f / renderer.sprite.bounds.size.x / scale.x,
                0.85f / renderer.sprite.bounds.size.x / scale.y, 1f);
            var animator = player.GetComponent<PlayerTimeVfxAnimator>();
            if (animator == null) animator = player.AddComponent<PlayerTimeVfxAnimator>();
            var settings = new SerializedObject(animator);
            settings.FindProperty("targetRenderer").objectReferenceValue = renderer;
            Assign(settings, "slowCharge", sprites["TimeRing"]);
            Assign(settings, "slowReady", sprites["TimeRing"]);
            Assign(settings, "heal", sprites["HealGlint"]);
            settings.ApplyModifiedPropertiesWithoutUndo();
            if (before != CollisionSignature()) throw new InvalidOperationException("碰撞数据变化，停止保存。");
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("V2_VFX_ART_READY: blue/orange impact, world clock ring, player charge/heal; colliders unchanged.");
        }

        private static void Assign(SerializedObject settings, string field, Sprite sprite)
        {
            SerializedProperty array = settings.FindProperty(field);
            array.arraySize = 1;
            array.GetArrayElementAtIndex(0).objectReferenceValue = sprite;
        }
    }
}
#endif
