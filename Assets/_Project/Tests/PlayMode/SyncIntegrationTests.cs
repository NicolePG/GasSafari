using System.Collections;
using GasSafari.Core;
using GasSafari.Team;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class SyncIntegrationTests
    {
        private RaftTestRig rig;
        private ScriptedCommandSource left;
        private ScriptedCommandSource right;
        private int syncEvents;
        private int lastSyncSize;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            left = rig.AddAvatar(210f);
            right = rig.AddAvatar(150f);
            syncEvents = 0;
            GameEvents.OnSync += CountSync;
            yield return new WaitForSeconds(0.5f);
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.OnSync -= CountSync;
            rig.Destroy();
        }

        private void CountSync(int players, float multiplier)
        {
            syncEvents++;
            lastSyncSize = players;
        }

        [UnityTest]
        public IEnumerator TwoPuffsTogether_FormGroupAndFillMeter()
        {
            left.Command.FartPresses++;
            right.Command.FartPresses++;
            yield return null;
            yield return new WaitForFixedUpdate();

            Assert.AreEqual(1, syncEvents);
            Assert.AreEqual(2, lastSyncSize);
            Assert.AreEqual(10f, rig.Get<TeamMeter>().Value, 0.001f);
        }

        [UnityTest]
        public IEnumerator TRaft02_TwoSyncedRearBlasts_KeepHeading()
        {
            float startYaw = rig.Raft.transform.eulerAngles.y;
            left.Command.FartHeld = true;
            right.Command.FartHeld = true;
            yield return new WaitForSeconds(2f);
            left.Command.FartHeld = false;
            right.Command.FartHeld = false;

            float deviation = Mathf.Abs(Mathf.DeltaAngle(startYaw, rig.Raft.transform.eulerAngles.y));
            Assert.Less(deviation, 5f);
            Assert.Greater(rig.Get<Rigidbody>().velocity.z, 3f);
            Assert.AreEqual(1, syncEvents);
        }
    }
}
