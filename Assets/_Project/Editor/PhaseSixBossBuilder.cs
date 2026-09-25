#if UNITY_EDITOR
using TimeMetroidvania.Boss;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Level;
using TimeMetroidvania.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimeMetroidvania.Editor
{
    /// <summary>
    /// 在阶段五七房间灰盒的房间七中生成两阶段 Boss 综合考试。
    /// </summary>
    internal static class PhaseSixBossBuilder
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/Prototype/SevenRoomSlice.unity";
        private const string SquareSpritePath =
            "Assets/_Project/Physics/PrototypeSquare.asset";

        [MenuItem("Tools/Time Metroidvania/Build Phase 6 Boss")]
        public static void Build()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            GameObject player = GameObject.Find("Player");
            Transform roomSeven = GameObject.Find("Room_07")?.transform;
            WorldTimeService worldTime = Object.FindFirstObjectByType<WorldTimeService>();
            StageFiveLevelController level =
                Object.FindFirstObjectByType<StageFiveLevelController>();
            Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
            if (player == null || roomSeven == null || worldTime == null
                || level == null || square == null)
            {
                throw new MissingReferenceException(
                    "阶段六生成需要玩家、房间七、时间服务、关卡控制器和灰盒 Sprite。");
            }

            DestroyIfPresent("BossArenaMarker");
            DestroyIfPresent("BossArenaFloorMarker");
            DestroyIfPresent("BossProjectilePool");
            DestroyIfPresent("Boss_TimeWarden");

            GameObject poolObject = new("BossProjectilePool");
            poolObject.transform.SetParent(roomSeven);
            BossProjectilePool pool = poolObject.AddComponent<BossProjectilePool>();
            SetReference(pool, "worldTime", worldTime);
            SetReference(pool, "projectileSprite", square);
            SetInteger(pool, "capacity", 24);

            Vector2 arenaCenter = new(roomSeven.position.x + 1.5f, -1.25f);
            GameObject boss = new("Boss_TimeWarden");
            boss.transform.SetParent(roomSeven);
            boss.transform.position = new Vector3(arenaCenter.x + 2f, -1.15f, 0f);
            boss.transform.localScale = new Vector3(1.65f, 2.25f, 1f);

            SpriteRenderer renderer = boss.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = new Color(0.82f, 0.24f, 0.28f);
            BoxCollider2D collider = boss.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            HealthSystem health = boss.AddComponent<HealthSystem>();
            SetInteger(health, "maxHealth", 28);
            SetFloat(health, "damageProtectionDuration", 0.1f);

            LocalTimeController localTime = boss.AddComponent<LocalTimeController>();
            SetReference(localTime, "worldTime", worldTime);
            TimeAwareDamageReceiver receiver =
                boss.AddComponent<TimeAwareDamageReceiver>();
            SetReference(receiver, "worldTime", worldTime);
            SetInteger(receiver, "maxPendingDamage", 6);

            ContactDamage2D contactDamage = boss.AddComponent<ContactDamage2D>();
            SetInteger(contactDamage, "damage", 1);
            BossBrain brain = boss.AddComponent<BossBrain>();
            brain.Configure(player.transform, pool, level, arenaCenter);

            CreateArenaMarker(roomSeven, arenaCenter, square);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log($"阶段 6 两阶段 Boss 已生成：{ScenePath}");
        }

        private static void CreateArenaMarker(
            Transform parent, Vector2 center, Sprite square)
        {
            GameObject marker = new("BossArenaFloorMarker");
            marker.transform.SetParent(parent);
            marker.transform.position = new Vector3(center.x, -1.92f, 0f);
            marker.transform.localScale = new Vector3(12f, 0.12f, 1f);
            SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = new Color(0.58f, 0.18f, 0.68f);
        }

        private static void DestroyIfPresent(string name)
        {
            GameObject target;
            while ((target = GameObject.Find(name)) != null)
            {
                Object.DestroyImmediate(target);
            }
        }

        private static void SetReference(Object target, string property, Object value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string property, float value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(property).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInteger(Object target, string property, int value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(property).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
