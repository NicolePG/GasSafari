using System.Collections;
using GasSafari.Core;
using GasSafari.Hazards;
using GasSafari.Raft;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M5-02: cada obstáculo de W1 con la balsa del prefab sobre agua plana.
    public class W1HazardTests
    {
        private RaftTestRig rig;
        private RaftController raft;
        private Rigidbody body;
        private readonly System.Collections.Generic.List<GameObject> spawned = new System.Collections.Generic.List<GameObject>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            raft = rig.Get<RaftController>();
            body = raft.Body;
            yield return new WaitForSeconds(0.3f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var go in spawned)
            {
                if (go != null)
                    Object.Destroy(go);
            }
            spawned.Clear();
            rig.Destroy();
            yield return null;
        }

        private GameObject Spawn(string prefab, Vector3 position, float yaw = 0f)
        {
            GameObject asset = null;
#if UNITY_EDITOR
            asset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Hazards/{prefab}.prefab");
#endif
            var go = Object.Instantiate(asset, position, Quaternion.Euler(0f, yaw, 0f));
            spawned.Add(go);
            return go;
        }

        [UnityTest]
        public IEnumerator H01_Wall_BouncesTheRaftBack()
        {
            Spawn("H01_Wall", new Vector3(0f, 0f, 4f));
            body.velocity = new Vector3(0f, 0f, 6f);

            yield return new WaitForSeconds(1.2f);

            Assert.Less(body.velocity.z, 0.5f, "la pared la frena y la devuelve");
            Assert.Less(body.position.z, 4f, "no la atraviesa");
        }

        [UnityTest]
        public IEnumerator H27_Trampoline_LaunchesTheRaftUpAt10MetersPerSecond()
        {
            Spawn("H27_Awning", new Vector3(0f, 0f, 0f));
            float maxY = body.position.y;
            float maxVy = 0f;
            for (float t = 0f; t < 1.5f; t += Time.deltaTime)
            {
                maxY = Mathf.Max(maxY, body.position.y);
                maxVy = Mathf.Max(maxVy, body.velocity.y);
                yield return null;
            }

            Assert.GreaterOrEqual(maxVy, 9f, "sale a ~10 m/s hacia arriba");
            Assert.Greater(maxY, 2f, "sube bastante");
        }

        [UnityTest]
        public IEnumerator H03_Gate_WhenClosedPushesTheRaftBack()
        {
            var gate = Spawn("H03_TimedGate", new Vector3(0f, 0f, 0f)).GetComponent<TimedGate>();
            gate.SetPhase(1.4f); // cerrada durante el primer segundo
            body.position = new Vector3(0f, 0.2f, -5f);
            body.velocity = new Vector3(0f, 0f, 6f);

            float maxZ = -5f;
            for (float t = 0f; t < 0.9f; t += Time.deltaTime)
            {
                maxZ = Mathf.Max(maxZ, body.position.z);
                yield return null;
            }

            Assert.IsTrue(gate.IsClosed || gate.Cycle > 0f);
            Assert.Less(maxZ, 0f, "la compuerta cerrada no la deja pasar");
            Assert.Less(body.velocity.z, 1f, "y la devuelve");
        }

        [UnityTest]
        public IEnumerator H03_Gate_WarnsBeforeClosing()
        {
            var gate = Spawn("H03_TimedGate", new Vector3(0f, 0f, 20f)).GetComponent<TimedGate>();
            bool warned = false;
            System.Action<HazardBase> onWarn = h => warned |= h == gate;
            GameEvents.OnHazardWarning += onWarn;

            float closedAt = -1f;
            float warnedAt = -1f;
            for (float t = 0f; t < 3f; t += Time.deltaTime)
            {
                if (warned && warnedAt < 0f)
                    warnedAt = t;
                if (gate.IsClosed && closedAt < 0f)
                    closedAt = t;
                yield return null;
            }
            GameEvents.OnHazardWarning -= onWarn;

            Assert.GreaterOrEqual(warnedAt, 0f, "avisó");
            Assert.GreaterOrEqual(closedAt, 0f, "cerró");
            Assert.GreaterOrEqual(closedAt - warnedAt, 0.4f, "con al menos ~0.5 s de aviso");
        }

        [UnityTest]
        public IEnumerator H07_Crate_BreaksOnAStrongImpact_ButNotOnATouch()
        {
            var weak = Spawn("H07_Crate", new Vector3(0f, 0f, 3f)).GetComponent<BreakableCrate>();
            body.velocity = new Vector3(0f, 0f, 0.15f);
            yield return new WaitForSeconds(1.5f);
            Assert.IsFalse(weak.Broken, "un roce no la rompe");

            // La misma caja, ahora con un golpe fuerte.
            body.velocity = new Vector3(0f, 0f, 7f);
            yield return new WaitForSeconds(1.5f);
            Assert.IsTrue(weak.Broken, "un golpe fuerte sí");
        }

        [UnityTest]
        public IEnumerator H07_Crate_BreaksWithTheRoar()
        {
            var crate = Spawn("H07_Crate", new Vector3(0f, 0f, 20f)).GetComponent<BreakableCrate>();
            crate.Break(Vector3.zero);
            yield return null;
            Assert.IsTrue(crate.Broken);
            Assert.IsFalse(crate.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator H04_Wave_LiftsAndPushesTheRaft()
        {
            var wave = Spawn("H04_Wave", new Vector3(0f, 0f, -8f)).GetComponent<WaveHazard>();
            wave.SpawnCrest();
            Assert.AreEqual(1, wave.ActiveCrests);

            float maxVy = 0f;
            float maxVz = 0f;
            for (float t = 0f; t < 4f; t += Time.deltaTime)
            {
                maxVy = Mathf.Max(maxVy, body.velocity.y);
                maxVz = Mathf.Max(maxVz, body.velocity.z);
                yield return null;
            }

            Assert.Greater(maxVy, 2f, "la levanta");
            Assert.Greater(maxVz, 0.1f, "y la empuja en el sentido de la ola");
        }

        [UnityTest]
        public IEnumerator H05_Blocker_SlidesAtTwoMetersPerSecond()
        {
            var blocker = Spawn("H05_MovingBlocker", new Vector3(0f, 0f, 30f)).GetComponent<MovingBlocker>();
            var start = blocker.transform.position;
            // 1 s de parada y luego 2 m/s: a los 2.5 s lleva 3 m.
            yield return new WaitForSeconds(2.5f);
            float moved = Vector3.Distance(start, blocker.transform.position);

            Assert.AreEqual(3f, moved, 0.8f);
            Assert.AreEqual(1, blocker.Direction);
        }

        [UnityTest]
        public IEnumerator H05_Blocker_HitsTheRaftAside()
        {
            var blocker = Spawn("H05_MovingBlocker", new Vector3(-6f, 0f, 0f)).GetComponent<MovingBlocker>();
            // Sale de la izquierda hacia la derecha; la balsa está en x = 0, dentro del recorrido.
            body.constraints = RigidbodyConstraints.None;
            yield return new WaitForSeconds(5f);

            Assert.Greater(body.position.x, 0.3f, "el contenedor la empujó hacia la derecha");
            Assert.IsFalse(blocker.Broken);
        }

        [UnityTest]
        public IEnumerator H28_Boat_LeavesAWakeWhenCrossingTheCenter()
        {
            var boat = Spawn("H28_Speedboat", new Vector3(-6f, 0f, 40f), 90f).GetComponent<CrossingBoat>();
            boat.Configure(new Vector3(0f, 0f, 6f), 0f);
            var wake = boat.GetComponentInChildren<WaveHazard>();

            int maxCrests = 0;
            for (float t = 0f; t < 5f; t += Time.deltaTime)
            {
                maxCrests = Mathf.Max(maxCrests, wake.ActiveCrests);
                yield return null;
            }

            Assert.GreaterOrEqual(maxCrests, 1, "su estela nace al pasar por el centro del cauce");
            Assert.AreEqual(HazardTag.Heavy, boat.GetComponent<HazardTags>().Tags);
        }

        [UnityTest]
        public IEnumerator H24_Crane_MarksTheShadowForOneSecondThenStrikes()
        {
            var crane = Spawn("H24_Crane", new Vector3(-8f, 0f, -10.3f)).GetComponent<CraneBoss>();
            crane.SetRaft(raft);
            var chambersBefore = raft.GetComponent<RaftChambers>().IntactCount;
            bool warned = false;
            System.Action<HazardBase> onWarn = h => warned |= h == crane;
            GameEvents.OnHazardWarning += onWarn;

            crane.StartAttacks(1, 2f);
            float warnedAt = -1f;
            float struckAt = -1f;
            for (float t = 0f; t < 6f; t += Time.deltaTime)
            {
                if (warned && warnedAt < 0f)
                    warnedAt = t;
                if (crane.Strikes >= 1 && struckAt < 0f)
                    struckAt = t;
                yield return null;
            }
            GameEvents.OnHazardWarning -= onWarn;

            Assert.GreaterOrEqual(warnedAt, 0f, "avisó con la sombra");
            Assert.GreaterOrEqual(struckAt, 0f, "el gancho cayó");
            Assert.AreEqual(1f, struckAt - warnedAt, 0.35f, "aviso de 1 s");
            Assert.IsFalse(crane.Active, "terminó su ataque único");
            Assert.Less(raft.GetComponent<RaftChambers>().IntactCount, chambersBefore, "la balsa estaba bajo la sombra: pierde una cámara");
        }

        [UnityTest]
        public IEnumerator H24_Crane_DoesNotHitARaftOutsideTheShadow()
        {
            var crane = Spawn("H24_Crane", new Vector3(-8f, 0f, -10.3f)).GetComponent<CraneBoss>();
            crane.SetRaft(raft);
            var chambersBefore = raft.GetComponent<RaftChambers>().IntactCount;
            crane.StartAttacks(1, 2f);
            // La balsa se aleja después de que la sombra quede marcada.
            while (!crane.IsWarningPhase)
                yield return null;
            body.position = new Vector3(0f, 0.2f, 12f);
            body.velocity = Vector3.zero;
            yield return new WaitForSeconds(1.6f);

            Assert.AreEqual(1, crane.Strikes);
            Assert.AreEqual(chambersBefore, raft.GetComponent<RaftChambers>().IntactCount, "esquivó la sombra");
        }
    }
}
