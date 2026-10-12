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
    /// <summary>把旋转刀臂和 Boss 封门的可见轮廓收进原有实体碰撞盒。</summary>
    internal static class V2RotorBossSealArtBuilder
    {
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/V2/EightRoomArtPreview.unity");
            string before = CollisionSignature();
            const string name = "RotorBossSeal_Runtime_v01.png";
            Copy("ArtSource/V2/06_Props/" + name, name);
            AssetDatabase.Refresh();
            var sprites = Import(name, new List<Region>
            {
                new("RotorBlade", new RectInt(0, 0, 768, 1024), true, false),
                new("BossSeal", new RectInt(768, 0, 768, 1024), true, false)
            });
            var rotor = GameObject.Find("R4_TimeRotor")?.GetComponent<BoxCollider2D>();
            if (rotor == null) throw new InvalidOperationException("缺少 R4 转闸刀臂。");
            Replace(rotor, sprites["RotorBlade"], new Vector2(0.44f, 6.7f));
            int seals = 0;
            foreach (string label in new[] { "BossBattleBlocker_R6_R8", "BossBattleBlocker_R8_R6" })
            {
                var blocker = GameObject.Find(label)?.GetComponent<BoxCollider2D>();
                if (blocker == null) throw new InvalidOperationException("缺少战斗封门：" + label);
                SpriteRenderer visual = Replace(blocker, sprites["BossSeal"], new Vector2(0.55f, 3f));
                int controllers = 0;
                foreach (var door in UnityEngine.Object.FindObjectsByType<StageFiveDoor>(FindObjectsSortMode.None))
                {
                    var settings = new SerializedObject(door);
                    if (settings.FindProperty("bossBattleBlocker").objectReferenceValue != blocker) continue;
                    settings.FindProperty("bossBattleBlockerRenderer").objectReferenceValue = visual;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                    controllers++;
                }
                if (controllers != 1) throw new InvalidOperationException("封门控制器数量异常：" + label);
                seals++;
            }
            if (before != CollisionSignature()) throw new InvalidOperationException("碰撞数据变化，停止保存。");
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"V2_ROTOR_SEAL_ART_READY: rotor=1, seals={seals}; visible widths=0.44/0.55; collider widths=0.5/0.6; colliders unchanged.");
        }

        private static SpriteRenderer Replace(BoxCollider2D collider, Sprite sprite, Vector2 worldSize)
        {
            SpriteRenderer old = collider.GetComponent<SpriteRenderer>();
            if (old == null) throw new InvalidOperationException("缺少灰盒渲染层：" + collider.name);
            bool wasVisible = old.enabled;
            old.enabled = false;
            Transform child = collider.transform.Find("ArtVisual");
            if (child == null) { child = new GameObject("ArtVisual").transform; child.SetParent(collider.transform, false); }
            var art = child.GetComponent<SpriteRenderer>();
            if (art == null) art = child.gameObject.AddComponent<SpriteRenderer>();
            art.sprite = sprite;
            art.color = Color.white;
            art.sortingLayerID = old.sortingLayerID;
            art.sortingOrder = old.sortingOrder;
            art.enabled = wasVisible;
            child.localPosition = Vector3.zero;
            Vector3 parentScale = collider.transform.lossyScale;
            child.localScale = new Vector3(worldSize.x / sprite.bounds.size.x / parentScale.x,
                worldSize.y / sprite.bounds.size.y / parentScale.y, 1f);
            return art;
        }
    }
}
#endif
