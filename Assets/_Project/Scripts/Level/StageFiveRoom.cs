using TimeMetroidvania.Boss;
using TimeMetroidvania.Enemy;
using UnityEngine;

namespace TimeMetroidvania.Level
{
    /// <summary>
    /// 为正式关卡保留稳定房间和区域 ID，供阶段七的区域死亡重置使用。
    /// </summary>
    public sealed class StageFiveRoom : MonoBehaviour
    {
        [SerializeField] private string roomId;
        [SerializeField] private string areaId;

        public string RoomId => roomId;
        public string AreaId => areaId;

        public void Configure(string room, string area)
        {
            roomId = room;
            areaId = area;
        }

        public void RefreshEnemies()
        {
            foreach (EnemyBrain enemy in GetComponentsInChildren<EnemyBrain>(true))
            {
                enemy.ResetEncounter();
            }
            foreach (BossBrain boss in GetComponentsInChildren<BossBrain>(true))
            {
                boss.ResetEncounter();
            }
        }
    }
}
