using System.Collections;
using GasSafari.Characters;
using GasSafari.Raft;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class AvatarStateMachineTests
    {
        private RaftTestRig rig;
        private ScriptedCommandSource input;
        private AvatarStateMachine states;
        private GasTank gas;
        private PlayerSeat seat;
        private FartCharger charger;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            input = rig.AddAvatar(180f);
            states = input.GetComponent<AvatarStateMachine>();
            gas = input.GetComponent<GasTank>();
            seat = input.GetComponent<PlayerSeat>();
            charger = input.GetComponent<FartCharger>();
            yield return new WaitForSeconds(0.3f);
        }

        [TearDown]
        public void TearDown()
        {
            rig.Destroy();
        }

        [UnityTest]
        public IEnumerator Grab_BlocksMovementAndFarts()
        {
            input.Command.GrabHeld = true;
            yield return null;
            Assert.AreEqual(AvatarState.Grabbing, states.State);

            input.Command.SeatMove = 1f;
            input.Command.FartPresses++;
            input.Command.FartHeld = true;
            yield return new WaitForSeconds(0.4f);

            Assert.AreEqual(180f, seat.Angle, 0.001f);
            Assert.AreEqual(gas.Capacity, gas.Gas, 0.001f);
            Assert.Less(rig.Get<Rigidbody>().velocity.magnitude, 0.2f);
        }

        [UnityTest]
        public IEnumerator ReleaseGrab_IsInstant()
        {
            input.Command.GrabHeld = true;
            yield return null;
            input.Command.GrabHeld = false;
            yield return null;
            Assert.AreEqual(AvatarState.Seated, states.State);
        }

        [UnityTest]
        public IEnumerator Hold_EntersCharging()
        {
            input.Command.HoldHeld = true;
            yield return null;
            Assert.AreEqual(AvatarState.Charging, states.State);
            Assert.IsTrue(charger.IsCharging);

            input.Command.HoldHeld = false;
            yield return null;
            Assert.AreEqual(AvatarState.Seated, states.State);
        }

        [UnityTest]
        public IEnumerator GrabDuringCharge_LosesCharge()
        {
            bool lost = false;
            charger.ChargeLost += () => lost = true;
            input.Command.HoldHeld = true;
            yield return new WaitForSeconds(0.7f);

            input.Command.GrabHeld = true;
            input.Command.FartPresses++;
            yield return null;
            yield return null;

            Assert.AreEqual(AvatarState.Grabbing, states.State);
            Assert.IsTrue(lost);
            Assert.AreEqual(gas.Capacity, gas.Gas, 0.001f);
        }
    }
}
