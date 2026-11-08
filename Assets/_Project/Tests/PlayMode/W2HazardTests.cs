using System.Collections;
using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Hazards;
using GasSafari.Raft;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M5-08: obstáculos de W2 (docs/05) sobre la balsa del prefab en agua quieta.
    public class W2HazardTests
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
        public IEnumerator H06_Barrel_ExplodesOneAndAHalfSecondsAfterTouch_PopsOneChamber()
        {
            var barrel = Make<ExplosiveBarrel>("H06_ExplosiveBarrel", new Vector3(0f, 0f, 3f));
            rig.Get<Rigidbody>().velocity = Vector3.forward * 2f;
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(barrel.IsWarning, "la mecha se enciende al tocarlo");
            Assert.AreEqual(0, popped);
            yield return new WaitForSeconds(1.3f);
            Assert.IsTrue(barrel.Gone);
            Assert.AreEqual(1, popped, "revienta 1 cámara");
        }

        [UnityTest]
        public IEnumerator H06_Barrel_BrokenByRoarDoesNotExplode()
        {
            var barrel = Make<ExplosiveBarrel>("H06_ExplosiveBarrel", new Vector3(0f, 0f, 3f));
            barrel.Break(Vector3.zero);
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(0, popped);
        }

        [UnityTest]
        public IEnumerator H08_Wind_PushesSidewaysOnlyDuringTheGustAfterTheWarning()
        {
            var wind = Make<GasWindZone>("H08_Wind", Vector3.zero);
            var body = rig.Get<Rigidbody>();
            float t = 0f;
            while (!wind.IsWarning && t < 5f)
            {
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(wind.IsWarning, "avisa antes de soplar");
            Assert.Less(Mathf.Abs(body.velocity.x), 0.2f, "durante el aviso no empuja");
            yield return new WaitForSeconds(1.8f);
            Assert.Greater(body.velocity.x, 1f, "la ráfaga empuja hacia transform.right");
        }

        [UnityTest]
        public IEnumerator H09_Whirlpool_PullsToCenterAndSpins()
        {
            var pool = Make<Whirlpool>("H09_Whirlpool", new Vector3(6f, 0f, 0f));
            yield return new WaitForSeconds(1.5f);
            var body = rig.Get<Rigidbody>();
            Assert.Less(Vector3.Distance(Flat(body.position), Flat(pool.transform.position)), 5.5f, "se acerca al centro");
            Assert.Greater(body.angularVelocity.y * Mathf.Rad2Deg, 30f, "gira hacia 60°/s");
        }

        [UnityTest]
        public IEnumerator H10_Spikes_PopTheTouchingChamber()
        {
            Make<SpikeHazard>("H10_Spikes", new Vector3(0f, 0f, 3f));
            rig.Get<Rigidbody>().velocity = Vector3.forward * 3f;
            yield return new WaitForSeconds(1f);
            Assert.GreaterOrEqual(popped, 1);
        }

        [UnityTest]
        public IEnumerator H32_Net_TriplesDragUntilBroken()
        {
            var net = Make<FishingNet>("H32_FishingNet", Vector3.zero);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            var buoyancy = rig.Get<RaftBuoyancy>();
            Assert.AreEqual(3f, buoyancy.ZoneDragMultiplier, 0.01f);
            net.Break(Vector3.zero);
            Assert.AreEqual(1f, buoyancy.ZoneDragMultiplier, 0.01f, "el Rugido la rompe");
        }

        [UnityTest]
        public IEnumerator H25_Tentacles_FollowPhaseIntervalsAndPush()
        {
            var boss = Make<TentacleBoss>("H25_Tentacles", new Vector3(5f, 0f, 0f)); // el 1.º tentáculo queda a 2 m
            boss.SetPhase(2);                                        // cada 1.5 s
            yield return new WaitForSeconds(1.6f);
            Assert.AreEqual(1, boss.Strikes);
            var body = rig.Get<Rigidbody>();
            Assert.Greater(Flat(body.velocity).magnitude, 0.5f, "el primer golpe (a 2 m) empuja la balsa");
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(2, boss.Strikes);
            boss.SetPhase(-1);
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(2, boss.Strikes, "fase -1: se detiene");
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
