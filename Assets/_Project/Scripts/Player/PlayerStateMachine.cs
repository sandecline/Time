using System;
using UnityEngine;

namespace TimeMetroidvania.Player
{
    public enum PlayerState
    {
        Normal,
        Attack,
        Dash,
        Healing,
        Hurt,
        Dead
    }

    /// <summary>
    /// 统一管理玩家互斥状态，避免移动、攻击、Dash 与受击同时控制刚体。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerStateMachine : MonoBehaviour
    {
        public event Action<PlayerState, PlayerState> StateChanged;

        public PlayerState CurrentState { get; private set; } = PlayerState.Normal;
        public bool CanMove => CurrentState is PlayerState.Normal or PlayerState.Attack or PlayerState.Healing;
        public bool CanAttack => CurrentState == PlayerState.Normal;
        public bool CanDash => CurrentState is PlayerState.Normal or PlayerState.Attack;

        public bool TryEnter(PlayerState nextState)
        {
            if (!CanTransition(CurrentState, nextState))
            {
                return false;
            }

            SetState(nextState);
            return true;
        }

        public bool TryReturnToNormal(PlayerState expectedState)
        {
            if (CurrentState != expectedState)
            {
                return false;
            }

            SetState(PlayerState.Normal);
            return true;
        }

        public void ResetToNormal()
        {
            SetState(PlayerState.Normal);
        }

        private static bool CanTransition(PlayerState currentState, PlayerState nextState)
        {
            if (currentState == nextState)
            {
                return false;
            }

            if (nextState == PlayerState.Dead)
            {
                return currentState != PlayerState.Dead;
            }

            if (nextState == PlayerState.Hurt)
            {
                return currentState != PlayerState.Dead && currentState != PlayerState.Hurt;
            }

            if (nextState == PlayerState.Dash && currentState == PlayerState.Attack)
            {
                return true;
            }

            return currentState == PlayerState.Normal;
        }

        private void SetState(PlayerState nextState)
        {
            PlayerState previousState = CurrentState;
            CurrentState = nextState;
            StateChanged?.Invoke(previousState, nextState);
        }
    }
}
