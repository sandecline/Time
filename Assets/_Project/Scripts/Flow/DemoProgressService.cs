using System;
using System.Collections.Generic;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Level;
using UnityEngine;

namespace TimeMetroidvania.Flow
{
    /// <summary>
    /// 测试 Demo 的本次运行进度；不读写 PlayerPrefs 或磁盘文件。
    /// </summary>
    public sealed class DemoProgressService : MonoBehaviour
    {
        [SerializeField] private StageFiveLevelController level;
        [SerializeField] private WorldTimeService worldTime;

        private readonly HashSet<string> discoveredRooms = new();

        public event Action ProgressChanged;

        public bool PersistenceEnabled => false;
        public string ActiveCheckpointId { get; private set; } = "CP_R1_Start";
        public bool BossDefeated { get; private set; }
        public bool HasTimeStop => worldTime != null && worldTime.IsTimeStopUnlocked;
        public bool HasDurationUpgrade => worldTime != null && worldTime.HasDurationUpgrade;
        public IReadOnlyCollection<string> DiscoveredRooms => discoveredRooms;

        public void Configure(StageFiveLevelController controller, WorldTimeService time)
        {
            Unsubscribe();
            level = controller;
            worldTime = time;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void StartNewSession()
        {
            discoveredRooms.Clear();
            discoveredRooms.Add("R1");
            ActiveCheckpointId = "CP_R1_Start";
            BossDefeated = false;
            ProgressChanged?.Invoke();
        }

        public bool IsRoomDiscovered(string roomId)
        {
            return !string.IsNullOrEmpty(roomId) && discoveredRooms.Contains(roomId);
        }

        private void Subscribe()
        {
            if (level == null)
            {
                return;
            }

            level.RoomEntered -= HandleRoomEntered;
            level.CheckpointActivated -= HandleCheckpointActivated;
            level.BossDefeatedConfirmed -= HandleBossDefeated;
            level.RoomEntered += HandleRoomEntered;
            level.CheckpointActivated += HandleCheckpointActivated;
            level.BossDefeatedConfirmed += HandleBossDefeated;
        }

        private void Unsubscribe()
        {
            if (level == null)
            {
                return;
            }

            level.RoomEntered -= HandleRoomEntered;
            level.CheckpointActivated -= HandleCheckpointActivated;
            level.BossDefeatedConfirmed -= HandleBossDefeated;
        }

        private void HandleRoomEntered(string roomId)
        {
            if (discoveredRooms.Add(roomId))
            {
                ProgressChanged?.Invoke();
            }
        }

        private void HandleCheckpointActivated(string checkpointId, string roomId)
        {
            ActiveCheckpointId = checkpointId;
            discoveredRooms.Add(roomId);
            ProgressChanged?.Invoke();
        }

        private void HandleBossDefeated()
        {
            BossDefeated = true;
            ProgressChanged?.Invoke();
        }
    }
}
