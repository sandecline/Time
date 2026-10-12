using System.Collections.Generic;
using TimeMetroidvania.GameTime;
using UnityEngine;

namespace TimeMetroidvania.Presentation
{
    /// <summary>
    /// 为时停中被斩碎的弹幕保留无伤害碎片，并在时间恢复后播放飞散。
    /// </summary>
    public sealed class TimeStopProjectileShatter : MonoBehaviour
    {
        private static readonly Vector2[] Directions =
        {
            new(-1f, 0.7f),
            new(1f, 0.8f),
            new(-0.75f, -0.9f),
            new(0.85f, -0.75f)
        };

        private const float ScatterDuration = 0.22f;
        private const float ScatterSpeed = 2.4f;
        private const int MaximumGroups = 64;
        private static readonly List<TimeStopProjectileShatter> Pool = new();

        private WorldTimeService worldTime;
        private Transform[] pieces;
        private SpriteRenderer[] renderers;
        private Vector2[] velocities;
        private float scatterElapsed;
        private bool scattering;

        public static void Prewarm(int count, Transform parent)
        {
            Pool.RemoveAll(item => item == null);
            while (Pool.Count < Mathf.Min(count, MaximumGroups))
                CreateGroup(parent);
        }

        private static TimeStopProjectileShatter CreateGroup(Transform parent)
        {
            GameObject root = new("FrozenProjectileShatter");
            if (parent != null) root.transform.SetParent(parent, false);
            TimeStopProjectileShatter shatter = root.AddComponent<TimeStopProjectileShatter>();
            root.SetActive(false);
            Pool.Add(shatter);
            return shatter;
        }

        private void Awake()
        {
            pieces = new Transform[Directions.Length];
            renderers = new SpriteRenderer[Directions.Length];
            velocities = new Vector2[Directions.Length];
            for (int index = 0; index < pieces.Length; index++)
            {
                GameObject piece = new($"Fragment_{index + 1}");
                piece.transform.SetParent(transform, false);
                renderers[index] = piece.AddComponent<SpriteRenderer>();
                pieces[index] = piece.transform;
            }
        }

        public static void TryCreate(SpriteRenderer source, WorldTimeService worldTime)
        {
            if (source == null || source.sprite == null || worldTime == null
                || worldTime.CurrentMode != WorldTimeMode.Stopped)
            {
                return;
            }

            TimeStopProjectileShatter shatter = null;
            foreach (TimeStopProjectileShatter entry in Pool)
            {
                if (entry != null && !entry.gameObject.activeSelf)
                {
                    shatter = entry;
                    break;
                }
            }
            if (shatter == null && Pool.Count < MaximumGroups)
                shatter = CreateGroup(null);
            if (shatter == null) return;
            shatter.transform.SetPositionAndRotation(
                source.transform.position,
                source.transform.rotation);
            shatter.Initialize(source, worldTime);
            shatter.gameObject.SetActive(true);
        }

        private void Initialize(SpriteRenderer source, WorldTimeService service)
        {
            worldTime = service;
            scattering = false;
            scatterElapsed = 0f;
            Vector3 sourceScale = source.transform.lossyScale;

            for (int index = 0; index < pieces.Length; index++)
            {
                Vector2 offset = Directions[index].normalized * 0.08f;
                pieces[index].localPosition = offset;
                pieces[index].localRotation = Quaternion.identity;
                pieces[index].localScale = new Vector3(
                    sourceScale.x * 0.48f,
                    sourceScale.y * 0.48f,
                    sourceScale.z);

                SpriteRenderer renderer = renderers[index];
                renderer.sprite = source.sprite;
                renderer.color = source.color;
                renderer.flipX = source.flipX;
                renderer.flipY = source.flipY;
                renderer.sortingLayerID = source.sortingLayerID;
                renderer.sortingOrder = source.sortingOrder + 1;
                velocities[index] = Directions[index].normalized * ScatterSpeed;
            }

            worldTime.ModeChanged += HandleModeChanged;
        }

        private void Update()
        {
            if (!scattering)
            {
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;
            scatterElapsed += deltaTime;
            for (int index = 0; index < pieces.Length; index++)
            {
                Transform piece = pieces[index];
                if (piece == null)
                {
                    continue;
                }

                velocities[index] += Vector2.down * (5f * deltaTime);
                piece.position += (Vector3)(velocities[index] * deltaTime);
                piece.Rotate(0f, 0f, (index % 2 == 0 ? -360f : 360f) * deltaTime);
                SpriteRenderer renderer = renderers[index];
                if (renderer != null)
                {
                    Color color = renderer.color;
                    color.a = 1f - Mathf.Clamp01(scatterElapsed / ScatterDuration);
                    renderer.color = color;
                }
            }

            if (scatterElapsed >= ScatterDuration)
            {
                gameObject.SetActive(false);
            }
        }

        private void HandleModeChanged(WorldTimeMode previousMode, WorldTimeMode nextMode)
        {
            if (previousMode == WorldTimeMode.Stopped && nextMode != WorldTimeMode.Stopped)
            {
                scattering = true;
            }
        }

        private void OnDisable()
        {
            if (worldTime != null)
            {
                worldTime.ModeChanged -= HandleModeChanged;
                worldTime = null;
            }
        }
    }
}
