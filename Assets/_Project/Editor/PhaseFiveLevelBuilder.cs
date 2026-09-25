#if UNITY_EDITOR
using System.Linq;
using TimeMetroidvania.Enemy;
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
    /// 以已验证的遭遇场景为基础，生成可回访的七房间阶段五灰盒。
    /// </summary>
    internal static class PhaseFiveLevelBuilder
    {
        private const string SourceScenePath =
            "Assets/_Project/Scenes/Prototype/EnemyEncounter.unity";
        private const string LevelScenePath =
            "Assets/_Project/Scenes/Prototype/SevenRoomSlice.unity";
        private const string SquareSpritePath =
            "Assets/_Project/Physics/PrototypeSquare.asset";
        private const float RoomSpacing = 32f;

        [MenuItem("Tools/Time Metroidvania/Build Phase 5 Seven Rooms")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene source = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
            EditorSceneManager.SaveScene(source, LevelScenePath, true);
            EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);

            GameObject player = GameObject.Find("Player");
            WorldTimeService worldTime = Object.FindFirstObjectByType<WorldTimeService>();
            ProjectilePool pool = Object.FindFirstObjectByType<ProjectilePool>();
            CameraFollow2D follow = Camera.main.GetComponent<CameraFollow2D>();
            Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
            if (player == null || worldTime == null || pool == null || follow == null
                || square == null)
            {
                throw new MissingReferenceException("阶段四场景缺少玩家、时间服务、弹幕池、镜头或灰盒 Sprite。");
            }

            GameObject guardTemplate = GameObject.Find("Guard_A");
            GameObject bugTemplate = GameObject.Find("ClockworkBug_A");
            GameObject archerTemplate = GameObject.Find("ClockArcher_B");
            if (guardTemplate == null || bugTemplate == null || archerTemplate == null)
            {
                throw new MissingReferenceException("阶段四场景缺少三类敌人模板。");
            }

            DestroyIfPresent("Ground");
            DestroyIfPresent("EncounterGround");
            DestroyIfPresent("Environment");
            SetBoolean(worldTime, "timeStopUnlocked", false);
            SetBoolean(worldTime, "durationUpgradeUnlocked", false);

            player.transform.position = new Vector3(-7.2f, -1.15f, 0f);
            follow.SnapToTarget();
            StageFiveLevelController level = new GameObject("Stage5_LevelController")
                .AddComponent<StageFiveLevelController>();
            level.Configure(player.GetComponent<HealthSystem>(), follow, worldTime, pool);

            Transform roomContainer = new GameObject("Stage5_SevenRooms").transform;
            Transform[] rooms = new Transform[7];
            Color[] accents =
            {
                new(0.3f, 0.8f, 0.9f),
                new(0.7f, 0.35f, 0.85f),
                new(0.95f, 0.72f, 0.25f),
                new(0.28f, 0.82f, 0.72f),
                new(0.9f, 0.42f, 0.32f),
                new(0.73f, 0.47f, 0.95f),
                new(0.92f, 0.3f, 0.42f)
            };
            for (int index = 0; index < rooms.Length; index++)
            {
                rooms[index] = CreateRoom(
                    roomContainer, index + 1, index * RoomSpacing, accents[index], square);
            }

            Transform[] leftEntry = new Transform[7];
            Transform[] rightEntry = new Transform[7];
            for (int index = 0; index < rooms.Length; index++)
            {
                leftEntry[index] = CreateAnchor(rooms[index], "Entry_Left",
                    RoomX(index) - 7.2f);
                rightEntry[index] = CreateAnchor(rooms[index], "Entry_Right",
                    RoomX(index) + 7.2f);
            }

            StageFiveTimeGate gate = CreateGate(
                rooms[0], new Vector3(-3f, -1.1f, 0f),
                worldTime, player, level, square);

            CreateDoor(rooms[0], "Door_R1_R2", RoomX(0) + 11.15f,
                leftEntry[1], "R2", "房间 2 · 高速敌人", level, square);
            CreateDoor(rooms[1], "Door_R2_R1", RoomX(1) - 11.15f,
                rightEntry[0], "R1", "房间 1", level, square);
            CreateDoor(rooms[1], "Door_R2_R3", RoomX(1) + 11.15f,
                leftEntry[2], "R3", "房间 3 · 时停能力", level, square);
            CreateDoor(rooms[2], "Door_R3_R2", RoomX(2) - 11.15f,
                rightEntry[1], "R2", "房间 2", level, square);
            CreateDoor(rooms[2], "Door_R3_Return", RoomX(2) + 11.15f,
                leftEntry[0], "R1", "旧区域 · 房间 1", level, square,
                requireUnlock: true);
            CreateDoor(rooms[0], "Door_R1_R4_Gated", RoomX(0) - 11.15f,
                leftEntry[3], "R4", "房间 4 · 新路线", level, square, gate);
            CreateDoor(rooms[3], "Door_R4_R1", RoomX(3) - 11.15f,
                leftEntry[0], "R1", "房间 1", level, square);
            CreateDoor(rooms[3], "Door_R4_R5", RoomX(3) + 11.15f,
                leftEntry[4], "R5", "房间 5 · 钟弩手", level, square);
            CreateDoor(rooms[4], "Door_R5_R4", RoomX(4) - 11.15f,
                rightEntry[3], "R4", "房间 4", level, square);
            CreateDoor(rooms[4], "Door_R5_R7", RoomX(4) + 11.15f,
                leftEntry[6], "R7", "房间 7 · Boss 入口", level, square);
            CreateDoor(rooms[6], "Door_R7_R5", RoomX(6) - 11.15f,
                rightEntry[4], "R5", "房间 5", level, square);

            Transform hiddenReturn = CreateAnchor(rooms[4], "Entry_HiddenReturn",
                RoomX(4) - 3.8f);
            CreateDoor(rooms[4], "Door_R5_Hidden_R6", RoomX(4) - 1.5f,
                leftEntry[5], "R6", "隐藏房间 6", level, square,
                hidden: true, y: 1.15f);
            CreateDoor(rooms[5], "Door_R6_R5", RoomX(5) - 11.15f,
                hiddenReturn, "R5", "房间 5", level, square);

            CreatePickup(rooms[2], "Pickup_TimeStop",
                RoomX(2) + 3f, StageFivePickupKind.TimeStop,
                worldTime, player.GetComponent<TimeGauge>(), level, square);
            CreatePickup(rooms[5], "Pickup_SevenSecondUpgrade",
                RoomX(5) + 3f, StageFivePickupKind.DurationUpgrade,
                worldTime, player.GetComponent<TimeGauge>(), level, square);

            CreateCheckpoint(rooms[0], "CP_R1_Start", "R1",
                RoomX(0) - 7.2f, new Vector3(-7.2f, -1.15f, 0f), level, square);
            CreateCheckpoint(rooms[3], "CP_R4_Return", "R4",
                RoomX(3) - 5.5f, new Vector3(RoomX(3) - 5.5f, -1.15f, 0f),
                level, square);
            CreateCheckpoint(rooms[6], "CP_R7_Boss", "R7",
                RoomX(6) - 5.5f, new Vector3(RoomX(6) - 5.5f, -1.15f, 0f),
                level, square);

            CreateEnemy(guardTemplate, rooms[0], "R1_Guard",
                new Vector3(RoomX(0) + 4f, -1.25f, 0f));
            CreateEnemy(bugTemplate, rooms[1], "R2_Bug_A",
                new Vector3(RoomX(1) - 1f, -1.55f, 0f));
            CreateEnemy(bugTemplate, rooms[1], "R2_Bug_B",
                new Vector3(RoomX(1) + 4f, -1.55f, 0f));
            CreateEnemy(guardTemplate, rooms[3], "R4_Guard",
                new Vector3(RoomX(3) + 2f, -1.25f, 0f));
            CreateEnemy(bugTemplate, rooms[3], "R4_Bug",
                new Vector3(RoomX(3) + 6f, -1.55f, 0f));
            CreateEnemy(archerTemplate, rooms[4], "R5_ClockArcher",
                new Vector3(RoomX(4) + 3.5f, -1.25f, 0f));
            CreateEnemy(guardTemplate, rooms[4], "R5_Guard",
                new Vector3(RoomX(4) + 6.5f, -1.25f, 0f));

            DestroyIfPresent("Encounter_1_Guard_Bug");
            DestroyIfPresent("Encounter_2_Bug_Archer_Guard");

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AddSceneToBuildSettings(LevelScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"阶段 5 七房间灰盒已生成：{LevelScenePath}");
        }

        private static float RoomX(int index) => index * RoomSpacing;

        private static Transform CreateRoom(
            Transform parent, int number, float centerX, Color accent, Sprite square)
        {
            Transform room = new GameObject($"Room_{number:00}").transform;
            room.SetParent(parent);
            room.position = new Vector3(centerX, 0f, 0f);
            string areaId = number switch
            {
                1 or 2 or 4 => "A",
                3 or 5 or 6 => "B",
                _ => "C"
            };
            room.gameObject.AddComponent<StageFiveRoom>()
                .Configure($"R{number}", areaId);

            CreateBox(room, "Floor", new Vector3(centerX, -2.5f, 0f),
                new Vector3(22f, 1f, 1f), new Color(0.16f, 0.25f, 0.29f),
                square, true, true);
            CreateSideWall(room, "LeftWall", centerX - 11.3f, true, square);
            CreateSideWall(room, "RightWall", centerX + 11.3f,
                number <= 5, square);
            CreateBox(room, "RoomAccent", new Vector3(centerX, 2.7f, 0f),
                new Vector3(20f, 0.16f, 1f), accent, square, false);

            if (number is 1 or 2 or 4)
            {
                CreateBox(room, "JumpPlatform", new Vector3(centerX, -0.1f, 0f),
                    new Vector3(2.8f, 0.35f, 1f), accent, square, true, true);
            }
            if (number == 5)
            {
                CreateBox(room, "HiddenDoorPlatform",
                    new Vector3(centerX - 1.5f, -0.15f, 0f),
                    new Vector3(2.6f, 0.3f, 1f), accent, square, true, true);
            }
            if (number == 7)
            {
                CreateBox(room, "BossArenaMarker",
                    new Vector3(centerX + 3.5f, -1.2f, 0f),
                    new Vector3(2f, 1.6f, 1f), accent, square, false);
            }

            return room;
        }

        private static GameObject CreateBox(
            Transform parent, string name, Vector3 position, Vector3 size,
            Color color, Sprite sprite, bool solid, bool groundLayer = false)
        {
            GameObject box = new(name);
            box.transform.SetParent(parent);
            box.transform.position = position;
            box.transform.localScale = size;
            if (groundLayer)
            {
                box.layer = LayerMask.NameToLayer("Ground");
            }
            SpriteRenderer renderer = box.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            if (solid)
            {
                box.AddComponent<BoxCollider2D>();
            }
            return box;
        }

        private static void CreateSideWall(
            Transform room, string name, float x, bool hasEntrance, Sprite square)
        {
            Color wallColor = new(0.2f, 0.27f, 0.33f);
            if (!hasEntrance)
            {
                CreateBox(room, name, new Vector3(x, 0f, 0f),
                    new Vector3(0.7f, 6f, 1f), wallColor, square, true);
                return;
            }

            // 地面上方保留足够主角行走和跳跃穿过的真实缺口。
            CreateBox(room, $"{name}_Upper", new Vector3(x, 2f, 0f),
                new Vector3(0.7f, 2.6f, 1f), wallColor, square, true);
        }

        private static Transform CreateAnchor(Transform room, string name, float x)
        {
            Transform anchor = new GameObject(name).transform;
            anchor.SetParent(room);
            anchor.position = new Vector3(x, -1.15f, 0f);
            return anchor;
        }

        private static StageFiveDoor CreateDoor(
            Transform room, string name, float x, Transform destination,
            string roomId, string label, StageFiveLevelController level, Sprite square,
            StageFiveTimeGate gate = null, bool requireUnlock = false,
            bool hidden = false, float y = -0.65f)
        {
            GameObject door = CreateBox(room, name, new Vector3(x, y, 0f),
                new Vector3(1.1f, 2.7f, 1f),
                hidden ? new Color(0.19f, 0.24f, 0.31f)
                    : gate != null ? new Color(0.86f, 0.5f, 0.22f)
                    : new Color(0.28f, 0.68f, 0.8f),
                square, false);
            door.GetComponent<SpriteRenderer>().enabled = hidden;
            BoxCollider2D trigger = door.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            if (gate != null || requireUnlock)
            {
                GameObject blocker = CreateBox(door.transform, "LockedBlocker",
                    door.transform.position, new Vector3(0.35f, 2.7f, 1f),
                    new Color(0.86f, 0.5f, 0.22f), square, true);
                blocker.transform.SetParent(door.transform, true);
            }
            StageFiveDoor component = door.AddComponent<StageFiveDoor>();
            component.Configure(destination, roomId, label, level, gate, requireUnlock);
            string sourceRoomId = room.GetComponent<StageFiveRoom>()?.RoomId;
            if (sourceRoomId == "R7" || roomId == "R7")
            {
                CreateBossBattleBlocker(door, component, square);
            }
            return component;
        }

        private static void CreateBossBattleBlocker(
            GameObject door, StageFiveDoor component, Sprite square)
        {
            GameObject blocker = new("BossBattleBlocker");
            blocker.transform.SetParent(door.transform, false);
            blocker.transform.localPosition = Vector3.zero;
            float scaleX = Mathf.Max(0.001f, Mathf.Abs(door.transform.lossyScale.x));
            float scaleY = Mathf.Max(0.001f, Mathf.Abs(door.transform.lossyScale.y));
            blocker.transform.localScale = new Vector3(1f / scaleX, 1f / scaleY, 1f);

            BoxCollider2D collider = blocker.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.42f, 2.7f);
            collider.enabled = false;
            SpriteRenderer renderer = blocker.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = collider.size;
            renderer.color = new Color(0.95f, 0.3f, 0.18f);
            renderer.sortingOrder = 15;
            renderer.enabled = false;
            component.ConfigureBossBattleBlocker(collider, renderer);
        }

        private static StageFiveTimeGate CreateGate(
            Transform room, Vector3 position, WorldTimeService worldTime,
            GameObject player, StageFiveLevelController level, Sprite square)
        {
            GameObject marker = CreateBox(room, "TimeStopGate_R1_R4", position,
                new Vector3(1.1f, 1.8f, 1f),
                new Color(0.9f, 0.42f, 0.2f), square, false);
            StageFiveTimeGate gate = marker.AddComponent<StageFiveTimeGate>();
            gate.Configure(worldTime, player.GetComponent<TimeGauge>(),
                player.transform, level);
            return gate;
        }

        private static void CreatePickup(
            Transform room, string name, float x, StageFivePickupKind kind,
            WorldTimeService worldTime, TimeGauge gauge,
            StageFiveLevelController level, Sprite square)
        {
            GameObject pickup = CreateBox(room, name, new Vector3(x, -1.05f, 0f),
                new Vector3(0.95f, 1.15f, 1f),
                kind == StageFivePickupKind.TimeStop
                    ? new Color(0.95f, 0.72f, 0.25f)
                    : new Color(0.7f, 0.4f, 0.95f), square, false);
            BoxCollider2D trigger = pickup.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            pickup.AddComponent<StageFivePickup>()
                .Configure(kind, worldTime, gauge, level);
        }

        private static void CreateCheckpoint(
            Transform room, string id, string roomId, float x,
            Vector3 respawnPosition, StageFiveLevelController level, Sprite square)
        {
            GameObject marker = CreateBox(room, id,
                new Vector3(x, -1.94f, 0f), new Vector3(1.2f, 0.18f, 1f),
                new Color(0.2f, 0.65f, 0.6f), square, false);
            CreateBox(marker.transform, "BenchSeat",
                new Vector3(x, -1.35f, 0f), new Vector3(1.2f, 0.15f, 1f),
                new Color(0.4f, 0.8f, 0.72f), square, false);
            CreateBox(marker.transform, "BenchBack",
                new Vector3(x + 0.45f, -0.9f, 0f), new Vector3(0.16f, 1f, 1f),
                new Color(0.4f, 0.8f, 0.72f), square, false);
            BoxCollider2D trigger = marker.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            Transform anchor = new GameObject("RespawnAnchor").transform;
            anchor.SetParent(marker.transform);
            anchor.position = respawnPosition;
            marker.AddComponent<StageFiveCheckpoint>()
                .Configure(id, roomId, anchor, level);
        }

        private static void CreateEnemy(
            GameObject template, Transform room, string name, Vector3 position)
        {
            GameObject enemy = Object.Instantiate(template, room);
            enemy.name = name;
            enemy.transform.position = position;
        }

        private static void SetBoolean(Object target, string name, bool value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(name).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void DestroyIfPresent(string name)
        {
            GameObject target = GameObject.Find(name);
            if (target != null)
            {
                Object.DestroyImmediate(target);
            }
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
