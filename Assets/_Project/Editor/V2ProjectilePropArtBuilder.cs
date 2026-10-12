#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TimeMetroidvania.Boss;
using TimeMetroidvania.Enemy;
using TimeMetroidvania.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static TimeMetroidvania.Editor.V2ArtSupplementBuilder;

namespace TimeMetroidvania.Editor
{
    /// <summary>接入实际弹幕与机关精灵，碰撞形状和位置沿用试玩场景。</summary>
    internal static class V2ProjectilePropArtBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/V2/EightRoomArtPreview.unity";

        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            string before = CollisionSignature();
            const string projectileFile = "Projectile_RuntimeAtlas_v03.png";
            const string propFile = "Mechanisms_RuntimeAtlas_v02.png";
            Copy("ArtSource/V2/04_Projectiles/" + projectileFile, projectileFile);
            Copy("ArtSource/V2/06_Props/" + propFile, propFile);
            AssetDatabase.Refresh();
            var bullets = Import(projectileFile, new List<Region>
            {
                new("BossBlue", new RectInt(0, 0, 543, 724), true, false),
                new("BossOrange", new RectInt(543, 0, 543, 724), true, false),
                new("BossPurple", new RectInt(1086, 0, 543, 724), true, false),
                new("ArcherBolt", new RectInt(1629, 0, 543, 724), true, false)
            });
            var props = Import(propFile, new List<Region>
            {
                new("GateClosed", new RectInt(0, 0, 512, 512), true, false),
                new("GateOpen", new RectInt(512, 0, 512, 512), true, false),
                new("LeverOff", new RectInt(1024, 0, 512, 512), true, false),
                new("LeverOn", new RectInt(0, 512, 512, 512), true, false),
                new("RotorOff", new RectInt(512, 512, 512, 512), true, false),
                new("RotorOn", new RectInt(1024, 512, 512, 512), true, false)
            });

            var bossPool = GameObject.Find("V2_BossProjectilePool")?.GetComponent<BossProjectilePool>();
            if (bossPool == null) throw new InvalidOperationException("缺少 Boss 弹幕池。 ");
            var bossSettings = new SerializedObject(bossPool);
            bossSettings.FindProperty("breakableProjectileSprite").objectReferenceValue = bullets["BossBlue"];
            bossSettings.FindProperty("unbreakableProjectileSprite").objectReferenceValue = bullets["BossOrange"];
            bossSettings.FindProperty("timeAnchorProjectileSprite").objectReferenceValue = bullets["BossPurple"];
            bossSettings.ApplyModifiedPropertiesWithoutUndo();

            int archerPools = 0;
            foreach (var pool in UnityEngine.Object.FindObjectsByType<ProjectilePool>(FindObjectsSortMode.None))
            {
                var settings = new SerializedObject(pool);
                settings.FindProperty("projectileSprite").objectReferenceValue = bullets["ArcherBolt"];
                settings.ApplyModifiedPropertiesWithoutUndo();
                archerPools++;
            }
            if (archerPools == 0) throw new InvalidOperationException("缺少弓手弹幕池。");

            var gate = GameObject.Find("R4_ShortcutGate")?.GetComponent<V2ShortcutGate>();
            var blocker = gate?.transform.Find("SolidGate")?.GetComponent<BoxCollider2D>();
            if (gate == null || blocker == null) throw new InvalidOperationException("缺少捷径门或门体。");
            blocker.GetComponent<SpriteRenderer>().enabled = false;
            var door = Create(gate.transform, "ArtDoor", props["GateClosed"], blocker.bounds.center,
                new Vector2(1.8f, 3.2f), 4);
            var lever = Create(gate.transform, "ArtLever", props["LeverOff"],
                gate.transform.position + new Vector3(1.65f, 0f, 0f), new Vector2(0.9f, 1.1f), 5);
            gate.ConfigureArt(door, props["GateClosed"], props["GateOpen"],
                lever, props["LeverOff"], props["LeverOn"]);

            var rotorHub = GameObject.Find("R4_TimeRotorHub")?.GetComponent<SpriteRenderer>();
            if (rotorHub == null) throw new InvalidOperationException("缺少转闸中心。");
            rotorHub.sprite = props["RotorOn"];
            rotorHub.color = Color.white;
            // 转闸的致伤体是窄长刀臂，中心齿轮压在刀臂宽度以内。
            SetWorldSize(rotorHub.transform, rotorHub.sprite, new Vector2(0.44f, 0.44f));

            if (before != CollisionSignature()) throw new InvalidOperationException("碰撞数据变化，停止保存。");
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            Vector3 oldPosition = camera.transform.position;
            float oldSize = camera.orthographicSize;
            try
            {
                camera.transform.position = new Vector3(gate.transform.position.x, gate.transform.position.y, oldPosition.z);
                camera.orthographicSize = 3.2f;
                V2ArtPreviewSmoke.Capture(camera, "ArtSource/V2/Previews/R4_ShortcutGate_v02.png");
            }
            finally { camera.transform.position = oldPosition; camera.orthographicSize = oldSize; }
            Debug.Log($"V2_PROJECTILE_PROP_ART_READY: boss=3, archerPools={archerPools}, gate=2, lever=2, rotorHub=1; colliders unchanged.");
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
            SetWorldSize(child, sprite, worldSize);
            return renderer;
        }

        private static void SetWorldSize(Transform target, Sprite sprite, Vector2 size)
        {
            Vector3 parentScale = target.parent == null ? Vector3.one : target.parent.lossyScale;
            target.localScale = new Vector3(size.x / sprite.bounds.size.x / parentScale.x,
                size.y / sprite.bounds.size.y / parentScale.y, 1f);
        }
    }
}
#endif
