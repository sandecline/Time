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
    /// <summary>为八个房间分别接入远景；沿用现有背景边界和房间碰撞布局。</summary>
    internal static class V2RoomBackdropBuilder
    {
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/V2/EightRoomArtPreview.unity");
            string before = CollisionSignature();
            const string first = "Environment_R1_R4_Backdrops_v02.png";
            const string second = "Environment_R5_R8_Backdrops_v02.png";
            Copy("ArtSource/V2/05_Environment/" + first, first);
            Copy("ArtSource/V2/05_Environment/" + second, second);
            AssetDatabase.Refresh();
            var regionsA = new List<Region>();
            var regionsB = new List<Region>();
            for (int i = 0; i < 4; i++)
            {
                RectInt rect = new((i % 2) * 768, (i / 2) * 512, 768, 512);
                regionsA.Add(new Region("RoomR" + (i + 1), rect, false, false));
                regionsB.Add(new Region("RoomR" + (i + 5), rect, false, false));
            }
            var spritesA = Import(first, regionsA);
            var spritesB = Import(second, regionsB);
            int count = 0;
            foreach (var room in UnityEngine.Object.FindObjectsByType<StageFiveRoom>(FindObjectsSortMode.None))
            {
                var target = room.transform.Find("GreyboxBackdrop")?.GetComponent<SpriteRenderer>();
                if (target == null) throw new InvalidOperationException("缺少房间背景：" + room.RoomId);
                Bounds bounds = target.bounds;
                int index = int.Parse(room.RoomId.Substring(1));
                target.sprite = index <= 4 ? spritesA["RoomR" + index] : spritesB["RoomR" + index];
                Vector3 parent = target.transform.parent == null ? Vector3.one : target.transform.parent.lossyScale;
                target.transform.localScale = new Vector3(bounds.size.x / target.sprite.bounds.size.x / parent.x,
                    bounds.size.y / target.sprite.bounds.size.y / parent.y, 1f / parent.z);
                target.color = index switch
                {
                    1 => new Color(0.78f, 0.84f, 0.82f),
                    2 => new Color(0.82f, 0.88f, 0.87f),
                    3 => new Color(0.76f, 0.86f, 0.87f),
                    4 => new Color(0.82f, 0.82f, 0.76f),
                    8 => new Color(0.74f, 0.78f, 0.85f),
                    _ => new Color(0.79f, 0.84f, 0.84f)
                };
                count++;
            }
            if (count != 8 || before != CollisionSignature()) throw new InvalidOperationException("房间数或碰撞数据异常，停止保存。");
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            Vector3 original = camera.transform.position;
            float originalSize = camera.orthographicSize;
            try
            {
                camera.orthographicSize = 5f;
                foreach (int index in new[] { 1, 3, 4, 7, 8 })
                {
                    var backdrop = GameObject.Find("Room_R" + index).transform.Find("GreyboxBackdrop").GetComponent<SpriteRenderer>();
                    camera.transform.position = new Vector3(backdrop.bounds.center.x, backdrop.bounds.center.y, original.z);
                    V2ArtPreviewSmoke.Capture(camera, $"ArtSource/V2/Previews/R{index}_RoomBackdrop_v02.png");
                }
            }
            finally { camera.transform.position = original; camera.orthographicSize = originalSize; }
            Debug.Log("V2_ROOM_BACKDROPS_READY: eight unique backgrounds; colliders unchanged.");
        }
    }
}
#endif
