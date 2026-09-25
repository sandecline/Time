#if UNITY_EDITOR
using System.Linq;
using TimeMetroidvania.Enemy;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimeMetroidvania.Editor
{
    /// <summary>
    /// 基于战斗灰盒生成两段阶段四混合敌人遭遇。
    /// </summary>
    internal static class PhaseFourEncounterBuilder
    {
        private const string CombatScenePath = "Assets/_Project/Scenes/Prototype/CombatPrototype.unity";
        private const string EncounterScenePath = "Assets/_Project/Scenes/Prototype/EnemyEncounter.unity";
        private const string SquareSpritePath = "Assets/_Project/Physics/PrototypeSquare.asset";

        [MenuItem("Tools/Time Metroidvania/Build Phase 4 Encounter")]
        public static void Build()
        {
            PhaseOneSceneBuilder.Build();
            Scene source = EditorSceneManager.OpenScene(CombatScenePath, OpenSceneMode.Single);
            EditorSceneManager.SaveScene(source, EncounterScenePath, true);
            EditorSceneManager.OpenScene(EncounterScenePath, OpenSceneMode.Single);

            DestroyIfPresent("TrainingDummy");
            DestroyIfPresent("BreakableProjectile");
            DestroyIfPresent("DashWall");
            WorldTimeService worldTime = Object.FindFirstObjectByType<WorldTimeService>();
            Transform player = GameObject.Find("Player").transform;
            Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);

            GameObject extraGround = new("EncounterGround");
            extraGround.layer = LayerMask.NameToLayer("Ground");
            extraGround.transform.position = new Vector3(18.5f, -2.5f, 0f);
            extraGround.transform.localScale = new Vector3(21f, 1f, 1f);
            extraGround.AddComponent<BoxCollider2D>();
            SpriteRenderer extraGroundRenderer = extraGround.AddComponent<SpriteRenderer>();
            extraGroundRenderer.sprite = square;
            extraGroundRenderer.color = GameObject.Find("Ground")
                .GetComponent<SpriteRenderer>().color;

            GameObject poolObject = new("ClockBoltPool");
            ProjectilePool pool = poolObject.AddComponent<ProjectilePool>();
            SetReference(pool, "worldTime", worldTime);
            SetReference(pool, "projectileSprite", square);

            Transform first = new GameObject("Encounter_1_Guard_Bug").transform;
            CreateEnemy("Guard_A", EnemyKind.Guard, new Vector3(3.5f, -1.25f, 0f),
                first, player, worldTime, pool, square);
            CreateEnemy("ClockworkBug_A", EnemyKind.ClockworkBug,
                new Vector3(6.1f, -1.55f, 0f), first, player, worldTime, pool, square);

            Transform second = new GameObject("Encounter_2_Bug_Archer_Guard").transform;
            CreateEnemy("ClockworkBug_B", EnemyKind.ClockworkBug,
                new Vector3(13.2f, -1.55f, 0f), second, player, worldTime, pool, square);
            CreateEnemy("ClockArcher_B", EnemyKind.ClockArcher,
                new Vector3(17.4f, -1.25f, 0f), second, player, worldTime, pool, square);
            CreateEnemy("Guard_B", EnemyKind.Guard,
                new Vector3(20.5f, -1.25f, 0f), second, player, worldTime, pool, square);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AddSceneToBuildSettings(EncounterScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"阶段 4 敌人遭遇场景已生成：{EncounterScenePath}");
        }

        private static void CreateEnemy(
            string name, EnemyKind kind, Vector3 position, Transform parent, Transform player,
            WorldTimeService worldTime, ProjectilePool pool, Sprite sprite)
        {
            GameObject enemy = new(name);
            enemy.transform.SetParent(parent);
            enemy.transform.position = position;
            enemy.transform.localScale = kind == EnemyKind.ClockworkBug
                ? new Vector3(0.8f, 0.8f, 1f)
                : new Vector3(0.95f, 1.45f, 1f);
            SpriteRenderer renderer = enemy.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = kind switch
            {
                EnemyKind.Guard => new Color(0.85f, 0.32f, 0.25f),
                EnemyKind.ClockworkBug => new Color(0.7f, 0.28f, 0.85f),
                _ => new Color(0.25f, 0.7f, 0.85f)
            };

            BoxCollider2D collider = enemy.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            HealthSystem health = enemy.AddComponent<HealthSystem>();
            SetInteger(health, "maxHealth", kind == EnemyKind.Guard ? 4 : 3);
            SetFloat(health, "damageProtectionDuration", 0.12f);
            LocalTimeController localTime = enemy.AddComponent<LocalTimeController>();
            SetReference(localTime, "worldTime", worldTime);
            TimeAwareDamageReceiver receiver = enemy.AddComponent<TimeAwareDamageReceiver>();
            SetReference(receiver, "worldTime", worldTime);
            EnemyBrain brain = enemy.AddComponent<EnemyBrain>();
            SetEnum(brain, "kind", (int)kind);
            SetReference(brain, "target", player);
            SetReference(brain, "projectilePool", pool);

            if (kind == EnemyKind.ClockworkBug)
            {
                SetFloat(brain, "chaseSpeed", 4.8f);
                SetFloat(brain, "patrolSpeed", 2.2f);
                SetFloat(brain, "attackWindup", 0.2f);
                SetFloat(brain, "attackCooldown", 0.7f);
                SetFloat(brain, "attackRange", 0.85f);
            }
            else if (kind == EnemyKind.ClockArcher)
            {
                SetFloat(brain, "patrolHalfDistance", 0f);
                SetFloat(brain, "detectionRange", 6.5f);
                SetFloat(brain, "loseRange", 8.5f);
                SetFloat(brain, "attackRange", 6f);
                SetFloat(brain, "attackWindup", 0.6f);
                SetFloat(brain, "attackCooldown", 1.6f);
                SetInteger(brain, "maxShots", 8);
            }
        }

        private static void DestroyIfPresent(string name)
        {
            GameObject target = GameObject.Find(name);
            if (target != null)
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

        private static void SetEnum(Object target, string property, int value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(property).enumValueIndex = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddSceneToBuildSettings(string path)
        {
            if (EditorBuildSettings.scenes.Any(scene => scene.path == path))
            {
                return;
            }
            EditorBuildSettings.scenes = EditorBuildSettings.scenes
                .Append(new EditorBuildSettingsScene(path, true))
                .ToArray();
        }
    }
}
#endif
