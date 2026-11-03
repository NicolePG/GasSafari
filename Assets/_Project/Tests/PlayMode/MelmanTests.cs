using System.Collections;
using GasSafari.Abilities;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Raft;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class MelmanTests
    {
        private readonly System.Collections.Generic.List<GameObject> anchors = new System.Collections.Generic.List<GameObject>();
        private RaftTestRig rig;
        private ScriptedCommandSource melman;
        private NeckHookAbility hook;

        private GameObject CreateAnchor(Vector3 position)
        {
            var go = new GameObject("TestAnchor");
            go.transform.position = position;
            go.AddComponent<Anchor>();
            anchors.Add(go);
            return go;
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            melman = rig.AddAvatar(0f, "Melman");
            hook = (NeckHookAbility)melman.GetComponent<AbilityRunner>().Ability;
            yield return new WaitForSeconds(0.2f);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in anchors)
                Object.DestroyImmediate(go);
            anchors.Clear();
            rig.Destroy();
        }

        [UnityTest]
        public IEnumerator Hook_PullsSwimmerBackAsRescue()
        {
            var mate = rig.AddAvatar(180f);
            PlayerAvatar rescuer = null;
            GameEvents.OnPlayerRescued += (who, by) => rescuer = by;
            mate.GetComponent<AvatarFall>().Fall(new Vector3(0f, 0f, -4f));
            yield return new WaitForSeconds(1f);
            Assert.IsFalse(mate.GetComponent<PlayerSeat>().IsSeated);

            melman.Command.AbilityPresses++;
            yield return new WaitForSeconds(hook.pullTime + 0.15f);

            Assert.IsTrue(mate.GetComponent<PlayerSeat>().IsSeated);
            Assert.AreSame(melman.GetComponent<PlayerAvatar>(), rescuer);
        }

        [UnityTest]
        public IEnumerator Hook_WithoutTargets_DoesNotActivate()
        {
            melman.Command.AbilityPresses++;
            yield return null;
            Assert.IsTrue(melman.GetComponent<AbilityRunner>().Cooldown.IsReady);
        }

        [UnityTest]
        public IEnumerator Hook_TethersToAnchor_KeepsDistance_ReleasesOnButtonUp()
        {
            // Al costado: al avanzar la cuerda se tensa enseguida y la balsa pivota.
            var anchorGo = CreateAnchor(new Vector3(6f, 0.2f, 0f));
            var body = rig.Get<Rigidbody>();
            body.velocity = new Vector3(0f, 0f, 4f);

            melman.Command.AbilityHeld = true;
            melman.Command.AbilityPresses++;
            yield return null;
            var tether = rig.Raft.GetComponent<HookTether>();
            Assert.IsNotNull(tether);
            float start = Vector3.Distance(body.worldCenterOfMass, anchorGo.transform.position);

            yield return new WaitForSeconds(1f);
            Assert.LessOrEqual(Vector3.Distance(body.worldCenterOfMass, anchorGo.transform.position), start + 0.15f);
            Assert.Greater(Mathf.Abs(body.velocity.x), 0.3f, $"pivota alrededor del ancla (v={body.velocity})");

            melman.Command.AbilityHeld = false;
            yield return null;
            yield return null;
            Assert.IsNull(rig.Raft.GetComponent<HookTether>());
            Assert.IsNull(rig.Raft.GetComponent<ConfigurableJoint>());
        }

        [UnityTest]
        public IEnumerator Hook_ReleasesAfterMaxTime()
        {
            CreateAnchor(new Vector3(5f, 0.2f, 0f));
            melman.Command.AbilityHeld = true;
            melman.Command.AbilityPresses++;
            yield return new WaitForSeconds(hook.maxHookTime + 0.2f);
            Assert.IsNull(rig.Raft.GetComponent<HookTether>());
        }

        [UnityTest]
        public IEnumerator Medkit_HoldRepairsOncePerCheckpoint()
        {
            var chambers = rig.Get<RaftChambers>();
            chambers.PopChamber(0);
            melman.Command.AbilityHeld = true;
            melman.Command.AbilityPresses++;
            yield return new WaitForSeconds(rig.Balance.medkitHoldTime + 0.1f);
            Assert.AreEqual(RaftChambers.CHAMBER_COUNT, chambers.IntactCount, "reparó");

            melman.Command.AbilityHeld = false;
            yield return null;
            chambers.PopChamber(1);
            melman.Command.AbilityHeld = true;
            melman.Command.AbilityPresses++;
            yield return new WaitForSeconds(rig.Balance.medkitHoldTime + 0.1f);
            Assert.IsFalse(chambers.IsIntact(1), "ya usado en este checkpoint");

            melman.Command.AbilityHeld = false;
            yield return null;
            GameEvents.RaiseCheckpoint(1);
            melman.Command.AbilityHeld = true;
            melman.Command.AbilityPresses++;
            yield return new WaitForSeconds(rig.Balance.medkitHoldTime + 0.1f);
            Assert.IsTrue(chambers.IsIntact(1), "nuevo checkpoint, nuevo uso");
        }

        [UnityTest]
        public IEnumerator Lookout_AddsWarningLeadWhileOnBoard()
        {
            var effects = rig.Get<RaftEffects>();
            Assert.AreEqual(rig.Balance.lookoutWarningLead, effects.WarningLeadBonus, 1e-5f);
            melman.GetComponent<AvatarFall>().Fall(new Vector3(0f, 0f, 4f));
            yield return null;
            Assert.AreEqual(0f, effects.WarningLeadBonus, 1e-5f);
        }
    }
}
