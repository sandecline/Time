using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TimeMetroidvania.Boss;
using TimeMetroidvania.Enemy;
using TimeMetroidvania.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TimeMetroidvania.Tests
{
    public sealed class OcclusionPlayModeTests
    {
        private const string Scene = "Assets/_Project/Scenes/V2/EightRoomGreybox.unity";
        private readonly List<GameObject> created = new();

        [UnitySetUp]
        public IEnumerator 加载实际灰盒()
        {
            yield return SceneManager.LoadSceneAsync(Scene);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator 清理临时测试对象()
        {
            foreach (GameObject go in created) if (go != null) Object.Destroy(go);
            created.Clear();
            yield return null;
        }

        private GameObject Box(string name, Vector2 position, Vector2 size)
        {
            GameObject go = new(name);
            created.Add(go);
            go.layer = LayerMask.NameToLayer("Ground");
            go.transform.position = position;
            go.AddComponent<BoxCollider2D>().size = size;
            return go;
        }

        private static GameObject HoldPlayer(Vector2 position)
        {
            GameObject player = GameObject.Find("Player");
            player.GetComponent<PlayerMovement>().enabled = false;
            player.GetComponent<PlayerDash>().enabled = false;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            body.position = position;
            body.linearVelocity = Vector2.zero;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
            player.transform.position = position;
            Physics2D.SyncTransforms();
            return player;
        }

        [UnityTest]
        public IEnumerator 远程隔墙不索敌且瞄准后新墙阻止射击()
        {
            HoldPlayer(new Vector2(903f, 10f));
            EnemyBrain archer = Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None)
                .First(e => e.Kind == EnemyKind.ClockArcher);
            archer.SetEncounterSpawn(new Vector3(900f, 10f));
            archer.ResetEncounter();
            typeof(EnemyBrain).GetField("patrolHalfDistance", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(archer, 0f);
            Physics2D.SyncTransforms();
            float bottom = archer.GetComponent<Collider2D>().bounds.min.y;
            Box("测试站立地面", new Vector2(901f, bottom - 0.525f), new Vector2(8f, 1f));
            GameObject wall = Box("测试视线薄墙", new Vector2(901.5f, 10f), new Vector2(0.03f, 4f));
            Physics2D.SyncTransforms();
            int ammunition = archer.ShotsRemaining;
            yield return new WaitForSeconds(0.8f);
            Assert.That(archer.CurrentState, Is.EqualTo(EnemyState.Patrol));
            Assert.That(archer.ShotsRemaining, Is.EqualTo(ammunition));

            wall.SetActive(false);
            Physics2D.SyncTransforms();
            float deadline = Time.time + 2f;
            while (!archer.IsAttackTelegraphing && Time.time < deadline) yield return new WaitForFixedUpdate();
            Assert.That(archer.IsAttackTelegraphing, Is.True, "无墙时应正常发现并瞄准玩家");
            wall.SetActive(true);
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(1.1f);
            Assert.That(archer.ShotsRemaining, Is.EqualTo(ammunition), "前摇中出现墙也不能穿墙开火");
            wall.SetActive(false);
            Physics2D.SyncTransforms();
            deadline = Time.time + 4f;
            while (archer.ShotsRemaining == ammunition && Time.time < deadline) yield return new WaitForFixedUpdate();
            Assert.That(archer.ShotsRemaining, Is.LessThan(ammunition), "不能通过禁止所有射击来通过遮挡测试");
        }

        [UnityTest]
        public IEnumerator 玩家平A能穿墙伤敌和斩弹且弹幕仍被地形拦截()
        {
            GameObject player = HoldPlayer(new Vector2(900f, 10f));
            PlayerCombat combat = player.GetComponent<PlayerCombat>();
            int facing = player.GetComponent<PlayerMovement>().FacingDirection;
            Vector2 origin = player.transform.position;
            GameObject wall = Box("玩家攻击薄墙", origin + Vector2.right * facing * 0.5f, new Vector2(0.02f, 4f));
            GameObject target = Box("攻击测试目标", origin + Vector2.right * facing * 1.1f, new Vector2(0.3f, 0.4f));
            target.layer = LayerMask.NameToLayer("Default");
            target.GetComponent<Collider2D>().isTrigger = true;
            HealthSystem health = target.AddComponent<HealthSystem>();
            ProjectilePool pool = Object.FindFirstObjectByType<ProjectilePool>();
            pool.ClearActive();
            Assert.That(pool.TryFire(origin + new Vector2(facing * 1.05f, 0.25f), Vector2.up), Is.True);
            PooledProjectile shot = pool.GetComponentsInChildren<PooledProjectile>().Single();
            shot.enabled = false;
            Physics2D.SyncTransforms();
            int hp = health.CurrentHealth;
            Assert.That(combat.BeginAttack(AttackDirection.Horizontal), Is.True);
            yield return new WaitForSeconds(0.7f);
            Assert.That(health.CurrentHealth, Is.LessThan(hp));
            Assert.That(pool.ActiveCount, Is.Zero);
            wall.SetActive(false);
            Physics2D.SyncTransforms();
            health.ResetForEncounter();
            Assert.That(pool.TryFire(origin + new Vector2(facing * 1.05f, 0.25f), Vector2.up), Is.True);
            shot = pool.GetComponentsInChildren<PooledProjectile>().Single();
            shot.enabled = false;
            Assert.That(combat.BeginAttack(AttackDirection.Horizontal), Is.True);
            yield return new WaitForSeconds(0.4f);
            Assert.That(health.CurrentHealth, Is.LessThan(hp));
            Assert.That(pool.ActiveCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator 三类高速Boss弹幕被薄墙拦截并可重复回池()
        {
            GameObject holder = new("Boss弹幕测试池"); created.Add(holder);
            BossProjectilePool pool = holder.AddComponent<BossProjectilePool>();
            GameObject wall = Box("高速弹幕薄墙", new Vector2(951f, 10f), new Vector2(0.02f, 4f));
            Physics2D.SyncTransforms();
            for (int kind = 0; kind < 3; kind++)
            {
                Assert.That(pool.TryFire(new Vector2(950f, 10f), Vector2.right, 200f, kind == 0, kind == 2), Is.True);
                yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                Assert.That(pool.ActiveCount, Is.Zero, "弹幕类型 " + kind);
            }
            wall.GetComponent<Collider2D>().isTrigger = true;
            Physics2D.SyncTransforms();
            Assert.That(pool.TryFire(new Vector2(950f, 10f), Vector2.right, 200f, true, false), Is.True);
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.That(pool.ActiveCount, Is.EqualTo(1), "水体等触发器不应挡弹");
            pool.ClearActive();
        }
    }
}
