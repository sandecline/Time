#if UNITY_EDITOR
using System;
using System.IO;
using TimeMetroidvania.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TimeMetroidvania.Editor
{
    /// <summary>
    /// 将已校验的待机试样和背景参考接入独立试玩场景，保留原场景玩法。
    /// </summary>
    internal static class V2ArtPlayablePreview
    {
        private const string Root = "Assets/_Project/Art/V2/Preview";
        private const string Scene = "Assets/_Project/Scenes/V2/EightRoomArtPreview.unity";

        [MenuItem("Tools/时序旅人/Open V2 Art Preview")]
        public static void Open()
        {
            if (EditorApplication.isPlaying) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(Scene);
        }

        public static void Build()
        {
            if (File.Exists(Scene)) throw new IOException("试玩场景已存在，停止覆盖。");
            Directory.CreateDirectory(Root);
            CopySource("ArtSource/V2/01_Player/KeyPoses/A1Trial_v01/Player_Idle_Right_Composite.png", "Player_Idle.png");
            CopySource("ArtSource/V2/05_Environment/Background_Far_Mid_Reference_v01.png", "Background_Reference.png");
            AssetDatabase.Refresh();
            Sprite playerSprite = ImportSprite("Player_Idle.png", null);
            // 仅使用上方完整横幅；白色分隔线不进入显示范围。
            Sprite background = ImportSprite("Background_Reference.png", new Rect(0, 382, 2079, 374));
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/V2/EightRoomGreybox.unity");
            GameObject player = GameObject.FindWithTag("Player");
            SpriteRenderer visual = player.transform.Find("ArtVisual").GetComponent<SpriteRenderer>();
            visual.sprite = playerSprite;
            visual.color = Color.white;
            // 当前只接入通过像素尺寸检查的待机帧；动作继续由现有碰撞可视化表达。
            var animator = new SerializedObject(player.GetComponent<PlayerArtAnimator>());
            foreach (string field in new[] { "idle", "run", "jumpRise", "fall", "attackGround", "attackAir", "dash", "hurt", "death", "heal", "slowCharge" })
            {
                var frames = animator.FindProperty(field);
                frames.arraySize = 1;
                frames.GetArrayElementAtIndex(0).objectReferenceValue = playerSprite;
            }
            animator.ApplyModifiedPropertiesWithoutUndo();
            var room = GameObject.Find("Room_R1").transform;
            var backdrop = new GameObject("V2_Background_Reference_Preview");
            backdrop.transform.SetParent(room, false);
            var renderer = backdrop.AddComponent<SpriteRenderer>();
            renderer.sprite = background;
            renderer.sortingOrder = -1;
            renderer.color = new Color(0.82f, 0.88f, 0.87f, 1f);
            backdrop.transform.position = new Vector3(player.transform.position.x + 8f, player.transform.position.y + 3f, 5f);
            backdrop.transform.localScale = Vector3.one * (16f / background.bounds.size.y);
            player.transform.position += new Vector3(2f, 0f, 0f);
            EditorSceneManager.SaveScene(scene, Scene);
            AssetDatabase.SaveAssets();
            Camera camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            camera.transform.position = new Vector3(player.transform.position.x + 1f, player.transform.position.y + 2f, camera.transform.position.z);
            camera.orthographicSize = 5f;
            Directory.CreateDirectory("ArtSource/V2/Previews");
            Capture(camera, "ArtSource/V2/Previews/R1_PlayableArtPreview_v01.png");
            Debug.Log("V2_ART_PREVIEW_READY: " + Scene);
        }

        public static void RefreshCapture()
        {
            var scene = EditorSceneManager.OpenScene(Scene);
            var backdrop = GameObject.Find("V2_Background_Reference_Preview").GetComponent<SpriteRenderer>();
            backdrop.sortingOrder = -1;
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                if (renderer.transform.IsChildOf(GameObject.Find("Room_R1").transform))
                    Debug.Log($"R1_VISUAL: {renderer.name} order={renderer.sortingOrder} bounds={renderer.bounds}");
            EditorSceneManager.SaveScene(scene);
            var player = GameObject.FindWithTag("Player");
            var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            camera.transform.position = new Vector3(player.transform.position.x + 1f, player.transform.position.y + 2f, camera.transform.position.z);
            camera.orthographicSize = 5f;
            Capture(camera, "ArtSource/V2/Previews/R1_PlayableArtPreview_v02.png");
        }

        private static void CopySource(string source, string name)
        {
            string destination = Root + "/" + name;
            if (!File.Exists(destination)) File.Copy(source, destination);
        }

        private static Sprite ImportSprite(string name, Rect? rect)
        {
            string path = Root + "/" + name;
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = rect.HasValue ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            importer.maxTextureSize = 4096;
            if (rect.HasValue)
            {
#pragma warning disable CS0618
                importer.spritesheet = new[] { new SpriteMetaData { name = "Background_Top", rect = rect.Value, alignment = (int)SpriteAlignment.Center, pivot = new Vector2(0.5f, 0.5f) } };
#pragma warning restore CS0618
            }
            importer.SaveAndReimport();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sprite) return sprite;
            throw new InvalidOperationException("素材导入失败：" + path);
        }

        private static void Capture(Camera camera, string path)
        {
            var target = RenderTexture.GetTemporary(1920, 1080, 24);
            var previousActive = RenderTexture.active;
            var previousTarget = camera.targetTexture;
            var texture = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
#endif
