using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Level
{
    /// <summary>
    /// 为单个房间保存镜头可移动范围，进入房间时由关卡控制器应用。
    /// </summary>
    public sealed class RoomCameraBounds2D : MonoBehaviour
    {
        [SerializeField] private string roomId;
        [SerializeField] private Vector2 center;
        [SerializeField] private Vector2 size = new(24f, 14f);
        [SerializeField, Min(0.1f)] private float orthographicSize = 5f;

        public string RoomId => roomId;
        public Bounds WorldBounds => new(center, new Vector3(size.x, size.y, 1f));
        public float OrthographicSize => orthographicSize;

        public void Configure(string id, Vector2 worldCenter, Vector2 worldSize)
        {
            roomId = id;
            center = worldCenter;
            size = new Vector2(
                Mathf.Max(1f, worldSize.x),
                Mathf.Max(1f, worldSize.y));
        }

        public void Apply(CameraFollow2D cameraFollow)
        {
            if (cameraFollow == null)
            {
                return;
            }

            cameraFollow.SetOrthographicSize(orthographicSize);
            cameraFollow.SetBounds(WorldBounds);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireCube(center, new Vector3(size.x, size.y, 0f));
        }
    }
}
