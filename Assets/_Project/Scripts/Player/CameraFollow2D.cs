using UnityEngine;

namespace TimeMetroidvania.Player
{
    /// <summary>
    /// 使用平滑阻尼跟随目标，并可将镜头限制在当前房间范围内。
    /// </summary>
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(0.01f)] private float smoothTime = 0.18f;
        [SerializeField] private Vector2 offset = new(0f, 0.6f);

        private Vector3 velocity;
        private bool useBounds;
        private Bounds activeBounds;
        private float roomSize;
        private bool patternZoom;
        private const float BossPatternSize = 6.5f;
        public bool HasBounds => useBounds;
        public Bounds ActiveBounds => activeBounds;

        public void SetTarget(Transform followTarget)
        {
            target = followTarget;
        }

        public void SetOrthographicSize(float size)
        {
            roomSize = Mathf.Max(0.1f, size);
            patternZoom = false;
            Camera cameraComponent = GetComponent<Camera>();
            if (cameraComponent != null && cameraComponent.orthographic)
            {
                cameraComponent.orthographicSize = roomSize;
            }
        }

        public void SetPatternZoom(bool active)
        {
            patternZoom = active;
        }

        public void SetBounds(Bounds worldBounds)
        {
            activeBounds = worldBounds;
            useBounds = true;
        }

        public void ClearBounds()
        {
            useBounds = false;
        }

        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            velocity = Vector3.zero;
            transform.position = ClampToBounds(new Vector3(
                target.position.x + offset.x,
                target.position.y + offset.y,
                transform.position.z));
        }

        private void LateUpdate()
        {
            Camera cameraComponent = GetComponent<Camera>();
            if (cameraComponent != null && cameraComponent.orthographic && roomSize > 0f)
            {
                float requested = patternZoom ? BossPatternSize : roomSize;
                cameraComponent.orthographicSize = Mathf.MoveTowards(
                    cameraComponent.orthographicSize, requested,
                    Mathf.Abs(BossPatternSize - roomSize) * Time.unscaledDeltaTime / 0.45f);
            }
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = ClampToBounds(new Vector3(
                target.position.x + offset.x,
                target.position.y + offset.y,
                transform.position.z));
            transform.position = ClampToBounds(Vector3.SmoothDamp(
                transform.position, desiredPosition, ref velocity, smoothTime));
        }

        private Vector3 ClampToBounds(Vector3 desired)
        {
            Camera cameraComponent = GetComponent<Camera>();
            if (!useBounds || cameraComponent == null || !cameraComponent.orthographic)
            {
                return desired;
            }

            float halfHeight = cameraComponent.orthographicSize;
            float halfWidth = halfHeight * cameraComponent.aspect;
            float minimumX = activeBounds.min.x + halfWidth;
            float maximumX = activeBounds.max.x - halfWidth;
            float minimumY = activeBounds.min.y + halfHeight;
            float maximumY = activeBounds.max.y - halfHeight;

            desired.x = minimumX <= maximumX
                ? Mathf.Clamp(desired.x, minimumX, maximumX)
                : activeBounds.center.x;
            desired.y = minimumY <= maximumY
                ? Mathf.Clamp(desired.y, minimumY, maximumY)
                : activeBounds.center.y;
            return desired;
        }
    }
}
