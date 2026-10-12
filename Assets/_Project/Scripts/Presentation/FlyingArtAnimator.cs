using TimeMetroidvania.Enemy;
using TimeMetroidvania.GameTime;
using UnityEngine;

namespace TimeMetroidvania.Presentation
{
    /// <summary>飞行敌人的悬停、撞击、受伤与死亡逐帧表现。</summary>
    [DisallowMultipleComponent]
    public sealed class FlyingArtAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField, Min(1f)] private float framesPerSecond = 11f;
        [SerializeField] private Sprite[] hover;
        [SerializeField] private Sprite[] charge;
        [SerializeField] private Sprite[] hurt;
        [SerializeField] private Sprite[] death;
        private readonly SpriteFramePlayer framePlayer = new();
        private V2FlyingEnemy flyer;
        private LocalTimeController localTime;

        private void Awake()
        {
            flyer = GetComponent<V2FlyingEnemy>();
            localTime = GetComponent<LocalTimeController>();
            if (targetRenderer == null) targetRenderer = transform.Find("ArtVisual")?.GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (targetRenderer == null || flyer == null) return;
            float delta = localTime != null ? localTime.LocalDeltaTime : Time.deltaTime;
            Sprite[] frames = flyer.State switch
            {
                FlyingEnemyState.Dead => death,
                FlyingEnemyState.Hurt => hurt,
                FlyingEnemyState.Chase => charge,
                _ => hover
            };
            framePlayer.Play(targetRenderer, frames, framesPerSecond,
                flyer.State is FlyingEnemyState.Hover or FlyingEnemyState.Return or FlyingEnemyState.Chase,
                delta);
        }
    }
}
