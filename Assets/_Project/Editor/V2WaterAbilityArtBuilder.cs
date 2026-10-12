#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TimeMetroidvania.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static TimeMetroidvania.Editor.V2ArtSupplementBuilder;

namespace TimeMetroidvania.Editor
{
    /// <summary>将水池两态和时停能力道具接入独立试玩场景。</summary>
    internal static class V2WaterAbilityArtBuilder
    {
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/V2/EightRoomArtPreview.unity");
            string before = CollisionSignature();
            const string waterFile = "Water_RuntimeAtlas_v02.png";
            const string abilityFile = "Ability_ChronoCore_v01.png";
            Copy("ArtSource/V2/06_Props/" + waterFile, waterFile);
            Copy("ArtSource/V2/06_Props/" + abilityFile, abilityFile);
            AssetDatabase.Refresh();
            var waterSprites = Import(waterFile, new List<Region>
            {
                new("NormalSurface", new RectInt(0, 0, 768, 512), true, false),
                new("FrozenSurface", new RectInt(768, 0, 768, 512), true, false),
                new("NormalBody", new RectInt(0, 512, 768, 512), true, false),
                new("FrozenBody", new RectInt(768, 512, 768, 512), true, false)
            });
            var abilitySprites = Import(abilityFile, new List<Region>
            {
                new("ChronoCoreIdle", new RectInt(0, 0, 610, 860), true, false),
                new("ChronoCoreGlow", new RectInt(610, 0, 610, 860), true, false),
                new("ChronoCoreEmpty", new RectInt(1220, 0, 609, 860), true, false)
            });

            var water = GameObject.Find("R3_TimeStopWater")?.GetComponent<V2TimeStopWater>();
            var ice = GameObject.Find("R3_FrozenSurface")?.GetComponent<BoxCollider2D>();
            var pickup = GameObject.Find("Pickup_TimeStop_R5")?.GetComponent<StageFivePickup>();
            if (water == null || ice == null || pickup == null)
                throw new InvalidOperationException("缺少水池、冰面或时停道具。 ");

            var waterCollider = water.GetComponent<BoxCollider2D>();
            water.GetComponent<SpriteRenderer>().enabled = false;
            ice.GetComponent<SpriteRenderer>().enabled = false;
            var waterVisual = Create(water.transform, "ArtWater", waterSprites["NormalBody"],
                waterCollider.bounds.center, waterCollider.bounds.size, 1);
            waterVisual.color = new Color(1f, 1f, 1f, 0.62f);
            var iceVisual = Create(ice.transform, "ArtIceSurface", waterSprites["FrozenSurface"],
                ice.bounds.center, new Vector2(ice.bounds.size.x, 0.34f), 3);
            iceVisual.enabled = false;
            var normalSurface = Create(water.transform, "ArtNormalSurface", waterSprites["NormalSurface"],
                new Vector3(waterCollider.bounds.center.x, waterCollider.bounds.max.y - 0.08f, 0f),
                new Vector2(waterCollider.bounds.size.x, 0.28f), 3);
            var waterSettings = new SerializedObject(water);
            waterSettings.FindProperty("waterRenderer").objectReferenceValue = waterVisual;
            waterSettings.FindProperty("surfaceRenderer").objectReferenceValue = iceVisual;
            waterSettings.FindProperty("normalSurfaceRenderer").objectReferenceValue = normalSurface;
            waterSettings.FindProperty("normalWaterSprite").objectReferenceValue = waterSprites["NormalBody"];
            waterSettings.FindProperty("frozenWaterSprite").objectReferenceValue = waterSprites["FrozenBody"];
            waterSettings.FindProperty("frozenSurfaceSprite").objectReferenceValue = waterSprites["FrozenSurface"];
            waterSettings.ApplyModifiedPropertiesWithoutUndo();

            pickup.GetComponent<SpriteRenderer>().enabled = false;
            var pickupVisual = Create(pickup.transform, "ArtVisual", abilitySprites["ChronoCoreIdle"],
                pickup.transform.position, new Vector2(1.7f, 1.55f), 4);
            var pickupSettings = new SerializedObject(pickup);
            pickupSettings.FindProperty("artRenderer").objectReferenceValue = pickupVisual;
            pickupSettings.FindProperty("timeStopSprite").objectReferenceValue = abilitySprites["ChronoCoreIdle"];
            pickupSettings.ApplyModifiedPropertiesWithoutUndo();

            if (before != CollisionSignature()) throw new InvalidOperationException("碰撞数据变化，停止保存。");
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            Vector3 oldPosition = camera.transform.position;
            float oldSize = camera.orthographicSize;
            try
            {
                camera.orthographicSize = 4f;
                camera.transform.position = new Vector3(water.transform.position.x, water.transform.position.y, oldPosition.z);
                V2ArtPreviewSmoke.Capture(camera, "ArtSource/V2/Previews/R3_Water_v02.png");
                camera.transform.position = new Vector3(pickup.transform.position.x, pickup.transform.position.y, oldPosition.z);
                V2ArtPreviewSmoke.Capture(camera, "ArtSource/V2/Previews/R5_ChronoCore_v02.png");
            }
            finally { camera.transform.position = oldPosition; camera.orthographicSize = oldSize; }
            Debug.Log("V2_WATER_ABILITY_ART_READY: water=2, iceSurface=1, timeStopPickup=1; colliders unchanged.");
        }

        private static SpriteRenderer Create(Transform parent, string name, Sprite sprite,
            Vector3 worldPosition, Vector2 worldSize, int order)
        {
            Transform child = parent.Find(name);
            if (child == null) { child = new GameObject(name).transform; child.SetParent(parent, false); }
            var renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = child.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = order;
            child.position = worldPosition;
            Vector3 scale = parent.lossyScale;
            child.localScale = new Vector3(worldSize.x / sprite.bounds.size.x / scale.x,
                worldSize.y / sprite.bounds.size.y / scale.y, 1f);
            return renderer;
        }
    }
}
#endif
