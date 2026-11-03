using System.Collections;
using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Input;
using GasSafari.Team;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class PingSystemTests
    {
        private RaftTestRig rig;
        private ScriptedCommandSource a;
        private ScriptedCommandSource b;
        private readonly List<PingType> pings = new List<PingType>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            a = rig.AddAvatar(150f);
            b = rig.AddAvatar(210f);
            pings.Clear();
            GameEvents.OnPing += Record;
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.OnPing -= Record;
            rig.Destroy();
        }

        private void Record(PlayerAvatar avatar, PingType type) => pings.Add(type);

        [UnityTest]
        public IEnumerator Ping_FiresOnPressAndRespectsCooldown()
        {
            a.Command.Ping = PingType.Wait;
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(1, pings.Count, "mantener no repite");

            a.Command.Ping = PingType.None;
            yield return null;
            a.Command.Ping = PingType.TurnLeft;
            yield return null;
            Assert.AreEqual(1, pings.Count, "en enfriamiento");

            a.Command.Ping = PingType.None;
            yield return new WaitForSeconds(rig.Balance.pingCooldown);
            a.Command.Ping = PingType.TurnLeft;
            yield return null;
            Assert.AreEqual(2, pings.Count);
            Assert.AreEqual(PingType.TurnLeft, pings[1]);
        }

        [UnityTest]
        public IEnumerator TwoNowPings_StartCountdownAndWidenSyncAtGo()
        {
            var ticks = new List<int>();
            var system = rig.Get<PingSystem>();
            system.CountdownTick += ticks.Add;
            var sync = rig.Get<SyncDetector>();
            float baseWindow = sync.Window;

            a.Command.Ping = PingType.Now;
            yield return new WaitForSeconds(0.5f);
            b.Command.Ping = PingType.Now;
            yield return null;
            Assert.IsTrue(system.IsCountingDown);

            yield return new WaitForSeconds(rig.Balance.pingCountdownStep * 3f + 0.05f);
            CollectionAssert.AreEqual(new[] { 3, 2, 1, 0 }, ticks);
            Assert.AreEqual(baseWindow + rig.Balance.pingSyncBonus, sync.Window, 1e-4f);

            yield return new WaitForSeconds(sync.Window + 0.1f);
            Assert.AreEqual(baseWindow, sync.Window, 1e-4f);
        }

        [UnityTest]
        public IEnumerator LowGas_SendsNoGasPingOnce()
        {
            a.GetComponent<GasTank>().SetFraction(rig.Balance.noGasPingThreshold * 0.5f);
            yield return null;
            yield return null;
            Assert.AreEqual(1, pings.Count);
            Assert.AreEqual(PingType.NoGas, pings[0]);
        }
    }
}
