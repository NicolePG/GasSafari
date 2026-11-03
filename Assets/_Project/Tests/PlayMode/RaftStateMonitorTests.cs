using System.Collections;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Raft;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class RaftStateMonitorTests
    {
        private RaftTestRig rig;
        private int capsizes;

        [SetUp]
        public void SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            capsizes = 0;
            GameEvents.OnRaftCapsized += CountCapsize;
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.OnRaftCapsized -= CountCapsize;
            rig.Destroy();
        }

        private void CountCapsize() => capsizes++;

        [UnityTest]
        public IEnumerator TRaft04_TiltAboveCapsizeAngle_CapsizesAfterCapsizeTime()
        {
            var lowSide = rig.AddAvatar(90f);
            var highSide = rig.AddAvatar(270f);
            var body = rig.Get<Rigidbody>();
            body.isKinematic = true;
            // Rodar sobre Z en negativo hunde el lado +X (90°).
            rig.Raft.transform.rotation = Quaternion.Euler(0f, 0f, -(rig.Balance.capsizeAngle + 10f));
            yield return new WaitForSeconds(rig.Balance.capsizeTime - 0.3f);
            Assert.AreEqual(0, capsizes);

            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(1, capsizes);
            Assert.IsFalse(lowSide.GetComponent<PlayerSeat>().IsSeated, "cae el del lado que se hunde");
            Assert.IsTrue(highSide.GetComponent<PlayerSeat>().IsSeated);
        }

        [UnityTest]
        public IEnumerator CrookedLanding_KnocksOffUnlessGrabbing()
        {
            var loose = rig.AddAvatar(180f);
            var grabber = rig.AddAvatar(0f);
            grabber.Command.GrabHeld = true;
            bool crookedLanding = false;
            rig.Get<RaftStateMonitor>().Landed += crooked => crookedLanding |= crooked;

            rig.Raft.transform.SetPositionAndRotation(new Vector3(0f, 4f, 0f), Quaternion.Euler(50f, 0f, 0f));
            var body = rig.Get<Rigidbody>();
            body.position = rig.Raft.transform.position;
            body.rotation = rig.Raft.transform.rotation;
            body.velocity = new Vector3(0f, -6f, 0f);
            body.angularVelocity = Vector3.zero;
            yield return new WaitForSeconds(1f);

            Assert.IsTrue(crookedLanding);
            Assert.IsFalse(loose.GetComponent<PlayerSeat>().IsSeated);
            Assert.IsTrue(grabber.GetComponent<PlayerSeat>().IsSeated);
        }

        [UnityTest]
        public IEnumerator Airborne_FartsUseAirControlFactor()
        {
            var input = rig.AddAvatar(180f);
            var body = rig.Get<Rigidbody>();
            body.useGravity = false;
            body.position = new Vector3(0f, 5f, 0f);
            body.velocity = Vector3.zero;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(rig.Get<RaftBuoyancy>().IsAirborne);

            input.Command.FartPresses++;
            yield return null;
            yield return new WaitForFixedUpdate();

            float puff = input.GetComponent<PlayerAvatar>().Character.puffImpulse;
            float expected = puff * rig.Balance.airControlFactor / body.mass;
            Assert.AreEqual(expected, body.velocity.z, expected * 0.1f);
        }
    }
}
