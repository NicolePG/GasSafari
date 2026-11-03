using System.Collections;
using System.Collections.Generic;
using GasSafari.Abilities;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Hazards;
using GasSafari.Level;
using GasSafari.Raft;
using GasSafari.Team;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class SynergyTests
    {
        private readonly List<GameObject> props = new List<GameObject>();
        private readonly List<string> synergies = new List<string>();
        private RaftTestRig rig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            synergies.Clear();
            GameEvents.OnSynergy += (id, first) => synergies.Add(id);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var go in props)
                Object.DestroyImmediate(go);
            props.Clear();
            rig.Destroy();
            yield return new WaitForSecondsRealtime(0.8f);
        }

        [UnityTest]
        public IEnumerator RoyalRescue_MelmanHooksJulien_FullGas()
        {
            var melman = rig.AddAvatar(0f, "Melman");
            var julien = rig.AddAvatar(180f, "Julien");
            julien.GetComponent<GasTank>().SetFraction(0.2f);
            julien.GetComponent<AvatarFall>().Fall(new Vector3(0f, 0f, -4f));
            yield return new WaitForSeconds(1f);

            melman.Command.AbilityPresses++;
            yield return new WaitForSeconds(0.7f);

            Assert.IsTrue(julien.GetComponent<PlayerSeat>().IsSeated);
            Assert.AreEqual(1f, julien.GetComponent<GasTank>().Normalized, 0.01f);
            CollectionAssert.Contains(synergies, AbilitySynergySystem.ROYAL_RESCUE);
        }

        [UnityTest]
        public IEnumerator Earthquake_RoarAndBellyFlopTogether_BreakHeavy()
        {
            var alex = rig.AddAvatar(180f, "Alex");
            var gloria = rig.AddAvatar(90f, "Gloria");
            var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            props.Add(rock);
            rock.layer = GameLayers.Hazard;
            rock.transform.position = new Vector3(-5f, 0.5f, 0f);
            rock.GetComponent<Collider>().isTrigger = true;
            rock.AddComponent<HazardTags>().Configure(HazardTag.Heavy);
            yield return null;

            alex.Command.AbilityPresses++;
            yield return null;
            Assert.IsTrue(rock.activeSelf, "el rugido solo no rompe lo Heavy");

            gloria.Command.AbilityPresses++;
            yield return null;
            Assert.IsFalse(rock.activeSelf);
            CollectionAssert.Contains(synergies, AbilitySynergySystem.EARTHQUAKE);
        }

        [UnityTest]
        public IEnumerator PartyTotal_SuperFartDuringParty_Is150Percent()
        {
            var julien = rig.AddAvatar(150f, "Julien");
            var mate = rig.AddAvatar(210f);
            julien.Command.AbilityPresses++;
            yield return null;
            Assert.IsTrue(rig.Get<SyncDetector>().IsPartyActive);

            var meter = rig.Get<TeamMeter>();
            meter.Set(meter.Max);
            julien.Command.SuperHeld = true;
            mate.Command.SuperHeld = true;
            yield return null;
            yield return new WaitForFixedUpdate();

            var expected = rig.Balance.superFartForward * rig.Balance.partySuperFartPower;
            Assert.AreEqual(expected, rig.Get<Rigidbody>().velocity.z, 2f);
            CollectionAssert.Contains(synergies, AbilitySynergySystem.PARTY);
        }

        [UnityTest]
        public IEnumerator HeavyDuo_CloseSeatsInSameSync_BoostAndNoTilt()
        {
            var alex = rig.AddAvatar(150f, "Alex");
            var gloria = rig.AddAvatar(200f, "Gloria");
            var effects = rig.Get<RaftEffects>();
            var raft = rig.Get<RaftController>();
            yield return null;

            alex.Command.FartPresses++;
            gloria.Command.FartPresses++;
            yield return null;

            Assert.AreEqual(rig.Balance.heavyDuoForceMultiplier, effects.FartForceMultiplier, 1e-5f);
            Assert.AreEqual(0f, raft.WeightTiltScale);
            CollectionAssert.Contains(synergies, AbilitySynergySystem.HEAVY_DUO);

            yield return new WaitForSeconds(rig.Balance.heavyDuoDuration + 0.1f);
            Assert.AreEqual(1f, effects.FartForceMultiplier, 1e-5f);
            Assert.AreEqual(1f, raft.WeightTiltScale);
        }

        [UnityTest]
        public IEnumerator HeavyDuo_SeatsTooFar_NoEffect()
        {
            var alex = rig.AddAvatar(90f, "Alex");
            var gloria = rig.AddAvatar(210f, "Gloria");
            yield return null;

            alex.Command.FartPresses++;
            gloria.Command.FartPresses++;
            yield return null;

            Assert.AreEqual(1f, rig.Get<RaftEffects>().FartForceMultiplier, 1e-5f);
            CollectionAssert.DoesNotContain(synergies, AbilitySynergySystem.HEAVY_DUO);
        }

        [UnityTest]
        public IEnumerator Sling_GallopDuringHook_FasterRelease()
        {
            var melman = rig.AddAvatar(0f, "Melman");
            var marty = rig.AddAvatar(180f, "Marty");
            var anchor = new GameObject("TestAnchor");
            props.Add(anchor);
            anchor.transform.position = new Vector3(6f, 0.2f, 0f);
            anchor.AddComponent<Anchor>();
            var body = rig.Get<Rigidbody>();
            body.velocity = new Vector3(0f, 0f, 4f);

            melman.Command.AbilityHeld = true;
            melman.Command.AbilityPresses++;
            yield return null;
            var tether = rig.Raft.GetComponent<HookTether>();
            Assert.IsNotNull(tether);

            marty.Command.AbilityPresses++;
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(rig.Balance.slingReleaseMultiplier, tether.ReleaseSpeedMultiplier, 1e-5f);
            CollectionAssert.Contains(synergies, AbilitySynergySystem.SLING);

            float before = body.velocity.magnitude;
            melman.Command.AbilityHeld = false;
            yield return null;
            Assert.AreEqual(rig.Balance.slingReleaseMultiplier, body.velocity.magnitude / before, 0.15f);
        }
    }
}
