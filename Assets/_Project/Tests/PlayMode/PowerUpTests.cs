using System.Collections;
using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Hazards;
using GasSafari.Level;
using GasSafari.PowerUps;
using GasSafari.Raft;
using GasSafari.Team;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class PowerUpTests
    {
        private class TestFauna : MonoBehaviour, IScareable
        {
            public float ScaredFor;

            public void Scare(float duration, Vector3 source) => ScaredFor = duration;
        }

        private RaftTestRig rig;
        private readonly List<GameObject> created = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in created)
                Object.Destroy(go);
            created.Clear();
            rig.Destroy();
        }

        private PowerUpBase Spawn(string name, Vector3 position)
        {
            PowerUpBase powerUp = null;
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/PowerUps/" + name + ".prefab");
            var go = Object.Instantiate(prefab, position, Quaternion.identity);
            created.Add(go);
            powerUp = go.GetComponent<PowerUpBase>();
#endif
            return powerUp;
        }

        [UnityTest]
        public IEnumerator P01_Beans_RefillsNearestAnimalOnly()
        {
            var near = rig.AddAvatar(90f);
            var far = rig.AddAvatar(270f);
            yield return null;
            var nearGas = near.GetComponent<GasTank>();
            var farGas = far.GetComponent<GasTank>();
            nearGas.SetFraction(0.2f);
            farGas.SetFraction(0.2f);

            var powerUp = Spawn("P01_Beans", near.transform.position + near.transform.up * 0.5f);
            powerUp.Collect(rig.Get<RaftSeats>(), null);

            Assert.AreEqual(1f, nearGas.Normalized, 1e-4f);
            Assert.AreEqual(0.2f, farGas.Normalized, 1e-4f);
            Assert.IsTrue(powerUp.IsCollected);
            Assert.IsFalse(powerUp.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator P02_Locoto_BoostsFartsThenExpires()
        {
            var avatar = rig.AddAvatar(90f);
            yield return null;
            var powerUp = Spawn("P02_Locoto", avatar.transform.position);
            powerUp.Collect(rig.Get<RaftSeats>(), null);
            var thruster = avatar.GetComponent<FartThruster>();

            yield return null;
            Assert.AreEqual(powerUp.Data.magnitude, thruster.Delivery.Multiplier, 1e-4f);
            Assert.IsNotNull(avatar.GetComponent<FireFartEffect>());

            yield return new WaitForSeconds(powerUp.Data.duration + 0.3f);
            Assert.AreEqual(1f, thruster.Delivery.Multiplier, 1e-4f);
            Assert.IsNull(avatar.GetComponent<FireFartEffect>());
        }

        [UnityTest]
        public IEnumerator P03_Mango_AddsHalfTankToWholeTeam()
        {
            var a = rig.AddAvatar(90f);
            var b = rig.AddAvatar(270f);
            yield return null;
            a.GetComponent<GasTank>().SetFraction(0.2f);
            b.GetComponent<GasTank>().SetFraction(0.8f);

            Spawn("P03_Mango", Vector3.zero).Collect(rig.Get<RaftSeats>(), null);

            Assert.AreEqual(0.7f, a.GetComponent<GasTank>().Normalized, 1e-4f);
            Assert.AreEqual(1f, b.GetComponent<GasTank>().Normalized, 1e-4f);
        }

        [UnityTest]
        public IEnumerator P04_Coconut_AbsorbsNextChamberDamage()
        {
            rig.AddAvatar(90f);
            yield return null;
            var chambers = rig.Get<RaftChambers>();
            Spawn("P04_CoconutShield", Vector3.zero).Collect(rig.Get<RaftSeats>(), null);
            Assert.IsTrue(chambers.HasShield);

            Assert.IsFalse(chambers.PopChamber(0));
            Assert.IsFalse(chambers.HasShield);
            Assert.AreEqual(RaftChambers.CHAMBER_COUNT, chambers.IntactCount);
        }

        [UnityTest]
        public IEnumerator P05_Soda_RaisesRaftThenSettles()
        {
            rig.AddAvatar(90f);
            yield return new WaitForSeconds(0.5f);
            var buoyancy = rig.Get<RaftBuoyancy>();
            var powerUp = Spawn("P05_Soda", Vector3.zero);
            powerUp.Collect(rig.Get<RaftSeats>(), null);

            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(powerUp.Data.magnitude, buoyancy.HoverOffset, 1e-3f);

            yield return new WaitForSeconds(powerUp.Data.duration);
            Assert.AreEqual(0f, buoyancy.HoverOffset, 1e-3f);
        }

        [UnityTest]
        public IEnumerator P06_Onion_ScaresFaunaInsideRadiusOnly()
        {
            rig.AddAvatar(90f);
            yield return null;
            var inside = CreateFauna(new Vector3(5f, 0f, 0f));
            var outside = CreateFauna(new Vector3(12f, 0f, 0f));

            var powerUp = Spawn("P06_Onion", Vector3.zero);
            powerUp.Collect(rig.Get<RaftSeats>(), null);
            yield return new WaitForSeconds(0.5f);

            Assert.Greater(inside.ScaredFor, 0f);
            Assert.LessOrEqual(inside.ScaredFor, powerUp.Data.duration);
            Assert.AreEqual(0f, outside.ScaredFor);
        }

        private TestFauna CreateFauna(Vector3 position)
        {
            var go = new GameObject("Fauna") { layer = GameLayers.Hazard };
            go.transform.position = position;
            go.AddComponent<SphereCollider>().isTrigger = true;
            go.AddComponent<HazardTags>().Configure(HazardTag.Fauna);
            created.Add(go);
            return go.AddComponent<TestFauna>();
        }

        [UnityTest]
        public IEnumerator P07_GoldenMango_AddsMeterAndRaisesEvent()
        {
            rig.AddAvatar(90f);
            yield return null;
            PowerUpData seen = null;
            int slotSeen = -1;
            GameEvents.OnPowerUp += (data, taker) => seen = data;
            GameEvents.OnGoldenMango += slot => slotSeen = slot;
            var meter = rig.Get<TeamMeter>();

            Spawn("P07_GoldenMango", Vector3.zero).Collect(rig.Get<RaftSeats>(), null);

            Assert.AreEqual(15f, meter.Value, 1e-4f);
            Assert.IsNotNull(seen);
            Assert.AreEqual("P07", seen.id);
            Assert.AreEqual(0, slotSeen, "ranura del mango dorado");
        }

        [UnityTest]
        public IEnumerator Pool_ReusesInstanceCleanAtNewPosition()
        {
            rig.AddAvatar(90f);
            yield return null;
            var prefab = Spawn("P07_GoldenMango", new Vector3(50f, 0f, 50f));
            prefab.gameObject.SetActive(false);
            var pool = new PowerUpPool(prefab, null);

            var first = pool.Get(new Vector3(30f, 0f, 0f));
            created.Add(first.gameObject);
            first.Collect(rig.Get<RaftSeats>(), null);
            Assert.IsTrue(first.IsCollected);
            pool.Release(first);
            Assert.AreEqual(1, pool.CountInactive);

            var second = pool.Get(new Vector3(35f, 0f, 0f));
            Assert.AreSame(first, second);
            Assert.IsFalse(second.IsCollected, "el permanente vuelve limpio desde el pool");
            Assert.IsTrue(second.gameObject.activeSelf);
            Assert.AreEqual(35f, second.transform.position.x, 1e-4f);
        }

        [UnityTest]
        public IEnumerator Contact_WithRaft_CollectsByTrigger()
        {
            rig.AddAvatar(90f);
            yield return null;
            var powerUp = Spawn("P01_Beans", rig.Raft.transform.position + Vector3.up * 0.3f);
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(powerUp.IsCollected);
        }

        [UnityTest]
        public IEnumerator Respawn_ReturnsCollectedExceptGoldenMango()
        {
            rig.AddAvatar(90f);
            var cp = new GameObject("CP0");
            cp.layer = GameLayers.Trigger;
            cp.AddComponent<BoxCollider>().isTrigger = true;
            var checkpoint = cp.AddComponent<Checkpoint>();
            created.Add(cp);
            var level = new GameObject("Level");
            level.SetActive(false);
            level.AddComponent<LevelTimer>();
            var system = level.AddComponent<CheckpointSystem>();
            system.Configure(rig.Balance, rig.Get<RaftController>(), new[] { checkpoint });
            var respawner = level.AddComponent<PowerUpRespawner>();
            respawner.Configure(system);
            level.SetActive(true);
            created.Add(level);
            yield return null;

            var beans = Spawn("P01_Beans", new Vector3(20f, 0f, 20f));
            var golden = Spawn("P07_GoldenMango", new Vector3(22f, 0f, 20f));
            beans.Collect(rig.Get<RaftSeats>(), null);
            golden.Collect(rig.Get<RaftSeats>(), null);

            system.RequestRespawn();
            yield return new WaitForSeconds(rig.Balance.respawnFade + 0.3f);

            Assert.IsFalse(beans.IsCollected, "P01 reaparece");
            Assert.IsTrue(beans.gameObject.activeSelf);
            Assert.IsTrue(golden.IsCollected, "P07 queda recogido");
            Assert.IsFalse(golden.gameObject.activeSelf);
        }
    }
}
