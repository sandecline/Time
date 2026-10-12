#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TimeMetroidvania.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static TimeMetroidvania.Editor.V2ArtSupplementBuilder;

namespace TimeMetroidvania.Editor
{
    /// <summary>为八个房间替换地形表面与落脚边缘，保留关卡碰撞数据。</summary>
    internal static class V2RoomTerrainArtBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/V2/EightRoomArtPreview.unity";
        private const string WetAtlas = "Terrain_R3_R5_R8_Atlas_v01.png";
        private const string RoomAtlas = "Terrain_R1_R2_R6_R7_Atlas_v01.png";
        private const string ArenaTrim = "BossArenaRuneTrim_v01.png";

        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            string before = CollisionSignature();
            Copy("ArtSource/V2/05_Environment/" + WetAtlas, WetAtlas);
            Copy("ArtSource/V2/05_Environment/" + RoomAtlas, RoomAtlas);
            Copy("ArtSource/V2/05_Environment/" + ArenaTrim, ArenaTrim);
            AssetDatabase.Refresh();

            var wetRegions = new List<Region>();
            string[] wetNames = { "Wet", "Brass", "Dark" };
            for (int column = 0; column < 3; column++)
            {
                wetRegions.Add(new Region(wetNames[column] + "Fill",
                    new RectInt(column * 512, 0, 512, 730), true, false));
                wetRegions.Add(new Region(wetNames[column] + "Edge",
                    new RectInt(column * 512, 730, 512, 294), true, false));
            }
            var roomRegions = new List<Region>();
            string[] roomNames = { "Sun", "Gallery", "Mist", "Shrine" };
            int[] x = { 0, 443, 887, 1330, 1774 };
            for (int column = 0; column < 4; column++)
            {
                roomRegions.Add(new Region(roomNames[column] + "Fill",
                    new RectInt(x[column], 0, x[column + 1] - x[column], 620), true, false));
                roomRegions.Add(new Region(roomNames[column] + "Edge",
                    new RectInt(x[column], 620, x[column + 1] - x[column], 267), true, false));
            }
            var wet = Import(WetAtlas, wetRegions);
            var rooms = Import(RoomAtlas, roomRegions);
            var trim = Import(ArenaTrim, new List<Region> {
                new Region("ArenaRune", new RectInt(0, 0, 2167, 726), true, false)
            })["ArenaRune"];
            var surfaces = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                .Where(renderer => renderer.name == "V2_StoneSurface").ToArray();
            if (surfaces.Length != 86) throw new InvalidOperationException("地形显示数异常：" + surfaces.Length);

