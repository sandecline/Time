using System.Collections;
using NUnit.Framework;
using TimeMetroidvania.Enemy;
using TimeMetroidvania.GameTime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TimeMetroidvania.Tests
{
    public sealed class EnemyEncounterPlayModeTests
    {
        [UnityTest]
        public IEnumerator 弹幕时停冻结恢复后飞行且斩除后复用()
        {
            SceneManager.LoadScene("EnemyEncounter");
            yield return null;

            WorldTimeService world = Object.FindFirstObjectByType<WorldTimeService>();
            ProjectilePool pool = Object.FindFirstObjectByType<ProjectilePool>();
            GameObject player = GameObject.Find("Player");
            TimeGauge gauge = player.GetComponent<TimeGauge>();
            Assert.That(world, Is.Not.Null);
            Assert.That(pool, Is.Not.Null);
            gauge.Gain(60f);

            Assert.That(pool.TryFire(new Vector2(-4f, 0.5f), Vector2.right), Is.True);
            PooledProjectile first = pool.transform.GetChild(0).GetComponent<PooledProjectile>();
            Assert.That(first.gameObject.activeSelf, Is.True);
            yield return new WaitForFixedUpdate();

            Assert.That(world.TryStartTimeStop(gauge), Is.True);
            float frozenX = first.transform.position.x;
            yield return new WaitForSecondsRealtime(0.12f);
            Assert.That(first.transform.position.x, Is.EqualTo(frozenX).Within(0.01f));

            Assert.That(world.EndTimeStopEarly(), Is.True);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(first.transform.position.x, Is.GreaterThan(frozenX + 0.01f));

            Assert.That(first.TryDestroyByAttack(player), Is.True);
            Assert.That(pool.ActiveCount, Is.Zero);
            Assert.That(pool.TryFire(new Vector2(-4f, 0.5f), Vector2.right), Is.True);
            Assert.That(pool.transform.GetChild(0).GetComponent<PooledProjectile>(), Is.SameAs(first));
        }
    }
}
