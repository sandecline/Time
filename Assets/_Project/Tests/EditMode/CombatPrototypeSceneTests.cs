using NUnit.Framework;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TimeMetroidvania.Tests
{
    public sealed class CombatPrototypeSceneTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Prototype/CombatPrototype.unity";

        [Test]
        public void 战斗原型包含完整玩家闭环与验收对象()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject player = GameObject.Find("Player");
            Assert.That(player, Is.Not.Null);
            Assert.That(player.GetComponent<PlayerStateMachine>(), Is.Not.Null);
            Assert.That(player.GetComponent<PlayerCombat>(), Is.Not.Null);
            Assert.That(player.GetComponent<PlayerDash>(), Is.Not.Null);
            Assert.That(player.GetComponent<TimeGauge>(), Is.Not.Null);
            Assert.That(player.GetComponent<PlayerHealing>(), Is.Not.Null);
            Assert.That(player.GetComponent<PlayerTimeAbilities>(), Is.Not.Null);

            HealthSystem health = player.GetComponent<HealthSystem>();
            Assert.That(health, Is.Not.Null);

            SerializedObject combat = new(player.GetComponent<PlayerCombat>());
            SerializedObject dash = new(player.GetComponent<PlayerDash>());
            SerializedObject healing = new(player.GetComponent<PlayerHealing>());
            SerializedObject timeAbilities = new(player.GetComponent<PlayerTimeAbilities>());
            Assert.That(combat.FindProperty("attackAction").objectReferenceValue, Is.Not.Null);
            Assert.That(combat.FindProperty("worldTime").objectReferenceValue, Is.Not.Null);
            Assert.That(dash.FindProperty("dashAction").objectReferenceValue, Is.Not.Null);
            Assert.That(healing.FindProperty("timeAbilityAction").objectReferenceValue, Is.Not.Null);
            Assert.That(healing.FindProperty("worldTime").objectReferenceValue, Is.Not.Null);
            Assert.That(timeAbilities.FindProperty("attackAction").objectReferenceValue, Is.Not.Null);
            Assert.That(timeAbilities.FindProperty("worldTime").objectReferenceValue, Is.Not.Null);

            WorldTimeService worldTime = Object.FindFirstObjectByType<WorldTimeService>();
            Assert.That(worldTime, Is.Not.Null);
            Assert.That(worldTime.IsTimeStopUnlocked, Is.True);

            GameObject dummy = GameObject.Find("TrainingDummy");
            Assert.That(dummy, Is.Not.Null);
            Assert.That(dummy.GetComponent<LocalTimeController>(), Is.Not.Null);
            Assert.That(dummy.GetComponent<TimeAwareDamageReceiver>(), Is.Not.Null);
            Assert.That(GameObject.Find("DashWall"), Is.Not.Null);
            GameObject projectile = GameObject.Find("BreakableProjectile");
            Assert.That(projectile, Is.Not.Null);
            Assert.That(projectile.GetComponent<LocalTimeController>(), Is.Not.Null);
            Assert.That(projectile.GetComponent<PrototypeTimeMover>(), Is.Not.Null);
            Assert.That(GameObject.Find("PrototypeHUD"), Is.Not.Null);
        }
    }
}
