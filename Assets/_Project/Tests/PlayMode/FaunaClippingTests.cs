using System.Collections;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // Auditoría de traspasos (docs/11, M9-09): la balsa no se mete dentro de la fauna (prefabs de Prefabs/Fauna) al chocar con
    // ella a velocidad de crucero, ni la atraviesa a 12 m/s.
    public class FaunaClippingTests
    {
        private const float MAX_PENETRATION = 0.15f;

        private RaftTestRig rig;
        private GameObject fauna;

        [TearDown]
        public void TearDown()
        {
            if (fauna != null)
                Object.Destroy(fauna);
            rig?.Destroy();
            rig = null;
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator RaftDoesNotPenetrateTheFauna(
            [Values("Bat", "Crab", "Croc", "Fossa", "GiantCroc", "Tentacle")] string name,
            [Values(6f, 12f)] float speed)
        {
            rig = new RaftTestRig(new Vector3(0f, 0.3f, 0f));
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Fauna/{name}.prefab");
            Assert.IsNotNull(prefab, $"falta el prefab de fauna {name}");
            fauna = Object.Instantiate(prefab, new Vector3(0f, 0f, 14f), Quaternion.identity);
#endif
            var faunaColliders = fauna.GetComponentsInChildren<Collider>();
            Assert.IsNotEmpty(faunaColliders, "la fauna tiene collider");
            // Lo bastante grande para que el choque ocurra aunque el modelo sea bajo (el murciélago vuela alto).
            var raftColliders = rig.Raft.GetComponentsInChildren<Collider>();
            yield return new WaitForSeconds(0.3f);

            rig.Get<Rigidbody>().velocity = new Vector3(0f, 0f, speed);
            float maxPenetration = 0f;
            float end = Time.time + 2.5f;
            while (Time.time < end)
            {
                foreach (var a in raftColliders)
                {
                    if (a == null || a.isTrigger || !a.enabled)
                        continue;
                    foreach (var b in faunaColliders)
                    {
                        if (b.isTrigger || !b.enabled)
                            continue;
                        if (Physics.ComputePenetration(a, a.transform.position, a.transform.rotation, b, b.transform.position, b.transform.rotation,
                            out _, out float distance))
                            maxPenetration = Mathf.Max(maxPenetration, distance);
                    }
                }
                yield return new WaitForFixedUpdate();
            }
            Assert.LessOrEqual(maxPenetration, MAX_PENETRATION, $"{name} a {speed} m/s: la balsa se metió {maxPenetration:F2} m en la fauna");
        }
    }
}
