using System.Linq;
using NUnit.Framework;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Level;
using TimeMetroidvania.Player;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TimeMetroidvania.Tests
{
    public sealed class SevenRoomSliceTests
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/Prototype/SevenRoomSlice.unity";

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Assert.That(GameObject.Find("Player").GetComponent<HealthSystem>().IsFullHealth,
                Is.True);
        }

        [Test]
        public void 七房间场景包含完整主线可选房间和三个检查点()
        {
            Transform rooms = GameObject.Find("Stage5_SevenRooms").transform;
            Assert.That(rooms.childCount, Is.EqualTo(7));
            StageFiveRoom[] roomMetadata =
                Object.FindObjectsByType<StageFiveRoom>(FindObjectsSortMode.None);
            Assert.That(roomMetadata.Length, Is.EqualTo(7));
            Assert.That(roomMetadata.Single(room => room.RoomId == "R4").AreaId,
                Is.EqualTo("A"));
            Assert.That(roomMetadata.Single(room => room.RoomId == "R6").AreaId,
                Is.EqualTo("B"));
            Assert.That(roomMetadata.Single(room => room.RoomId == "R7").AreaId,
                Is.EqualTo("C"));
            Assert.That(Object.FindObjectsByType<StageFiveDoor>(FindObjectsSortMode.None).Length,
                Is.EqualTo(13));
            Assert.That(Object.FindObjectsByType<StageFivePickup>(FindObjectsSortMode.None).Length,
                Is.EqualTo(2));

            string[] checkpointIds = Object
                .FindObjectsByType<StageFiveCheckpoint>(FindObjectsSortMode.None)
                .Select(checkpoint => checkpoint.CheckpointId)
                .OrderBy(id => id)
                .ToArray();
            Assert.That(checkpointIds, Is.EqualTo(new[]
            {
                "CP_R1_Start", "CP_R4_Return", "CP_R7_Boss"
            }));

            WorldTimeService world = Object.FindFirstObjectByType<WorldTimeService>();
            Assert.That(world.IsTimeStopUnlocked, Is.False);
            Assert.That(world.HasDurationUpgrade, Is.False);
            Assert.That(GameObject.Find("Boss_TimeWarden"), Is.Not.Null);
        }

        [Test]
        public void 主线入口是墙体真实缺口且锁定路线有实体挡板()
        {
            Transform roomOne = GameObject.Find("Room_01").transform;
            Transform roomSix = GameObject.Find("Room_06").transform;
            Assert.That(roomOne.Find("LeftWall_Upper"), Is.Not.Null);
            Assert.That(roomOne.Find("RightWall_Upper"), Is.Not.Null);
            Assert.That(roomOne.Find("RightWall"), Is.Null);
            Assert.That(roomSix.Find("RightWall"), Is.Not.Null);

            StageFiveDoor openEntrance = GameObject.Find("Door_R1_R2")
                .GetComponent<StageFiveDoor>();
            Assert.That(openEntrance.GetComponent<SpriteRenderer>().enabled, Is.False);
            Assert.That(openEntrance.GetComponent<BoxCollider2D>().isTrigger, Is.True);
            Physics2D.SyncTransforms();
            Collider2D[] contacts = Physics2D.OverlapPointAll(
                new Vector2(openEntrance.transform.position.x, -1.15f));
            Assert.That(contacts.Any(collider => !collider.isTrigger), Is.False);

            Transform blocker = GameObject.Find("Door_R1_R4_Gated")
                .transform.Find("LockedBlocker");
            Assert.That(blocker, Is.Not.Null);
            Assert.That(blocker.GetComponent<Collider2D>().enabled, Is.True);
        }

        [Test]
        public void 房间三解锁后必须返回旧区域并用时停开启新路线()
        {
            GameObject player = GameObject.Find("Player");
            WorldTimeService world = Object.FindFirstObjectByType<WorldTimeService>();
            TimeGauge gauge = player.GetComponent<TimeGauge>();
            StageFiveLevelController level =
                Object.FindFirstObjectByType<StageFiveLevelController>();
            StageFiveDoor returnDoor =
                GameObject.Find("Door_R3_Return").GetComponent<StageFiveDoor>();

            player.transform.position = returnDoor.transform.position;
            Assert.That(returnDoor.TryTravel(player), Is.False);
            Assert.That(GameObject.Find("Pickup_TimeStop")
                .GetComponent<StageFivePickup>().TryCollect(player), Is.True);
            Assert.That(world.IsTimeStopUnlocked, Is.True);
            Assert.That(gauge.CurrentValue, Is.EqualTo(60f));
            Assert.That(returnDoor.TryTravel(player), Is.True);
            Assert.That(level.CurrentRoomId, Is.EqualTo("R1"));

            StageFiveDoor gateDoor =
                GameObject.Find("Door_R1_R4_Gated").GetComponent<StageFiveDoor>();
            player.transform.position = gateDoor.transform.position;
            Assert.That(gateDoor.TryTravel(player), Is.False);

            StageFiveTimeGate gate =
                GameObject.Find("TimeStopGate_R1_R4").GetComponent<StageFiveTimeGate>();
            player.transform.position = gate.transform.position;
            Assert.That(world.TryStartTimeStop(gauge), Is.True);
            Assert.That(gate.TryOpen(), Is.True);
            player.transform.position = gateDoor.transform.position;
            Assert.That(gateDoor.TryTravel(player), Is.True);
            Assert.That(level.CurrentRoomId, Is.EqualTo("R4"));
        }

        [Test]
        public void 隐藏升级可选且房间五可直接前往Boss入口()
        {
            GameObject player = GameObject.Find("Player");
            StageFiveLevelController level =
                Object.FindFirstObjectByType<StageFiveLevelController>();
            WorldTimeService world = Object.FindFirstObjectByType<WorldTimeService>();
            StageFiveDoor hidden =
                GameObject.Find("Door_R5_Hidden_R6").GetComponent<StageFiveDoor>();
            StageFiveDoor boss =
                GameObject.Find("Door_R5_R7").GetComponent<StageFiveDoor>();

            player.transform.position = boss.transform.position;
            Assert.That(boss.TryTravel(player), Is.True);
            Assert.That(level.CurrentRoomId, Is.EqualTo("R7"));
            Assert.That(world.HasDurationUpgrade, Is.False);

            player.transform.position = hidden.transform.position;
            Assert.That(hidden.TryTravel(player), Is.True);
            Assert.That(level.CurrentRoomId, Is.EqualTo("R6"));
            Assert.That(GameObject.Find("Pickup_SevenSecondUpgrade")
                .GetComponent<StageFivePickup>().TryCollect(player), Is.True);
            Assert.That(world.HasDurationUpgrade, Is.True);
            Assert.That(world.TimeStopMaximum, Is.EqualTo(7f));
        }

        [Test]
        public void 检查点只更新本次运行的复活位置()
        {
            GameObject player = GameObject.Find("Player");
            StageFiveLevelController level =
                Object.FindFirstObjectByType<StageFiveLevelController>();
            StageFiveCheckpoint checkpoint =
                GameObject.Find("CP_R4_Return").GetComponent<StageFiveCheckpoint>();

            player.transform.position = new Vector3(
                checkpoint.transform.position.x, -1.15f, 0f);
            Assert.That(checkpoint.TryActivate(player), Is.True);
            Assert.That(level.ActiveCheckpointId, Is.EqualTo("CP_R4_Return"));
            Assert.That(checkpoint.RoomId, Is.EqualTo("R4"));
            Assert.That(player.GetComponent<HealthSystem>(), Is.Not.Null);
        }
    }
}
