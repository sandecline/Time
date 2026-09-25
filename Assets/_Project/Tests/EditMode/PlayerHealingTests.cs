using System.Reflection;
using NUnit.Framework;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TimeMetroidvania.Tests
{
    public sealed class PlayerHealingTests
    {
        private GameObject player;
        private GameObject damageSource;
        private InputActionAsset inputAsset;
        private InputActionReference moveReference;
        private InputActionReference jumpReference;
        private InputActionReference timeReference;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(damageSource);
            Object.DestroyImmediate(moveReference);
            Object.DestroyImmediate(jumpReference);
            Object.DestroyImmediate(timeReference);
            Object.DestroyImmediate(inputAsset);
        }

        [Test]
        public void 治疗持续扣槽且受伤中断不返还()
        {
            player = new GameObject("PlayerHealingTests");
            player.SetActive(false);
            damageSource = new GameObject("DamageSource");

            PlayerStateMachine stateMachine = player.AddComponent<PlayerStateMachine>();
            PlayerMovement movement = player.AddComponent<PlayerMovement>();
            HealthSystem health = player.AddComponent<HealthSystem>();
            TimeGauge gauge = player.AddComponent<TimeGauge>();
            PlayerHealing healing = player.AddComponent<PlayerHealing>();

            Transform groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.SetParent(player.transform);
            CreateInputReferences();
            ConfigureMovement(movement, groundCheck);
            ConfigureObjectReference(healing, "timeAbilityAction", timeReference);
            ConfigureFloat(health, "damageProtectionDuration", 0f);

            player.SetActive(true);
            InvokeLifecycle(movement, "Awake");
            InvokeLifecycle(health, "Awake");
            InvokeLifecycle(gauge, "Awake");
            InvokeLifecycle(gauge, "OnEnable");
            InvokeLifecycle(healing, "Awake");
            InvokeLifecycle(movement, "OnEnable");
            InvokeLifecycle(healing, "OnEnable");
            SetGrounded(movement, true);

            health.TryTakeDamage(new DamageInfo(
                damageSource,
                1,
                Vector2.zero,
                Vector2.zero));
            stateMachine.ResetToNormal();
            gauge.Gain(60f);

            Assert.That(healing.TryBeginHealing(), Is.True);
            healing.AdvanceHealing(0.75f);
            Assert.That(gauge.CurrentValue, Is.EqualTo(30f).Within(0.001f));
            Assert.That(healing.HealingProgress, Is.EqualTo(0.5f).Within(0.001f));

            health.TryTakeDamage(new DamageInfo(
                damageSource,
                1,
                Vector2.zero,
                Vector2.zero));

            Assert.That(healing.IsHealing, Is.False);
            Assert.That(stateMachine.CurrentState, Is.EqualTo(PlayerState.Hurt));
            Assert.That(gauge.CurrentValue, Is.EqualTo(30f).Within(0.001f));
        }

        private void CreateInputReferences()
        {
            inputAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap map = inputAsset.AddActionMap("Gameplay");
            InputAction move = map.AddAction("Move", InputActionType.Value);
            move.expectedControlType = "Vector2";
            InputAction jump = map.AddAction("Jump", InputActionType.Button);
            InputAction time = map.AddAction("TimeAbility", InputActionType.Button);
            moveReference = InputActionReference.Create(move);
            jumpReference = InputActionReference.Create(jump);
            timeReference = InputActionReference.Create(time);
        }

        private void ConfigureMovement(PlayerMovement movement, Transform groundCheck)
        {
            ConfigureObjectReference(movement, "moveAction", moveReference);
            ConfigureObjectReference(movement, "jumpAction", jumpReference);
            ConfigureObjectReference(movement, "groundCheck", groundCheck);
        }

        private static void ConfigureObjectReference(
            Object target,
            string propertyName,
            Object value)
        {
            SerializedObject serializedObject = new(target);
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureFloat(Object target, string propertyName, float value)
        {
            SerializedObject serializedObject = new(target);
            serializedObject.FindProperty(propertyName).floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetGrounded(PlayerMovement movement, bool grounded)
        {
            FieldInfo field = typeof(PlayerMovement).GetField(
                "isGrounded",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(movement, grounded);
        }

        private static void InvokeLifecycle(MonoBehaviour component, string methodName)
        {
            MethodInfo method = component.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(component, null);
        }
    }
}
