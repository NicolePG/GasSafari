using System.Collections;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Raft;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class CheckpointTests
    {
        private RaftTestRig rig;
        private CheckpointSystem system;
        private LevelTimer timer;
        private Checkpoint start;
        private Checkpoint second;
        private Checkpoint familiarExtra;
        private RaftChambers chambers;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            chambers = rig.Get<RaftChambers>();
            start = CreateCheckpoint(new Vector3(0f, 0.3f, 0f), false, true);
            familiarExtra = CreateCheckpoint(new Vector3(0f, 0.3f, 10f), true, false);
            second = CreateCheckpoint(new Vector3(0f, 0.3f, 20f), false, false);

            var go = new GameObject("TestLevel");
            go.SetActive(false);
            timer = go.AddComponent<LevelTimer>();
            system = go.AddComponent<CheckpointSystem>();
            system.Configure(rig.Balance, rig.Get<RaftController>(), new[] { start, familiarExtra, second });
            go.SetActive(true);
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            rig.Destroy();
            Object.Destroy(system.gameObject);
            Object.Destroy(start.gameObject);
            Object.Destroy(familiarExtra.gameObject);
            Object.Destroy(second.gameObject);
        }

        private static Checkpoint CreateCheckpoint(Vector3 position, bool familiarOnly, bool hardKeep)
        {
            var go = new GameObject("TestCheckpoint");
            go.SetActive(false);
            go.layer = GameLayers.Trigger;
            go.transform.position = position;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(12f, 6f, 1f);
            var cp = go.AddComponent<Checkpoint>();
            SetPrivate(cp, "familiarOnly", familiarOnly);
            SetPrivate(cp, "hardKeep", hardKeep);
            go.SetActive(true);
            return cp;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            target.GetType().GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(target, value);
        }

        private IEnumerator PassThrough(Checkpoint cp)
        {
            var body = rig.Get<Rigidbody>();
            body.position = cp.transform.position + Vector3.back * 2f;
            body.velocity = Vector3.forward * 5f;
            yield return new WaitForSeconds(0.8f);
        }

        private IEnumerator WaitRespawn()
        {
            float limit = rig.Balance.respawnFade + rig.Balance.sinkDuration + 1f;
            float t = 0f;
            while (t < limit && (system.IsRespawning || chambers.IsSinking))
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        [Test]
        public void Filter_FollowsDifficulty()
        {
            system.ApplyFilter(CheckpointFilter.NormalPlusFamiliarOnly);
            Assert.AreEqual(3, system.Active.Count);
            system.ApplyFilter(CheckpointFilter.Normal);
            Assert.AreEqual(2, system.Active.Count);
            system.ApplyFilter(CheckpointFilter.HardKeepOnly);
            Assert.AreEqual(1, system.Active.Count);
            Assert.AreSame(start, system.Active[0]);
        }

        [UnityTest]
        public IEnumerator TCp01_RespawnKeepsChambersFromCheckpointPass()
        {
            chambers.PopChamber(0);
            yield return PassThrough(second);
            Assert.AreSame(second, system.Current);

            chambers.PopChamber(1);
            system.RequestRespawn();
            yield return WaitRespawn();

            Assert.IsFalse(chambers.IsIntact(0), "seguía reventada al pasar el checkpoint");
            Assert.IsTrue(chambers.IsIntact(1), "se reventó después: vuelve");
            Assert.AreEqual(second.transform.position.z, rig.Raft.transform.position.z, 0.5f);
        }

        [UnityTest]
        public IEnumerator TRaft03_FourPoppedChambers_SinkThenRespawnAtCheckpoint()
        {
            int respawns = 0;
            GameEvents.OnRespawn += () => respawns++;
            for (int i = 0; i < RaftChambers.CHAMBER_COUNT; i++)
                chambers.PopChamber(i);
            Assert.IsTrue(chambers.IsSinking);

            yield return new WaitForSeconds(rig.Balance.sinkDuration + 0.1f);
            Assert.IsTrue(system.IsRespawning);
            yield return WaitRespawn();

            Assert.AreEqual(1, respawns);
            Assert.IsFalse(chambers.IsSinking);
            Assert.AreEqual(RaftChambers.CHAMBER_COUNT, chambers.IntactCount);
            Assert.Less(rig.Get<Rigidbody>().velocity.magnitude, 0.5f);
            Assert.AreEqual(Services.Session.Difficulty.respawnPenalty, timer.PenaltyTime, 0.001f);
        }

        // T-RAFT-04 (docs/11 §2): vuelco de 70° durante 1.5 s → reaparición.
        [UnityTest]
        public IEnumerator TRaft04_CapsizeHeld_RespawnsAtCheckpointUpright()
        {
            var body = rig.Get<Rigidbody>();
            body.isKinematic = true;
            rig.Raft.transform.rotation = Quaternion.Euler(0f, 0f, rig.Balance.capsizeAngle + 5f);

            float t = 0f;
            while (!system.IsRespawning && t < rig.Balance.capsizeTime + 1f)
            {
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(system.IsRespawning, "debió pedir reaparición");
            Assert.AreEqual(rig.Balance.capsizeTime, t, 0.2f);

            body.isKinematic = false;
            yield return WaitRespawn();

            Assert.Less(Vector3.Angle(rig.Raft.transform.up, Vector3.up), 5f);
            Assert.AreEqual(start.transform.position.z, rig.Raft.transform.position.z, 0.5f);
            Assert.IsFalse(rig.Get<RaftStateMonitor>().HasCapsized);
        }

        [UnityTest]
        public IEnumerator Respawn_ReturnsCrewWithFullGas()
        {
            var input = rig.AddAvatar(180f);
            var gas = input.GetComponent<GasTank>();
            gas.SetFraction(0.2f);
            input.GetComponent<AvatarFall>().Fall(new Vector3(0f, 0f, -3f));
            yield return new WaitForSeconds(0.5f);

            system.RequestRespawn();
            yield return WaitRespawn();

            Assert.IsTrue(input.GetComponent<PlayerSeat>().IsSeated);
            Assert.AreEqual(1f, gas.Normalized, 0.01f);
        }
    }
}
