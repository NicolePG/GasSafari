using System.Collections;
using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Hazards;
using GasSafari.Raft;
using GasSafari.Team;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M5-14: obstáculos de W3 (docs/05) sobre la balsa del prefab en agua quieta.
    public class W3HazardTests
    {
        private RaftTestRig rig;
        private readonly List<GameObject> created = new List<GameObject>();
        private int popped;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            popped = 0;
            GameEvents.OnChamberPopped += CountPop;
            yield return new WaitForSeconds(0.3f);
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.OnChamberPopped -= CountPop;
            foreach (var go in created)
                Object.Destroy(go);
            created.Clear();
            rig.Destroy();
        }

        private void CountPop(int chamber) => popped++;

        private T Make<T>(string prefab, Vector3 offset, float yaw = 0f) where T : Component
        {
            GameObject go = null;
#if UNITY_EDITOR
            go = Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Hazards/" + prefab + ".prefab"),
                rig.Raft.transform.position + offset, Quaternion.Euler(0f, yaw, 0f));
#endif
            created.Add(go);
            return go.GetComponent<T>();
        }

        [UnityTest]
        public IEnumerator H11_SandBar_GroundsASlowRaftButNotAFastOne()
        {
            Make<SandBar>("H11_SandBar", Vector3.zero);
            var body = rig.Get<Rigidbody>();
            body.velocity = Vector3.forward * 1.5f;
            yield return new WaitForSeconds(0.6f);
            Assert.Less(new Vector2(body.velocity.x, body.velocity.z).magnitude, 0.4f, "a menos de 3 m/s encalla");

            // Una balsa rápida cruza.
            rig.Raft.transform.position = new Vector3(0f, 0.2f, -12f);
            body.position = rig.Raft.transform.position;
            body.velocity = Vector3.forward * 8f;
            yield return new WaitForSeconds(0.7f);
            Assert.Greater(body.velocity.z, 3.5f, "con velocidad no encalla");
        }

        [UnityTest]
        public IEnumerator H12_Vine_BlocksUntilBurned()
        {
            var vine = Make<BurnableWall>("H12_Vine", new Vector3(0f, 0f, 3f));
            Assert.IsTrue(vine.GetComponent<HazardTags>().Has(HazardTag.Burnable));
            vine.Burn(Vector3.zero);
            yield return new WaitForSeconds(0.7f);
            Assert.IsTrue(vine.Burned, "se quema en 0.5 s");
        }

        [UnityTest]
        public IEnumerator H13_Log_SpinsNinetyDegreesPerSecond()
        {
            var log = Make<SpinningLog>("H13_SpinningLog", new Vector3(8f, 0f, 8f));
            float start = log.transform.eulerAngles.y;
            yield return new WaitForSeconds(1f);
            float turned = Mathf.DeltaAngle(start, log.transform.eulerAngles.y);
            Assert.AreEqual(90f, turned, 12f);
            log.Break(Vector3.zero);
            Assert.IsTrue(log.Broken);
        }

        [UnityTest]
        public IEnumerator H14_Pit_ThrowsTheClosestLooseAnimalAfterTheWarning_NotAGrabberNorAScaredOne()
        {
            var loose = rig.AddAvatar(90f);
            var grabber = rig.AddAvatar(270f);
            grabber.Command.GrabHeld = true;
            yield return new WaitForSeconds(0.3f);

            var pit = Make<PitAmbush>("H14_Pit", new Vector3(2.5f, 0f, 0f));
            yield return new WaitForSeconds(0.25f);
            Assert.IsTrue(pit.IsWarning, "el arbusto se mueve 1 s antes");
            Assert.IsTrue(loose.GetComponent<PlayerSeat>().IsSeated);
            yield return new WaitForSeconds(1.2f);
            Assert.IsFalse(loose.GetComponent<PlayerSeat>().IsSeated, "tira al agua al animal suelto");
            Assert.IsTrue(grabber.GetComponent<PlayerSeat>().IsSeated, "el agarrado se queda");
        }

        [UnityTest]
        public IEnumerator H14_Pit_ScaredDoesNotAttack()
        {
            var loose = rig.AddAvatar(90f);
            yield return new WaitForSeconds(0.3f);
            var pit = Make<PitAmbush>("H14_Pit", Vector3.zero);
            pit.transform.position = rig.Raft.transform.position + new Vector3(30f, 0f, 0f);
            pit.Scare(5f, Vector3.zero);
            pit.transform.position = rig.Raft.transform.position + new Vector3(2.5f, 0f, 0f);
            yield return new WaitForSeconds(1.6f);
            Assert.IsTrue(loose.GetComponent<PlayerSeat>().IsSeated, "espantada no ataca");
        }

        [UnityTest]
        public IEnumerator H15_Croc_WakesOnlyWithLoudFartsNearby_AndBitesOneChamber()
        {
            var shooter = rig.AddAvatar(180f);
            yield return new WaitForSeconds(0.3f);
            var croc = Make<Crocodile>("H15_Crocodile", new Vector3(6f, 0f, 0f));
            var avatar = shooter.GetComponent<PlayerAvatar>();
            yield return null;

            GameEvents.RaiseFart(avatar, FartType.Puff, 5f);
            yield return null;
            Assert.AreEqual(Crocodile.State.Sleeping, croc.Current, "los toques suaves no lo despiertan");

            GameEvents.RaiseFart(avatar, FartType.Blast, 30f);
            yield return null;
            Assert.AreEqual(Crocodile.State.Chasing, croc.Current, "un pedo fuerte a menos de 8 m lo despierta");

            yield return new WaitForSeconds(7f);
            Assert.GreaterOrEqual(popped, 1, $"muerde y revienta una cámara (estado {croc.Current}, a {Vector3.Distance(croc.transform.position, rig.Raft.transform.position):F1} m)");
        }

        [UnityTest]
        public IEnumerator H15_Croc_FarAwayStaysAsleep_ScaredRetreats()
        {
            var shooter = rig.AddAvatar(180f);
            yield return new WaitForSeconds(0.3f);
            var croc = Make<Crocodile>("H15_Crocodile", new Vector3(20f, 0f, 0f));
            yield return null;
            GameEvents.RaiseFart(shooter.GetComponent<PlayerAvatar>(), FartType.Charged, 80f);
            yield return null;
            Assert.AreEqual(Crocodile.State.Sleeping, croc.Current, "a más de 8 m no despierta");

            croc.transform.position = rig.Raft.transform.position + new Vector3(6f, 0f, 0f);
            GameEvents.RaiseFart(shooter.GetComponent<PlayerAvatar>(), FartType.Charged, 80f);
            yield return null;
            Assert.AreEqual(Crocodile.State.Chasing, croc.Current);
            croc.Scare(4f, Vector3.zero);
            yield return new WaitForSeconds(3f);
            Assert.AreEqual(0, popped, "espantado no muerde");
        }

        [UnityTest]
        public IEnumerator H16_Rhythm_FollowsTheBeatAndAFartOnTheBeatFillsTheMeter()
        {
            var clockObject = new GameObject("Clock");
            created.Add(clockObject);
            var clock = clockObject.AddComponent<RhythmClock>();
            clock.Configure(110f);
            var bonus = clockObject.AddComponent<RhythmSyncBonus>();
            var block = Make<RhythmObstacle>("H16_Rhythm", new Vector3(12f, 0f, 12f));
            block.Configure(Vector3.right, 6f, 4f, 0);
            yield return null;

            // Un ciclo de 4 pulsos a 110 BPM dura 2.18 s: la pieza llega al extremo a la mitad.
            float max = 0f;
            float end = Time.time + 2.3f;
            while (Time.time < end)
            {
                max = Mathf.Max(max, block.Displacement);
                yield return null;
            }
            Assert.AreEqual(6f, max, 0.8f, "recorre 6 m en un compás");

            var shooter = rig.AddAvatar(180f);
            var meter = rig.Get<TeamMeter>();
            float before = meter.Value;
            // Espera a estar sobre el pulso y dispara.
            float wait = 0f;
            while (!clock.IsOnBeat() && wait < 2f)
            {
                wait += Time.deltaTime;
                yield return null;
            }
            GameEvents.RaiseFart(shooter.GetComponent<PlayerAvatar>(), FartType.Puff, 5f);
            Assert.Greater(meter.Value, before, "un pedo en el pulso suma al medidor");
            Assert.AreEqual(1, bonus.OnBeatFarts);

            clock.SetBpm(128f);
            Assert.AreEqual(128f, clock.Bpm);
        }

        [UnityTest]
        public IEnumerator H29_Crab_StealsTwentyGasPerSecondForThreeSeconds_ButLeavesWhenTheAnimalFarts()
        {
            var victim = rig.AddAvatar(90f);
            var gas = victim.GetComponent<GasTank>();
            gas.SetFraction(1f);
            yield return new WaitForSeconds(0.3f);
            float start = gas.Gas;
            var crab = Make<CrabHazard>("H29_Crab", Vector3.zero);
            crab.transform.position = rig.Raft.transform.position + new Vector3(2.5f, 0f, 0f);
            yield return new WaitForSeconds(1.4f);
            Assert.IsTrue(crab.Attached, "sube a bordo");
            Assert.Greater(start - gas.Gas, 12f, "roba gas");

            yield return new WaitForSeconds(2.5f);
            Assert.IsFalse(crab.Attached, "se va tras 3 s");
            Assert.AreEqual(60f, crab.StolenGas, 12f, "unos 20 por segundo durante 3 s");
        }

        [UnityTest]
        public IEnumerator H29_Crab_FallsOffWhenTheVictimFarts()
        {
            var victim = rig.AddAvatar(90f);
            yield return new WaitForSeconds(0.3f);
            var crab = Make<CrabHazard>("H29_Crab", Vector3.zero);
            crab.transform.position = rig.Raft.transform.position + new Vector3(2.5f, 0f, 0f);
            yield return new WaitForSeconds(0.8f);
            Assert.IsTrue(crab.Attached);
            GameEvents.RaiseFart(victim.GetComponent<PlayerAvatar>(), FartType.Puff, 5f);
            Assert.IsFalse(crab.Attached, "un pedo se lo quita de encima");
        }
    }
}
