using System.Collections;
using GasSafari.Characters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class FartChargerTests
    {
        private RaftTestRig rig;
        private ScriptedCommandSource input;
        private GasTank gas;
        private FartThruster thruster;
        private FartCharger charger;
        private Rigidbody raftBody;
        private float lastImpulse;
        private FartType lastType;
        private int farts;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            input = rig.AddAvatar(180f);
            gas = input.GetComponent<GasTank>();
            thruster = input.GetComponent<FartThruster>();
            charger = input.GetComponent<FartCharger>();
            raftBody = rig.Get<Rigidbody>();
            farts = 0;
            thruster.Farted += (type, impulse) => { farts++; lastType = type; lastImpulse = impulse; };
            yield return new WaitForSeconds(0.5f);
        }

        [TearDown]
        public void TearDown()
        {
            rig.Destroy();
        }

        [Test]
        public void ImpulseFor_IsLinearAndCapped()
        {
            var b = rig.Balance;
            Assert.AreEqual(b.chargedImpulseMin, charger.ImpulseFor(b.chargeTimeMin), 0.001f);
            Assert.AreEqual((b.chargedImpulseMin + b.chargedImpulseMax) / 2f,
                charger.ImpulseFor((b.chargeTimeMin + b.chargeTimeMax) / 2f), 0.001f);
            Assert.AreEqual(b.chargedImpulseMax, charger.ImpulseFor(b.chargeTimeMax + 1f), 0.001f);
        }

        [UnityTest]
        public IEnumerator HoldThenFart_FiresChargedImpulse()
        {
            input.Command.HoldHeld = true;
            yield return new WaitForSeconds(1f);
            input.Command.FartPresses++;
            yield return null;
            yield return new WaitForFixedUpdate();

            Assert.AreEqual(1, farts);
            Assert.AreEqual(FartType.Charged, lastType);
            Assert.AreEqual(75f, lastImpulse, 5f);
            Assert.AreEqual(gas.Capacity - rig.Balance.chargedCost, gas.Gas, 0.01f);
            Assert.Greater(raftBody.velocity.z, 3f);
        }

        [UnityTest]
        public IEnumerator ReleaseHoldWithoutFart_LosesChargeAndKeepsGas()
        {
            input.Command.HoldHeld = true;
            yield return new WaitForSeconds(1f);
            input.Command.HoldHeld = false;
            yield return new WaitForSeconds(0.2f);

            Assert.AreEqual(0, farts);
            Assert.AreEqual(gas.Capacity, gas.Gas, 0.01f);
            Assert.IsFalse(charger.IsCharging);
        }

        [UnityTest]
        public IEnumerator WhileCharging_NoPuffOrBlast()
        {
            input.Command.HoldHeld = true;
            yield return null;
            input.Command.FartHeld = true;
            yield return new WaitForSeconds(0.3f);

            Assert.AreEqual(0, farts, "antes de chargeTimeMin no dispara y no hay ráfaga");
            Assert.IsFalse(thruster.IsBlasting);

            input.Command.HoldHeld = false;
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(thruster.IsBlasting, "debe soltar Pedo antes de una nueva ráfaga");
        }
    }
}
