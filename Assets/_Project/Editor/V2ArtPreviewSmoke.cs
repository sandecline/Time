#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TimeMetroidvania.Presentation;
using TimeMetroidvania.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace TimeMetroidvania.Editor
{
    /// <summary>运行试玩场景，检查素材引用、动画更新和启动异常，并保留实际运行镜头。</summary>
    [InitializeOnLoad]
    internal static class V2ArtPreviewSmoke
    {
        private const string Key = "V2ArtPreviewSmoke.Active";
        private static int frames;
        private static string firstSprite;
        private static bool animationAdvanced;
        private static int previousGameFrame = -1;
        private static int motionPhase;
        private static float motionPhaseTime;
        private static readonly HashSet<string> motionSprites = new();
        private static bool carriedHidden = true;
        private static bool motionPassed = true;
        private static Keyboard runKeyboard;
        private static int runPhase;
        private static float runStart;
        private static readonly HashSet<string> rightRun = new();
        private static readonly HashSet<string> leftRun = new();
        private static Vector3 runWeaponScale;
        private static bool runWeaponValid = true;
        private static InputSettings previousInputSettings;
        private static InputSettings runInputSettings;

        public static void BeginRun()
        {
            Begin();
            SessionState.SetBool(Key + ".Run", true);
        }

        public static void BeginAir()
        {
            Begin();
            SessionState.SetBool(Key + ".Air", true);
        }

        static V2ArtPreviewSmoke()
        {
            EditorApplication.update += Tick;
            Application.logMessageReceived += OnLog;
        }

        public static void Begin()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/V2/EightRoomArtPreview.unity");
            SessionState.SetInt(Key + ".Surfaces", UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Count(r => r.name == "V2_StoneSurface"));
            SessionState.SetBool(Key, true);
            SessionState.SetInt(Key + ".Errors", 0);
            SessionState.SetBool(Key + ".Air", false);
            SessionState.SetBool(Key + ".Run", false);
            SessionState.SetString(Key + ".Start", DateTime.UtcNow.ToString("O"));
            EditorApplication.EnterPlaymode();
        }

        private static void OnLog(string message, string stack, LogType type)
        {
            if (SessionState.GetBool(Key, false) && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                SessionState.SetInt(Key + ".Errors", SessionState.GetInt(Key + ".Errors", 0) + 1);
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Key, false)) return;
            if ((DateTime.UtcNow - DateTime.Parse(SessionState.GetString(Key + ".Start", ""))).TotalSeconds > 60)
            { Finish(false, "运行模式检查超时"); return; }
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (previousGameFrame == Time.frameCount) return;
            previousGameFrame = Time.frameCount;
            frames++;
            var player = GameObject.FindWithTag("Player");
            var renderer = player == null ? null : player.transform.Find("ArtVisual")?.GetComponent<SpriteRenderer>();
            if (renderer?.sprite != null)
            {
                firstSprite ??= renderer.sprite.name;
                animationAdvanced |= renderer.sprite.name != firstSprite;
            }
            if (frames < 180 || Time.timeSinceLevelLoad < 3f) return;
            if (SessionState.GetBool(Key + ".Run", false) && !CheckRun(player, renderer)) return;
            if (SessionState.GetBool(Key + ".Air", false) && !CheckMotion(player, renderer)) return;
            var animator = player == null ? null : player.GetComponent<PlayerArtAnimator>();
            bool references = animator != null && renderer?.sprite != null;
            if (references)
            {
                var serialized = new SerializedObject(animator);
                foreach (string field in new[] { "idle", "run" })
                {
                    var array = serialized.FindProperty(field);
                    references &= array.arraySize == (field == "idle" ? 4 : 6);
                    for (int i = 0; i < array.arraySize; i++) references &= array.GetArrayElementAtIndex(i).objectReferenceValue != null;
                }
            }
            int surfaces = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                .Count(r => r.name == "V2_StoneSurface" && r.sprite != null && r.enabled);
            var camera = GameObject.Find("Main Camera")?.GetComponent<Camera>();
            if (camera != null)
            {
                Capture(camera, "ArtSource/V2/Previews/R1_RuntimeWeaponFix_v03.png");
                var carried = player.GetComponent<PlayerCarriedWeaponVisual>();
                if (carried != null && renderer != null)
                {
                    Vector3 previousPosition = camera.transform.position;
                    float previousSize = camera.orthographicSize;
                    bool previousFlip = renderer.flipX;
                    camera.transform.position = new Vector3(renderer.bounds.center.x, renderer.bounds.center.y, previousPosition.z);
                    camera.orthographicSize = 1.05f;
                    renderer.flipX = false;
                    carried.RefreshVisual();
                    Capture(camera, "ArtSource/V2/Previews/Player_Idle_Weapon_Right_v03.png");
                    renderer.flipX = true;
                    carried.RefreshVisual();
                    Capture(camera, "ArtSource/V2/Previews/Player_Idle_Weapon_Left_v03.png");
                    renderer.flipX = previousFlip;
                    carried.RefreshVisual();
                    camera.transform.position = previousPosition;
                    camera.orthographicSize = previousSize;
                }
            }
            int errors = SessionState.GetInt(Key + ".Errors", 0);
            Finish(references && surfaces > 0 && surfaces == SessionState.GetInt(Key + ".Surfaces", 0) && errors == 0 && animationAdvanced && motionPassed,
                $"references={references}; surfaces={surfaces}; animationAdvanced={animationAdvanced}; animatorEnabled={animator?.enabled}; motionPassed={motionPassed}; errors={errors}; timeScale={Time.timeScale}; gameTime={Time.timeSinceLevelLoad}");
        }

        private static bool CheckMotion(GameObject player, SpriteRenderer renderer)
        {
            var movement = player.GetComponent<PlayerMovement>();
            var dash = player.GetComponent<PlayerDash>();
            if (motionPhase == 0)
            {
                movement.ApplyPogoBounce(7f);
                motionPhase = 1;
                motionPhaseTime = Time.time;
                return false;
            }
            string sprite = renderer.sprite == null ? "" : renderer.sprite.name;
            if (sprite is "JumpRise" or "JumpApex" or "Fall" || sprite.StartsWith("Dash", StringComparison.Ordinal))
            {
                motionSprites.Add(sprite);
                var carried = renderer.transform.Find("CarriedStraightPointer").GetComponent<SpriteRenderer>();
                carriedHidden &= !carried.enabled;
            }
            if (motionPhase == 1 && movement.IsGrounded && Time.time - motionPhaseTime > 0.3f)
            {
                motionPassed &= dash.BeginDash();
                motionPhase = 2;
                motionPhaseTime = Time.time;
            }
            else if (motionPhase == 2 && !dash.IsDashing && Time.time - motionPhaseTime > 0.2f)
            {
                motionPhase = 3;
                motionPhaseTime = Time.time;
            }
            else if (motionPhase == 3 && Time.time - motionPhaseTime > 0.2f)
            {
                motionPassed &= motionSprites.Contains("JumpRise") && motionSprites.Contains("JumpApex")
                    && motionSprites.Contains("Fall") && motionSprites.Contains("Dash0")
                    && motionSprites.Contains("Dash3") && carriedHidden;
                Debug.Log("V2_MOTION_CHECK: " + string.Join(",", motionSprites) + "; carriedHidden=" + carriedHidden);
                SessionState.SetBool(Key + ".Air", false);
                return true;
            }
            return false;
        }

        private static bool CheckRun(GameObject player, SpriteRenderer renderer)
        {
            var weapon = renderer.transform.Find("CarriedStraightPointer").GetComponent<SpriteRenderer>();
            if (runPhase == 0)
            {
                // 批处理没有获得焦点的 Game View，仅在检查期间使用临时输入设置。
                previousInputSettings = InputSystem.settings;
                runInputSettings = UnityEngine.Object.Instantiate(previousInputSettings);
                runInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                runInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings = runInputSettings;
                runKeyboard = InputSystem.AddDevice<Keyboard>();
                runWeaponScale = weapon.transform.localScale;
                var floor = GameObject.Find("Room_R1").transform.Find("FloorLow").GetComponent<Collider2D>();
                player.transform.position = new Vector3(floor.bounds.min.x + 2f, player.transform.position.y, player.transform.position.z);
                Physics2D.SyncTransforms();
                runStart = Time.time;
                runPhase = 1;
            }
            float elapsed = Time.time - runStart;
            if (elapsed < 0.75f) InputSystem.QueueStateEvent(runKeyboard, new KeyboardState(UnityEngine.InputSystem.Key.D));
            else if (elapsed < 1.5f) InputSystem.QueueStateEvent(runKeyboard, new KeyboardState(UnityEngine.InputSystem.Key.A));
            else InputSystem.QueueStateEvent(runKeyboard, new KeyboardState());
            string name = renderer.sprite == null ? "" : renderer.sprite.name;
            if (name.StartsWith("RunBody", StringComparison.Ordinal))
            {
                (renderer.flipX ? leftRun : rightRun).Add(name);
                runWeaponValid &= weapon.enabled && Vector3.Distance(weapon.transform.localScale, runWeaponScale) < 0.0001f;
            }
            if (elapsed < 1.75f) return false;
            motionPassed &= rightRun.Count == 6 && leftRun.Count == 6 && runWeaponValid;
            Debug.Log($"V2_RUN_CHECK: rightFrames={rightRun.Count}; leftFrames={leftRun.Count}; constantWeaponScale={runWeaponValid}");
            SessionState.SetBool(Key + ".Run", false);
            InputSystem.RemoveDevice(runKeyboard);
            if (previousInputSettings != null)
            {
                InputSystem.settings = previousInputSettings;
                UnityEngine.Object.DestroyImmediate(runInputSettings);
            }
            return true;
        }

        internal static void Capture(Camera camera, string path)
        {
            var target = RenderTexture.GetTemporary(1920, 1080, 24);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
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
                UnityEngine.Object.Destroy(texture);
            }
        }

        private static void Finish(bool passed, string details)
        {
            SessionState.SetBool(Key, false);
            Debug.Log("V2_ART_RUNTIME_" + (passed ? "PASS: " : "FAIL: ") + details);
            EditorApplication.Exit(passed ? 0 : 1);
        }
    }
}
#endif
