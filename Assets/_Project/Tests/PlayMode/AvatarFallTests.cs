using System.Collections;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Raft;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class AvatarFallTests
    {
        private RaftTestRig rig;
        private ScriptedCommandSource input;
        private AvatarFall fall;
        private AvatarStateMachine states;
        private PlayerSeat seat;
        private GasTank gas;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            input = rig.AddAvatar(180f);
            fall = input.GetComponent<AvatarFall>();
            states = input.GetComponent<AvatarStateMachine>();
            seat = input.GetComponent<PlayerSeat>();
            gas = input.GetComponent<GasTank>();
            yield return new WaitForSeconds(0.3f);
        }

        [TearDown]
        public void TearDown()
        {
            rig.Destroy();
        }

        [UnityTest]
        public IEnumerator TFall01_RespawnsAfterAutoRespawnTimeWithHalfGas()
        {
            float limit = Services.Session.Difficulty.autoRespawnTime;
            fall.Fall(new Vector3(0f, 0f, -2f));
            Assert.AreEqual(AvatarState.Falling, states.State);
            float start = Time.time;

            while (!seat.IsSeated && Time.time - start < limit + 2f)
                yield return null;

            Assert.IsTrue(seat.IsSeated);
            Assert.AreEqual(limit, Time.time - start, 0.25f);
            Assert.AreEqual(rig.Balance.respawnGasFraction, gas.Normalized, 0.05f);
            Assert.AreEqual(AvatarState.Seated, states.State);
        }

        [Test]
        public void Knockoff_RespectsThresholdAndGrab()
        {
            Assert.AreEqual(6f, rig.Balance.knockoffSpeed, "docs/02 §14: solo caen con impactos de más de 6 m/s");
            Assert.IsFalse(fall.Knockoff(rig.Balance.knockoffSpeed, 0f));
            Assert.IsTrue(seat.IsSeated);

            states.Enter(AvatarState.Grabbing);
            Assert.IsFalse(fall.Knockoff(rig.Balance.knockoffSpeed * 10f, 0f));
            Assert.IsTrue(seat.IsSeated);

            states.Enter(AvatarState.Seated);
            Assert.IsTrue(fall.Knockoff(rig.Balance.knockoffSpeed + 1f, 0f));
            Assert.IsFalse(seat.IsSeated);
        }

        private GameObject BuildWall(Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.layer = GameLayers.Environment;
            wall.transform.position = position;
            wall.transform.localScale = scale;
            return wall;
        }

        // Choque de frente a velocidad normal (5 m/s): nadie cae.
        [UnityTest]
        public IEnumerator HeadOnBelowThreshold_DoesNotKnockOff()
        {
            var wall = BuildWall(new Vector3(0f, 0f, 4f), new Vector3(10f, 3f, 1f));
            yield return null;

            rig.Get<Rigidbody>().velocity = new Vector3(0f, 0f, 5f);
            yield return new WaitForSeconds(0.6f);
            Object.Destroy(wall);

            Assert.IsTrue(seat.IsSeated, "a 5 m/s nadie cae");
        }

        // Roce: la balsa corre a 10 m/s pegada a una pared lateral; la componente contra la pared es mínima.
        [UnityTest]
        public IEnumerator GrazingAlongWall_DoesNotKnockOff()
        {
            var wall = BuildWall(new Vector3(2.6f, 0f, 0f), new Vector3(1f, 3f, 60f));
            yield return null;

            rig.Get<Rigidbody>().velocity = new Vector3(0.5f, 0f, 10f);
            yield return new WaitForSeconds(0.8f);
            Object.Destroy(wall);

            Assert.IsTrue(seat.IsSeated, "un roce a 10 m/s no tira a nadie");
        }

        // El caído queda nadando cerca de la balsa tras un choque fuerte (no sale disparado).
        [UnityTest]
        public IEnumerator KnockedOff_SwimsBesideRaft()
        {
            var wall = BuildWall(new Vector3(0f, 0f, 4f), new Vector3(10f, 3f, 1f));
            yield return null;
            rig.Get<Rigidbody>().velocity = new Vector3(0f, 0f, 9f);
            yield return new WaitForSeconds(1.5f);
            Object.Destroy(wall);

            Assert.IsFalse(seat.IsSeated);
            Assert.AreEqual(AvatarState.Swimming, states.State);
            Assert.Less(fall.HomeRaft.DistanceToEdge(input.transform.position), 6f, "nada cerca de la balsa");
        }

        // Cayó fuera del agua (sin superficie debajo): se le pone junto a la balsa, nunca desaparece.
        [UnityTest]
        public IEnumerator FallenOutsideWater_IsPlacedBesideRaft()
        {
            Object.Destroy(rig.Water);
            yield return null;
            fall.Fall(new Vector3(0f, 2f, -3f));
            yield return new WaitForSeconds(2.2f);

            Assert.IsTrue(fall.IsOverboard);
            Assert.Less(fall.HomeRaft.DistanceToEdge(input.transform.position), 3f, "queda pegado a la balsa");
        }

        // Reaparición a los 6 s: instantánea, con rebote y sin fundido.
        [UnityTest]
        public IEnumerator AutoRespawn_IsInstantWithBounce()
        {
            fall.Fall(new Vector3(0f, 0f, -2f));
            float limit = Services.Session.Difficulty.autoRespawnTime;
            float start = Time.time;
            while (!seat.IsSeated && Time.time - start < limit + 2f)
                yield return null;

            Assert.IsTrue(seat.IsSeated, "vuelve a su asiento de golpe");
            float bounceSeen = 0f;
            float watch = Time.time + 1.2f;
            while (Time.time < watch)
            {
                bounceSeen = Mathf.Max(bounceSeen, seat.HopOffset);
                yield return null;
            }
            Assert.Greater(bounceSeen, 0.2f, "rebota al llegar");
            Assert.AreEqual(0f, seat.HopOffset, 0.001f, "y se queda quieto");
        }

        // Fuera de cámara aparece su icono en el borde de la pantalla.
        [UnityTest]
        public IEnumerator FallenOffCamera_ShowsEdgeMarker()
        {
            var cameraObject = new GameObject("TestMainCamera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            var markers = cameraObject.AddComponent<GasSafari.UI.SwimmerEdgeMarkers>();
            // Mira hacia el lado contrario de la balsa: el caído no sale en pantalla.
            cameraObject.transform.position = new Vector3(0f, 20f, 60f);
            cameraObject.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
            yield return null;

            fall.Fall(new Vector3(0f, 0f, -2f));
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(1, markers.VisibleCount, "icono visible mientras está fuera de cámara");

            cameraObject.transform.position = new Vector3(0f, 12f, -6f);
            cameraObject.transform.LookAt(input.transform.position);
            yield return null;
            yield return null;
            Assert.AreEqual(0, markers.VisibleCount, "sin icono cuando está a la vista");
            Object.Destroy(cameraObject);
        }

        [UnityTest]
        public IEnumerator Fallen_SwimsAtSwimSpeed()
        {
            fall.Fall(new Vector3(-3f, 0f, -3f));
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(AvatarState.Swimming, states.State);

            input.Command.Swim = new Vector2(-1f, 0f);
            yield return new WaitForSeconds(0.3f);
            var velocity = input.GetComponent<Rigidbody>().velocity;
            Assert.AreEqual(rig.Balance.swimSpeed, new Vector2(velocity.x, velocity.z).magnitude, 0.2f);
        }

        [UnityTest]
        public IEnumerator Swimmer_NearEdge_ClimbsWithJump()
        {
            fall.Fall(new Vector3(0f, 0f, -1.5f));
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(AvatarState.Swimming, states.State);
            Assert.LessOrEqual(fall.HomeRaft.DistanceToEdge(input.transform.position), rig.Balance.climbReach);

            input.Command.JumpPresses++;
            yield return null;
            Assert.IsTrue(seat.IsSeated);
            Assert.AreEqual(180f, seat.Angle, 10f);
        }

        [UnityTest]
        public IEnumerator RaftRunningOverSwimmer_RescuesIt()
        {
            fall.Fall(new Vector3(0f, 0f, -4f));
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(AvatarState.Swimming, states.State);

            bool rescued = false;
            GameEvents.OnPlayerRescued += (who, by) => rescued |= who == input.GetComponent<PlayerAvatar>();
            var toSwimmer = input.transform.position - rig.Raft.transform.position;
            toSwimmer.y = 0f;
            rig.Get<Rigidbody>().velocity = toSwimmer.normalized * (rig.Balance.swimSpeed * 2f);
            yield return new WaitForSeconds(1.5f);

            Assert.IsTrue(rescued);
            Assert.IsTrue(seat.IsSeated);
        }

        [UnityTest]
        public IEnumerator KnockedOntoDeck_SitsAgain()
        {
            fall.Fall(new Vector3(0f, 3f, 4f));
            yield return new WaitForSeconds(1.5f);
            Assert.IsTrue(seat.IsSeated);
            Assert.AreEqual(AvatarState.Seated, states.State);
        }

        [UnityTest]
        public IEnumerator HardImpact_KnocksOffUnlessGrabbing()
        {
            var grabber = rig.AddAvatar(90f);
            grabber.Command.GrabHeld = true;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.layer = GameLayers.Environment;
            wall.transform.position = new Vector3(0f, 0f, 4f);
            wall.transform.localScale = new Vector3(10f, 3f, 1f);
            yield return null;

            rig.Get<Rigidbody>().velocity = new Vector3(0f, 0f, 9f);
            yield return new WaitForSeconds(0.5f);
            Object.Destroy(wall);

            Assert.IsFalse(seat.IsSeated, "el que no se agarra cae");
            Assert.IsTrue(grabber.GetComponent<PlayerSeat>().IsSeated, "el agarrado no cae");
        }
    }
}
