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

        private string feedback;
        private float feedbackUntil;
        private string prompt;
        private int promptFrame = -1;
        private int promptPriority;
        private GUIStyle labelStyle;

        public string CurrentRoomId => currentRoomId;
        public string ActiveCheckpointId => activeCheckpointId;
        public WorldTimeService WorldTime => worldTime;
        public bool BossDefeated => bossDefeated;
        public bool BossBattleActive => bossBattleActive && !bossDefeated;
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

        private void OnEnable()
        {
            SubscribeHealth();
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
            RoomEntered?.Invoke(roomId);
            ShowMessage(roomId == "R7"
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
            ShowMessage("Boss 已击败！测试流程完成，本次运行不会写入存档。");
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
            currentRoomId = checkpointRoomId;
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
            GUI.Box(new Rect(left, 18f, 490f, 175f), GUIContent.none);
            GUI.Label(new Rect(left + 14f, 30f, 460f, 27f),
                $"阶段 5 · 房间 {currentRoomId[1..]}/7 · {activeCheckpointId}", labelStyle);
            GUI.Label(new Rect(left + 14f, 59f, 460f, 27f),
                worldTime != null && worldTime.IsTimeStopUnlocked
                    ? $"时停：已解锁    时长：{worldTime.TimeStopMaximum:0} 秒"
                    : "目标：前往房间 3 获得时停", labelStyle);
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
