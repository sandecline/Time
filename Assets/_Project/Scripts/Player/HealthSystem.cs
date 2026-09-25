using System;
using TimeMetroidvania.GameTime;
using UnityEngine;

namespace TimeMetroidvania.Player
{
    /// <summary>
    /// 提供统一伤害入口、受伤保护、击退、死亡和玩家检查点重生。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HealthSystem : MonoBehaviour, IDamageable
    {
        [Header("生命")]
        [SerializeField, Min(1)] private int maxHealth = 5;
        [SerializeField, Min(0f)] private float damageProtectionDuration = 0.8f;

        [Header("受伤与死亡")]
        [SerializeField, Min(0f)] private float hurtDuration = 0.18f;
        [SerializeField] private bool respawnOnDeath;
        [SerializeField, Min(0f)] private float respawnDelay = 1.2f;

        private Rigidbody2D body;
        private LocalTimeController localTime;
        private PlayerStateMachine stateMachine;
        private Vector3 respawnPosition;
        private float protectionRemaining;
        private float hurtRemaining;
        private float deathRemaining;
        private bool initialized;

        public event Action<int, int> HealthChanged;
        public event Action<DamageInfo> Damaged;
        public event Action Died;
        public event Action Respawned;

        public int CurrentHealth { get; private set; }
        public int MaxHealth => maxHealth;
        public bool IsDead => CurrentHealth <= 0;
        public bool IsProtected => protectionRemaining > 0f;
        public bool IsFullHealth
        {
            get
            {
                EnsureInitialized();
                return CurrentHealth >= maxHealth;
            }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            body = GetComponent<Rigidbody2D>();
            localTime = GetComponent<LocalTimeController>();
            stateMachine = GetComponent<PlayerStateMachine>();
            respawnPosition = transform.position;
            CurrentHealth = maxHealth;
            initialized = true;
        }

        private void Update()
        {
            float deltaTime = localTime != null ? localTime.LocalDeltaTime : Time.deltaTime;
            protectionRemaining = Mathf.Max(0f, protectionRemaining - deltaTime);

            if (hurtRemaining > 0f)
            {
                hurtRemaining -= deltaTime;
                if (hurtRemaining <= 0f)
                {
                    stateMachine?.TryReturnToNormal(PlayerState.Hurt);
                }
            }

            if (!IsDead || !respawnOnDeath)
            {
                return;
            }

            deathRemaining -= deltaTime;
            if (deathRemaining <= 0f)
            {
                Respawn();
            }
        }

        public bool TryTakeDamage(DamageInfo damageInfo)
        {
            EnsureInitialized();

            if (IsDead || IsProtected || damageInfo.Amount <= 0 || damageInfo.Source == gameObject)
            {
                return false;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - damageInfo.Amount);
            protectionRemaining = damageProtectionDuration;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
            Damaged?.Invoke(damageInfo);

            if (CurrentHealth == 0)
            {
                Die();
                return true;
            }

            stateMachine?.TryEnter(PlayerState.Hurt);
            hurtRemaining = hurtDuration;

            if (body != null)
            {
                body.linearVelocity = damageInfo.Knockback;
            }

            return true;
        }

        public void SetRespawnPoint(Vector3 worldPosition)
        {
            respawnPosition = worldPosition;
        }

        public void RestoreToFull()
        {
            EnsureInitialized();

            if (IsDead)
            {
                return;
            }

            CurrentHealth = maxHealth;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void ResetForEncounter()
        {
            EnsureInitialized();
            CurrentHealth = maxHealth;
            protectionRemaining = 0f;
            hurtRemaining = 0f;
            deathRemaining = 0f;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
            stateMachine?.ResetToNormal();
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        private void Die()
        {
            stateMachine?.TryEnter(PlayerState.Dead);
            deathRemaining = respawnDelay;

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }

            Died?.Invoke();
        }

        private void Respawn()
        {
            transform.position = respawnPosition;
            CurrentHealth = maxHealth;
            protectionRemaining = 0f;
            hurtRemaining = 0f;

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }

            stateMachine?.ResetToNormal();
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
            Respawned?.Invoke();
        }
    }
}
