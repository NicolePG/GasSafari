using System.Collections;
using GasSafari.Characters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class FartThrusterTests
    {
        private RaftTestRig rig;
        private ScriptedCommandSource input;
        private GasTank gas;
        private FartThruster thruster;
        private Rigidbody raftBody;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            input = rig.AddAvatar(180f);
            gas = input.GetComponent<GasTank>();
            thruster = input.GetComponent<FartThruster>();
            raftBody = rig.Get<Rigidbody>();
            yield return new WaitForSeconds(0.5f);
        }

        [TearDown]
        public void TearDown()
        {
            rig.Destroy();
        }

        [UnityTest]
        public IEnumerator Puff_PushesTowardBowAndCostsGas()
        {
            input.Command.FartHeld = true;
            yield return null;
            input.Command.FartHeld = false;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.Greater(raftBody.velocity.z, 1f);
            Assert.AreEqual(gas.Capacity - rig.Balance.puffCost, gas.Gas, 0.01f);
        }

        [UnityTest]
        public IEnumerator TapBetweenFrames_StillPuffs()
        {
            input.Command.FartPresses++;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.Greater(raftBody.velocity.z, 1f);
            Assert.AreEqual(gas.Capacity - rig.Balance.puffCost, gas.Gas, 0.01f);
        }

        [UnityTest]
        public IEnumerator Blast_HeldOneSecond_DrainsGasAndAccelerates()
        {
            input.Command.FartHeld = true;
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(thruster.IsBlasting);
            input.Command.FartHeld = false;
            yield return null;

            Assert.IsFalse(thruster.IsBlasting);
            Assert.Greater(raftBody.velocity.z, 3f);
            float expectedSpent = rig.Balance.blastCostPerSecond * (1f - rig.Balance.puffMaxPressTime);
            Assert.AreEqual(gas.Capacity - expectedSpent, gas.Gas, 4f);
        }

        [UnityTest]
        public IEnumerator AimRight_TurnsRaftRight()
        {
            input.Command.Aim = 1f;
            input.Command.FartHeld = true;
            yield return new WaitForSeconds(1f);
            input.Command.FartHeld = false;

            Assert.Greater(raftBody.angularVelocity.y, 0.1f);
        }

        [UnityTest]
        public IEnumerator NoGas_CoughsWithoutThrust()
        {
            int coughs = 0;
            thruster.Coughed += () => coughs++;
            gas.SetFraction(0f);
            input.Command.FartHeld = true;
            yield return null;
            input.Command.FartHeld = false;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.AreEqual(1, coughs);
            Assert.IsTrue(thruster.IsCoughing);
            Assert.Less(raftBody.velocity.magnitude, 0.2f);
        }
    }
}
