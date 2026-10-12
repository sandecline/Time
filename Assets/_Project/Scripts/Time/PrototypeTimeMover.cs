using UnityEngine;

namespace TimeMetroidvania.GameTime
{
    /// <summary>
    /// 灰盒时间对象，在两个水平端点间移动以直观看出正常、时缓与时停。
    /// </summary>
    [RequireComponent(typeof(LocalTimeController))]
    public sealed class PrototypeTimeMover : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float speed = 2.5f;
        [SerializeField, Min(0.1f)] private float halfDistance = 2f;

        private LocalTimeController localTime;
        private Vector3 center;
        private float elapsed;

        private void Awake()
        {
            localTime = GetComponent<LocalTimeController>();
            center = transform.position;
        }

        private void Update()
        {
            elapsed += localTime.LocalDeltaTime;
            float distance = Mathf.PingPong(elapsed * speed, halfDistance * 2f) - halfDistance;
            transform.position = center + Vector3.right * distance;
        }
    }
}
