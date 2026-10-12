using System.Collections;
using System.Linq;
using NUnit.Framework;
using TimeMetroidvania.Boss;
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
    public sealed class V2ActionRevisionPlayModeTests
    {
        private const string Scene = "Assets/_Project/Scenes/V2/EightRoomGreybox.unity";

        [UnitySetUp]
        public IEnumerator 打开八房间场景()
        {
            yield return SceneManager.LoadSceneAsync(Scene);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 初次运行R1即有限界小房间启用房外遮蔽()
        {
            StageFiveLevelController level = Object.FindFirstObjectByType<StageFiveLevelController>();
            CameraFollow2D camera = Object.FindFirstObjectByType<CameraFollow2D>();
            RoomViewportMask mask = camera.GetComponent<RoomViewportMask>();
            Assert.That(level.CurrentRoomId, Is.EqualTo("R1"));
            Assert.That(camera.HasBounds, Is.True);
            Assert.That(mask.HasRoom, Is.True);
            Assert.That(mask.CurrentRoom.center, Is.EqualTo(camera.ActiveBounds.center));
            level.EnterRoom("R7");
            yield return null;
            Assert.That(mask.CurrentRoom.size.x, Is.LessThan(21.34f));
            Camera view = camera.GetComponent<Camera>();
            float halfWidth = view.orthographicSize * view.aspect;
            float halfHeight = view.orthographicSize;
            bool viewExtendsOutsideRoom = view.transform.position.x - halfWidth
                < mask.CurrentRoom.min.x - 0.001f
                || view.transform.position.x + halfWidth
                > mask.CurrentRoom.max.x + 0.001f
                || view.transform.position.y - halfHeight
                < mask.CurrentRoom.min.y - 0.001f
                || view.transform.position.y + halfHeight
                > mask.CurrentRoom.max.y + 0.001f;
            Assert.That(mask.GetComponentsInChildren<SpriteRenderer>()
                .Any(renderer => renderer.gameObject.activeSelf),
                Is.EqualTo(viewExtendsOutsideRoom));
            level.EnterRoom("R1");
            Assert.That(mask.CurrentRoom.center, Is.EqualTo(camera.ActiveBounds.center));
        }

        [UnityTest]
        public IEnumerator 进出Boss房与复活时使用各房间镜头尺寸()
        {
            StageFiveLevelController level = Object.FindFirstObjectByType<StageFiveLevelController>();
            Camera view = Object.FindFirstObjectByType<CameraFollow2D>()
                .GetComponent<Camera>();
            Assert.That(view.orthographicSize, Is.EqualTo(5f).Within(0.001f));
            level.EnterRoom("R8");
            Assert.That(view.orthographicSize, Is.EqualTo(5.5f).Within(0.001f));
            level.EnterRoom("R6");
            Assert.That(view.orthographicSize, Is.EqualTo(5f).Within(0.001f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator 检查点休息会恢复生命值()
        {
            yield return SceneManager.LoadSceneAsync(
                "Assets/_Project/Scenes/V2/EightRoomArtPreview.unity");
            yield return null;
            StageFiveLevelController level = Object.FindFirstObjectByType<StageFiveLevelController>();
            StageFiveCheckpoint checkpoint = Object.FindObjectsByType<StageFiveCheckpoint>(
                FindObjectsSortMode.None).First(item => item.RoomId == "R7");
            GameObject player = GameObject.Find("Player");
            HealthSystem health = player.GetComponent<HealthSystem>();
            level.EnterRoom("R7");
            player.transform.position = checkpoint.transform.position;
            Physics2D.SyncTransforms();
            Assert.That(health.TryTakeDamage(new DamageInfo(checkpoint.gameObject, 1,
                player.transform.position, Vector2.zero)), Is.True);
            Assert.That(health.CurrentHealth, Is.LessThan(health.MaxHealth));
            Assert.That(checkpoint.TryActivate(player), Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Boss战死亡后开门并停止跨房间索敌直到再次进入()
        {
            yield return SceneManager.LoadSceneAsync(
                "Assets/_Project/Scenes/V2/EightRoomArtPreview.unity");
            yield return null;
            StageFiveLevelController level = Object.FindFirstObjectByType<StageFiveLevelController>();
            StageFiveCheckpoint checkpoint = Object.FindObjectsByType<StageFiveCheckpoint>(
                FindObjectsSortMode.None).First(item => item.RoomId == "R7");
            StageFiveDoor bossExit = GameObject.Find("Door_R8_R6").GetComponent<StageFiveDoor>();
            Collider2D blocker = GameObject.Find("BossBattleBlocker_R8_R6")
                .GetComponent<Collider2D>();
            BossBrain boss = Object.FindFirstObjectByType<BossBrain>();
            GameObject player = GameObject.Find("Player");
            HealthSystem health = player.GetComponent<HealthSystem>();

            level.EnterRoom("R7");
            player.transform.position = checkpoint.transform.position;
            Physics2D.SyncTransforms();
            Assert.That(checkpoint.TryActivate(player), Is.True);
            level.EnterRoom("R8");
            player.transform.position = boss.transform.position + Vector3.right * 2f;
            Physics2D.SyncTransforms();
            boss.Advance(0.01f);
            Assert.That(boss.IsEngaged, Is.True);
            Assert.That(bossExit.IsBossBattleBlocked, Is.True);

            Assert.That(health.TryTakeDamage(new DamageInfo(boss.gameObject,
                health.MaxHealth, player.transform.position, Vector2.zero)), Is.True);
            boss.Advance(1f);
            Assert.That(boss.IsEngaged, Is.False, "角色死亡时 Boss 不应重新索敌");
            Assert.That(bossExit.IsBossBattleBlocked, Is.False);
            yield return null;
            Assert.That(blocker.enabled, Is.False, "死亡后场地实体门应打开");

            yield return new WaitForSeconds(1.4f);
            Assert.That(level.CurrentRoomId, Is.EqualTo("R7"));
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth));
            Assert.That(bossExit.IsBossBattleBlocked, Is.False);
            player.transform.position = boss.transform.position + Vector3.right * 2f;
            Physics2D.SyncTransforms();
            boss.Advance(1f);
            Assert.That(boss.IsEngaged, Is.False, "检查点所在房间不能触发 Boss 战");
            level.EnterRoom("R8");
            boss.Advance(0.01f);
            Assert.That(boss.IsEngaged, Is.True, "再次进入 Boss 房后应能重新开战");
            Assert.That(bossExit.IsBossBattleBlocked, Is.True);
        }

        [UnityTest]
        public IEnumerator Boss两阶段钟面扇形逐波发射且缺口边缘可斩()
        {
            StageFiveLevelController level = Object.FindFirstObjectByType<StageFiveLevelController>();
            level.EnterRoom("R8");
            BossBrain boss = Object.FindFirstObjectByType<BossBrain>();
            BossProjectilePool pool = Object.FindFirstObjectByType<BossProjectilePool>();
            GameObject player = GameObject.Find("Player");
            player.transform.position = boss.transform.position + Vector3.right * 2f;
            Physics2D.SyncTransforms();

            boss.Advance(0.71f);
            Assert.That(boss.CurrentAttack, Is.EqualTo(BossAttackType.ClockFan));
            boss.Advance(0.86f);
            int gap = boss.PatternGapStart;
            Assert.That(boss.TryGetPatternShot(0, gap - 1, out _, out Vector2 leftGap,
                out bool leftBreakable), Is.True);
            Assert.That(boss.TryGetPatternShot(0, gap, out _, out Vector2 rightGap,
                out bool rightBreakable), Is.True);
            Assert.That(Vector2.Angle(leftGap, rightGap), Is.GreaterThan(14f));
            Assert.That(leftBreakable && rightBreakable, Is.True);
            boss.Advance(0.15f);
            Assert.That(pool.TotalShotsFired, Is.EqualTo(28));
            for (int wave = 1; wave < 5; wave++) boss.Advance(0.41f);
            Assert.That(pool.TotalShotsFired, Is.EqualTo(140));

            Assert.That(boss.GetComponent<HealthSystem>().TryTakeDamage(
                new DamageInfo(player, 48, boss.transform.position, Vector2.zero)), Is.True);
            Assert.That(boss.Phase, Is.EqualTo(BossPhase.Two));
            boss.Advance(0.91f);
            boss.Advance(0.61f);
            Assert.That(boss.CurrentAttack, Is.EqualTo(BossAttackType.ClockFan));
            boss.Advance(0.71f);
            gap = boss.PatternGapStart;
            Assert.That(boss.TryGetPatternShot(0, gap - 1, out _, out leftGap,
                out leftBreakable), Is.True);
            Assert.That(boss.TryGetPatternShot(0, gap, out _, out rightGap,
                out rightBreakable), Is.True);
            Assert.That(Vector2.Angle(leftGap, rightGap), Is.GreaterThan(15f));
            Assert.That(leftBreakable && rightBreakable, Is.True);
            boss.Advance(0.15f);
            Assert.That(pool.TotalShotsFired, Is.EqualTo(176));
            for (int wave = 1; wave < 7; wave++) boss.Advance(0.33f);
            Assert.That(pool.TotalShotsFired, Is.EqualTo(392));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Boss普通弹幕在时停中暂停且恢复后不补发积压波次()
        {
            StageFiveLevelController level = Object.FindFirstObjectByType<StageFiveLevelController>();
            level.EnterRoom("R8");
            BossBrain boss = Object.FindFirstObjectByType<BossBrain>();
            BossProjectilePool pool = Object.FindFirstObjectByType<BossProjectilePool>();
            WorldTimeService world = Object.FindFirstObjectByType<WorldTimeService>();
            GameObject player = GameObject.Find("Player");
            player.transform.position = boss.transform.position + Vector3.right * 2f;
            Physics2D.SyncTransforms();
            boss.Advance(0.71f);
            boss.Advance(1.01f);
            Assert.That(pool.TotalShotsFired, Is.EqualTo(28));

            player.transform.position = boss.transform.position + Vector3.right * 100f;
            Physics2D.SyncTransforms();
            TimeGauge gauge = player.GetComponent<TimeGauge>();
            world.UnlockTimeStop();
            gauge.Fill();
            Assert.That(world.TryStartTimeStop(gauge), Is.True);
            yield return new WaitForSeconds(0.2f);
            Assert.That(pool.TotalShotsFired, Is.EqualTo(28));

            world.EndTimeStopEarly();
            boss.Advance(0.41f);
            Assert.That(pool.TotalShotsFired, Is.EqualTo(56));
        }

        [UnityTest]
        public IEnumerator 落雨和交叉弹幕具有可通行缺口且整轮数量准确()
        {
            StageFiveLevelController level = Object.FindFirstObjectByType<StageFiveLevelController>();
            level.EnterRoom("R8");
            BossBrain boss = Object.FindFirstObjectByType<BossBrain>();
            BossProjectilePool pool = Object.FindFirstObjectByType<BossProjectilePool>();
            GameObject player = GameObject.Find("Player");
            player.transform.position = boss.transform.position + Vector3.right * 2f;
            Physics2D.SyncTransforms();

            AdvanceUntilAttack(boss, BossAttackType.Rain);
            int before = pool.TotalShotsFired;
            boss.Advance(0.91f);
            Assert.That(boss.PatternShotCount, Is.EqualTo(30));
            int gap = boss.PatternGapStart;
            Assert.That(boss.TryGetPatternShot(0, gap - 1, out Vector2 leftOrigin,
                out _, out bool leftBreakable), Is.True);
            Assert.That(boss.TryGetPatternShot(0, gap, out Vector2 rightOrigin,
                out _, out bool rightBreakable), Is.True);
            Assert.That(rightOrigin.x - leftOrigin.x, Is.GreaterThan(1.3f));
            Assert.That(leftBreakable && rightBreakable, Is.True);
            for (int row = 1; row < 6; row++) boss.Advance(0.56f);
            Assert.That(pool.TotalShotsFired - before, Is.EqualTo(180));

            AdvanceUntilAttack(boss, BossAttackType.Crossfire);
            before = pool.TotalShotsFired;
            Assert.That(boss.TryGetPatternShot(0, 0, out leftOrigin,
                out _, out _), Is.True);
            Assert.That(boss.TryGetPatternShot(1, 0, out rightOrigin,
                out _, out _), Is.True);
            Assert.That(Mathf.Abs(rightOrigin.x - leftOrigin.x), Is.GreaterThan(10f));
            boss.Advance(0.91f);
            for (int group = 1; group < 8; group++) boss.Advance(0.39f);
            Assert.That(pool.TotalShotsFired - before, Is.EqualTo(112));
            Assert.That(pool.FailedShots, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 同一刀斩碎多枚Boss蓝弹最多获得十六点能量()
        {
            StageFiveLevelController level = Object.FindFirstObjectByType<StageFiveLevelController>();
            level.EnterRoom("R8");
            BossProjectilePool pool = Object.FindFirstObjectByType<BossProjectilePool>();
            GameObject player = GameObject.Find("Player");
            player.transform.position = new Vector3(80f, 12f, 0f);
            Physics2D.SyncTransforms();
            PlayerCombat combat = player.GetComponent<PlayerCombat>();
            TimeGauge gauge = player.GetComponent<TimeGauge>();
            float before = gauge.CurrentValue;
            Assert.That(combat.BeginAttack(AttackDirection.Horizontal), Is.True);
            Vector2 center = combat.CurrentAttackCenter;
            for (int index = 0; index < 20; index++)
                Assert.That(pool.TryFire(center, Vector2.right, 0.01f, true, false), Is.True);
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(0.12f);
            Assert.That(pool.ActiveCount, Is.Zero);
            Assert.That(gauge.CurrentValue - before, Is.EqualTo(16f).Within(0.2f));
        }

        [UnityTest]
        public IEnumerator 二阶段落雨交叉弹幕和时间锚数量正确()
        {
            StageFiveLevelController level = Object.FindFirstObjectByType<StageFiveLevelController>();
            level.EnterRoom("R8");
            BossBrain boss = Object.FindFirstObjectByType<BossBrain>();
            BossProjectilePool pool = Object.FindFirstObjectByType<BossProjectilePool>();
            GameObject player = GameObject.Find("Player");
            player.transform.position = boss.transform.position + Vector3.right * 2f;
            Physics2D.SyncTransforms();
            Assert.That(boss.GetComponent<HealthSystem>().TryTakeDamage(
                new DamageInfo(player, 48, boss.transform.position, Vector2.zero)), Is.True);
            Assert.That(boss.Phase, Is.EqualTo(BossPhase.Two));
            boss.Advance(0.91f);
            AdvanceUntilAttack(boss, BossAttackType.Rain);
            int before = pool.TotalShotsFired;
            boss.Advance(0.76f);
            for (int row = 1; row < 8; row++) boss.Advance(0.46f);
            Assert.That(pool.TotalShotsFired - before, Is.EqualTo(304));

            AdvanceUntilAttack(boss, BossAttackType.Crossfire);
            before = pool.TotalShotsFired;
            boss.Advance(0.76f);
            for (int group = 1; group < 12; group++) boss.Advance(0.31f);
            Assert.That(pool.TotalShotsFired - before, Is.EqualTo(219));
            Assert.That(pool.FailedShots, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Boss弹幕期间镜头拉远且重置后恢复R8尺寸()
        {
            StageFiveLevelController level = Object.FindFirstObjectByType<StageFiveLevelController>();
            level.EnterRoom("R8");
            BossBrain boss = Object.FindFirstObjectByType<BossBrain>();
            Camera view = Object.FindFirstObjectByType<CameraFollow2D>().GetComponent<Camera>();
            GameObject player = GameObject.Find("Player");
            player.transform.position = boss.transform.position + Vector3.right * 2f;
            Physics2D.SyncTransforms();
            boss.Advance(0.71f);
            yield return new WaitForSeconds(0.5f);
            Assert.That(view.orthographicSize, Is.EqualTo(6.5f).Within(0.05f));
            boss.ResetEncounter();
            yield return new WaitForSeconds(0.5f);
            Assert.That(view.orthographicSize, Is.EqualTo(5.5f).Within(0.05f));
        }

        private static void AdvanceUntilAttack(BossBrain boss, BossAttackType expected)
        {
            for (int step = 0; step < 360 && boss.CurrentAttack != expected; step++)
                boss.Advance(0.1f);
            Assert.That(boss.CurrentAttack, Is.EqualTo(expected));
        }

        [UnityTest]
        public IEnumerator 攻击有效段前请求Dash会在有效段结束后取消攻击()
        {
            GameObject player = GameObject.Find("Player");
            PlayerCombat combat = player.GetComponent<PlayerCombat>();
            PlayerDash dash = player.GetComponent<PlayerDash>();
            Assert.That(combat.BeginAttack(AttackDirection.Horizontal), Is.True);
            yield return new WaitForSeconds(0.08f);
            Assert.That(dash.RequestDash(), Is.True);
            Assert.That(dash.IsDashing, Is.False);
            float deadline = Time.time + 0.35f;
            while (!dash.IsDashing && Time.time < deadline) yield return null;
            Assert.That(dash.IsDashing, Is.True);
            Assert.That(combat.AttackHitbox.IsActive, Is.False);
            Assert.That(combat.AttackCooldownRemaining, Is.GreaterThan(0f));
            Assert.That(dash.BufferedDashRemaining, Is.Zero);
        }

        [UnityTest]
        public IEnumerator 飞行怪靠近玩家后实际位移受击闪白并继续追踪()
        {
            StageFiveLevelController level = Object.FindFirstObjectByType<StageFiveLevelController>();
            level.EnterRoom("R3");
            GameObject player = GameObject.Find("Player");
            player.GetComponent<PlayerMovement>().enabled = false;
            player.GetComponent<PlayerDash>().enabled = false;
            Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
            playerBody.constraints = RigidbodyConstraints2D.FreezeAll;
            V2FlyingEnemy enemy = GameObject.Find("R3_Flying_A").GetComponent<V2FlyingEnemy>();
            Rigidbody2D enemyBody = enemy.GetComponent<Rigidbody2D>();
            enemyBody.position = new Vector2(1000f, 10f);
            enemy.transform.position = enemyBody.position;
            playerBody.position = new Vector2(1003f, 10f);
            player.transform.position = playerBody.position;
            enemy.BindTarget(player.transform);
            Physics2D.SyncTransforms();
            float before = Vector2.Distance(enemyBody.position, playerBody.position);
            yield return new WaitForSeconds(0.35f);
            Assert.That(enemy.IsChasing, Is.True);
            Assert.That(Vector2.Distance(enemyBody.position, playerBody.position), Is.LessThan(before - 0.25f));
            Vector2 hitPoint = enemyBody.position;
            Assert.That(enemy.GetComponent<HealthSystem>().TryTakeDamage(new DamageInfo(
                player, 1, hitPoint, Vector2.left * 3f)), Is.True);
            Assert.That(enemy.State, Is.EqualTo(FlyingEnemyState.Hurt));
            yield return null;
            Assert.That(enemy.GetComponent<SpriteRenderer>().color, Is.EqualTo(Color.white));
            yield return new WaitForSeconds(0.4f);
            Assert.That(enemy.State, Is.EqualTo(FlyingEnemyState.Chase));
        }

        [UnityTest]
        public IEnumerator Boss横扫使用独立有效框且一次攻击只伤一次()
        {
            StageFiveLevelController level = Object.FindFirstObjectByType<StageFiveLevelController>();
            level.EnterRoom("R8");
            BossBrain boss = Object.FindFirstObjectByType<BossBrain>();
            GameObject player = GameObject.Find("Player");
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            player.GetComponent<PlayerMovement>().enabled = false;
            player.GetComponent<PlayerDash>().enabled = false;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
            body.position = (Vector2)boss.transform.position + new Vector2(2.8f, 0f);
            player.transform.position = body.position;
            Physics2D.SyncTransforms();
            boss.enabled = false;
            HealthSystem health = player.GetComponent<HealthSystem>();
            int before = health.CurrentHealth;
            boss.Advance(0.71f);
            for (int index = 0; index < 200 && boss.CurrentAttack != BossAttackType.Sweep;
                index++) boss.Advance(0.1f);
            Assert.That(boss.CurrentAttack, Is.EqualTo(BossAttackType.Sweep));
            boss.Advance(0.69f);
            Assert.That(health.CurrentHealth, Is.EqualTo(before), "前摇没有刀光伤害");
            boss.Advance(0.02f);
            BossAttackHitbox2D sweep = boss.transform.Find("SweepHitbox")
                .GetComponent<BossAttackHitbox2D>();
            Assert.That(sweep.IsActive, Is.True);
            Assert.That(sweep.WorldSize.x, Is.EqualTo(3.5f).Within(0.02f));
            Assert.That(health.CurrentHealth, Is.EqualTo(before - 1));
            boss.Advance(0.02f);
            Assert.That(health.CurrentHealth, Is.EqualTo(before - 1));
            boss.Advance(0.15f);
            Assert.That(sweep.IsActive, Is.False);
            yield return null;
        }
    }
}
