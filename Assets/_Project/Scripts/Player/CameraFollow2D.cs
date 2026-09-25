using UnityEngine;

namespace TimeMetroidvania.Player
{
    /// <summary>
    /// 使用平滑阻尼跟随目标，并保留摄像机的 Z 轴距离。
    /// </summary>
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(0.01f)] private float smoothTime = 0.18f;
        [SerializeField] private Vector2 offset = new(0f, 0.6f);

        private Vector3 velocity;

        public void SetTarget(Transform followTarget)
        {
            target = followTarget;
        }

        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            velocity = Vector3.zero;
            transform.position = new Vector3(
                target.position.x + offset.x,
                target.position.y + offset.y,
                transform.position.z);
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = new(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        }
    }
}
