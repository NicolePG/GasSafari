using System.Collections;
using GasSafari.Core;
using GasSafari.Team;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class SuperFartTests
    {
        private RaftTestRig rig;
        private ScriptedCommandSource a;
        private ScriptedCommandSource b;
        private TeamMeter meter;
        private Rigidbody body;
        private float lastPower;
        private int launches;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            a = rig.AddAvatar(150f);
            b = rig.AddAvatar(210f);
            meter = rig.Get<TeamMeter>();
            body = rig.Get<Rigidbody>();
            launches = 0;
            GameEvents.OnSuperFart += OnSuper;
            yield return new WaitForSeconds(0.5f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameEvents.OnSuperFart -= OnSuper;
            yield return new WaitForSecondsRealtime(1f);
            rig.Destroy();
        }

        private void OnSuper(float power)
        {
            launches++;
            lastPower = power;
        }

        [UnityTest]
        public IEnumerator AllPress_FullPowerLaunchAndSlowMotion()
        {
            meter.Set(meter.Max);
            a.Command.SuperHeld = true;
            b.Command.SuperHeld = true;
            yield return null;
            yield return new WaitForFixedUpdate();

            Assert.AreEqual(1, launches);
            Assert.AreEqual(1f, lastPower, 0.001f);
            Assert.AreEqual(0f, meter.Value);
            Assert.AreEqual(rig.Balance.superFartForward, body.velocity.z, 1.5f);
            Assert.AreEqual(rig.Balance.superFartUp, body.velocity.y, 1.5f);
            Assert.AreEqual(rig.Balance.superFartSlowMoScale, Time.timeScale, 0.001f);

            yield return new WaitForSecondsRealtime(rig.Balance.superFartSlowMoDuration + 0.1f);
            Assert.AreEqual(1f, Time.timeScale, 0.001f);
        }

        [UnityTest]
        public IEnumerator OnePress_HalfPowerAfterWindow()
        {
            meter.Set(meter.Max);
            a.Command.SuperHeld = true;
            yield return new WaitForSeconds(rig.Balance.superFartWindow * 0.5f);
            Assert.AreEqual(0, launches);

            yield return new WaitForSeconds(rig.Balance.superFartWindow * 0.6f);
            Assert.AreEqual(1, launches);
            Assert.AreEqual(0.5f, lastPower, 0.001f);
        }

        [UnityTest]
        public IEnumerator MeterNotFull_NoLaunch()
        {
            meter.Set(meter.Max - 1f);
            a.Command.SuperHeld = true;
            b.Command.SuperHeld = true;
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(0, launches);
        }
    }
}
