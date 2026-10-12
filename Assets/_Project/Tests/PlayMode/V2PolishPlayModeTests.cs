using System.Collections;
using System.Linq;
using NUnit.Framework;
using TimeMetroidvania.Enemy;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Level;
using TimeMetroidvania.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TimeMetroidvania.Tests
{
    public sealed class V2PolishPlayModeTests
    {
        private const string Scene = "Assets/_Project/Scenes/V2/EightRoomGreybox.unity";
        [UnityTest]
        public IEnumerator 十六个入口接触不换房穿出换房且落点无墙()
        {
            yield return SceneManager.LoadSceneAsync(Scene);
            yield return null;
            GameObject player = GameObject.Find("Player");
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            Collider2D shape = player.GetComponent<Collider2D>();
            StageFiveLevelController level = Object.FindFirstObjectByType<StageFiveLevelController>();
            foreach (StageFiveDoor door in Object.FindObjectsByType<StageFiveDoor>(FindObjectsSortMode.None))
            {
                level.EndBossBattle(); level.EnterRoom(door.SourceRoomId);
                body.position = door.transform.position; player.transform.position = body.position;
                Physics2D.SyncTransforms();
                Assert.That(door.TryTravel(player), Is.False, door.name + " 接触就换房");
                float half = Mathf.Abs(door.OutwardNormal.x) > 0.5f ? shape.bounds.extents.x : shape.bounds.extents.y;
                body.position = (Vector2)door.transform.position + door.OutwardNormal * (half + 0.1f);
                player.transform.position = body.position; Physics2D.SyncTransforms();
                Assert.That(door.TryTravel(player), Is.True, door.name + " 穿出不换房");
                Assert.That(level.CurrentRoomId, Is.EqualTo(door.DestinationRoomId));
                Physics2D.SyncTransforms();
                Collider2D[] hits = Physics2D.OverlapBoxAll(shape.bounds.center,
                    (Vector2)shape.bounds.size * 0.95f, 0f, LayerMask.GetMask("Ground"));
                Assert.That(hits.Any(c => c.enabled && !c.isTrigger), Is.False, door.name + " 落点在墙里");
            }
        }
        [UnityTest]
        public IEnumerator 地面敌人会下落到地板且不能穿墙()
        {
            GameObject floor = new("测试地面"); floor.layer = LayerMask.NameToLayer("Ground");
            floor.transform.position = new Vector3(500, -1, 0); floor.transform.localScale = new Vector3(12, 1, 1);
            floor.AddComponent<BoxCollider2D>();
            GameObject wall = new("测试墙"); wall.layer = floor.layer;
            wall.transform.position = new Vector3(502, 1, 0); wall.transform.localScale = new Vector3(0.25f, 5, 1);
            wall.AddComponent<BoxCollider2D>();
            GameObject actor = new("测试敌人"); actor.transform.position = new Vector3(500, 3, 0);
            actor.AddComponent<BoxCollider2D>().isTrigger = true;
            EnemyMotor2D motor = actor.AddComponent<EnemyMotor2D>();
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(0.8f);
            Assert.That(actor.transform.position.y, Is.InRange(-0.02f, 0.15f), "重力落地");
            motor.DesiredVelocity = Vector2.right * 9f;
            yield return new WaitForSeconds(0.6f);
            Assert.That(actor.transform.position.x, Is.LessThan(501.4f), "扫掠不能穿墙");
            Object.Destroy(actor); Object.Destroy(floor); Object.Destroy(wall);
        }
        [UnityTest]
        public IEnumerator 远程弹幕遇到薄墙回池且视线受阻()
        {
            yield return SceneManager.LoadSceneAsync(Scene);
            yield return null;
            ProjectilePool pool = Object.FindFirstObjectByType<ProjectilePool>();
            GameObject wall = new("测试弹幕薄墙"); wall.layer = LayerMask.NameToLayer("Ground");
            wall.transform.position = new Vector3(601, 0, 0); wall.transform.localScale = new Vector3(0.03f, 4, 1);
            wall.AddComponent<BoxCollider2D>(); Physics2D.SyncTransforms();
            Assert.That(WorldCollision2D.Blocks(new Vector2(600, 0), new Vector2(603, 0)), Is.True);
            Assert.That(pool.TryFire(new Vector2(600, 0), Vector2.right), Is.True);
            yield return new WaitForSeconds(0.35f);
            Assert.That(pool.ActiveCount, Is.Zero);
            Object.Destroy(wall);
        }
        [UnityTest]
        public IEnumerator 离水恢复重力且冰面行走不会被冻住()
        {
            yield return SceneManager.LoadSceneAsync(Scene);
            yield return null;
            GameObject player = GameObject.Find("Player");
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            V2TimeStopWater water = Object.FindFirstObjectByType<V2TimeStopWater>();
            float gravity = body.gravityScale;
            body.position = water.transform.position; player.transform.position = body.position;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.That(water.HasSubmergedPlayer, Is.True);
            Assert.That(body.gravityScale, Is.LessThan(gravity));
            V2TimeStopWater.ReleasePlayer(player);
            Assert.That(body.gravityScale, Is.EqualTo(gravity).Within(0.001f));
            WorldTimeService time = Object.FindFirstObjectByType<WorldTimeService>();
            time.UnlockTimeStop(); player.GetComponent<TimeGauge>().Fill();
            time.TryStartTimeStop(player.GetComponent<TimeGauge>());
            Bounds volume = water.GetComponent<Collider2D>().bounds;
            body.position = new Vector2(volume.center.x, volume.max.y + player.GetComponent<Collider2D>().bounds.extents.y + 0.06f);
            player.transform.position = body.position; Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.That(V2TimeStopWater.IsPlayerFrozen(player), Is.False);
            Assert.That(body.constraints, Is.Not.EqualTo(RigidbodyConstraints2D.FreezeAll));
        }
        [UnityTest]
        public IEnumerator 机关逆时针且时缓角速度降低()
        {
            yield return SceneManager.LoadSceneAsync(Scene);
            yield return null;
            V2TimeRotor rotor = Object.FindFirstObjectByType<V2TimeRotor>();
            Rigidbody2D body = rotor.GetComponent<Rigidbody2D>();
            float before = body.rotation;
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.That(Mathf.DeltaAngle(before, body.rotation), Is.GreaterThan(0f));
            Object.FindFirstObjectByType<WorldTimeService>().TryStartSlow();
            Assert.That(rotor.CurrentScale, Is.EqualTo(0.35f).Within(0.001f));
        }
    }
}
