using System.Collections;
using GasSafari.Characters;
using GasSafari.Raft;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class PlayerSeatTests
    {
        private RaftTestRig rig;

        [SetUp]
        public void SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.3f, 0f));
        }

        [TearDown]
        public void TearDown()
        {
            rig.Destroy();
        }

        [Test]
        public void Sit_AddsWeightToRaftMass()
        {
            var source = rig.AddAvatar(180f);
            float weight = source.GetComponent<PlayerAvatar>().Character.weight;
            Assert.AreEqual(rig.Balance.raftBaseMass + weight, rig.Get<Rigidbody>().mass, 0.001f);
        }

        [UnityTest]
        public IEnumerator Move_TurnsAtSeatMoveSpeed()
        {
            var source = rig.AddAvatar(180f);
            var seat = source.GetComponent<PlayerSeat>();
            source.Command.SeatMove = 1f;
            float start = Time.time;
            yield return new WaitForSeconds(0.25f);
            source.Command.SeatMove = 0f;
            float elapsed = Time.time - start;
            yield return null;

            float expected = 180f - rig.Balance.seatMoveSpeed * elapsed;
            Assert.AreEqual(expected, seat.Angle, rig.Balance.seatMoveSpeed * 0.05f);
        }

        [UnityTest]
        public IEnumerator Move_StopsAtMinimumSeparation()
        {
            rig.AddAvatar(150f);
            var mover = rig.AddAvatar(210f);
            mover.Command.SeatMove = 1f;
            yield return new WaitForSeconds(1f);

            float expected = 150f + rig.Balance.minSeatSeparation;
            Assert.AreEqual(expected, mover.GetComponent<PlayerSeat>().Angle, 0.01f);
        }

        [Test]
        public void Leave_RemovesWeight()
        {
            var source = rig.AddAvatar(90f);
            source.GetComponent<PlayerSeat>().Leave();
            Assert.AreEqual(rig.Balance.raftBaseMass, rig.Get<Rigidbody>().mass, 0.001f);
            Assert.AreEqual(0, rig.Get<RaftSeats>().Seats.Count);
        }
    }
}
