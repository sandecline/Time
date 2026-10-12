#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TimeMetroidvania.Editor
{
    /// <summary>
    /// 使用现有八房间场景和玩家显示比例截取 A1 待机素材预览，不保存场景改动。
    /// </summary>
    internal static class V2PlayerArtScalePreview
    {
        private const string ScenePath = "Assets/_Project/Scenes/V2/EightRoomGreybox.unity";
        private const string SourcePath = "ArtSource/V2/01_Player/KeyPoses/A1Trial_v01/Player_Idle_Right_Composite.png";
        private const string OutputFolder = "ArtSource/V2/01_Player/KeyPoses/A1Trial_v01";

        /// <summary>
        /// 载入试验待机图，在 R1 与 R8 当前灰盒中按对应镜头尺寸各截取一张预览。
        /// 此方法只修改内存中的场景对象；批处理退出时不保存场景。
        /// </summary>
        public static void Capture()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string imagePath = Path.Combine(projectRoot, SourcePath);
            string outputPath = Path.Combine(projectRoot, OutputFolder);
            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException("缺少 A1 待机组合试样", imagePath);
            }

            string r1Path = Path.Combine(outputPath, "Player_Idle_Right_R1_Size5_Preview_v02.png");
            string r8Path = Path.Combine(outputPath, "Player_Idle_Right_R8_Size5_5_Preview_v02.png");
            if (File.Exists(r1Path) || File.Exists(r8Path))
            {
                throw new IOException("预览图已存在，请先另起版本号，避免覆盖用户修改。 ");
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = GameObject.FindWithTag("Player");
            GameObject cameraObject = GameObject.Find("Main Camera");
            GameObject r8Room = GameObject.Find("Room_R8");
            if (player == null || cameraObject == null || r8Room == null)
            {
                throw new InvalidOperationException("八房间场景缺少玩家、主镜头或 R8 房间。 ");
            }

            SpriteRenderer visual = player.transform.Find("ArtVisual")?.GetComponent<SpriteRenderer>();
            Camera camera = cameraObject.GetComponent<Camera>();
            if (visual == null || camera == null)
            {
                throw new InvalidOperationException("玩家显示层或主镜头组件缺失。 ");
            }

            byte[] sourceBytes = File.ReadAllBytes(imagePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Sprite sprite = null;
            Vector3 originalPlayerPosition = player.transform.position;
            Vector3 originalCameraPosition = camera.transform.position;
            float originalCameraSize = camera.orthographicSize;
            Sprite originalSprite = visual.sprite;
            try
            {
                if (!texture.LoadImage(sourceBytes, false) || texture.width != 64 || texture.height != 80)
                {
                    throw new InvalidOperationException("A1 待机试样必须是 64×80 PNG。 ");
                }

                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                sprite = Sprite.Create(texture, new Rect(0, 0, 64, 80), new Vector2(0.5f, 0.5f), 32f);
                visual.sprite = sprite;
                visual.color = Color.white;
                visual.flipX = false;

                // R1 将玩家暂移到检查点右侧，避免检查点图形遮挡角色。
                player.transform.position = originalPlayerPosition + new Vector3(2f, 0f, 0f);
                SetView(camera, player.transform.position, 5f, originalCameraPosition.z);
                SaveCameraFrame(camera, r1Path);

                // R8 仅在内存中把玩家放到 Boss 左侧的地面附近。
                player.transform.position = r8Room.transform.position + new Vector3(-4f, -6.2f, 0f);
                SetView(camera, player.transform.position, 5.5f, originalCameraPosition.z);
                SaveCameraFrame(camera, r8Path);
                Debug.Log($"A1 待机镜头预览已保存：{r1Path}；{r8Path}");
            }
            finally
            {
                visual.sprite = originalSprite;
                player.transform.position = originalPlayerPosition;
                camera.transform.position = originalCameraPosition;
                camera.orthographicSize = originalCameraSize;
                if (sprite != null) UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void SetView(Camera camera, Vector3 playerPosition, float size, float cameraZ)
        {
            camera.orthographic = true;
            camera.orthographicSize = size;
            camera.transform.position = new Vector3(playerPosition.x + 1f, playerPosition.y + 2f, cameraZ);
        }

        /// <summary>
        /// 将当前相机画面保存为 1920×1080 PNG，同时恢复相机和渲染目标状态。
        /// </summary>
        private static void SaveCameraFrame(Camera camera, string path)
        {
            const int width = 1920;
            const int height = 1080;
            RenderTexture target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            Texture2D capture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                capture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                capture.Apply(false);
                File.WriteAllBytes(path, capture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(capture);
            }
        }
    }
}
#endif
