#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using TimeMetroidvania.Player;
using TimeMetroidvania.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TimeMetroidvania.Editor
{
    /// <summary>在运行模式验证三向攻击及镜像、有效帧、单武器和刀光边界。</summary>
    [InitializeOnLoad]
    internal static class V2AttackArtSmoke
    {
        private const string Key = "V2AttackArtSmoke";
        private static int previousFrame = -1;
        private static int swing;
        private static float nextSwing = 3.3f;
        private static bool setup;
        private static bool attacking;
        private static bool captured;
        private static readonly HashSet<string> frames = new();
        private static int checkedActiveFrames;

        static V2AttackArtSmoke() { EditorApplication.update += Tick; }

        public static void Begin()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/V2/EightRoomArtPreview.unity");
            SessionState.SetBool(Key, true);
            SessionState.SetString(Key + ".Start", DateTime.UtcNow.ToString("O"));
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Key, false)) return;
            try
            {
                if ((DateTime.UtcNow - DateTime.Parse(SessionState.GetString(Key + ".Start", ""))).TotalSeconds > 75)
                    throw new Exception("攻击运行检查超时");
                if (!EditorApplication.isPlaying || EditorApplication.isCompiling || previousFrame == Time.frameCount) return;
                previousFrame = Time.frameCount;
                if (Time.timeSinceLevelLoad < 3f) return;
                var player = GameObject.FindWithTag("Player");
                var movement = player.GetComponent<PlayerMovement>();
                var combat = player.GetComponent<PlayerCombat>();
                var body = player.transform.Find("ArtVisual").GetComponent<SpriteRenderer>();
                if (!setup)
                {
                    // 截图读回耗时不计入下一帧，防止检查本身跳过短攻击的后半段。
                    Time.captureDeltaTime = 1f / 120f;
                    player.transform.position += Vector3.up * 5f;
                    player.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                    player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
                    Physics2D.SyncTransforms();
                    setup = true;
                    return;
                }
                if (Time.timeSinceLevelLoad < nextSwing) return;
                movement.enabled = false;
                if (!attacking)
                {
                    if (swing == 6) { Finish(true, $"swings=6; activeFrames={checkedActiveFrames}; direction/mirror/frame/weapon/slash checks passed"); return; }
                    typeof(PlayerMovement).GetProperty("FacingDirection", BindingFlags.Instance | BindingFlags.Public)
                        .SetValue(movement, swing < 3 ? 1 : -1);
                    player.GetComponent<PlayerStateMachine>().ResetToNormal();
                    if (!combat.BeginAttack((AttackDirection)(swing % 3))) throw new Exception("未能开始攻击 " + swing);
                    frames.Clear();
                    attacking = true;
                    captured = false;
                    return;
                }
                if (combat.IsAttacking)
                {
                    string prefix = new[] { "AttackHorizontal", "AttackUp", "AttackDown" }[swing % 3];
                    if (!body.sprite.name.StartsWith(prefix)) throw new Exception("方向动画不匹配：" + body.sprite.name);
                    if (body.flipX != (swing >= 3)) throw new Exception("攻击朝向错误");
                    frames.Add(body.sprite.name);
                    var carried = body.transform.Find("CarriedStraightPointer").GetComponent<SpriteRenderer>();
                    var weapon = player.transform.Find("AttackWeaponPivot/StraightPointer").GetComponent<SpriteRenderer>();
                    if (carried.enabled || !weapon.enabled) throw new Exception("攻击出现双武器或缺少武器");
                    if (combat.IsAttackActive)
                    {
                        checkedActiveFrames++;
                        if (!(body.sprite.name.EndsWith("1") || body.sprite.name.EndsWith("2"))) throw new Exception("有效判定期间播放了前摇或收招帧");
                        if (combat.CurrentAttackDirection != AttackDirection.Horizontal
                            && Vector2.Distance(combat.AttackSize, new Vector2(1.15f, 1.35f)) > 0.01f)
                            throw new Exception("上下劈仍使用旧判定范围：" + combat.AttackSize);
                        CheckBounds(player, combat, weapon);
                        if (!captured)
                        {
                            Capture(player, combat, swing);
                            captured = true;
                        }
                    }
                    return;
                }
                if (frames.Count != 4 || !captured) throw new Exception("攻击帧未完整播放：" + string.Join(",", frames));
                Debug.Log($"V2_ATTACK_SWING_PASS: index={swing}; frames={frames.Count}");
                swing++;
                attacking = false;
                nextSwing = Time.timeSinceLevelLoad + 0.25f;
            }
            catch (Exception e) { Finish(false, e.Message); }
        }

        private static void CheckBounds(GameObject player, PlayerCombat combat, SpriteRenderer weapon)
        {
            Vector2 center = combat.CurrentAttackCenter;
            Vector2 half = combat.AttackSize * 0.5f;
            var slash = player.transform.Find("SlashVisual").GetComponent<LineRenderer>();
            for (int i = 0; i < slash.positionCount; i++)
            {
                Vector2 delta = (Vector2)slash.GetPosition(i) - center;
                float radius = slash.widthMultiplier * 0.5f;
                if (Mathf.Abs(delta.x) + radius > half.x + 0.006f || Mathf.Abs(delta.y) + radius > half.y + 0.006f)
                    throw new Exception("刀光越过攻击判定边界");
            }
            Bounds rect = new Bounds(center, combat.AttackSize);
            rect.Expand(0.012f);
            Bounds bodyBounds = player.GetComponent<Collider2D>().bounds;
            // 刀刃可以从身体内部伸出；身体外的刀刃必须处于攻击判定内。
            Bounds sprite = weapon.sprite.bounds;
            float length = Vector3.Distance(weapon.transform.TransformPoint(new Vector3(0f, sprite.min.y)),
                weapon.transform.TransformPoint(new Vector3(0f, sprite.max.y)));
            if (Mathf.Abs(length - 1.16f) > 0.001f) throw new Exception("攻击指针长度发生变化");
            for (int i = 3; i <= 10; i++)
            {
                for (int side = -1; side <= 1; side++)
                {
                    Vector3 point = weapon.transform.TransformPoint(new Vector3(side * sprite.extents.x, Mathf.Lerp(sprite.min.y, sprite.max.y, i / 10f), 0f));
                    point.z = rect.center.z;
                    if (!rect.Contains(point) && !bodyBounds.Contains(point))
                        throw new Exception($"指针刀刃超出判定/身体范围：direction={combat.CurrentAttackDirection}; point={point - player.transform.position}; size={combat.AttackSize}");
                }
            }
        }

        private static void Capture(GameObject player, PlayerCombat combat, int index)
        {
            var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            Vector3 previous = camera.transform.position;
            float previousSize = camera.orthographicSize;
            camera.transform.position = player.transform.position + new Vector3(0f, 0f, -10f);
            camera.orthographicSize = 1.8f;
            var go = new GameObject("HitboxCheckOnly");
            var line = go.AddComponent<LineRenderer>();
            var material = new Material(Shader.Find("Sprites/Default"));
            line.material = material;
            line.startColor = line.endColor = new Color(0.3f, 1f, 0.45f, 1f);
            line.widthMultiplier = 0.016f;
            line.sortingOrder = 100;
            line.positionCount = 4;
            line.loop = true;
            Vector2 c = combat.CurrentAttackCenter, h = combat.AttackSize * 0.5f;
            line.SetPositions(new[] { (Vector3)(c + new Vector2(-h.x,-h.y)), (Vector3)(c + new Vector2(h.x,-h.y)),
                (Vector3)(c + new Vector2(h.x,h.y)), (Vector3)(c + new Vector2(-h.x,h.y)) });
            V2ArtPreviewSmoke.Capture(camera, $"ArtSource/V2/Previews/Player_Attack_Hitbox_{index}_v04.png");
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(material);
            camera.transform.position = previous;
            camera.orthographicSize = previousSize;
        }

        private static void Finish(bool success, string detail)
        {
            SessionState.SetBool(Key, false);
            Debug.Log("V2_ATTACK_RUNTIME_" + (success ? "PASS: " : "FAIL: ") + detail);
            EditorApplication.Exit(success ? 0 : 1);
        }
    }
}
#endif
