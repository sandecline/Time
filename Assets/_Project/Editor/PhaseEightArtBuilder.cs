#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TimeMetroidvania.Boss;
using TimeMetroidvania.Enemy;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Level;
using TimeMetroidvania.Presentation;
using TimeMetroidvania.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimeMetroidvania.Editor
{
    /// <summary>
    /// 阶段八美术导入器：固定网格切片，并把角色动画与区域背景接入七房间场景。
    /// </summary>
    internal static class PhaseEightArtBuilder
    {
        private const string BuildVersionKey = "TimeMetroidvania.Phase8ArtBuilder.v13";
        private const string PlayerPrefabPath =
            "Assets/_Project/Prefabs/Characters/Player/Player.prefab";
        private const string SevenRoomScenePath =
            "Assets/_Project/Scenes/Prototype/SevenRoomSlice.unity";
        private const string PlayerRoot =
            "Assets/_Project/Art/Sprites/Characters/Player/";
        private const string EnemyRoot =
            "Assets/_Project/Art/Sprites/Characters/Enemies/";
        private const string BossRoot =
            "Assets/_Project/Art/Sprites/Characters/Bosses/EclipseWarden/";
        private const string ProjectileAtlas =
            "Assets/_Project/Art/Sprites/Projectiles/Projectiles_Combat_Master.png";
        private const string PropAtlas =
            "Assets/_Project/Art/Sprites/Environment/Props/Checkpoint_AbilityProps.png";
        private const string UiAtlas = "Assets/_Project/Art/UI/UI_Master.png";
        private const string VfxAtlas = "Assets/_Project/Art/VFX/Time_VFX_Master.png";

        private readonly struct StripDefinition
        {
            public StripDefinition(string path, int frames, int cellSize)
            {
                Path = path;
                Frames = frames;
                CellSize = cellSize;
            }

            public string Path { get; }
            public int Frames { get; }
            public int CellSize { get; }
        }

        private static readonly StripDefinition[] AnimationStrips =
        {
            new(PlayerRoot + "Player_Idle.png", 4, 96),
            new(PlayerRoot + "Player_Run.png", 8, 96),
            new(PlayerRoot + "Player_JumpRise.png", 2, 96),
            new(PlayerRoot + "Player_Fall.png", 2, 96),
            new(PlayerRoot + "Player_AttackGround.png", 6, 96),
            new(PlayerRoot + "Player_AttackAir.png", 6, 96),
            new(PlayerRoot + "Player_Dash.png", 4, 96),
            new(PlayerRoot + "Player_Hurt.png", 3, 96),
            new(PlayerRoot + "Player_Death.png", 8, 96),
            new(PlayerRoot + "Player_Heal.png", 8, 96),
            new(PlayerRoot + "Player_SlowCharge.png", 6, 96),

            new(EnemyRoot + "ClockGuard/ClockGuard_Idle.png", 4, 96),
            new(EnemyRoot + "ClockGuard/ClockGuard_Patrol.png", 6, 96),
            new(EnemyRoot + "ClockGuard/ClockGuard_Attack.png", 6, 96),
            new(EnemyRoot + "ClockGuard/ClockGuard_Hurt.png", 2, 96),
            new(EnemyRoot + "ClockGuard/ClockGuard_Death.png", 6, 96),

            new(EnemyRoot + "ClockworkBug/ClockworkBug_Patrol.png", 6, 96),
            new(EnemyRoot + "ClockworkBug/ClockworkBug_Charge.png", 4, 96),
            new(EnemyRoot + "ClockworkBug/ClockworkBug_Hurt.png", 2, 96),
            new(EnemyRoot + "ClockworkBug/ClockworkBug_Death.png", 5, 96),

            new(EnemyRoot + "ClockArcher/ClockArcher_Idle.png", 4, 96),
            new(EnemyRoot + "ClockArcher/ClockArcher_Aim.png", 5, 96),
            new(EnemyRoot + "ClockArcher/ClockArcher_Shoot.png", 4, 96),
            new(EnemyRoot + "ClockArcher/ClockArcher_Hurt.png", 2, 96),
            new(EnemyRoot + "ClockArcher/ClockArcher_Death.png", 6, 96),

            new(BossRoot + "Boss_Idle.png", 6, 128),
            new(BossRoot + "Boss_SweepTelegraph.png", 4, 128),
            new(BossRoot + "Boss_SweepAttack.png", 6, 128),
            new(BossRoot + "Boss_Jump.png", 4, 128),
            new(BossRoot + "Boss_Slam.png", 6, 128),
            new(BossRoot + "Boss_Shoot.png", 6, 128),
            new(BossRoot + "Boss_TimeAnchor.png", 8, 128),
            new(BossRoot + "Boss_Hurt.png", 3, 128),
            new(BossRoot + "Boss_PhaseChange.png", 10, 128),
            new(BossRoot + "Boss_Death.png", 12, 128)
        };

        private static readonly string[] TileSheets =
        {
            "Assets/_Project/Art/Sprites/Environment/Tiles/AreaA/Tiles_AreaA.png",
            "Assets/_Project/Art/Sprites/Environment/Tiles/AreaB/Tiles_AreaB.png",
            "Assets/_Project/Art/Sprites/Environment/Tiles/AreaC/Tiles_AreaC.png"
        };

        [InitializeOnLoadMethod]
        private static void ScheduleAutomaticBuild()
        {
            if (EditorPrefs.GetBool(BuildVersionKey, false))
            {
                return;
            }

            EditorApplication.delayCall += RunAutomaticBuild;
        }

        private static void RunAutomaticBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += RunAutomaticBuild;
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += RunAutomaticBuild;
                return;
            }

            try
            {
                Build();
                EditorPrefs.SetBool(BuildVersionKey, true);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem("Time Metroidvania/阶段 8/重新导入并接入原创美术")]
        private static void BuildFromMenu()
        {
            Build();
            EditorPrefs.SetBool(BuildVersionKey, true);
        }

        private static void Build()
        {
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (StripDefinition strip in AnimationStrips)
                {
                    SliceHorizontalStrip(strip);
                }

                foreach (string tileSheet in TileSheets)
                {
                    SliceGrid(tileSheet, 32, 32);
                }

                SliceSemanticAtlases();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            ConfigurePlayerPrefab();
            ConfigureSevenRoomScene();
            AssetDatabase.SaveAssets();
            Debug.Log("阶段 8 原创美术已完成固定网格切片，并接入玩家、敌人、Boss 与区域背景。");
        }

        private static void SliceHorizontalStrip(StripDefinition strip)
        {
            TextureImporter importer = AssetImporter.GetAtPath(strip.Path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"找不到动画条：{strip.Path}");
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(strip.Path);
            if (texture == null
                || texture.width != strip.Frames * strip.CellSize
                || texture.height != strip.CellSize)
            {
                throw new InvalidOperationException(
                    $"动画条尺寸不符合 {strip.Frames}×{strip.CellSize}：{strip.Path}");
            }

            string baseName = System.IO.Path.GetFileNameWithoutExtension(strip.Path);
            SpriteRect[] rects = new SpriteRect[strip.Frames];
            for (int index = 0; index < strip.Frames; index++)
            {
                rects[index] = CreateSpriteRect(
                    $"{baseName}_{index}",
                    new Rect(index * strip.CellSize, 0f, strip.CellSize, strip.CellSize));
            }

            ApplySpriteRects(importer, rects);
        }

        private static void SliceGrid(string path, int cellWidth, int cellHeight)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (importer == null || texture == null)
            {
                throw new InvalidOperationException($"找不到 Tile 图集：{path}");
            }

            int columns = texture.width / cellWidth;
            int rows = texture.height / cellHeight;
            if (columns * cellWidth != texture.width || rows * cellHeight != texture.height)
            {
                throw new InvalidOperationException($"Tile 图集未对齐固定网格：{path}");
            }

            string baseName = System.IO.Path.GetFileNameWithoutExtension(path);
            List<SpriteRect> rects = new(columns * rows);
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int index = row * columns + column;
                    float y = texture.height - (row + 1) * cellHeight;
                    rects.Add(CreateSpriteRect(
                        $"{baseName}_{index}",
                        new Rect(column * cellWidth, y, cellWidth, cellHeight)));
                }
            }

            ApplySpriteRects(importer, rects.ToArray());
        }

        private static void SliceSemanticAtlases()
        {
            SliceNamedAtlas(
                ProjectileAtlas,
                CreateTopSpriteRect("Projectile_BlueBreakable", 40, 20, 440, 330),
                CreateTopSpriteRect("Projectile_OrangeUnbreakable", 20, 350, 455, 260),
                CreateTopSpriteRect("Projectile_PurpleTimeAnchor", 820, 455, 415, 385),
                CreateTopSpriteRect("HitSpark_Normal", 10, 810, 390, 340),
                CreateTopSpriteRect("HitSpark_Player", 345, 800, 430, 350),
                CreateTopSpriteRect("SlashArc_Player", 430, 785, 485, 469),
                CreateTopSpriteRect("Impact_Slam", 815, 755, 439, 499));

            SliceNamedAtlas(
                PropAtlas,
                CreateTopSpriteRect("Checkpoint_Inactive", 0, 0, 460, 680),
                CreateTopSpriteRect("Checkpoint_Active", 430, 0, 455, 680),
                CreateTopSpriteRect("Pickup_TimeStop", 875, 0, 359, 440),
                CreateTopSpriteRect("Pickup_DurationUpgrade", 820, 330, 414, 390),
                CreateTopSpriteRect("Entrance_Left", 0, 650, 620, 604),
                CreateTopSpriteRect("Entrance_Right", 634, 650, 620, 604));

            SliceNamedAtlas(
                UiAtlas,
                CreateTopSpriteRect("UI_Health", 20, 205, 520, 235),
                CreateTopSpriteRect("UI_TimeGauge", 490, 0, 425, 445),
                CreateTopSpriteRect("UI_IconSlow", 895, 0, 180, 225),
                CreateTopSpriteRect("UI_IconTimeStop", 1065, 0, 189, 225),
                CreateTopSpriteRect("UI_IconHeal", 895, 205, 180, 230),
                CreateTopSpriteRect("UI_IconCheckpoint", 1065, 205, 189, 230),
                CreateTopSpriteRect("UI_BossBar", 20, 430, 1214, 190),
                CreateTopSpriteRect("UI_MapUnknown", 20, 600, 160, 245),
                CreateTopSpriteRect("UI_MapDiscovered", 175, 600, 160, 245),
                CreateTopSpriteRect("UI_MapCurrent", 330, 600, 160, 245),
                CreateTopSpriteRect("UI_MapCheckpoint", 485, 600, 160, 245),
                CreateTopSpriteRect("UI_MapBoss", 640, 600, 180, 245),
                CreateTopSpriteRect("UI_ButtonNormal", 865, 550, 369, 105),
                CreateTopSpriteRect("UI_ButtonHover", 865, 645, 369, 105),
                CreateTopSpriteRect("UI_ButtonPressed", 865, 740, 369, 105),
                CreateTopSpriteRect("UI_PauseFrame", 20, 815, 620, 439),
                CreateTopSpriteRect("UI_TitleFrame", 634, 790, 620, 464));

            SliceTimeVfx();
        }

        private static void SliceNamedAtlas(string path, params SpriteRect[] rects)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"找不到语义图集：{path}");
            }

            ApplySpriteRects(importer, rects);
        }

        private static SpriteRect CreateTopSpriteRect(
            string name,
            float x,
            float top,
            float width,
            float height)
        {
            return CreateSpriteRect(
                name,
                new Rect(x, 1254f - top - height, width, height));
        }

        private static void SliceTimeVfx()
        {
            List<SpriteRect> rects = new();
            AddVfxRow(rects, "VFX_SlowCharge", 6, 0, 165);
            AddVfxRow(rects, "VFX_SlowReady", 6, 140, 180);
            AddVfxRow(rects, "VFX_SlowStart", 8, 285, 175);
            AddVfxRow(rects, "VFX_TimeStop", 8, 430, 195);
            AddVfxRow(rects, "VFX_TimeAnchor", 8, 590, 190);
            AddVfxRow(rects, "VFX_Heal", 8, 750, 180);
            AddVfxRow(rects, "VFX_HealBreak", 5, 895, 165);
            AddVfxRow(rects, "VFX_Dash", 4, 1010, 140);
            AddVfxRow(rects, "VFX_DeathRewind", 10, 1110, 144);
            SliceNamedAtlas(VfxAtlas, rects.ToArray());
        }

        private static void AddVfxRow(
            List<SpriteRect> rects,
            string baseName,
            int frameCount,
            float top,
            float height)
        {
            float cellWidth = 1254f / frameCount;
            for (int index = 0; index < frameCount; index++)
            {
                float x = index * cellWidth;
                float width = index == frameCount - 1 ? 1254f - x : cellWidth;
                rects.Add(CreateTopSpriteRect(
                    $"{baseName}_{index}",
                    x,
                    top,
                    width,
                    height));
            }
        }

        private static SpriteRect CreateSpriteRect(string name, Rect rect)
        {
            return new SpriteRect
            {
                name = name,
                rect = rect,
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                border = Vector4.zero,
                spriteID = GUID.Generate()
            };
        }

        private static void ApplySpriteRects(TextureImporter importer, SpriteRect[] rects)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32f;
            TextureImporterSettings textureSettings = new();
            importer.ReadTextureSettings(textureSettings);
            textureSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(textureSettings);

            SpriteDataProviderFactories factories = new();
            factories.Init();
            ISpriteEditorDataProvider provider =
                factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            provider.SetSpriteRects(rects);
            provider.Apply();
            importer.SaveAndReimport();
        }

        private static void ConfigurePlayerPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                SpriteRenderer art = CreateArtVisual(root, 0.58f);
                PlayerArtAnimator animator = root.GetComponent<PlayerArtAnimator>();
                if (animator == null)
                {
                    animator = root.AddComponent<PlayerArtAnimator>();
                }
                SerializedObject serialized = new(animator);
                SetObject(serialized, "targetRenderer", art);
                SetSprites(serialized, "idle", PlayerRoot + "Player_Idle.png");
                SetSprites(serialized, "run", PlayerRoot + "Player_Run.png");
                SetSprites(serialized, "jumpRise", PlayerRoot + "Player_JumpRise.png");
                SetSprites(serialized, "fall", PlayerRoot + "Player_Fall.png");
                SetSprites(serialized, "attackGround", PlayerRoot + "Player_AttackGround.png");
                SetSprites(serialized, "attackAir", PlayerRoot + "Player_AttackAir.png");
                SetSprites(serialized, "dash", PlayerRoot + "Player_Dash.png");
                SetSprites(serialized, "hurt", PlayerRoot + "Player_Hurt.png");
                SetSprites(serialized, "death", PlayerRoot + "Player_Death.png");
                SetSprites(serialized, "heal", PlayerRoot + "Player_Heal.png");
                SetSprites(serialized, "slowCharge", PlayerRoot + "Player_SlowCharge.png");
                serialized.FindProperty("damageFlashDuration").floatValue = 0.32f;
                serialized.FindProperty("damageShakeDistance").floatValue = 0.075f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PlayerSlashVisual slashVisual = root.GetComponent<PlayerSlashVisual>();
                if (slashVisual == null)
                {
                    root.AddComponent<PlayerSlashVisual>();
                }

                PlayerCombat combat = root.GetComponent<PlayerCombat>();
                if (combat != null)
                {
                    SerializedObject combatSettings = new(combat);
                    combatSettings.FindProperty("attackDuration").floatValue = 0.32f;
                    combatSettings.FindProperty("attackInterval").floatValue = 0.72f;
                    combatSettings.FindProperty("attackBufferDuration").floatValue = 0.1f;
                    combatSettings.ApplyModifiedPropertiesWithoutUndo();
                }

                PlayerMovement movement = root.GetComponent<PlayerMovement>();
                if (movement != null)
                {
                    SerializedObject movementSettings = new(movement);
                    movementSettings.FindProperty("moveSpeed").floatValue = 6.7f;
                    movementSettings.FindProperty("groundAcceleration").floatValue = 90f;
                    movementSettings.FindProperty("groundDeceleration").floatValue = 110f;
                    movementSettings.FindProperty("airAcceleration").floatValue = 55f;
                    movementSettings.FindProperty("airDeceleration").floatValue = 45f;
                    movementSettings.FindProperty("attackMoveMultiplier").floatValue = 0.88f;
                    movementSettings.FindProperty("jumpSpeed").floatValue = 13f;
                    movementSettings.FindProperty("jumpCutMultiplier").floatValue = 0.42f;
                    movementSettings.FindProperty("fallGravityMultiplier").floatValue = 2.1f;
                    movementSettings.FindProperty("maxFallSpeed").floatValue = 24f;
                    movementSettings.FindProperty("groundCheckSize").vector2Value =
                        new Vector2(0.62f, 0.1f);
                    movementSettings.ApplyModifiedPropertiesWithoutUndo();
                }

                BoxCollider2D playerHitbox = root.GetComponent<BoxCollider2D>();
                if (playerHitbox != null)
                {
                    playerHitbox.size = new Vector2(0.72f, 0.78f);
                    playerHitbox.offset = new Vector2(0f, -0.02f);
                }
                Transform groundCheck = root.transform.Find("GroundCheck");
                if (groundCheck != null)
                {
                    groundCheck.localPosition = new Vector3(0f, -0.41f, 0f);
                }

                HealthSystem playerHealth = root.GetComponent<HealthSystem>();
                if (playerHealth != null)
                {
                    SerializedObject healthSettings = new(playerHealth);
                    healthSettings.FindProperty("hurtDuration").floatValue = 0.28f;
                    healthSettings.FindProperty("damageProtectionDuration").floatValue = 0.9f;
                    healthSettings.ApplyModifiedPropertiesWithoutUndo();
                }

                Rigidbody2D body = root.GetComponent<Rigidbody2D>();
                if (body != null)
                {
                    body.gravityScale = 3.5f;
                }

                SpriteRenderer vfxRenderer =
                    CreateOverlayVisual(root, "TimeVfx", 0.3f, 24);
                PlayerTimeVfxAnimator vfxAnimator =
                    root.GetComponent<PlayerTimeVfxAnimator>();
                if (vfxAnimator == null)
                {
                    vfxAnimator = root.AddComponent<PlayerTimeVfxAnimator>();
                }
                SerializedObject vfxSettings = new(vfxAnimator);
                SetObject(vfxSettings, "targetRenderer", vfxRenderer);
                SetNamedSprites(vfxSettings, "slowCharge", VfxAtlas, "VFX_SlowCharge_");
                SetNamedSprites(vfxSettings, "slowReady", VfxAtlas, "VFX_SlowReady_");
                SetNamedSprites(vfxSettings, "heal", VfxAtlas, "VFX_Heal_");
                SetNamedSprites(vfxSettings, "dash", VfxAtlas, "VFX_Dash_");
                SetNamedSprites(vfxSettings, "deathRewind", VfxAtlas, "VFX_DeathRewind_");
                vfxSettings.ApplyModifiedPropertiesWithoutUndo();
                vfxRenderer.enabled = false;
                art.sprite = LoadSprites(PlayerRoot + "Player_Idle.png").FirstOrDefault();
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureSevenRoomScene()
        {
            Scene scene = SceneManager.GetSceneByPath(SevenRoomScenePath);
            bool openedForBuild = !scene.isLoaded;
            if (openedForBuild)
            {
                scene = EditorSceneManager.OpenScene(SevenRoomScenePath, OpenSceneMode.Additive);
            }

            try
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (EnemyBrain enemy in root.GetComponentsInChildren<EnemyBrain>(true))
                    {
                        ConfigureEnemy(enemy);
                    }

                    foreach (BossBrain boss in root.GetComponentsInChildren<BossBrain>(true))
                    {
                        ConfigureBoss(boss);
                    }
                }

                ConfigureRoomBackgrounds(scene);
                ConfigureEnvironmentSkin(scene);
                ConfigureGameplayArt(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (openedForBuild && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void ConfigureEnemy(EnemyBrain enemy)
        {
            SpriteRenderer art = CreateArtVisual(enemy.gameObject, 0.68f);
            SerializedObject brainSettings = new(enemy);
            brainSettings.FindProperty("attackWindup").floatValue = enemy.Kind switch
            {
                EnemyKind.ClockworkBug => 0.58f,
                EnemyKind.ClockArcher => 0.78f,
                _ => 0.7f
            };
            brainSettings.FindProperty("hurtDuration").floatValue = 0.32f;
            brainSettings.FindProperty("meleeHitRange").vector2Value =
                enemy.Kind == EnemyKind.ClockworkBug
                    ? new Vector2(1.75f, 0.85f)
                    : new Vector2(1.5f, 1.4f);
            brainSettings.ApplyModifiedPropertiesWithoutUndo();
            if (enemy.GetComponent<EnemyAttackTelegraphVisual>() == null)
            {
                enemy.gameObject.AddComponent<EnemyAttackTelegraphVisual>();
            }
            EnemyArtAnimator animator = enemy.GetComponent<EnemyArtAnimator>();
            if (animator == null)
            {
                animator = enemy.gameObject.AddComponent<EnemyArtAnimator>();
            }
            SerializedObject serialized = new(animator);
            SetObject(serialized, "targetRenderer", art);

            string idlePath;
            string patrolPath;
            string preparePath;
            string attackPath;
            string hurtPath;
            string deathPath;
            switch (enemy.Kind)
            {
                case EnemyKind.ClockworkBug:
                    idlePath = EnemyRoot + "ClockworkBug/ClockworkBug_Patrol.png";
                    patrolPath = idlePath;
                    preparePath = EnemyRoot + "ClockworkBug/ClockworkBug_Charge.png";
                    attackPath = preparePath;
                    hurtPath = EnemyRoot + "ClockworkBug/ClockworkBug_Hurt.png";
                    deathPath = EnemyRoot + "ClockworkBug/ClockworkBug_Death.png";
                    break;
                case EnemyKind.ClockArcher:
                    idlePath = EnemyRoot + "ClockArcher/ClockArcher_Idle.png";
                    patrolPath = idlePath;
                    preparePath = EnemyRoot + "ClockArcher/ClockArcher_Aim.png";
                    attackPath = EnemyRoot + "ClockArcher/ClockArcher_Shoot.png";
                    hurtPath = EnemyRoot + "ClockArcher/ClockArcher_Hurt.png";
                    deathPath = EnemyRoot + "ClockArcher/ClockArcher_Death.png";
                    break;
                default:
                    idlePath = EnemyRoot + "ClockGuard/ClockGuard_Idle.png";
                    patrolPath = EnemyRoot + "ClockGuard/ClockGuard_Patrol.png";
                    attackPath = EnemyRoot + "ClockGuard/ClockGuard_Attack.png";
                    preparePath = attackPath;
                    hurtPath = EnemyRoot + "ClockGuard/ClockGuard_Hurt.png";
                    deathPath = EnemyRoot + "ClockGuard/ClockGuard_Death.png";
                    break;
            }

            SetSprites(serialized, "idle", idlePath);
            SetSprites(serialized, "patrol", patrolPath);
            SetSprites(serialized, "prepareAttack", preparePath);
            SetSprites(serialized, "attack", attackPath);
            SetSprites(serialized, "hurt", hurtPath);
            SetSprites(serialized, "death", deathPath);
            serialized.FindProperty("damageFlashDuration").floatValue = 0.32f;
            serialized.FindProperty("damageShakeDistance").floatValue = 0.065f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            art.sprite = LoadSprites(idlePath).FirstOrDefault();
        }

        private static void ConfigureBoss(BossBrain boss)
        {
            SpriteRenderer art = CreateArtVisual(boss.gameObject, 0.9f);
            if (boss.GetComponent<BossAttackTelegraphVisual>() == null)
            {
                boss.gameObject.AddComponent<BossAttackTelegraphVisual>();
            }
            BossArtAnimator animator = boss.GetComponent<BossArtAnimator>();
            if (animator == null)
            {
                animator = boss.gameObject.AddComponent<BossArtAnimator>();
            }
            SerializedObject serialized = new(animator);
            SetObject(serialized, "targetRenderer", art);
            SetSprites(serialized, "idle", BossRoot + "Boss_Idle.png");
            SetSprites(serialized, "sweepTelegraph", BossRoot + "Boss_SweepTelegraph.png");
            SetSprites(serialized, "sweepAttack", BossRoot + "Boss_SweepAttack.png");
            SetSprites(serialized, "jump", BossRoot + "Boss_Jump.png");
            SetSprites(serialized, "slam", BossRoot + "Boss_Slam.png");
            SetSprites(serialized, "shoot", BossRoot + "Boss_Shoot.png");
            SetSprites(serialized, "timeAnchor", BossRoot + "Boss_TimeAnchor.png");
            SetSprites(serialized, "hurt", BossRoot + "Boss_Hurt.png");
            SetSprites(serialized, "phaseChange", BossRoot + "Boss_PhaseChange.png");
            SetSprites(serialized, "death", BossRoot + "Boss_Death.png");
            serialized.FindProperty("damageFlashDuration").floatValue = 0.34f;
            serialized.FindProperty("damageShakeDistance").floatValue = 0.075f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject bossSettings = new(boss);
            bossSettings.FindProperty("firstPhaseTelegraph").floatValue = 0.8f;
            bossSettings.FindProperty("secondPhaseTelegraph").floatValue = 0.6f;
            bossSettings.FindProperty("sweepHitRange").vector2Value =
                new Vector2(3.5f, 1.25f);
            bossSettings.FindProperty("slamHitRange").vector2Value =
                new Vector2(2.35f, 1.6f);
            bossSettings.ApplyModifiedPropertiesWithoutUndo();
            art.sprite = LoadSprites(BossRoot + "Boss_Idle.png").FirstOrDefault();
        }

        private static void ConfigureGameplayArt(Scene scene)
        {
            Sprite blueProjectile =
                LoadSprite(ProjectileAtlas, "Projectile_BlueBreakable");
            Sprite orangeProjectile =
                LoadSprite(ProjectileAtlas, "Projectile_OrangeUnbreakable");
            Sprite purpleProjectile =
                LoadSprite(ProjectileAtlas, "Projectile_PurpleTimeAnchor");

            ProjectilePool enemyPool = FindComponent<ProjectilePool>(scene);
            if (enemyPool != null)
            {
                SerializedObject settings = new(enemyPool);
                SetObject(settings, "projectileSprite", blueProjectile);
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            BossProjectilePool bossPool = FindComponent<BossProjectilePool>(scene);
            if (bossPool != null)
            {
                SerializedObject settings = new(bossPool);
                SetObject(settings, "projectileSprite", blueProjectile);
                SetObject(settings, "breakableProjectileSprite", blueProjectile);
                SetObject(settings, "unbreakableProjectileSprite", orangeProjectile);
                SetObject(settings, "timeAnchorProjectileSprite", purpleProjectile);
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            foreach (StageFiveCheckpoint checkpoint in FindComponents<StageFiveCheckpoint>(scene))
            {
                ConfigureCheckpoint(checkpoint);
            }

            foreach (StageFivePickup pickup in FindComponents<StageFivePickup>(scene))
            {
                ConfigurePickup(pickup);
            }

            ConfigureHud(scene);
        }

        private static void ConfigureCheckpoint(StageFiveCheckpoint checkpoint)
        {
            Sprite inactive = LoadSprite(PropAtlas, "Checkpoint_Inactive");
            Sprite active = LoadSprite(PropAtlas, "Checkpoint_Active");
            SpriteRenderer art = CreateArtVisual(checkpoint.gameObject, 0.12f);
            art.sprite = inactive;
            SerializedObject settings = new(checkpoint);
            SetObject(settings, "artRenderer", art);
            SetObject(settings, "inactiveSprite", inactive);
            SetObject(settings, "activeSprite", active);
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePickup(StageFivePickup pickup)
        {
            Sprite timeStop = LoadSprite(PropAtlas, "Pickup_TimeStop");
            Sprite upgrade = LoadSprite(PropAtlas, "Pickup_DurationUpgrade");
            SpriteRenderer art = CreateArtVisual(pickup.gameObject, 0.085f);
            art.sprite = pickup.Kind == StageFivePickupKind.TimeStop
                ? timeStop
                : upgrade;
            SerializedObject settings = new(pickup);
            SetObject(settings, "artRenderer", art);
            SetObject(settings, "timeStopSprite", timeStop);
            SetObject(settings, "durationUpgradeSprite", upgrade);
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureHud(Scene scene)
        {
            StageFiveLevelController level = FindComponent<StageFiveLevelController>(scene);
            HealthSystem health = FindComponent<HealthSystem>(scene);
            TimeGauge gauge = FindComponent<TimeGauge>(scene);
            BossBrain boss = FindComponent<BossBrain>(scene);
            if (level == null || health == null || gauge == null)
            {
                return;
            }

            GothicHudPresenter hud = level.GetComponent<GothicHudPresenter>();
            if (hud == null)
            {
                hud = level.gameObject.AddComponent<GothicHudPresenter>();
            }

            SerializedObject settings = new(hud);
            SetObject(settings, "playerHealth", health);
            SetObject(settings, "timeGauge", gauge);
            SetObject(settings, "level", level);
            SetObject(settings, "boss", boss);
            SetObject(settings, "healthFrame", LoadSprite(UiAtlas, "UI_Health"));
            SetObject(settings, "timeGaugeFrame", LoadSprite(UiAtlas, "UI_TimeGauge"));
            SetObject(settings, "slowIcon", LoadSprite(UiAtlas, "UI_IconSlow"));
            SetObject(settings, "timeStopIcon", LoadSprite(UiAtlas, "UI_IconTimeStop"));
            SetObject(settings, "healIcon", LoadSprite(UiAtlas, "UI_IconHeal"));
            SetObject(settings, "checkpointIcon", LoadSprite(UiAtlas, "UI_IconCheckpoint"));
            SetObject(settings, "bossBarFrame", LoadSprite(UiAtlas, "UI_BossBar"));
            settings.ApplyModifiedPropertiesWithoutUndo();

            if (boss != null)
            {
                SerializedObject bossSettings = new(boss);
                bossSettings.FindProperty("showLegacyHud").boolValue = false;
                bossSettings.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static SpriteRenderer CreateArtVisual(GameObject root, float worldScale)
        {
            SpriteRenderer fallback = root.GetComponent<SpriteRenderer>();
            Transform visual = root.transform.Find("ArtVisual");
            if (visual == null)
            {
                GameObject visualObject = new("ArtVisual");
                visual = visualObject.transform;
                visual.SetParent(root.transform, false);
            }

            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            float rootScaleX = Mathf.Max(0.001f, Mathf.Abs(root.transform.lossyScale.x));
            float rootScaleY = Mathf.Max(0.001f, Mathf.Abs(root.transform.lossyScale.y));
            visual.localScale = new Vector3(
                worldScale / rootScaleX,
                worldScale / rootScaleY,
                1f);

            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = visual.gameObject.AddComponent<SpriteRenderer>();
            }
            if (fallback != null)
            {
                renderer.sortingLayerID = fallback.sortingLayerID;
                renderer.sortingOrder = fallback.sortingOrder;
                fallback.enabled = false;
            }

            renderer.enabled = true;
            renderer.color = Color.white;
            return renderer;
        }

        private static SpriteRenderer CreateOverlayVisual(
            GameObject root,
            string childName,
            float worldScale,
            int sortingOrder)
        {
            Transform visual = root.transform.Find(childName);
            if (visual == null)
            {
                GameObject visualObject = new(childName);
                visual = visualObject.transform;
                visual.SetParent(root.transform, false);
            }

            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            float rootScaleX = Mathf.Max(0.001f, Mathf.Abs(root.transform.lossyScale.x));
            float rootScaleY = Mathf.Max(0.001f, Mathf.Abs(root.transform.lossyScale.y));
            visual.localScale = new Vector3(
                worldScale / rootScaleX,
                worldScale / rootScaleY,
                1f);

            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = visual.gameObject.AddComponent<SpriteRenderer>();
            }
            renderer.sortingOrder = sortingOrder;
            renderer.color = Color.white;
            return renderer;
        }

        private static void ConfigureRoomBackgrounds(Scene scene)
        {
            string[] areaByRoom = { "AreaA", "AreaA", "AreaB", "AreaA", "AreaB", "AreaB", "AreaC" };
            for (int index = 0; index < areaByRoom.Length; index++)
            {
                GameObject room = FindGameObject(scene, $"Room_{index + 1:00}");
                if (room == null)
                {
                    continue;
                }

                Bounds bounds = CalculateRoomBounds(room);
                Transform backgroundRoot = room.transform.Find("ArtBackgrounds");
                if (backgroundRoot == null)
                {
                    GameObject holder = new("ArtBackgrounds");
                    backgroundRoot = holder.transform;
                    backgroundRoot.SetParent(room.transform, false);
                }

                string area = areaByRoom[index];
                ConfigureBackgroundLayer(
                    backgroundRoot,
                    "Far",
                    $"Assets/_Project/Art/Backgrounds/{area}/Background_{area}_Far.png",
                    bounds,
                    -100,
                    1.08f);
                ConfigureBackgroundLayer(
                    backgroundRoot,
                    "Mid",
                    $"Assets/_Project/Art/Backgrounds/{area}/Background_{area}_Mid.png",
                    bounds,
                    -90,
                    1.03f);
            }
        }

        private static void ConfigureEnvironmentSkin(Scene scene)
        {
            foreach (StageFiveRoom room in FindComponents<StageFiveRoom>(scene))
            {
                string area = string.IsNullOrWhiteSpace(room.AreaId)
                    ? "AreaA"
                    : $"Area{room.AreaId}";
                string tilePath =
                    $"Assets/_Project/Art/Sprites/Environment/Tiles/{area}/Tiles_{area}.png";
                Sprite floorTile = LoadSprite(tilePath, $"Tiles_{area}_0");
                Sprite accentTile = LoadSprite(tilePath, $"Tiles_{area}_16") ?? floorTile;
                Sprite gateTile = LoadSprite(tilePath, $"Tiles_{area}_24") ?? accentTile;

                foreach (SpriteRenderer renderer
                    in room.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    string objectName = renderer.gameObject.name;
                    if (objectName == "Floor"
                        || objectName.Contains("Wall")
                        || objectName.EndsWith("Platform"))
                    {
                        SkinGrayboxRenderer(renderer, floorTile, Color.white);
                    }
                    else if (objectName == "RoomAccent"
                        || objectName == "BossArenaFloorMarker")
                    {
                        SkinGrayboxRenderer(
                            renderer,
                            accentTile,
                            new Color(0.78f, 0.9f, 1f));
                    }
                    else if (objectName == "Door_R5_Hidden_R6")
                    {
                        SkinGrayboxRenderer(
                            renderer,
                            gateTile,
                            new Color(0.95f, 0.68f, 0.28f));
                    }
                    else if ((objectName == "BenchBack" || objectName == "BenchSeat")
                        && renderer.transform.parent?.Find("ArtVisual") != null)
                    {
                        renderer.enabled = false;
                    }
                }

                foreach (StageFiveDoor door
                    in room.GetComponentsInChildren<StageFiveDoor>(true))
                {
                    ConfigureBossBattleBlocker(door, gateTile);
                    SpriteRenderer blocker = door.transform
                        .Find("LockedBlocker")
                        ?.GetComponent<SpriteRenderer>();
                    if (blocker != null)
                    {
                        SkinGrayboxRenderer(
                            blocker,
                            gateTile,
                            new Color(0.95f, 0.68f, 0.28f));
                    }
                }

                foreach (StageFiveTimeGate gate
                    in room.GetComponentsInChildren<StageFiveTimeGate>(true))
                {
                    SpriteRenderer renderer = gate.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        SkinGrayboxRenderer(
                            renderer,
                            gateTile,
                            new Color(0.95f, 0.68f, 0.28f));
                    }
                }
            }
        }

        private static void ConfigureBossBattleBlocker(StageFiveDoor door, Sprite gateTile)
        {
            if (door == null || gateTile == null
                || door.SourceRoomId != "R7" && door.DestinationRoomId != "R7")
            {
                return;
            }

            Transform blocker = door.transform.Find("BossBattleBlocker");
            if (blocker == null)
            {
                GameObject blockerObject = new("BossBattleBlocker");
                blocker = blockerObject.transform;
                blocker.SetParent(door.transform, false);
            }
            blocker.localPosition = Vector3.zero;
            blocker.localRotation = Quaternion.identity;
            float scaleX = Mathf.Max(0.001f, Mathf.Abs(door.transform.lossyScale.x));
            float scaleY = Mathf.Max(0.001f, Mathf.Abs(door.transform.lossyScale.y));
            blocker.localScale = new Vector3(1f / scaleX, 1f / scaleY, 1f);

            BoxCollider2D collider = blocker.GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                collider = blocker.gameObject.AddComponent<BoxCollider2D>();
            }
            collider.size = new Vector2(0.42f, 2.7f);
            collider.enabled = false;

            SpriteRenderer renderer = blocker.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = blocker.gameObject.AddComponent<SpriteRenderer>();
            }
            renderer.sprite = gateTile;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = collider.size;
            renderer.color = new Color(1f, 0.28f, 0.16f);
            renderer.sortingOrder = 15;
            renderer.enabled = false;
            door.ConfigureBossBattleBlocker(collider, renderer);
        }

        private static void SkinGrayboxRenderer(
            SpriteRenderer source,
            Sprite tile,
            Color tint)
        {
            if (source == null || tile == null)
            {
                return;
            }

            Transform visual = source.transform.Find("EnvironmentArt");
            Bounds sourceBounds = source.bounds;
            if (visual == null)
            {
                GameObject visualObject = new("EnvironmentArt");
                visual = visualObject.transform;
                visual.SetParent(source.transform, false);
            }

            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            float scaleX = Mathf.Max(
                0.001f,
                Mathf.Abs(source.transform.lossyScale.x));
            float scaleY = Mathf.Max(
                0.001f,
                Mathf.Abs(source.transform.lossyScale.y));
            visual.localScale = new Vector3(1f / scaleX, 1f / scaleY, 1f);

            SpriteRenderer art = visual.GetComponent<SpriteRenderer>();
            if (art == null)
            {
                art = visual.gameObject.AddComponent<SpriteRenderer>();
            }

            art.sprite = tile;
            art.drawMode = SpriteDrawMode.Tiled;
            art.tileMode = SpriteTileMode.Continuous;
            if (sourceBounds.size.x > 0.01f && sourceBounds.size.y > 0.01f)
            {
                art.size = new Vector2(sourceBounds.size.x, sourceBounds.size.y);
            }
            art.sortingLayerID = source.sortingLayerID;
            art.sortingOrder = source.sortingOrder;
            art.color = tint;
            art.enabled = true;
            source.enabled = false;
        }

        private static void ConfigureBackgroundLayer(
            Transform parent,
            string layerName,
            string spritePath,
            Bounds roomBounds,
            int sortingOrder,
            float widthMultiplier)
        {
            Transform layer = parent.Find(layerName);
            if (layer == null)
            {
                GameObject layerObject = new(layerName);
                layer = layerObject.transform;
                layer.SetParent(parent, false);
            }

            SpriteRenderer renderer = layer.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = layer.gameObject.AddComponent<SpriteRenderer>();
            }
            Sprite sprite = LoadSprites(spritePath).FirstOrDefault();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.color = Color.white;

            float targetWidth = Mathf.Max(18f, roomBounds.size.x) * widthMultiplier;
            float scale = sprite == null || sprite.bounds.size.x <= 0f
                ? 1f
                : targetWidth / sprite.bounds.size.x;
            Vector3 worldCenter = new(
                roomBounds.center.x,
                roomBounds.min.y + 4.6f,
                0f);
            layer.position = worldCenter;
            layer.localScale = Vector3.one * scale;
        }

        private static Bounds CalculateRoomBounds(GameObject room)
        {
            Collider2D[] colliders = room.GetComponentsInChildren<Collider2D>(true);
            if (colliders.Length == 0)
            {
                return new Bounds(room.transform.position, new Vector3(24f, 9f, 0f));
            }

            Bounds bounds = colliders[0].bounds;
            for (int index = 1; index < colliders.Length; index++)
            {
                bounds.Encapsulate(colliders[index].bounds);
            }

            return bounds;
        }

        private static GameObject FindGameObject(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                Transform match = transforms.FirstOrDefault(item => item.name == name);
                if (match != null)
                {
                    return match.gameObject;
                }
            }

            return null;
        }

        private static T FindComponent<T>(Scene scene) where T : Component
        {
            return FindComponents<T>(scene).FirstOrDefault();
        }

        private static T[] FindComponents<T>(Scene scene) where T : Component
        {
            List<T> results = new();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                results.AddRange(root.GetComponentsInChildren<T>(true));
            }
            return results.ToArray();
        }

        private static Sprite[] LoadSprites(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => ParseFrameIndex(sprite.name))
                .ToArray();
        }

        private static Sprite LoadSprite(string path, string spriteName)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .FirstOrDefault(sprite => sprite.name == spriteName);
        }

        private static Sprite[] LoadSpritesByPrefix(string path, string prefix)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .Where(sprite => sprite.name.StartsWith(
                    prefix,
                    StringComparison.Ordinal))
                .OrderBy(sprite => ParseFrameIndex(sprite.name))
                .ToArray();
        }

        private static int ParseFrameIndex(string name)
        {
            int separator = name.LastIndexOf('_');
            return separator >= 0
                && int.TryParse(
                    name[(separator + 1)..],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int index)
                ? index
                : 0;
        }

        private static void SetObject(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object value)
        {
            serialized.FindProperty(propertyName).objectReferenceValue = value;
        }

        private static void SetSprites(
            SerializedObject serialized,
            string propertyName,
            string assetPath)
        {
            Sprite[] sprites = LoadSprites(assetPath);
            SerializedProperty property = serialized.FindProperty(propertyName);
            property.arraySize = sprites.Length;
            for (int index = 0; index < sprites.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = sprites[index];
            }
        }

        private static void SetNamedSprites(
            SerializedObject serialized,
            string propertyName,
            string assetPath,
            string prefix)
        {
            Sprite[] sprites = LoadSpritesByPrefix(assetPath, prefix);
            SerializedProperty property = serialized.FindProperty(propertyName);
            property.arraySize = sprites.Length;
            for (int index = 0; index < sprites.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = sprites[index];
            }
        }
    }
}
#endif
