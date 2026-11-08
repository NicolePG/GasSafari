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
    // M5-26: obstáculos de W5 (docs/05): H21 Rápidos, H37 compuerta de represa y H23 cocodrilo gigante.
    public class W5HazardTests
    {
        private RaftTestRig rig;
        private readonly List<GameObject> created = new List<GameObject>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            yield return new WaitForSeconds(0.3f);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in created)
                Object.Destroy(go);
            created.Clear();
            rig.Destroy();
        }

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
        public IEnumerator H21_Rapids_PushTheRaftDownstream_HalfAsHardInClass1()
        {
            var rapids = Make<RapidsZone>("H21_Rapids", Vector3.zero);
            var body = rig.Get<Rigidbody>();
            Assert.AreEqual(8f, rapids.Accel, 0.01f, "docs/05 H21: 8 m/s²");
            yield return new WaitForSeconds(1f);
            Assert.Greater(body.velocity.z, 3f, "la corriente la empuja hacia adelante");

            rapids.Configure(0.5f);
            Assert.AreEqual(4f, rapids.Accel, 0.01f, "clase 1: la mitad");
        }

        [UnityTest]
        public IEnumerator H37_DamGate_BlocksThenOpensAndReleasesASurge()
        {
            // La balsa en la zona aguas abajo de la puerta (z local entre 1 y 25).
            var gate = Make<DamGate>("H37_DamGate", new Vector3(0f, 0f, -6f));
            var door = gate.GetComponentInChildren<Collider>();
            var blocker = gate.transform.Find("Puerta").GetComponent<Collider>();
            Assert.AreEqual(DamGate.State.Closed, gate.Current);
            Assert.IsTrue(blocker.enabled, "cerrada: la puerta bloquea el cauce");

            float waited = 0f;
            while (gate.Current != DamGate.State.Open && waited < 12f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual(DamGate.State.Open, gate.Current, "abre pasado el tiempo cerrada");
            Assert.AreEqual(1, gate.Openings);
            Assert.IsFalse(blocker.enabled, "abierta: el paso queda libre");

            var body = rig.Get<Rigidbody>();
            yield return new WaitForSeconds(1.2f);
            Assert.Greater(body.velocity.z, 2f, "la oleada empuja a la balsa de aguas abajo");

            waited = 0f;
            while (gate.Current != DamGate.State.Closed && waited < 8f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual(DamGate.State.Closed, gate.Current, "vuelve a cerrar");
            Assert.IsTrue(blocker.enabled);
            Assert.IsNotNull(door);
        }

        [UnityTest]
        public IEnumerator H23_GiantCroc_ChasesAt90PercentOfTheTeamSpeed_RoarSlowsIt_AndCatchesAStoppedRaft()
        {
            var croc = Make<GiantCrocChaser>("H23_GiantCroc", new Vector3(0f, 0f, 3f));
            var body = croc.transform.Find("Cocodrilo");
            var path = new List<Vector3>();
            for (int i = 0; i < 40; i++)
                path.Add(rig.Raft.transform.position + new Vector3(0f, 0f, -40f + i * 10f));
            body.position = path[0];
            croc.Configure(body, path.ToArray(), 0.9f, 5f);

            var rb = rig.Get<Rigidbody>();
            // La balsa entra en la zona (en reposo) y sigue a 6 m/s.
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(croc.Chasing, "empieza la persecución al entrar la balsa en la zona");
            float end = Time.time + 6f;
            while (Time.time < end)
            {
                var v = rb.velocity;
                rb.velocity = new Vector3(0f, v.y, 6f);
                yield return null;
            }
            Assert.Greater(croc.AverageRaftSpeed, 3f);
            Assert.AreEqual(croc.AverageRaftSpeed * 0.9f, croc.CurrentSpeed, 0.3f, "docs/05 H23: 90 % de la velocidad media del equipo");

            croc.Scare(2f, Vector3.zero);
            yield return null;
            Assert.AreEqual(0f, croc.CurrentSpeed, 0.01f, "el Rugido lo frena");
            yield return new WaitForSeconds(2.2f);
            Assert.Greater(croc.CurrentSpeed, 1f, "pasados 2 s vuelve a perseguir");

            // Balsa quieta: la alcanza.
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
            float guard = Time.time + 25f;
            while (croc.Catches == 0 && Time.time < guard)
                yield return null;
            rb.isKinematic = false;
            Assert.GreaterOrEqual(croc.Catches, 1, "si el equipo se detiene, el cocodrilo la alcanza (reaparición)");
        }
    }
}
