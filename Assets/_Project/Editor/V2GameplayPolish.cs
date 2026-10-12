#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TimeMetroidvania.Enemy;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Level;
using TimeMetroidvania.Player;
using TimeMetroidvania.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TimeMetroidvania.Editor
{
    /// <summary>只修改当前场景的入口与玩法参数，依据实际墙体定位，不重新生成用户地图。</summary>
    public static class V2GameplayPolish
    {
        [MenuItem("Tools/时序旅人/V2/Align Entrances And Apply Gameplay Polish")]
        public static void Apply() => ApplyToCurrentScene(true);

        public static void ApplyToCurrentScene(bool backup)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("请先退出运行模式再对齐入口。");
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != "EightRoomGreybox") throw new InvalidOperationException("请打开 EightRoomGreybox。");
            Directory.CreateDirectory("Logs/V2Polish");
            if (backup)
            {
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.Copy(scene.path, "Logs/V2Polish/Before_" + stamp + ".unity", false);
                if (scene.isDirty) EditorSceneManager.SaveScene(scene);
                File.Copy(scene.path, "Logs/V2Polish/UserMap_" + stamp + ".unity", false);
            }
            Undo.RegisterFullObjectHierarchyUndo(GameObject.Find("V2_EightRoomGreybox"), "对齐房间入口");
            StageFiveDoor[] doors = Object.FindObjectsByType<StageFiveDoor>(FindObjectsSortMode.None);
            List<string> report = new();
            foreach (StageFiveDoor door in doors)
            {
                StageFiveRoom room = door.GetComponentInParent<StageFiveRoom>();
                BoxCollider2D trigger = door.GetComponent<BoxCollider2D>();
                bool horizontal = trigger.bounds.size.y > trigger.bounds.size.x;
                float sign = Mathf.Sign(horizontal ? door.transform.localPosition.x : door.transform.localPosition.y);
                Vector2 normal = horizontal ? new Vector2(sign, 0f) : new Vector2(0f, sign);
                Collider2D[] terrain = room.GetComponentsInChildren<Collider2D>()
                    .Where(c => c.enabled && !c.isTrigger && c.GetComponentInParent<HealthSystem>() == null
                        && c.GetComponent<V2TimeRotor>() == null && !c.name.Contains("FrozenSurface")
                        && !c.name.Contains("Gate") && !c.name.Contains("Blocker")).ToArray();
                string prefix = horizontal ? (sign < 0 ? "LeftWall" : "RightWall") : (sign < 0 ? "Floor" : "Ceiling");
                Collider2D[] edge = terrain.Where(c => c.name.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
                if (edge.Length == 0) throw new InvalidOperationException("入口缺少边界墙：" + door.name);
                float plane = horizontal ? edge[0].bounds.center.x
                    : sign < 0 ? edge.Min(c => c.bounds.center.y) : edge.Max(c => c.bounds.center.y);
                List<Vector2> intervals = terrain.Where(c => horizontal
                    ? c.bounds.min.x < plane && c.bounds.max.x > plane
                    : c.bounds.min.y < plane && c.bounds.max.y > plane)
                    .Select(c => horizontal ? new Vector2(c.bounds.min.y, c.bounds.max.y)
                        : new Vector2(c.bounds.min.x, c.bounds.max.x)).OrderBy(v => v.x).ToList();
                List<Vector2> gaps = new();
                float end = intervals[0].y;
                for (int i = 1; i < intervals.Count; i++)
                {
                    if (intervals[i].x - end > 0.5f) gaps.Add(new Vector2(end, intervals[i].x));
                    end = Mathf.Max(end, intervals[i].y);
                }
                if (gaps.Count == 0) throw new InvalidOperationException("找不到真实缺口：" + door.name);
                float previous = horizontal ? door.transform.position.y : door.transform.position.x;
                Vector2 gap = gaps.OrderBy(g => Mathf.Abs((g.x + g.y) * 0.5f - previous)).First();
                float center = (gap.x + gap.y) * 0.5f;
                float required = horizontal ? 3f : 4f;
                if (Mathf.Abs(gap.y - gap.x - required) > 0.1f)
                    throw new InvalidOperationException(door.name + " 实际缺口宽高为 " + (gap.y-gap.x) + "，请先核对地形。");
                door.transform.position = horizontal ? new Vector3(plane, center, 0f) : new Vector3(center, plane, 0f);
                door.transform.localScale = Vector3.one;
                trigger.offset = Vector2.zero;
                trigger.size = horizontal ? new Vector2(1.4f, 3f) : new Vector2(4f, 1.4f);
                StageFiveDoor pair = doors.Single(d => d.SourceRoomId == door.DestinationRoomId && d.DestinationRoomId == door.SourceRoomId);
                door.ConfigureBoundary(normal, pair);
                if (!horizontal) door.ConfigureExitMotion(sign > 0 ? new Vector2(4.5f, 12f) : new Vector2(0f, -2f), sign > 0);
                SerializedObject serialized = new(door);
                Collider2D blocker = serialized.FindProperty("bossBattleBlocker").objectReferenceValue as Collider2D;
                if (blocker != null)
                {
                    blocker.transform.position = door.transform.position;
                    blocker.transform.localScale = horizontal ? new Vector3(0.6f, 3.2f, 1f) : new Vector3(4.2f, 0.6f, 1f);
                }
                EditorUtility.SetDirty(door);
                report.Add(door.name + " center=" + door.transform.position + " size=" + trigger.size + " normal=" + normal);
            }
            foreach (StageFiveDoor door in doors)
            {
                StageFiveDoor pair = door.PairedDoor;
                door.Destination.position = pair.transform.position - (Vector3)pair.OutwardNormal * 1.1f;
                EditorUtility.SetDirty(door.Destination);
            }
            foreach (EnemyBrain enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))
            {
                Ensure<EnemyMotor2D>(enemy.gameObject);
                Ensure<EnemyAttackTelegraphVisual>(enemy.gameObject);
                Ensure<CombatImpactFeedback>(enemy.gameObject);
                enemy.GetComponent<Collider2D>().isTrigger = true;
                bool archer = enemy.Kind == EnemyKind.ClockArcher;
                bool bug = enemy.Kind == EnemyKind.ClockworkBug;
                Float(enemy, "detectionRange", archer ? 4.8f : 5f);
                Float(enemy, "loseRange", archer ? 6f : 7f);
                Float(enemy, "attackRange", archer ? 4.5f : bug ? 3.4f : 1.45f);
                Float(enemy, "attackWindup", archer ? 0.65f : bug ? 0.48f : 0.42f);
                Float(enemy, "attackCooldown", archer ? 1.45f : bug ? 1.35f : 1.1f);
                Float(enemy, "chaseSpeed", bug ? 2.7f : 2f);
                Vector(enemy, "meleeHitRange", new Vector2(1.65f, 0.6f));
            }
            foreach (V2FlyingEnemy enemy in Object.FindObjectsByType<V2FlyingEnemy>(FindObjectsSortMode.None))
            {
                Ensure<EnemyMotor2D>(enemy.gameObject).Flying = true;
                Ensure<CombatImpactFeedback>(enemy.gameObject);
            }
            GameObject player = GameObject.Find("Player");
            Ensure<CombatImpactFeedback>(player);
            Ensure<V2WorldFeedback>(Camera.main.gameObject);
            PlayerCombat combat = player.GetComponent<PlayerCombat>();
            Vector(combat, "attackOffset", new Vector2(0.85f, 0f));
            Vector(combat, "airAttackOffset", new Vector2(0.85f, 0f));
            Vector(combat, "attackSize", new Vector2(1.4f, 1.05f));
            Vector(combat, "upAttackOffset", new Vector2(0f, 0.85f));
            Vector(combat, "upAttackSize", new Vector2(1.05f, 1.4f));
            Vector(combat, "downAttackOffset", new Vector2(0f, -0.85f));
            Vector(combat, "downAttackSize", new Vector2(1.05f, 1.4f));
            Float(combat, "attackDuration", 0.28f);
            Float(combat, "activeStartNormalized", 0.12f);
            Float(combat, "activeEndNormalized", 0.62f);
            Float(combat, "attackInterval", 0.55f);
            Float(player.GetComponent<PlayerMovement>(), "attackMoveMultiplier", 1f);

            V2TimeStopWater water = Object.FindFirstObjectByType<V2TimeStopWater>();
            Bounds pool = water.GetComponent<Collider2D>().bounds;
            Transform surface = GameObject.Find("R3_FrozenSurface").transform;
            surface.position = new Vector3(pool.center.x, pool.max.y - 0.08f, 0f);
            surface.localScale = new Vector3(pool.size.x, 0.16f, 1f);
            Float(water, "underwaterGravityScale", 0.3f);
            Float(water, "maximumUnderwaterRiseSpeed", 8.5f);
            Float(water, "maximumUnderwaterFallSpeed", 5f);
            Float(water, "underwaterDamping", 0.35f);
            V2TimeRotor rotor = Object.FindFirstObjectByType<V2TimeRotor>();
            Float(rotor, "degreesPerSecond", 300f);
            ContactDamage2D oldContact = rotor.GetComponent<ContactDamage2D>();
            if (oldContact != null) Object.DestroyImmediate(oldContact);
            Transform rotorRoom = rotor.transform.parent;
            Transform left = Anchor(rotorRoom, "RotorSafeLeft", new Vector3(rotor.transform.localPosition.x - 4.2f, -11.5f, 0f));
            Transform right = Anchor(rotorRoom, "RotorSafeRight", new Vector3(rotor.transform.localPosition.x + 4.2f, -11.5f, 0f));
            rotor.ConfigureSafety(left, right);
            EditorUtility.SetDirty(rotor);
            // 镜头边界只根据外墙刷新，不改动任何地形位置。
            foreach (StageFiveRoom room in Object.FindObjectsByType<StageFiveRoom>(FindObjectsSortMode.None))
            {
                Collider2D[] walls = room.GetComponentsInChildren<Collider2D>().Where(c =>
                    c.name.StartsWith("LeftWall") || c.name.StartsWith("RightWall") || c.name.StartsWith("Ceiling")
                    || c.name.StartsWith("Floor")).ToArray();
                Bounds bounds = walls[0].bounds;
                foreach (Collider2D wall in walls) bounds.Encapsulate(wall.bounds);
                room.GetComponent<RoomCameraBounds2D>().Configure(room.RoomId, bounds.center, bounds.size);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            File.WriteAllLines("Logs/V2Polish/Entrances.txt", report, new System.Text.UTF8Encoding(false));
            Debug.Log("V2Polish：16 个入口、敌人物理、水池与反馈已迁移，用户地形保持不变。");
        }
        private static T Ensure<T>(GameObject go) where T : Component => go.GetComponent<T>() ?? go.AddComponent<T>();
        private static Transform Anchor(Transform parent, string label, Vector3 local)
        {
            Transform t = parent.Find(label);
            if (t == null) { t = new GameObject(label).transform; t.SetParent(parent, false); }
            t.localPosition = local; return t;
        }
        private static void Float(Object obj, string field, float value)
        { SerializedObject so = new(obj); so.FindProperty(field).floatValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Vector(Object obj, string field, Vector2 value)
        { SerializedObject so = new(obj); so.FindProperty(field).vector2Value = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
#endif
