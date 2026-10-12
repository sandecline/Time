using NUnit.Framework;
using TimeMetroidvania.Enemy;
using UnityEngine;

namespace TimeMetroidvania.Tests
{
    public sealed class WorldCollision2DTests
    {
        private GameObject wall;
        private BoxCollider2D shape;
        private bool startInColliders;

        [SetUp]
        public void 创建独立测试墙()
        {
            startInColliders = Physics2D.queriesStartInColliders;
            wall = new GameObject("遮挡测试墙");
            wall.layer = LayerMask.NameToLayer("Ground");
            wall.transform.position = new Vector3(1001f, 1000f);
            shape = wall.AddComponent<BoxCollider2D>();
            shape.size = new Vector2(0.02f, 4f);
            Physics2D.SyncTransforms();
        }

        [TearDown]
        public void 清理测试墙和查询设置()
        {
            Physics2D.queriesStartInColliders = startInColliders;
            Object.DestroyImmediate(wall);
        }

        [TestCase(0f, 0.02f)]
        [TestCase(0f, 1f)]
        [TestCase(0.2f, 0.02f)]
        [TestCase(0.22f, 1f)]
        public void 普通墙与薄墙双向遮挡射线和有体积扫掠(float radius, float thickness)
        {
            shape.size = new Vector2(thickness, 4f);
            Physics2D.SyncTransforms();
            Vector2 a = new(1000f, 1000f), b = new(1003f, 1000f);
            Assert.That(WorldCollision2D.Blocks(a, b, radius), Is.True);
            Assert.That(WorldCollision2D.Blocks(b, a, radius), Is.True);
        }

        [TestCase(0f)]
        [TestCase(0.2f)]
        public void 水体触发器和非地形碰撞体不挡视线(float radius)
        {
            Vector2 a = new(1000f, 1000f), b = new(1003f, 1000f);
            shape.isTrigger = true;
            Physics2D.SyncTransforms();
            Assert.That(WorldCollision2D.Blocks(a, b, radius), Is.False);
            shape.isTrigger = false;
            wall.layer = LayerMask.NameToLayer("Default");
            Physics2D.SyncTransforms();
            Assert.That(WorldCollision2D.Blocks(a, b, radius), Is.False);
        }

        [TestCase(0f)]
        [TestCase(0.2f)]
        public void 起点嵌墙不受全局起点查询开关影响(float radius)
        {
            Physics2D.queriesStartInColliders = false;
            Vector2 start = wall.transform.position;
            Assert.That(WorldCollision2D.Blocks(start, start + Vector2.right * 2f, radius), Is.True);
            Assert.That(WorldCollision2D.Blocks(start, start, radius), Is.True);
        }

        [Test]
        public void 有体积弹幕贴墙起步即被阻挡()
        {
            Physics2D.queriesStartInColliders = false;
            Vector2 start = new(1000.85f, 1000f);
            Assert.That(WorldCollision2D.Blocks(start, start + Vector2.left, 0.2f), Is.True);
        }

        [TestCase(0f)]
        [TestCase(0.2f)]
        public void 墙在路径之外或禁用时不阻挡(float radius)
        {
            Assert.That(WorldCollision2D.Blocks(new Vector2(1000f, 1003f), new Vector2(1003f, 1003f), radius), Is.False);
            shape.enabled = false;
            Physics2D.SyncTransforms();
            Assert.That(WorldCollision2D.Blocks(new Vector2(1000f, 1000f), new Vector2(1003f, 1000f), radius), Is.False);
        }
    }
}
