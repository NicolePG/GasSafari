using System.Collections;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Hazards;
using GasSafari.Raft;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class HazardBaseTests
    {
        private class TestHazard : HazardBase
        {
            public int Enters, Stays, Exits, Hits, Warns, WarningEnds;
            public bool Damaged;

            protected override void OnRaftEnter(RaftController raft) => Enters++;
            protected override void OnRaftStay(RaftController raft) => Stays++;
            protected override void OnRaftExit(RaftController raft) => Exits++;
            protected override void OnAvatarHit(PlayerAvatar avatar) => Hits++;
            protected override void OnWarn() => Warns++;
            protected override void OnWarningEnded() => WarningEnds++;

            public void Damage(RaftController raft, Vector3 point) => Damaged = DamageRaft(raft, point);
            public void Push(RaftController raft, Vector3 impulse) => PushRaft(raft, impulse, raft.transform.position);
        }

        private RaftTestRig rig;
        private TestHazard hazard;
        private HazardConfig config;

        [SetUp]
        public void SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, -6f));
            config = ScriptableObject.CreateInstance<HazardConfig>();
            config.warningTime = 0.6f;

            var go = new GameObject("Hazard");
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(4f, 4f, 4f);
            go.transform.position = new Vector3(0f, 0f, 0f);
            go.AddComponent<Rigidbody>().isKinematic = true;
            hazard = go.AddComponent<TestHazard>();
            hazard.Configure(config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(hazard.gameObject);
            Object.Destroy(config);
            rig.Destroy();
        }

        [UnityTest]
        public IEnumerator RaftTrigger_RaisesEnterStayExit()
        {
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(0, hazard.Enters);

            rig.Get<Rigidbody>().position = Vector3.up * 0.2f;
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(1, hazard.Enters);
            Assert.Greater(hazard.Stays, 0);

            rig.Get<Rigidbody>().position = new Vector3(0f, 0.2f, -8f);
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(1, hazard.Exits);
        }

        [UnityTest]
        public IEnumerator Warn_RaisesEventOnce_ThenActsAfterWarningTime()
        {
            HazardBase warned = null;
            int events = 0;
            GameEvents.OnHazardWarning += h => { warned = h; events++; };

            hazard.Warn();
            hazard.Warn();
            Assert.AreEqual(1, events);
            Assert.AreSame(hazard, warned);
            Assert.AreEqual(1, hazard.Warns);
            Assert.IsTrue(hazard.IsWarning);

            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(0, hazard.WarningEnds, "todavía avisando");
            Assert.AreEqual(0.5f, hazard.WarningProgress, 0.25f);

            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(1, hazard.WarningEnds);
            Assert.IsFalse(hazard.IsWarning);
        }

        [UnityTest]
        public IEnumerator CancelWarning_PreventsAction()
        {
            hazard.Warn();
            hazard.CancelWarning();
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(0, hazard.WarningEnds);
        }

        [UnityTest]
        public IEnumerator Damage_PopsChamber_UnlessDifficultyBlocksImpacts()
        {
            yield return null;
            var raft = rig.Get<RaftController>();
            var chambers = rig.Get<RaftChambers>();
            var difficulty = Services.Session.Difficulty;
            bool original = difficulty.impactsDamageChambers;
            try
            {
                difficulty.impactsDamageChambers = false;
                hazard.Damage(raft, raft.transform.position + Vector3.forward);
                Assert.IsFalse(hazard.Damaged);
                Assert.AreEqual(RaftChambers.CHAMBER_COUNT, chambers.IntactCount);

                config.alwaysDamagesChambers = true;
                hazard.Damage(raft, raft.transform.position + Vector3.forward);
                Assert.IsTrue(hazard.Damaged);
                Assert.AreEqual(RaftChambers.CHAMBER_COUNT - 1, chambers.IntactCount);
            }
            finally
            {
                difficulty.impactsDamageChambers = original;
            }
        }

        [UnityTest]
        public IEnumerator Push_AppliesImpulseToRaft()
        {
            yield return new WaitForSeconds(0.3f);
            var body = rig.Get<Rigidbody>();
            float before = body.velocity.z;
            hazard.Push(rig.Get<RaftController>(), Vector3.forward * 20f);
            yield return new WaitForFixedUpdate();
            Assert.Greater(body.velocity.z, before + 0.5f);
        }
    }
}
