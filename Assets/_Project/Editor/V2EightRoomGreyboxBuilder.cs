#if UNITY_EDITOR
using System;
using System.Linq;
using Object = UnityEngine.Object;
using TimeMetroidvania.Boss;
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
    /// 根据已归档的 R1～R8 草图生成 V2 独立灰盒场景。
    /// </summary>
    internal static class V2EightRoomGreyboxBuilder
    {
        private const string SourceScenePath = "Assets/_Project/Scenes/V2/V2RuntimeTemplate.unity";
        private const string ScenePath = "Assets/_Project/Scenes/V2/EightRoomGreybox.unity";
        private const string SquareSpritePath = "Assets/_Project/Physics/PrototypeSquare.asset";
        private static readonly Color Wall = new(0.12f, 0.17f, 0.22f);
        private static readonly Color Platform = new(0.22f, 0.31f, 0.36f);
        private static readonly Color Accent = new(0.18f, 0.72f, 0.82f);
        private static readonly Color Danger = new(0.92f, 0.24f, 0.2f);

        [MenuItem("Tools/时序旅人/V2/Build Eight Room Greybox")]
        public static void Build() => BuildScene(true);

        public static void BuildFromCommandLine() => BuildScene(false);

        private static void BuildScene(bool askToSave)
        {
            if (askToSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene source = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
            EnsureDirectory("Assets/_Project/Scenes/V2");
            EditorSceneManager.SaveScene(source, ScenePath, true);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject player = GameObject.Find("Player");
            WorldTimeService worldTime = Object.FindFirstObjectByType<WorldTimeService>();
            ProjectilePool projectilePool = Object.FindFirstObjectByType<ProjectilePool>();
            Camera mainCamera = Camera.main;
            CameraFollow2D cameraFollow = mainCamera != null ? mainCamera.GetComponent<CameraFollow2D>() : null;
            Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
            GameObject guardTemplate = GameObject.Find("Guard_A");
            GameObject bugTemplate = GameObject.Find("ClockworkBug_A");
            GameObject archerTemplate = GameObject.Find("ClockArcher_B");
            if (player == null || worldTime == null || projectilePool == null || cameraFollow == null
                || square == null || guardTemplate == null || bugTemplate == null || archerTemplate == null)
            {
                throw new MissingReferenceException("V2 灰盒缺少玩家、时间服务、弹幕池、镜头、Sprite 或敌人模板。");
            }

            GameObject guardSeed = CloneSeed(guardTemplate, "V2_GuardSeed");
            GameObject bugSeed = CloneSeed(bugTemplate, "V2_BugSeed");
            GameObject archerSeed = CloneSeed(archerTemplate, "V2_ArcherSeed");
            CleanupSourceGeometry();
            SetBoolean(worldTime, "timeStopUnlocked", false);
            TimeGauge gauge = player.GetComponent<TimeGauge>();
            gauge.Clear();
            mainCamera.orthographicSize = 6f;
            mainCamera.backgroundColor = new Color(0.025f, 0.045f, 0.065f);

            StageFiveLevelController level = new GameObject("V2_LevelController").AddComponent<StageFiveLevelController>();
            level.Configure(player.GetComponent<HealthSystem>(), cameraFollow, worldTime, projectilePool);
            level.ConfigureLayout(8, "R8", true);

            Transform root = new GameObject("V2_EightRoomGreybox").transform;
            Transform r1 = CreateRoom(root, "R1", "A", new Vector2(-82f, -24f), new Vector2(32f, 18f), square);
            Transform r2 = CreateRoom(root, "R2", "A", new Vector2(-42f, -20f), new Vector2(34f, 26f), square);
            Transform r3 = CreateRoom(root, "R3", "B", new Vector2(-18f, 18f), new Vector2(46f, 22f), square);
            Transform r4 = CreateRoom(root, "R4", "A", new Vector2(4f, -22f), new Vector2(38f, 28f), square);
            Transform r5 = CreateRoom(root, "R5", "B", new Vector2(44f, -22f), new Vector2(24f, 16f), square);
            Transform r6 = CreateRoom(root, "R6", "C", new Vector2(42f, 18f), new Vector2(36f, 28f), square);
            Transform r7 = CreateRoom(root, "R7", "C", new Vector2(42f, 54f), new Vector2(20f, 16f), square);
            Transform r8 = CreateRoom(root, "R8", "C", new Vector2(84f, 18f), new Vector2(32f, 20f), square);

            BuildR1(r1, square); BuildR2(r2, square); BuildR3(r3, worldTime, square);
            BuildR4(r4, worldTime, level, square); BuildR5(r5, worldTime, gauge, level, square);
            BuildR6(r6, square); BuildR7(r7, level, square); BuildR8(r8, square);
            ConnectRooms(level, square, r1, r2, r3, r4, r5, r6, r7, r8);
            PopulateEnemies(worldTime, square, guardSeed, bugSeed, archerSeed, r2, r3, r4, r6);
            CreateBoss(r8, player.transform, worldTime, level, square);

            Vector3 playerSpawn = World(r1, -11.5f, -6.55f);
            player.transform.position = playerSpawn;
            player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            player.GetComponent<HealthSystem>().SetRespawnPoint(playerSpawn);
            CreateClockMarker(r1, "CP_R1_Start", -11.5f, -7.05f, 0f, square);
            Object.DestroyImmediate(guardSeed); Object.DestroyImmediate(bugSeed); Object.DestroyImmediate(archerSeed);
            level.EnterRoom("R1");
            cameraFollow.SnapToTarget();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"V2 八房间灰盒已生成：{ScenePath}");
        }

        private static Transform CreateRoom(Transform parent, string id, string area, Vector2 center, Vector2 size, Sprite square)
        {
            Transform room = new GameObject($"Room_{id}").transform;
            room.SetParent(parent); room.position = center;
            room.gameObject.AddComponent<StageFiveRoom>().Configure(id, area);
            room.gameObject.AddComponent<RoomCameraBounds2D>().Configure(id, center, size);
            GameObject backdrop = Box(room, "GreyboxBackdrop", 0f, 0f, size.x, size.y,
                new Color(0.055f, 0.075f, 0.1f), square, false);
            backdrop.GetComponent<SpriteRenderer>().sortingOrder = -20;
            return room;
        }

        private static void BuildR1(Transform r, Sprite s)
        {
            Box(r,"Ceiling",0,8.5f,32,1,Wall,s,true); Box(r,"LeftWall",-15.5f,0,1,18,Wall,s,true);
            Box(r,"RightWallUpper",15.5f,6.5f,1,4,Wall,s,true); Box(r,"FloorLow",-10.5f,-8.5f,11,1,Wall,s,true);
            Box(r,"FloorMid",-2,-6.5f,6,5,Wall,s,true); Box(r,"FloorHigh",8.5f,-3,15,8,Wall,s,true);
            Box(r,"StepA",-5.7f,-6.5f,2.2f,1,Platform,s,true); Box(r,"StepB",-3.5f,-5.2f,2.2f,1,Platform,s,true);
        }

        private static void BuildR2(Transform r, Sprite s)
        {
            Box(r,"Floor",0,-12.5f,34,1,Wall,s,true); Box(r,"CeilingLeft",-9.5f,12.5f,15,1,Wall,s,true);
            Box(r,"CeilingRight",9.5f,12.5f,15,1,Wall,s,true); Box(r,"LeftWallLow",-16.5f,-7.5f,1,10,Wall,s,true);
            Box(r,"LeftWallHigh",-16.5f,8.5f,1,8,Wall,s,true); Box(r,"RightWallUpper",16.5f,3.5f,1,18,Wall,s,true);
            Box(r,"LeftMass",-12.8f,-2.5f,6.5f,6,Wall,s,true); Box(r,"RightMass",13.5f,-2,5.5f,12,Wall,s,true);
            PlatformBox(r,"PlatformA",-8,-3.5f,4,s); PlatformBox(r,"PlatformB",-2,5,2.5f,s);
            PlatformBox(r,"PlatformC",4.5f,1,2.5f,s); PlatformBox(r,"PlatformD",8.5f,-5,2.5f,s);
            PlatformBox(r,"PlatformE",2.5f,-8.5f,3,s);
        }
        private static void BuildR3(Transform r, WorldTimeService time, Sprite s)
        {
            Box(r,"FloorLeftA",-21,-10.5f,4,1,Wall,s,true); Box(r,"FloorLeftB",-5.5f,-10.5f,19,1,Wall,s,true);
            Box(r,"FloorMid",9.5f,-10.5f,2,1,Wall,s,true); Box(r,"FloorPool",13.5f,-10.5f,11,1,Wall,s,true);
            Box(r,"Ceiling",0,10.5f,46,1,Wall,s,true); Box(r,"LeftWallUpper",-22.5f,3.5f,1,14,Wall,s,true);
            Box(r,"RightWallUpper",22.5f,6.5f,1,8,Wall,s,true); Box(r,"CenterMass",1,-5.5f,7,10,Wall,s,true);
            Box(r,"PoolRightWall",19,-5.5f,1,10,Wall,s,true); Box(r,"PoolLeftStep",8.7f,-8.5f,2.2f,3,Wall,s,true);
            PlatformBox(r,"PlatformA",-13,-4,2.2f,s); PlatformBox(r,"PlatformB",-9,-2.2f,2.4f,s);
            PlatformBox(r,"PlatformC",-4.5f,0,2.8f,s); PlatformBox(r,"PlatformD",7.5f,-1.2f,2.4f,s);
            PlatformBox(r,"PoolExitLedge",20.7f,-0.6f,3.2f,s);
            CreateWater(r,13.5f,-5.3f,11,9.4f,time,s);
        }

        private static void BuildR4(Transform r, WorldTimeService time, StageFiveLevelController level, Sprite s)
        {
            Box(r,"Floor",0,-13.5f,38,1,Wall,s,true); Box(r,"CeilingLeft",-10.5f,13.5f,17,1,Wall,s,true);
            Box(r,"CeilingRight",10.5f,13.5f,17,1,Wall,s,true); Box(r,"LeftWallUpper",-18.5f,3.5f,1,20,Wall,s,true);
            Box(r,"RightWallUpper",18.5f,3.5f,1,20,Wall,s,true); PlatformBox(r,"PlatformA",-12,6,7,s);
            PlatformBox(r,"PlatformB",-2,7.5f,6,s); PlatformBox(r,"PlatformC",10.5f,10,7,s);
            PlatformBox(r,"PlatformD",-6,1.5f,6,s); PlatformBox(r,"PlatformE",5,-1.5f,4,s);
            PlatformBox(r,"PlatformF",-6,-7,4,s); CreateShortcutGate(r,-13.8f,-10.5f,level,s);
            CreateRotor(r,13.5f,-9.7f,time,s);
        }

        private static void BuildR5(Transform r, WorldTimeService time, TimeGauge gauge, StageFiveLevelController level, Sprite s)
        {
            Box(r,"Floor",0,-7.5f,24,1,Wall,s,true); Box(r,"Ceiling",0,7.5f,24,1,Wall,s,true);
            Box(r,"RightWall",11.5f,0,1,16,Wall,s,true); Box(r,"LeftWallUpper",-11.5f,3.5f,1,8,Wall,s,true);
            Box(r,"UpperMass",-8,4,7,7,Wall,s,true); CreatePickup(r,4.2f,-5.9f,time,gauge,level,s);
        }

        private static void BuildR6(Transform r, Sprite s)
        {
            Box(r,"FloorLeft",-11,-13.5f,14,1,Wall,s,true); Box(r,"FloorRight",9.5f,-13.5f,17,1,Wall,s,true);
            Box(r,"CeilingLeft",-8,13.5f,20,1,Wall,s,true); Box(r,"CeilingRight",14,13.5f,8,1,Wall,s,true);
            Box(r,"LeftWallUpper",-17.5f,3.5f,1,20,Wall,s,true); Box(r,"RightWallMid",17.5f,1,1,15,Wall,s,true);
            Box(r,"CenterMass",2,-9.5f,7,8,Wall,s,true); PlatformBox(r,"PlatformA",-12,-6,3,s);
            PlatformBox(r,"PlatformB",-7,5.5f,7,s); PlatformBox(r,"PlatformC",-4,-1,4,s);
            PlatformBox(r,"PlatformD",5,2,5,s); PlatformBox(r,"PlatformE",12,8,6,s);
        }

        private static void BuildR7(Transform r, StageFiveLevelController level, Sprite s)
        {
            Box(r,"FloorLeft",-4,-7.5f,12,1,Wall,s,true); Box(r,"Ceiling",0,7.5f,20,1,Wall,s,true);
            Box(r,"LeftWall",-9.5f,0,1,16,Wall,s,true); Box(r,"RightWallUpper",9.5f,3,1,9,Wall,s,true);
            CreateCheckpoint(r,"CP_R7_Boss","R7",-2,-6.55f,level,s);
        }

        private static void BuildR8(Transform r, Sprite s)
        {
            Box(r,"Floor",0,-9.5f,32,1,Wall,s,true); Box(r,"Ceiling",0,9.5f,32,1,Wall,s,true);
            Box(r,"LeftWallUpper",-15.5f,3,1,12,Wall,s,true); Box(r,"RightWall",15.5f,0,1,20,Wall,s,true);
            Box(r,"BossArenaLine",2,-8.85f,18,0.12f,new Color(0.7f,0.15f,0.25f),s,false);
        }

        private static void ConnectRooms(StageFiveLevelController level, Sprite s,
            Transform r1, Transform r2, Transform r3, Transform r4,
            Transform r5, Transform r6, Transform r7, Transform r8)
        {
            Transform r1Exit=Anchor(r1,"Entry_R2",12.5f,2);
            Transform r2FromR1=Anchor(r2,"Entry_R1",-13.8f,2.2f);
            PairHorizontal(r1,"R1_R2",15.2f,2,r2FromR1,"R2","房间 2",level,s);
            PairHorizontal(r2,"R2_R1",-16.2f,2.2f,r1Exit,"R1","房间 1",level,s);

            Transform r2Top=Anchor(r2,"Entry_R3",0,9.5f);
            Transform r3BottomLeft=Anchor(r3,"Entry_R2",-17,-7.6f);
            Door(r2,"R2_R3",0,12.2f,3,1.2f,r3BottomLeft,"R3","房间 3",level,s,new Vector2(3.2f,8),true);
            Door(r3,"R3_R2",-17,-10.2f,3,1.2f,r2Top,"R2","房间 2",level,s,new Vector2(0,-2),false);

            Transform r2Right=Anchor(r2,"Entry_R4",13.5f,-9.6f);
            Transform r4Left=Anchor(r4,"Entry_R2",-16.4f,-10.7f);
            PairHorizontal(r2,"R2_R4",16.2f,-9.6f,r4Left,"R4","房间 4 前室",level,s);
            PairHorizontal(r4,"R4_R2",-18.2f,-10.7f,r2Right,"R2","房间 2",level,s);

            Transform r3ToR4=Anchor(r3,"Entry_R4",7,-7.5f);
            Transform r4FromR3=Anchor(r4,"Entry_R3",0,10.5f);
            Door(r3,"R3_R4",7,-10.2f,3,1.2f,r4FromR3,"R4","房间 4",level,s,new Vector2(0,-2),false);
            Door(r4,"R4_R3",0,13.2f,3,1.2f,r3ToR4,"R3","房间 3",level,s,new Vector2(3.2f,8),true);

            Transform r4Right=Anchor(r4,"Entry_R5",16,-10.6f);
            Transform r5Left=Anchor(r5,"Entry_R4",-9.5f,-5.7f);
            PairHorizontal(r4,"R4_R5",18.2f,-10.6f,r5Left,"R5","房间 5",level,s);
            PairHorizontal(r5,"R5_R4",-11.2f,-5.7f,r4Right,"R4","房间 4",level,s);

            Transform r3Right=Anchor(r3,"Entry_R6",20,0.5f);
            Transform r6Left=Anchor(r6,"Entry_R3",-15,-10.5f);
            PairHorizontal(r3,"R3_R6",22.2f,0.5f,r6Left,"R6","房间 6",level,s);
            PairHorizontal(r6,"R6_R3",-17.2f,-10.5f,r3Right,"R3","房间 3",level,s);

            Transform r6Top=Anchor(r6,"Entry_R7",9,10.5f);
            Transform r7Bottom=Anchor(r7,"Entry_R6",6.5f,-5.2f);
            Door(r6,"R6_R7",9,13.2f,3,1.2f,r7Bottom,"R7","房间 7 检查点",level,s,new Vector2(3,8),true);
            Door(r7,"R7_R6",6.5f,-7.2f,3,1.2f,r6Top,"R6","房间 6",level,s,new Vector2(0,-2),false);

            Transform r6Right=Anchor(r6,"Entry_R8",15,-10.5f);
            Transform r8Left=Anchor(r8,"Entry_R6",-13.2f,-6.8f);
            PairHorizontal(r6,"R6_R8",17.2f,-10.5f,r8Left,"R8","Boss 房间 8",level,s,true);
            PairHorizontal(r8,"R8_R6",-15.2f,-6.8f,r6Right,"R6","房间 6",level,s,true);
        }
        private static void PopulateEnemies(WorldTimeService time, Sprite s,
            GameObject guard, GameObject bug, GameObject archer,
            Transform r2, Transform r3, Transform r4, Transform r6)
        {
            Enemy(guard,r2,"R2_Ground_A",-10,-4); Enemy(bug,r2,"R2_Ground_B",-7,-11.4f);
            Enemy(guard,r2,"R2_Ground_C",4,-11.4f); Enemy(bug,r2,"R2_Ground_D",13,4.5f);
            Enemy(bug,r3,"R3_Ground_A",-15,-9.4f); Enemy(guard,r3,"R3_Ground_B",-8,-9.2f);
            Enemy(guard,r3,"R3_Ground_C",1,0); Enemy(bug,r3,"R3_Ground_D",7,-9.4f);
            FlyingEnemy(r3,"R3_Flying_A",-12,2,time,s); FlyingEnemy(r3,"R3_Flying_B",-5,6,time,s);
            FlyingEnemy(r3,"R3_Flying_C",5,4,time,s);
            Enemy(guard,r4,"R4_Ground_A",-14,6.8f); Enemy(bug,r4,"R4_Ground_B",-12,-12.4f);
            Enemy(guard,r4,"R4_Ground_C",11,-12.4f); FlyingEnemy(r4,"R4_Flying_A",-12,-3,time,s);
            FlyingEnemy(r4,"R4_Flying_B",8,3,time,s); Enemy(archer,r4,"R4_Archer",-2,-12.2f);
            Enemy(guard,r6,"R6_Ground_A",-8,6.3f); Enemy(bug,r6,"R6_Ground_B",9,-12.4f);
            FlyingEnemy(r6,"R6_Flying_A",-13,-2,time,s); FlyingEnemy(r6,"R6_Flying_B",-4,3,time,s);
            Enemy(archer,r6,"R6_Archer",-12,-12.2f);
        }

        private static void CreateBoss(Transform room, Transform player, WorldTimeService time,
            StageFiveLevelController level, Sprite s)
        {
            GameObject poolObject=new("V2_BossProjectilePool"); poolObject.transform.SetParent(room);
            BossProjectilePool pool=poolObject.AddComponent<BossProjectilePool>();
            SetReference(pool,"worldTime",time); SetReference(pool,"projectileSprite",s); SetInteger(pool,"capacity",24);
            Vector2 arenaCenter=World(room,2,-7.8f);
            GameObject boss=new("Boss_TimeWarden_R8"); boss.transform.SetParent(room);
            boss.transform.position=World(room,5,-7.5f); boss.transform.localScale=new Vector3(1.7f,1.7f,1);
            boss.transform.rotation=Quaternion.Euler(0,0,45);
            SpriteRenderer renderer=boss.AddComponent<SpriteRenderer>(); renderer.sprite=s; renderer.color=Danger; renderer.sortingOrder=8;
            BoxCollider2D collider=boss.AddComponent<BoxCollider2D>(); collider.isTrigger=true;
            HealthSystem health=boss.AddComponent<HealthSystem>(); SetInteger(health,"maxHealth",28);
            SetFloat(health,"damageProtectionDuration",0.1f);
            LocalTimeController local=boss.AddComponent<LocalTimeController>(); local.SetWorldTime(time);
            TimeAwareDamageReceiver receiver=boss.AddComponent<TimeAwareDamageReceiver>();
            SetReference(receiver,"worldTime",time); SetFloat(receiver,"bossPendingDamageFraction",0.25f);
            boss.AddComponent<ContactDamage2D>();
            BossBrain brain=boss.AddComponent<BossBrain>(); brain.Configure(player,pool,level,arenaCenter);
        }

        private static void CreateWater(Transform room, float x, float y, float width, float height,
            WorldTimeService time, Sprite s)
        {
            GameObject water=Box(room,"R3_TimeStopWater",x,y,width,height,new Color(0.1f,0.62f,0.82f,0.46f),s,false);
            water.GetComponent<SpriteRenderer>().sortingOrder=-2;
            BoxCollider2D volume=water.AddComponent<BoxCollider2D>(); volume.isTrigger=true;
            GameObject surface=Box(room,"R3_FrozenSurface",x,y+height*0.5f,width,0.35f,
                new Color(0.65f,0.95f,1),s,true);
            BoxCollider2D surfaceCollider=surface.GetComponent<BoxCollider2D>();
            SpriteRenderer surfaceRenderer=surface.GetComponent<SpriteRenderer>();
            surfaceCollider.enabled=false; surfaceRenderer.enabled=false;
            water.AddComponent<V2TimeStopWater>().Configure(time,surfaceCollider,
                water.GetComponent<SpriteRenderer>(),surfaceRenderer);
        }

        private static void CreateShortcutGate(Transform room, float x, float y,
            StageFiveLevelController level, Sprite s)
        {
            GameObject interaction=new("R4_ShortcutGate"); interaction.transform.SetParent(room);
            interaction.transform.localPosition=new Vector3(x,y,0);
            BoxCollider2D trigger=interaction.AddComponent<BoxCollider2D>(); trigger.isTrigger=true;
            trigger.size=new Vector2(4,5.5f);
            GameObject barrier=Box(interaction.transform,"SolidGate",0,0,0.55f,5.5f,
                new Color(0.95f,0.72f,0.15f),s,true);
            interaction.AddComponent<V2ShortcutGate>().Configure(barrier.GetComponent<BoxCollider2D>(),
                barrier.GetComponent<SpriteRenderer>(),level,true);
        }

        private static void CreateRotor(Transform room, float x, float y, WorldTimeService time, Sprite s)
        {
            GameObject rotor=Box(room,"R4_TimeRotor",x,y,0.45f,7,new Color(0.2f,0.9f,0.45f),s,true);
            Rigidbody2D body=rotor.AddComponent<Rigidbody2D>(); body.bodyType=RigidbodyType2D.Kinematic; body.gravityScale=0;
            rotor.AddComponent<ContactDamage2D>(); rotor.AddComponent<V2TimeRotor>().Configure(time,540);
            GameObject hub=Box(room,"R4_TimeRotorHub",x,y,0.8f,0.8f,new Color(0.7f,1,0.8f),s,false);
            hub.GetComponent<SpriteRenderer>().sortingOrder=4;
        }

        private static void CreatePickup(Transform room, float x, float y, WorldTimeService time,
            TimeGauge gauge, StageFiveLevelController level, Sprite s)
        {
            GameObject pickup=Box(room,"Pickup_TimeStop_R5",x,y,1.2f,1.2f,new Color(0.2f,0.65f,1),s,false);
            BoxCollider2D trigger=pickup.AddComponent<BoxCollider2D>(); trigger.isTrigger=true;
            pickup.AddComponent<StageFivePickup>().Configure(StageFivePickupKind.TimeStop,time,gauge,level);
        }

        private static void CreateCheckpoint(Transform room, string id, string roomId, float x, float y,
            StageFiveLevelController level, Sprite s)
        {
            GameObject marker=CreateClockMarker(room,id,x,y,-30,s);
            BoxCollider2D trigger=marker.AddComponent<BoxCollider2D>(); trigger.isTrigger=true; trigger.size=new Vector2(2.6f,2.6f);
            Transform anchor=Anchor(room,"RespawnAnchor_R7",x,y+0.5f);
            marker.AddComponent<StageFiveCheckpoint>().Configure(id,roomId,anchor,level);
        }

        private static GameObject CreateClockMarker(Transform room, string name, float x, float y,
            float handAngle, Sprite s)
        {
            GameObject root=new(name); root.transform.SetParent(room); root.transform.localPosition=new Vector3(x,y,0);
            Color c=new(0.05f,0.75f,0.92f);
            Box(root.transform,"ClockTop",0,0.9f,1.8f,0.18f,c,s,false);
            Box(root.transform,"ClockBottom",0,-0.9f,1.8f,0.18f,c,s,false);
            Box(root.transform,"ClockLeft",-0.9f,0,0.18f,1.8f,c,s,false);
            Box(root.transform,"ClockRight",0.9f,0,0.18f,1.8f,c,s,false);
            GameObject hand=Box(root.transform,"ClockHand",0,0.35f,0.18f,0.8f,c,s,false);
            hand.transform.localRotation=Quaternion.Euler(0,0,handAngle);
            return root;
        }

        private static void Enemy(GameObject template, Transform room, string name, float x, float y)
        {
            GameObject enemy=Object.Instantiate(template,room); enemy.name=name; enemy.SetActive(true);
            EnemyBrain brain=enemy.GetComponent<EnemyBrain>(); brain.SetEncounterSpawn(World(room,x,y)); brain.ResetEncounter();
        }

        private static void FlyingEnemy(Transform room, string name, float x, float y,
            WorldTimeService time, Sprite s)
        {
            GameObject enemy=new(name); enemy.transform.SetParent(room); enemy.transform.position=World(room,x,y);
            SpriteRenderer renderer=enemy.AddComponent<SpriteRenderer>(); renderer.sprite=s;
            renderer.color=new Color(0.2f,0.5f,1); renderer.sortingOrder=6;
            enemy.transform.localScale=new Vector3(0.9f,0.9f,1);
            BoxCollider2D collider=enemy.AddComponent<BoxCollider2D>(); collider.isTrigger=true;
            HealthSystem health=enemy.AddComponent<HealthSystem>(); SetInteger(health,"maxHealth",2);
            SetFloat(health,"damageProtectionDuration",0.12f);
            LocalTimeController local=enemy.AddComponent<LocalTimeController>(); local.SetWorldTime(time);
            TimeAwareDamageReceiver receiver=enemy.AddComponent<TimeAwareDamageReceiver>(); SetReference(receiver,"worldTime",time);
            enemy.AddComponent<V2FlyingEnemy>();
        }
        private static StageFiveDoor PairHorizontal(Transform room, string name, float x, float y,
            Transform destination, string destinationRoom, string label,
            StageFiveLevelController level, Sprite s, bool bossDoor=false)
        {
            return Door(room,name,x,y,1.2f,3.2f,destination,destinationRoom,label,
                level,s,Vector2.zero,false,bossDoor);
        }

        private static StageFiveDoor Door(Transform room, string name, float x, float y,
            float width, float height, Transform destination, string destinationRoom, string label,
            StageFiveLevelController level, Sprite s, Vector2 exitVelocity,
            bool preserveDirection, bool bossDoor=false)
        {
            GameObject door=Box(room,$"Door_{name}",x,y,width,height,Accent,s,false);
            door.GetComponent<SpriteRenderer>().enabled=false;
            BoxCollider2D trigger=door.AddComponent<BoxCollider2D>(); trigger.isTrigger=true;
            StageFiveDoor component=door.AddComponent<StageFiveDoor>();
            component.Configure(destination,destinationRoom,label,level);
            component.ConfigureExitMotion(exitVelocity,preserveDirection);
            if (bossDoor)
            {
                GameObject blocker=Box(room,$"BossBattleBlocker_{name}",x,y,0.55f,height+0.4f,Danger,s,true);
                BoxCollider2D collider=blocker.GetComponent<BoxCollider2D>();
                SpriteRenderer renderer=blocker.GetComponent<SpriteRenderer>();
                collider.enabled=false; renderer.enabled=false; renderer.sortingOrder=15;
                component.ConfigureBossBattleBlocker(collider,renderer);
            }
            return component;
        }

        private static Transform Anchor(Transform room, string name, float x, float y)
        {
            Transform anchor=new GameObject(name).transform; anchor.SetParent(room);
            anchor.localPosition=new Vector3(x,y,0); return anchor;
        }

        private static GameObject PlatformBox(Transform room, string name, float x, float y,
            float width, Sprite s) => Box(room,name,x,y,width,0.35f,Platform,s,true);

        private static GameObject Box(Transform parent, string name, float x, float y,
            float width, float height, Color color, Sprite s, bool solid)
        {
            GameObject box=new(name); box.transform.SetParent(parent);
            box.transform.localPosition=new Vector3(x,y,0); box.transform.localScale=new Vector3(width,height,1);
            SpriteRenderer renderer=box.AddComponent<SpriteRenderer>(); renderer.sprite=s; renderer.color=color;
            if (solid)
            {
                box.layer=LayerMask.NameToLayer("Ground"); box.AddComponent<BoxCollider2D>();
            }
            return box;
        }

        private static Vector3 World(Transform room, float x, float y)
            => room.TransformPoint(new Vector3(x,y,0));

        private static GameObject CloneSeed(GameObject source, string name)
        {
            GameObject seed=Object.Instantiate(source); seed.name=name;
            seed.transform.SetParent(null); seed.SetActive(false); return seed;
        }

        private static void CleanupSourceGeometry()
        {
            string[] names={"Ground","EncounterGround","Environment","Encounter_1_Guard_Bug",
                "Encounter_2_Bug_Archer_Guard","Guard_A","Guard_B","ClockworkBug_A",
                "ClockworkBug_B","ClockArcher_B"};
            foreach (string name in names) DestroyIfPresent(name);
        }

        private static void DestroyIfPresent(string name)
        {
            GameObject target;
            while ((target=GameObject.Find(name))!=null) Object.DestroyImmediate(target);
        }

        private static void EnsureDirectory(string path)
        {
            string[] pieces=path.Split('/'); string current=pieces[0];
            for (int i=1;i<pieces.Length;i++)
            {
                string next=$"{current}/{pieces[i]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current,pieces[i]);
                current=next;
            }
        }

        private static void AddSceneToBuildSettings(string path)
        {
            if (EditorBuildSettings.scenes.Any(scene=>scene.path==path)) return;
            EditorBuildSettings.scenes=EditorBuildSettings.scenes
                .Append(new EditorBuildSettingsScene(path,true)).ToArray();
        }


        private static void SetReference(Object target, string property, Object value)
        {
            SerializedObject serialized=new(target);
            SerializedProperty field=serialized.FindProperty(property)
                ?? throw new MissingFieldException(target.GetType().Name,property);
            field.objectReferenceValue=value; serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBoolean(Object target, string property, bool value)
        {
            SerializedObject serialized=new(target);
            SerializedProperty field=serialized.FindProperty(property)
                ?? throw new MissingFieldException(target.GetType().Name,property);
            field.boolValue=value; serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string property, float value)
        {
            SerializedObject serialized=new(target);
            SerializedProperty field=serialized.FindProperty(property)
                ?? throw new MissingFieldException(target.GetType().Name,property);
            field.floatValue=value; serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInteger(Object target, string property, int value)
        {
            SerializedObject serialized=new(target);
            SerializedProperty field=serialized.FindProperty(property)
                ?? throw new MissingFieldException(target.GetType().Name,property);
            field.intValue=value; serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