            int edges = 0;
            foreach (var surface in surfaces)
            {
                var ground = surface.transform.parent.GetComponent<BoxCollider2D>();
                var room = ground == null ? null : ground.GetComponentInParent<StageFiveRoom>();
                if (ground == null || room == null) throw new InvalidOperationException("地形未归属房间：" + surface.name);
                string theme = Theme(room.RoomId);
                var art = room.RoomId == "R3" || room.RoomId == "R4" || room.RoomId == "R5" || room.RoomId == "R8" ? wet : rooms;
                surface.sprite = art[theme + "Fill"];
                surface.color = Color.white;
                if (Vector3.Distance(surface.bounds.center, ground.bounds.center) > 0.01f
                    || Vector2.Distance(surface.bounds.size, ground.bounds.size) > 0.01f)
                    throw new InvalidOperationException("地形显示与碰撞不齐：" + ground.name);

                var edge = ground.transform.Find("V2_MossContactEdge")?.GetComponent<SpriteRenderer>();
                if (edge == null) continue;
                Sprite edgeSprite = art[theme + "Edge"];
                edge.sprite = edgeSprite;
                edge.color = Color.white;
                Vector3 parentScale = edge.transform.parent.lossyScale;
                edge.transform.localScale = new Vector3(0.2f / parentScale.x,
                    (0.2f / edgeSprite.bounds.size.y) / parentScale.y, 1f / parentScale.z);
                edge.size = new Vector2(ground.bounds.size.x / 0.2f, edgeSprite.bounds.size.y);
                if (Mathf.Abs(edge.bounds.size.x - ground.bounds.size.x) > 0.01f
                    || Mathf.Abs(edge.bounds.size.y - 0.2f) > 0.01f)
                    throw new InvalidOperationException("落脚边缘尺寸异常：" + ground.name);
                edges++;

                // 旧前景小物件随房间石材调色，避免冷色房间出现明亮绿植。
                Color decoration = room.RoomId switch
                {
                    "R3" => new Color(0.60f, 0.85f, 0.85f),
                    "R6" => new Color(0.82f, 0.90f, 0.97f),
                    "R8" => new Color(0.47f, 0.59f, 0.75f),
                    _ => Color.white
                };
                foreach (string name in new[] { "V2_Grass", "V2_Rubble", "V2_HangingIvy" })
                {
                    var detail = ground.transform.Find(name)?.GetComponent<SpriteRenderer>();
                    if (detail != null) detail.color = decoration;
                }
            }
            var arenaLine = GameObject.Find("BossArenaLine")?.GetComponent<SpriteRenderer>();
            if (arenaLine == null || arenaLine.GetComponent<Collider2D>() != null)
                throw new InvalidOperationException("Boss 场地界线丢失或意外带碰撞。");
            arenaLine.sprite = trim;
            arenaLine.color = Color.white;
            arenaLine.drawMode = SpriteDrawMode.Tiled;
            arenaLine.tileMode = SpriteTileMode.Continuous;
            arenaLine.sortingOrder = 2;
            Vector3 arenaParentScale = arenaLine.transform.parent.lossyScale;
            float arenaScale = 0.18f / trim.bounds.size.y;
            arenaLine.transform.localScale = new Vector3(arenaScale / arenaParentScale.x,
                arenaScale / arenaParentScale.y, 1f / arenaParentScale.z);
            arenaLine.size = new Vector2(18f / arenaScale, trim.bounds.size.y);
            if (Mathf.Abs(arenaLine.bounds.size.x - 18f) > 0.01f
                || Mathf.Abs(arenaLine.bounds.size.y - 0.18f) > 0.01f)
                throw new InvalidOperationException("Boss 场地界线尺寸异常。");
            if (before != CollisionSignature()) throw new InvalidOperationException("碰撞数据发生变化，停止保存。");
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            Vector3 originalPosition = camera.transform.position;
            float originalSize = camera.orthographicSize;
            try
            {
                camera.orthographicSize = 5f;
                foreach (int index in new[] { 1, 3, 4, 7, 8 })
                {
                    var room = GameObject.Find("Room_R" + index).GetComponent<StageFiveRoom>();
                    var backdrop = room.transform.Find("GreyboxBackdrop").GetComponent<SpriteRenderer>();
                    var platform = surfaces
                        .Where(surface => surface.transform.parent.Find("V2_MossContactEdge") != null
                            && surface.GetComponentInParent<StageFiveRoom>() == room)
                        .OrderBy(surface => Mathf.Abs(surface.bounds.center.x - backdrop.bounds.center.x)
                            + 0.3f * Mathf.Abs(surface.bounds.max.y - backdrop.bounds.center.y))
                        .FirstOrDefault();
                    float cameraY = platform == null ? backdrop.bounds.center.y : platform.bounds.max.y + 2.5f;
                    camera.transform.position = new Vector3(backdrop.bounds.center.x, cameraY, originalPosition.z);
                    V2ArtPreviewSmoke.Capture(camera, $"ArtSource/V2/Previews/R{index}_RoomTerrain_v01.png");
                }
            }
            finally
            {
                camera.transform.position = originalPosition;
                camera.orthographicSize = originalSize;
            }
            Debug.Log($"V2_ROOM_TERRAIN_READY surfaces={surfaces.Length}; edges={edges}; themes=7; arenaLine=1; COLLIDERS_UNCHANGED");
        }

        private static string Theme(string roomId) => roomId switch
        {
            "R1" => "Sun",
            "R2" => "Gallery",
            "R3" => "Wet",
            "R4" or "R5" => "Brass",
            "R6" => "Mist",
            "R7" => "Shrine",
            "R8" => "Dark",
            _ => throw new InvalidOperationException("未知房间：" + roomId)
        };
    }
}
#endif
