using System.Collections;
using GasSafari.Abilities;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.PowerUps;
using GasSafari.Raft;
using GasSafari.Team;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class JulienTests
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
        public IEnumerator Party_EveryFartIsFullGroup_MeterDoubled()
        {
            var julien = rig.AddAvatar(90f, "Julien");
            var a = rig.AddAvatar(180f);
            rig.AddAvatar(270f);
            var party = (RoyalPartyAbility)julien.GetComponent<AbilityRunner>().Ability;
            var meter = rig.Get<TeamMeter>();
            int lastSize = 0;
            float lastMultiplier = 0f;
            GameEvents.OnSync += (size, mult) => { lastSize = size; lastMultiplier = mult; };
            yield return null;

            julien.Command.AbilityPresses++;
            yield return null;
            Assert.IsTrue(rig.Get<SyncDetector>().IsPartyActive);

            a.Command.FartPresses++;
            yield return null;
            Assert.AreEqual(3, lastSize);
            Assert.AreEqual(rig.Balance.syncMultipliers[3], lastMultiplier, 1e-5f);
            Assert.AreEqual(rig.Balance.syncMeterGain[3] * party.meterMultiplier, meter.Value, 1e-4f);

            yield return new WaitForSeconds(party.duration + 0.1f);
            Assert.IsFalse(rig.Get<SyncDetector>().IsPartyActive);
            lastSize = 0;
            float before = meter.Value;
            a.Command.FartPresses++;
            yield return null;
            Assert.AreEqual(0, lastSize, "fuera de la fiesta un pedo solo no es sync");
            Assert.AreEqual(before, meter.Value, 1e-4f);
        }

        [UnityTest]
        public IEnumerator Leap_LandsFarIntoWater_CountsAsFallOnlyOnLanding()
        {
            var julien = rig.AddAvatar(90f, "Julien");
            int falls = 0;
            GameEvents.OnPlayerFell += _ => falls++;
            yield return new WaitForSeconds(0.2f);

            julien.Command.JumpPresses++;
            yield return null;
            var fall = julien.GetComponent<AvatarFall>();
            Assert.IsTrue(fall.IsLeaping);
            Assert.AreEqual(0, falls, "saltar no es caer");

            yield return new WaitForSeconds(1.5f);
            Assert.IsFalse(fall.IsLeaping);
            Assert.IsTrue(fall.IsOverboard);
            Assert.AreEqual(1, falls);
            Assert.AreEqual(rig.Balance.julienJumpDistance,
                rig.Get<RaftSeats>().DistanceToEdge(julien.transform.position), 0.8f);
        }

        [UnityTest]
        public IEnumerator Leap_ReturnsWhenRaftCatchesUp_AndRespectsCooldown()
        {
            var julien = rig.AddAvatar(0f, "Julien");
            int falls = 0;
            GameEvents.OnPlayerFell += _ => falls++;
            yield return new WaitForSeconds(0.2f);

            julien.Command.JumpPresses++;
            yield return null;
            rig.Get<Rigidbody>().velocity = new Vector3(0f, 0f, 8f);
            yield return new WaitForSeconds(1.5f);

            Assert.IsTrue(julien.GetComponent<PlayerSeat>().IsSeated, "la balsa lo alcanzó: vuelve solo");
            Assert.AreEqual(0, falls);

            julien.Command.JumpPresses++;
            yield return null;
            Assert.IsTrue(julien.GetComponent<PlayerSeat>().IsSeated, "en recarga");
        }

        [UnityTest]
        public IEnumerator Magnet_ActiveOnlyWhileOnBoard()
        {
            var julien = rig.AddAvatar(90f, "Julien");
            yield return null;
            var near = julien.transform.position + Vector3.forward * (rig.Balance.julienMagnetRadius - 1f);
            var far = julien.transform.position + Vector3.forward * (rig.Balance.julienMagnetRadius + 1f);

            Assert.IsTrue(PowerUpMagnets.TryFind(near, out var target));
            Assert.AreSame(julien.transform, target);
            Assert.IsFalse(PowerUpMagnets.TryFind(far, out _));

            julien.GetComponent<AvatarFall>().Fall(new Vector3(3f, 0f, 0f));
            Assert.IsFalse(PowerUpMagnets.TryFind(near, out _));
        }
    }
}
