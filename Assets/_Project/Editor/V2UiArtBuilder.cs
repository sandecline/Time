#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TimeMetroidvania.Boss;
using TimeMetroidvania.Level;
using TimeMetroidvania.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static TimeMetroidvania.Editor.V2ArtSupplementBuilder;

namespace TimeMetroidvania.Editor
{
    /// <summary>切片 V2 界面图集，并替换独立试玩场景中实际运行的灰盒 HUD。</summary>
    internal static class V2UiArtBuilder
    {
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/V2/EightRoomArtPreview.unity");
            string before = CollisionSignature();
            string[] names = { "HUD_RuntimeAtlas_v02.png", "Menus_RuntimeAtlas_v02.png",
                "Map_Frame_v02.png", "Map_Markers_v02.png" };
            foreach (string name in names) Copy("ArtSource/V2/07_UI/" + name, name);
            AssetDatabase.Refresh();
            foreach (string name in names)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath("Assets/_Project/Art/V2/Preview/" + name);
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }
            var hud = Import(names[0], Grid(3, 2, 512, 512,
                "HealthFull", "HealthEmpty", "TimeStopIcon", "TimeBarFrame", "TimeBarFill", "BossBarFrame"));
            var menus = Import(names[1], Grid(2, 2, 768, 512,
                "TitlePlaque", "PausePanel", "DeathPlaque", "EndingPlaque"));
            var map = Import(names[2], new List<Region>
            {
                new("MapBoard", new RectInt(0, 0, 1774, 887), true, false)
            });
            var markers = Import(names[3], Grid(3, 2, 512, 512,
                "RoomDiscovered", "RoomCurrent", "RoomUnknown", "MapLink", "MapGate", "MapCheckpoint"));

            var playerHud = UnityEngine.Object.FindFirstObjectByType<PrototypeHud>();
            var level = UnityEngine.Object.FindFirstObjectByType<StageFiveLevelController>();
            var boss = UnityEngine.Object.FindFirstObjectByType<BossBrain>();
            if (playerHud == null || level == null || boss == null)
                throw new InvalidOperationException("缺少运行 HUD、关卡信息或 Boss 组件。");
            var playerSettings = new SerializedObject(playerHud);
            Set(playerSettings, "panelSprite", menus["PausePanel"]);
            Set(playerSettings, "fullClockSprite", hud["HealthFull"]);
            Set(playerSettings, "emptyClockSprite", hud["HealthEmpty"]);
            Set(playerSettings, "timeBarFrameSprite", hud["TimeBarFrame"]);
            Set(playerSettings, "timeBarFillSprite", hud["TimeBarFill"]);
            Set(playerSettings, "timeStopIconSprite", hud["TimeStopIcon"]);
            playerSettings.ApplyModifiedPropertiesWithoutUndo();
            var levelSettings = new SerializedObject(level);
            Set(levelSettings, "hudPanelSprite", menus["PausePanel"]);
            levelSettings.ApplyModifiedPropertiesWithoutUndo();
            var bossSettings = new SerializedObject(boss);
            Set(bossSettings, "bossHudFrameSprite", hud["BossBarFrame"]);
            Set(bossSettings, "bossHudFillSprite", hud["TimeBarFill"]);
            bossSettings.ApplyModifiedPropertiesWithoutUndo();
            if (map.Count != 1 || markers.Count != 6 || before != CollisionSignature())
                throw new InvalidOperationException("UI 切片或碰撞数据异常，停止保存。");
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("V2_UI_ART_READY: hud=6, menus=4, mapBoard=1, mapMarkers=6; runtime HUD bound; colliders unchanged.");
        }

        private static List<Region> Grid(int cols, int rows, int width, int height, params string[] names)
        {
            if (names.Length != cols * rows) throw new ArgumentException("UI 格数与名称数量不符。");
            var result = new List<Region>();
            for (int row = 0; row < rows; row++)
                for (int col = 0; col < cols; col++)
                    result.Add(new Region(names[row * cols + col],
                        new RectInt(col * width, row * height, width, height), true, false));
            return result;
        }

        private static void Set(SerializedObject settings, string property, Sprite sprite)
        {
            SerializedProperty field = settings.FindProperty(property);
            if (field == null) throw new InvalidOperationException("缺少界面字段：" + property);
            field.objectReferenceValue = sprite;
        }
    }
}
#endif
