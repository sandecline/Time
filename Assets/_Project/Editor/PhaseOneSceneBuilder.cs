#if UNITY_EDITOR
using System.Linq;
using TimeMetroidvania.Player;
using TimeMetroidvania.GameTime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TimeMetroidvania.Editor
{
    /// <summary>
    /// 将已验证的移动原型扩展为战斗、资源与世界时间灰盒，并保持构建过程可重复执行。
    /// </summary>
    internal static class PhaseOneSceneBuilder
    {
        private const string SourceScenePath = "Assets/_Project/Scenes/Prototype/MovementPrototype.unity";
        private const string CombatScenePath = "Assets/_Project/Scenes/Prototype/CombatPrototype.unity";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Characters/Player/Player.prefab";

        [MenuItem("Tools/Time Metroidvania/Build Current Prototype")]
        public static void Build()
        {
            Scene sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
            RestoreMovementPrototype();
            EditorSceneManager.SaveScene(sourceScene);
            EditorSceneManager.SaveScene(sourceScene, CombatScenePath, true);
            Scene scene = EditorSceneManager.OpenScene(CombatScenePath, OpenSceneMode.Single);

            WorldTimeService worldTime = CreateWorldTimeService();

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                throw new MissingReferenceException("MovementPrototype 中不存在 Player。");
            }

            player.tag = "Player";
            PlayerStateMachine stateMachine = GetOrAdd<PlayerStateMachine>(player);
            HealthSystem health = GetOrAdd<HealthSystem>(player);
            TimeGauge timeGauge = GetOrAdd<TimeGauge>(player);
            PlayerCombat combat = GetOrAdd<PlayerCombat>(player);
            PlayerDash dash = GetOrAdd<PlayerDash>(player);
            PlayerHealing healing = GetOrAdd<PlayerHealing>(player);
            PlayerTimeAbilities timeAbilities = GetOrAdd<PlayerTimeAbilities>(player);

            ConfigureObjectReference(combat, "attackAction",
                AssetDatabase.LoadAssetAtPath<InputActionReference>("Assets/_Project/Input/GameplayAttack.asset"));
            ConfigureObjectReference(dash, "dashAction",
                AssetDatabase.LoadAssetAtPath<InputActionReference>("Assets/_Project/Input/GameplayDash.asset"));
            ConfigureObjectReference(healing, "timeAbilityAction",
                AssetDatabase.LoadAssetAtPath<InputActionReference>("Assets/_Project/Input/GameplayTimeAbility.asset"));
            ConfigureObjectReference(combat, "worldTime", worldTime);
            ConfigureObjectReference(healing, "worldTime", worldTime);
            ConfigureObjectReference(timeAbilities, "attackAction",
                AssetDatabase.LoadAssetAtPath<InputActionReference>("Assets/_Project/Input/GameplayAttack.asset"));
            ConfigureObjectReference(timeAbilities, "worldTime", worldTime);
            ConfigureBoolean(health, "respawnOnDeath", true);

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                CameraFollow2D follow = GetOrAdd<CameraFollow2D>(mainCamera.gameObject);
                follow.SetTarget(player.transform);
                EditorUtility.SetDirty(follow);
            }

            CreateTrainingDummy(worldTime);
            CreateDashWall();
            CreateBreakableProjectile(worldTime);
            CreateHud(health, stateMachine, timeGauge, healing, timeAbilities, worldTime);

            PrefabUtility.SaveAsPrefabAssetAndConnect(
                player,
                PlayerPrefabPath,
                InteractionMode.AutomatedAction);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AddSceneToBuildSettings(CombatScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"阶段 3 世界时间原型已生成：{CombatScenePath}");
        }

        private static void RestoreMovementPrototype()
        {
            DestroyIfPresent("TrainingDummy");
            DestroyIfPresent("DashWall");
            DestroyIfPresent("BreakableProjectile");
            DestroyIfPresent("PrototypeHUD");
            DestroyIfPresent("WorldTimeService");

            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(player))
                {
                    PrefabUtility.UnpackPrefabInstance(
                        player,
                        PrefabUnpackMode.OutermostRoot,
                        InteractionMode.AutomatedAction);
                }

                DestroyComponentIfPresent<PlayerCombat>(player);
                DestroyComponentIfPresent<PlayerDash>(player);
                DestroyComponentIfPresent<PlayerHealing>(player);
                DestroyComponentIfPresent<PlayerTimeAbilities>(player);
                DestroyComponentIfPresent<TimeGauge>(player);
                DestroyComponentIfPresent<HealthSystem>(player);
            }

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                DestroyComponentIfPresent<CameraFollow2D>(mainCamera.gameObject);
            }
        }

        private static WorldTimeService CreateWorldTimeService()
        {
            DestroyIfPresent("WorldTimeService");
            GameObject serviceObject = new("WorldTimeService");
            WorldTimeService worldTime = serviceObject.AddComponent<WorldTimeService>();
            ConfigureBoolean(worldTime, "timeStopUnlocked", true);
            return worldTime;
        }

        private static void CreateTrainingDummy(WorldTimeService worldTime)
        {
            GameObject previous = GameObject.Find("TrainingDummy");
            if (previous != null)
            {
                Object.DestroyImmediate(previous);
            }

            GameObject dummy = new("TrainingDummy");
            dummy.transform.position = new Vector3(3.8f, -1.15f, 0f);
            dummy.transform.localScale = new Vector3(1.1f, 1.7f, 1f);

            SpriteRenderer renderer = dummy.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Physics/PrototypeSquare.asset");
            renderer.color = new Color(0.83f, 0.3f, 0.28f);

            dummy.AddComponent<BoxCollider2D>();
            Rigidbody2D body = dummy.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;

            HealthSystem health = dummy.AddComponent<HealthSystem>();
            ConfigureInteger(health, "maxHealth", 20);
            ConfigureFloat(health, "damageProtectionDuration", 0.15f);
            LocalTimeController localTime = dummy.AddComponent<LocalTimeController>();
            ConfigureObjectReference(localTime, "worldTime", worldTime);
            TimeAwareDamageReceiver damageReceiver = dummy.AddComponent<TimeAwareDamageReceiver>();
            ConfigureObjectReference(damageReceiver, "worldTime", worldTime);
            dummy.AddComponent<ContactDamage2D>();
        }

        private static void CreateBreakableProjectile(WorldTimeService worldTime)
        {
            DestroyIfPresent("BreakableProjectile");

            GameObject projectile = new("BreakableProjectile");
            projectile.transform.position = new Vector3(-3.2f, -1.15f, 0f);
            projectile.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

            SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Physics/PrototypeSquare.asset");
            renderer.color = new Color(0.25f, 0.85f, 0.95f);

            CircleCollider2D collider = projectile.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            LocalTimeController localTime = projectile.AddComponent<LocalTimeController>();
            ConfigureObjectReference(localTime, "worldTime", worldTime);
            projectile.AddComponent<PrototypeTimeMover>();
            projectile.AddComponent<PrototypeBreakableProjectile>();
        }

        private static void CreateDashWall()
        {
            GameObject previous = GameObject.Find("DashWall");
            if (previous != null)
            {
                Object.DestroyImmediate(previous);
            }

            GameObject wall = new("DashWall");
            wall.transform.position = new Vector3(7.2f, -0.5f, 0f);
            wall.transform.localScale = new Vector3(0.6f, 3f, 1f);

            SpriteRenderer renderer = wall.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Physics/PrototypeSquare.asset");
            renderer.color = new Color(0.22f, 0.3f, 0.42f);

            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
            collider.enabled = true;
        }

        private static void CreateHud(
            HealthSystem health,
            PlayerStateMachine stateMachine,
            TimeGauge timeGauge,
            PlayerHealing healing,
            PlayerTimeAbilities timeAbilities,
            WorldTimeService worldTime)
        {
            GameObject previous = GameObject.Find("PrototypeHUD");
            if (previous != null)
            {
                Object.DestroyImmediate(previous);
            }

            GameObject hudObject = new("PrototypeHUD");
            PrototypeHud hud = hudObject.AddComponent<PrototypeHud>();
            ConfigureObjectReference(hud, "playerHealth", health);
            ConfigureObjectReference(hud, "playerState", stateMachine);
            ConfigureObjectReference(hud, "timeGauge", timeGauge);
            ConfigureObjectReference(hud, "playerHealing", healing);
            ConfigureObjectReference(hud, "playerTimeAbilities", timeAbilities);
            ConfigureObjectReference(hud, "worldTime", worldTime);
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            return target.TryGetComponent(out T component)
                ? component
                : target.AddComponent<T>();
        }

        private static void DestroyIfPresent(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            if (target != null)
            {
                Object.DestroyImmediate(target);
            }
        }

        private static void DestroyComponentIfPresent<T>(GameObject target) where T : Component
        {
            if (target.TryGetComponent(out T component))
            {
                Object.DestroyImmediate(component);
            }
        }

        private static void ConfigureObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new(target);
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureBoolean(Object target, string propertyName, bool value)
        {
            SerializedObject serializedObject = new(target);
            serializedObject.FindProperty(propertyName).boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureInteger(Object target, string propertyName, int value)
        {
            SerializedObject serializedObject = new(target);
            serializedObject.FindProperty(propertyName).intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureFloat(Object target, string propertyName, float value)
        {
            SerializedObject serializedObject = new(target);
            serializedObject.FindProperty(propertyName).floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes.Any(scene => scene.path == scenePath))
            {
                return;
            }

            EditorBuildSettings.scenes = scenes
                .Append(new EditorBuildSettingsScene(scenePath, true))
                .ToArray();
        }
    }
}
#endif
