using System;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Enemy;
using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Level
{
    /// <summary>
    /// 管理七房间灰盒的当前房间、临时检查点和教学提示。
    /// </summary>
    public sealed class StageFiveLevelController : MonoBehaviour
    {
        [SerializeField] private HealthSystem playerHealth;
        [SerializeField] private CameraFollow2D cameraFollow;
        [SerializeField] private WorldTimeService worldTime;
        [SerializeField] private ProjectilePool projectilePool;
        [SerializeField] private string currentRoomId = "R1";
        [SerializeField] private string activeCheckpointId = "CP_R1_Start";
        [SerializeField] private string checkpointRoomId = "R1";
        [SerializeField] private bool bossDefeated;
        [SerializeField] private bool bossBattleActive;
        [SerializeField] private bool showHud = true;
        [SerializeField] private Sprite hudPanelSprite;
        [SerializeField, Min(1)] private int roomCount = 7;
        [SerializeField] private string bossRoomId = "R7";
        [SerializeField] private bool useV2Objectives;

        private string feedback;
        private float feedbackUntil;
        private string prompt;
        private int promptFrame = -1;
        private int promptPriority;
        private GUIStyle labelStyle;
        private readonly System.Collections.Generic.HashSet<string> missingCameraBounds = new();

        public string CurrentRoomId => currentRoomId;
        public string ActiveCheckpointId => activeCheckpointId;
        public WorldTimeService WorldTime => worldTime;
        public Transform PlayerTransform => playerHealth != null ? playerHealth.transform : null;
        public bool BossDefeated => bossDefeated;
        public bool BossBattleActive => bossBattleActive && !bossDefeated;
        public string BossRoomId => bossRoomId;
        public string ObjectiveText => BuildObjectiveText();
        public string CurrentPrompt => promptFrame == Time.frameCount ? prompt : null;
        public string CurrentFeedback => Time.unscaledTime < feedbackUntil ? feedback : null;

        public event Action<string> RoomEntered;
        public event Action<string, string> CheckpointActivated;
        public event Action BossDefeatedConfirmed;

        public void Configure(
            HealthSystem health, CameraFollow2D follow, WorldTimeService time,
            ProjectilePool pool = null)
        {
            UnsubscribeHealth();
            playerHealth = health;
            cameraFollow = follow;
            worldTime = time;
            projectilePool = pool;
            if (isActiveAndEnabled)
            {
                SubscribeHealth();
            }
        }

        public void ConfigureLayout(int totalRooms, string bossRoom, bool v2Objectives)
        {
            roomCount = Mathf.Max(1, totalRooms);
            bossRoomId = string.IsNullOrWhiteSpace(bossRoom) ? "R7" : bossRoom;
            useV2Objectives = v2Objectives;
        }

        public bool IsBossRoom(string roomId)
        {
            return !string.IsNullOrEmpty(roomId) && roomId == bossRoomId;
        }

        private void OnEnable()
        {
            SubscribeHealth();
        }

        private void Start()
        {
            ApplyCameraBounds(currentRoomId);
            cameraFollow?.SnapToTarget();
            BindFlyingEnemies();
        }

        private void OnDisable()
        {
            UnsubscribeHealth();
        }

        private void SubscribeHealth()
        {
            if (playerHealth == null) return;
            playerHealth.Died -= HandleDeath;
            playerHealth.Respawned -= HandleRespawn;
            playerHealth.Died += HandleDeath;
            playerHealth.Respawned += HandleRespawn;
        }

        private void UnsubscribeHealth()
        {
            if (playerHealth == null) return;
            playerHealth.Died -= HandleDeath;
            playerHealth.Respawned -= HandleRespawn;
        }

        public void TransitionRoom(string sourceRoomId, string destinationRoomId)
        {
            worldTime?.EndTimeStopForTransition();
            RefreshRoom(sourceRoomId);
            RefreshRoom(destinationRoomId);
            projectilePool?.ClearActive();
            EnterRoom(destinationRoomId);
        }

        private void RefreshRoom(string roomId)
        {
            foreach (StageFiveRoom room in FindObjectsByType<StageFiveRoom>(FindObjectsSortMode.None))
            {
                if (room.RoomId == roomId)
                {
                    room.RefreshEnemies();
                }
            }
        }

        private void RefreshArea(string roomId)
        {
            StageFiveRoom[] rooms = FindObjectsByType<StageFiveRoom>(FindObjectsSortMode.None);
            string areaId = null;
            foreach (StageFiveRoom room in rooms)
            {
                if (room.RoomId == roomId)
                {
                    areaId = room.AreaId;
                    break;
                }
            }
            foreach (StageFiveRoom room in rooms)
            {
                if (areaId != null && room.AreaId == areaId)
                {
                    room.RefreshEnemies();
                }
            }
        }

        public void EnterRoom(string roomId)
        {
            currentRoomId = roomId;
            ApplyCameraBounds(roomId);
            cameraFollow?.SnapToTarget();
            BindFlyingEnemies();
            RoomEntered?.Invoke(roomId);
            ShowMessage(IsBossRoom(roomId)
                ? bossDefeated
                    ? "Boss 已被击败。"
                    : "Boss 场地已抵达；穿过场地中央开始战斗。"
                : $"进入房间 {roomId[1..]}");
        }

        public void ConfirmBossDefeated()
        {
            bossDefeated = true;
            bossBattleActive = false;
            projectilePool?.ClearActive();
            ShowMessage("Boss 已击败！试玩流程完成，本次运行不会写入存档。");
            BossDefeatedConfirmed?.Invoke();
        }

        public void BeginBossBattle()
        {
            if (bossDefeated)
            {
                return;
            }
            bossBattleActive = true;
        }

        public void EndBossBattle()
        {
            bossBattleActive = false;
        }

        public void ActivateCheckpoint(string checkpointId, string roomId, Vector3 position)
        {
            activeCheckpointId = checkpointId;
            checkpointRoomId = roomId;
            playerHealth.SetRespawnPoint(position);
            playerHealth.RestoreToFull();
            RefreshArea(roomId);
            projectilePool?.ClearActive();
            ShowMessage($"已坐下休息：{checkpointId}，本区域敌人已重置（仅本次运行有效）");
            CheckpointActivated?.Invoke(checkpointId, roomId);
        }

        public void SetHudVisible(bool visible)
        {
            showHud = visible;
        }

        public void SnapCamera()
        {
            cameraFollow?.SnapToTarget();
        }

        private void ApplyCameraBounds(string roomId)
        {
            RoomViewportMask mask = cameraFollow != null
                ? cameraFollow.GetComponent<RoomViewportMask>() : null;
            foreach (RoomCameraBounds2D bounds in
                FindObjectsByType<RoomCameraBounds2D>(FindObjectsSortMode.None))
            {
                if (bounds.RoomId == roomId)
                {
                    bounds.Apply(cameraFollow);
                    mask?.SetRoom(bounds.WorldBounds);
                    return;
                }
            }
            if (missingCameraBounds.Add(roomId))
                Debug.LogError($"房间 {roomId} 缺少镜头边界，已限制在角色附近。", this);
            if (cameraFollow == null || playerHealth == null) return;
            Bounds fallback = new(playerHealth.transform.position, new Vector3(1f, 1f, 1f));
            cameraFollow.SetBounds(fallback);
            mask?.SetRoom(fallback);
        }

        private void BindFlyingEnemies()
        {
            if (playerHealth == null) return;
            foreach (V2FlyingEnemy enemy in FindObjectsByType<V2FlyingEnemy>(FindObjectsSortMode.None))
                enemy.BindTarget(playerHealth.transform);
        }

        public void ShowMessage(string message)
        {
            feedback = message;
            feedbackUntil = Time.unscaledTime + 4f;
        }

        public void OfferPrompt(string message, int priority = 0)
        {
            if (promptFrame != Time.frameCount)
            {
                promptPriority = int.MinValue;
            }

            if (priority < promptPriority)
            {
                return;
            }

            prompt = message;
            promptPriority = priority;
            promptFrame = Time.frameCount;
        }

        private void HandleRespawn()
        {
            bossBattleActive = false;
            currentRoomId = checkpointRoomId;
            ApplyCameraBounds(checkpointRoomId);
            SnapCamera();
            ShowMessage($"已返回检查点 {activeCheckpointId}");
        }

        private void HandleDeath()
        {
            bossBattleActive = false;
            RefreshArea(currentRoomId);
            projectilePool?.ClearActive();
            worldTime?.ResetToNormal();
        }

        private void OnGUI()
        {
            if (!showHud)
            {
                return;
            }
            labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                normal = { textColor = Color.white }
            };

            float left = Mathf.Max(20f, Screen.width - 510f);
            if (hudPanelSprite != null)
                TimeMetroidvania.Presentation.V2GuiSprite.Draw(hudPanelSprite, new Rect(left, 18f, 490f, 175f));
            else GUI.Box(new Rect(left, 18f, 490f, 175f), GUIContent.none);
            GUI.Label(new Rect(left + 14f, 30f, 460f, 27f),
                $"时序旅人 · 房间 {currentRoomId[1..]}/{roomCount} · {activeCheckpointId}", labelStyle);
            GUI.Label(new Rect(left + 14f, 59f, 460f, 27f),
                worldTime != null && worldTime.IsTimeStopUnlocked
                    ? $"时停：已解锁    时长：{worldTime.TimeStopMaximum:0} 秒"
                    : (useV2Objectives ? "目标：前往房间 5 获得时停" : "目标：前往房间 3 获得时停"), labelStyle);
            GUI.Label(new Rect(left + 14f, 88f, 470f, 27f),
                BuildObjectiveText(), labelStyle);

            if (promptFrame == Time.frameCount)
            {
                GUI.Label(new Rect(left + 14f, 117f, 470f, 27f), prompt, labelStyle);
            }

            if (Time.unscaledTime < feedbackUntil)
            {
                GUI.Label(new Rect(left + 14f, 145f, 470f, 27f), feedback, labelStyle);
            }
        }

        private string BuildObjectiveText()
        {
            if (useV2Objectives)
            {
                if (worldTime == null || !worldTime.IsTimeStopUnlocked)
                {
                    return currentRoomId switch
                    {
                        "R4" => "目标：时缓通过旋转机关，前往房间 5",
                        "R5" => "目标：触碰能力核心，获得时停",
                        _ => "路线：R1 → R2 → R3 → R4 → R5"
                    };
                }

                return currentRoomId switch
                {
                    "R3" => "目标：时停冻结水面，前往房间 6",
                    "R6" => "路线：先去房间 7 激活检查点，再进入房间 8",
                    "R7" => "目标：按 W 激活 Boss 前检查点",
                    "R8" => bossDefeated ? "目标完成：Boss 已击败" : "目标：击败两阶段 Boss",
                    _ => "目标：返回房间 3，利用时停跨越水池"
                };
            }

            if (worldTime == null || !worldTime.IsTimeStopUnlocked)
            {
                return "路线：房 1 → 房 2 → 房 3";
            }

            return currentRoomId switch
            {
                "R1" => "路线：门前短按 I 开门，再从左侧进入房 4",
                "R5" => "路线：右侧去 Boss；暗门可进入隐藏房 6",
                "R6" => "目标：拾取升级，再返回房 5",
                "R7" => bossDefeated ? "目标完成：Boss 已击败" : "目标：击败两阶段 Boss",
                _ => "路线：返回房 1，再去房 4 和房 5"
            };
        }
    }
}
