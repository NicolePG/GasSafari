using System.Collections;
using GasSafari.Core;
using GasSafari.Raft;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class RaftChambersTests
    {
        private RaftTestRig rig;
        private RaftChambers chambers;
        private int poppedEvents;
        private int sunkEvents;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.3f, 0f));
            chambers = rig.Get<RaftChambers>();
            poppedEvents = 0;
            sunkEvents = 0;
            GameEvents.OnChamberPopped += CountPopped;
            GameEvents.OnRaftSunk += CountSunk;
            yield return new WaitForFixedUpdate();
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.OnChamberPopped -= CountPopped;
            GameEvents.OnRaftSunk -= CountSunk;
            rig.Destroy();
        }

        private void CountPopped(int chamber) => poppedEvents++;
        private void CountSunk() => sunkEvents++;

        [TestCase(1f, 1f, 0)]
        [TestCase(-1f, 1f, 1)]
        [TestCase(1f, -1f, 2)]
        [TestCase(-1f, -1f, 3)]
        public void PopChamberAt_PopsQuadrant(float x, float z, int expected)
        {
            var point = rig.Raft.transform.TransformPoint(new Vector3(x, 0f, z));
            Assert.IsTrue(chambers.PopChamberAt(point));
            Assert.IsFalse(chambers.IsIntact(expected));
            Assert.AreEqual(3, chambers.IntactCount);
            Assert.AreEqual(1, poppedEvents);
        }

        [Test]
        public void Shield_AbsorbsNextDamage()
        {
            chambers.GiveShield();
            Assert.IsFalse(chambers.PopChamber(0));
            Assert.IsTrue(chambers.IsIntact(0));
            Assert.IsFalse(chambers.HasShield);
            Assert.IsTrue(chambers.PopChamber(0));
        }

        [UnityTest]
        public IEnumerator AllPopped_SinksAndRaisesRaftSunk()
        {
            float startY = rig.Raft.transform.position.y;
            for (int i = 0; i < RaftChambers.CHAMBER_COUNT; i++)
                chambers.PopChamber(i);

            Assert.IsTrue(chambers.IsSinking);
            yield return new WaitForSeconds(rig.Balance.sinkDuration + 0.2f);

            Assert.AreEqual(1, sunkEvents);
            Assert.Less(rig.Raft.transform.position.y, startY);
        }

        [UnityTest]
        public IEnumerator SlowImpactOnWall_DoesNotPopChamber()
        {
            var difficulty = Services.Session.Difficulty;
            float original = difficulty.chamberDamageSpeed;
            bool impacts = difficulty.impactsDamageChambers;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.layer = GameLayers.Environment;
            wall.transform.position = new Vector3(4f, 0f, 0f);
            wall.transform.localScale = new Vector3(1f, 3f, 10f);
            try
            {
                difficulty.impactsDamageChambers = true;
                difficulty.chamberDamageSpeed = 7f;
                rig.Get<Rigidbody>().velocity = new Vector3(3f, 0f, 0f);
                yield return new WaitForSeconds(0.8f);
                Assert.AreEqual(0, poppedEvents, "docs/02 §8: un roce a 3 m/s no revienta cámaras (umbral 7 m/s)");
            }
            finally
            {
                Object.Destroy(wall);
                difficulty.chamberDamageSpeed = original;
                difficulty.impactsDamageChambers = impacts;
            }
        }

        [UnityTest]
        public IEnumerator HardImpactOnWall_PopsChamberOnThatSide()
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.layer = GameLayers.Environment;
            wall.transform.position = new Vector3(4f, 0f, 0f);
            wall.transform.localScale = new Vector3(1f, 3f, 10f);

            rig.Get<Rigidbody>().velocity = new Vector3(10f, 0f, 0f);
            yield return new WaitForSeconds(0.5f);
            Object.Destroy(wall);

            Assert.AreEqual(1, poppedEvents);
            Assert.IsFalse(chambers.IsIntact(0) && chambers.IsIntact(2), "debe reventar una cámara del lado este");
        }

        [Test]
        public void SetState_RestoresSavedChambers()
        {
            for (int i = 0; i < RaftChambers.CHAMBER_COUNT; i++)
                chambers.PopChamber(i);

            chambers.SetState(0b0101);

            Assert.IsFalse(chambers.IsSinking);
            Assert.IsTrue(chambers.IsIntact(0));
            Assert.IsFalse(chambers.IsIntact(1));
            Assert.IsTrue(chambers.IsIntact(2));
            Assert.IsFalse(chambers.IsIntact(3));
        }
    }
}
