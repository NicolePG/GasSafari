using System.Collections;
using GasSafari.Abilities;
using GasSafari.Characters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class MartyTests
    {
        private RaftTestRig rig;

        [SetUp]
        public void SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
        }

        [TearDown]
        public void TearDown()
        {
            rig.Destroy();
        }

        [UnityTest]
        public IEnumerator Gallop_PushesAlongThrust_HalvesWaterDrag_WithoutGas()
        {
            var marty = rig.AddAvatar(180f, "Marty");
            var runner = marty.GetComponent<AbilityRunner>();
            var gallop = (GallopAbility)runner.Ability;
            var body = rig.Get<Rigidbody>();
            var states = marty.GetComponent<AvatarStateMachine>();
            string log = "";
            runner.AbilityPressed += ok => log += $"pulsó(activó={ok}, estado={states.State}) ";
            yield return new WaitForSeconds(0.3f);

            int frame = Time.frameCount;
            marty.Command.AbilityPresses++;
            yield return null;
            string afterPress = $"{log}| frames={Time.frameCount - frame} cmd={marty.GetComponent<PlayerAvatar>().Command.AbilityPresses} lista={runner.Cooldown.IsReady} estado={states.State} ts={Time.timeScale}";
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            float expected = gallop.impulse / body.mass;
            Assert.AreEqual(expected, body.velocity.z, expected * 0.15f, "impulso hacia la proa " + afterPress);
            Assert.AreEqual(rig.Balance.waterDrag * gallop.waterDragMultiplier, body.drag, 1e-4f);
            var gas = marty.GetComponent<GasTank>();
            Assert.AreEqual(gas.Capacity, gas.Gas, 0.01f, "sin costo de gas");

            yield return new WaitForSeconds(gallop.duration + 0.1f);
            Assert.AreEqual(rig.Balance.waterDrag, body.drag, 1e-4f);
        }

        [UnityTest]
        public IEnumerator Gallop_FollowsAim()
        {
            var marty = rig.AddAvatar(180f, "Marty");
            marty.Command.Aim = 1f;
            yield return new WaitForSeconds(0.3f);
            marty.Command.AbilityPresses++;
            yield return null;
            yield return new WaitForFixedUpdate();

            Assert.Less(rig.Get<Rigidbody>().velocity.x, -0.5f, "puntería a la derecha empuja la popa a la izquierda");
            Assert.Greater(rig.Get<Rigidbody>().angularVelocity.y, 0.1f, "y gira la balsa a la derecha");
        }
    }
}
