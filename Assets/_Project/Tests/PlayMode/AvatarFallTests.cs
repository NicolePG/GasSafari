using System.Collections;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Raft;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class AvatarFallTests
    {
        private RaftTestRig rig;
        private ScriptedCommandSource input;
        private AvatarFall fall;
        private AvatarStateMachine states;
        private PlayerSeat seat;
        private GasTank gas;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            input = rig.AddAvatar(180f);
            fall = input.GetComponent<AvatarFall>();
            states = input.GetComponent<AvatarStateMachine>();
            seat = input.GetComponent<PlayerSeat>();
            gas = input.GetComponent<GasTank>();
            yield return new WaitForSeconds(0.3f);
        }

        [TearDown]
        public void TearDown()
        {
            rig.Destroy();
        }

        [UnityTest]
        public IEnumerator TFall01_RespawnsAfterAutoRespawnTimeWithHalfGas()
        {
            float limit = Services.Session.Difficulty.autoRespawnTime;
            fall.Fall(new Vector3(0f, 0f, -2f));
            Assert.AreEqual(AvatarState.Falling, states.State);
            float start = Time.time;

            while (!seat.IsSeated && Time.time - start < limit + 2f)
                yield return null;

            Assert.IsTrue(seat.IsSeated);
            Assert.AreEqual(limit, Time.time - start, 0.25f);
            Assert.AreEqual(rig.Balance.respawnGasFraction, gas.Normalized, 0.05f);
            Assert.AreEqual(AvatarState.Seated, states.State);
        }

        [Test]
        public void Knockoff_RespectsThresholdAndGrab()
        {
            Assert.IsFalse(fall.Knockoff(rig.Balance.knockoffImpulse, 0f));
            Assert.IsTrue(seat.IsSeated);

            states.Enter(AvatarState.Grabbing);
            Assert.IsFalse(fall.Knockoff(rig.Balance.knockoffImpulse * 10f, 0f));
            Assert.IsTrue(seat.IsSeated);

            states.Enter(AvatarState.Seated);
            Assert.IsTrue(fall.Knockoff(rig.Balance.knockoffImpulse + 1f, 0f));
            Assert.IsFalse(seat.IsSeated);
        }

        [UnityTest]
        public IEnumerator Fallen_SwimsAtSwimSpeed()
        {
            fall.Fall(new Vector3(-3f, 0f, -3f));
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(AvatarState.Swimming, states.State);

            input.Command.Swim = new Vector2(-1f, 0f);
            yield return new WaitForSeconds(0.3f);
            var velocity = input.GetComponent<Rigidbody>().velocity;
            Assert.AreEqual(rig.Balance.swimSpeed, new Vector2(velocity.x, velocity.z).magnitude, 0.2f);
        }

        [UnityTest]
        public IEnumerator Swimmer_NearEdge_ClimbsWithJump()
        {
            fall.Fall(new Vector3(0f, 0f, -1.5f));
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(AvatarState.Swimming, states.State);
            Assert.LessOrEqual(fall.HomeRaft.DistanceToEdge(input.transform.position), rig.Balance.climbReach);

            input.Command.JumpPresses++;
            yield return null;
            Assert.IsTrue(seat.IsSeated);
            Assert.AreEqual(180f, seat.Angle, 10f);
        }

        [UnityTest]
        public IEnumerator RaftRunningOverSwimmer_RescuesIt()
        {
            fall.Fall(new Vector3(0f, 0f, -4f));
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(AvatarState.Swimming, states.State);

            bool rescued = false;
            GameEvents.OnPlayerRescued += (who, by) => rescued |= who == input.GetComponent<PlayerAvatar>();
            var toSwimmer = input.transform.position - rig.Raft.transform.position;
            toSwimmer.y = 0f;
            rig.Get<Rigidbody>().velocity = toSwimmer.normalized * (rig.Balance.swimSpeed * 2f);
            yield return new WaitForSeconds(1.5f);

            Assert.IsTrue(rescued);
            Assert.IsTrue(seat.IsSeated);
        }

        [UnityTest]
        public IEnumerator KnockedOntoDeck_SitsAgain()
        {
            fall.Fall(new Vector3(0f, 3f, 4f));
            yield return new WaitForSeconds(1.5f);
            Assert.IsTrue(seat.IsSeated);
            Assert.AreEqual(AvatarState.Seated, states.State);
        }

        [UnityTest]
        public IEnumerator HardImpact_KnocksOffUnlessGrabbing()
        {
            var grabber = rig.AddAvatar(90f);
            grabber.Command.GrabHeld = true;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.layer = GameLayers.Environment;
            wall.transform.position = new Vector3(0f, 0f, 4f);
            wall.transform.localScale = new Vector3(10f, 3f, 1f);
            yield return null;

            rig.Get<Rigidbody>().velocity = new Vector3(0f, 0f, 6f);
            yield return new WaitForSeconds(0.5f);
            Object.Destroy(wall);

            Assert.IsFalse(seat.IsSeated, "el que no se agarra cae");
            Assert.IsTrue(grabber.GetComponent<PlayerSeat>().IsSeated, "el agarrado no cae");
        }
    }
}
