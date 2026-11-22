using System.Collections;
using System.Collections.Generic;
using GasSafari.Bots;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Hazards;
using GasSafari.Raft;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // Avisos de remolinos (H09), pinchos (H10), redes (H32) y cocodrilo gigante (H23), docs/05 "todo peligro avisa":
    // el evento sale antes de tocar el obstáculo, Vista Alta (Melman) lo adelanta y los bots reaccionan.
    public class HazardWarningTests
    {
        private RaftTestRig rig;
        private readonly List<GameObject> created = new List<GameObject>();
        private readonly List<HazardBase> warned = new List<HazardBase>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            warned.Clear();
            GameEvents.OnHazardWarning += Record;
            yield return new WaitForSeconds(0.3f);
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.OnHazardWarning -= Record;
            foreach (var go in created)
                Object.Destroy(go);
            created.Clear();
            rig.Destroy();
        }

        private void Record(HazardBase hazard) => warned.Add(hazard);

        private T Make<T>(string prefab, Vector3 offset) where T : Component
        {
            GameObject go = null;
#if UNITY_EDITOR
            go = Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Hazards/" + prefab + ".prefab"),
                rig.Raft.transform.position + offset, Quaternion.identity);
#endif
            created.Add(go);
            return go.GetComponent<T>();
        }

        // Empuja la balsa hacia +z a 'speed' m/s hasta que llega el aviso de 'hazard' (o pasa 'seconds'); devuelve la distancia
        // al borde del obstáculo en ese momento (negativa si ya estaba dentro).
        private IEnumerator DriveUntilWarned(HazardBase hazard, float speed, float seconds, float[] gapAtWarning)
        {
            var rb = rig.Get<Rigidbody>();
            float end = Time.time + seconds;
            gapAtWarning[0] = float.NaN;
            while (Time.time < end)
            {
                var v = rb.velocity;
                rb.velocity = new Vector3(0f, v.y, speed);
                if (warned.Contains(hazard))
                {
                    var offset = hazard.WarningCenter - rb.worldCenterOfMass;
                    offset.y = 0f;
                    gapAtWarning[0] = offset.magnitude - hazard.WarningRadius;
                    yield break;
                }
                yield return new WaitForFixedUpdate();
            }
        }

        [UnityTest]
        public IEnumerator Whirlpool_WarnsAtLeastWarningTimeBeforeTheRaftReachesIt()
        {
            var pool = Make<Whirlpool>("H09_Whirlpool", new Vector3(0f, 0f, 40f));
            var gap = new float[1];
            yield return DriveUntilWarned(pool, 6f, 8f, gap);
            Assert.IsFalse(float.IsNaN(gap[0]), "avisa al acercarse");
            Assert.GreaterOrEqual(gap[0], 6f * pool.Config.warningTime - 0.5f, "llega a tiempo: al menos warningTime s antes del borde");
            Assert.AreEqual(1, warned.FindAll(h => h == pool).Count, "un solo aviso por acercamiento");
        }

        // Devuelve la balsa al origen, quieta, entre dos acercamientos (el agua de prueba mide 200 m).
        private IEnumerator ResetRaft()
        {
            var rb = rig.Get<Rigidbody>();
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = new Vector3(0f, rb.position.y, 0f);
            rb.rotation = Quaternion.identity;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator Spikes_WarnBeforeContact()
        {
            var spikes = Make<SpikeHazard>("H10_Spikes", new Vector3(0f, 0f, 30f));
            var gap = new float[1];
            yield return DriveUntilWarned(spikes, 5f, 8f, gap);
            Assert.IsFalse(float.IsNaN(gap[0]), "los pinchos avisan");
            Assert.Greater(gap[0], 0f, "antes de tocarlos");
        }

        [UnityTest]
        public IEnumerator Net_WarnsBeforeEntering_BrokenNetDoesNot()
        {
            var net = Make<FishingNet>("H32_FishingNet", new Vector3(0f, 0f, 30f));
            var gap = new float[1];
            yield return DriveUntilWarned(net, 5f, 8f, gap);
            Assert.IsFalse(float.IsNaN(gap[0]), "la red avisa");
            Assert.Greater(gap[0], 0f, "antes de entrar");
            Object.Destroy(net.gameObject);

            yield return ResetRaft();
            var broken = Make<FishingNet>("H32_FishingNet", new Vector3(0f, 0f, 20f));
            broken.Break(Vector3.zero);
            yield return DriveUntilWarned(broken, 5f, 3f, gap);
            Assert.IsTrue(float.IsNaN(gap[0]), "una red rota no avisa");
        }

        [UnityTest]
        public IEnumerator Lookout_MelmanAboard_WarnsAboutThreeSecondsEarlier()
        {
            var plain = Make<Whirlpool>("H09_Whirlpool", new Vector3(0f, 0f, 60f));
            var gap = new float[1];
            yield return DriveUntilWarned(plain, 6f, 12f, gap);
            float plainGap = gap[0];
            Object.Destroy(plain.gameObject);
            yield return ResetRaft();

            // Melman a bordo: su pasiva suma lookoutWarningLead (3 s) a la anticipación de la balsa.
            rig.AddAvatar(0f, "Melman");
            yield return null;
            Assert.AreEqual(rig.Balance.lookoutWarningLead, rig.Get<RaftEffects>().WarningLeadBonus, 1e-4f);
            var withMelman = Make<Whirlpool>("H09_Whirlpool", new Vector3(0f, 0f, 60f));
            yield return DriveUntilWarned(withMelman, 6f, 12f, gap);
            Assert.IsFalse(float.IsNaN(gap[0]));
            Assert.AreEqual(plainGap + 6f * rig.Balance.lookoutWarningLead, gap[0], 2f, "a 6 m/s, 3 s antes son ~18 m más");
        }

        [UnityTest]
        public IEnumerator GiantCroc_WarnsWhenTheChaseStarts_AndPointsAtTheCroc()
        {
            var croc = Make<GiantCrocChaser>("H23_GiantCroc", new Vector3(0f, 0f, 3f));
            var body = croc.transform.Find("Cocodrilo");
            var path = new List<Vector3>();
            for (int i = 0; i < 20; i++)
                path.Add(rig.Raft.transform.position + new Vector3(0f, 0f, -40f + i * 10f));
            body.position = path[0];
            croc.Configure(body, path.ToArray(), 0.9f, 5f);
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(croc.Chasing);
            Assert.Contains(croc, warned, "avisa al empezar la persecución");
            Assert.AreEqual(body.position, croc.WarningCenter, "la flecha del HUD apunta al cocodrilo, no a la zona");
        }

        [UnityTest]
        public IEnumerator Bots_DoNotGrabForTheGiantCroc_AndSteerAroundAWarnedWhirlpool()
        {
            GameObject go = null;
#if UNITY_EDITOR
            go = Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Avatar_Placeholder.prefab"));
#endif
            created.Add(go);
            go.GetComponent<PlayerSeat>().Sit(rig.Get<RaftSeats>(), 180f);
            var bot = go.AddComponent<BotBrain>();
            var config = ScriptableObject.CreateInstance<BotConfig>();
            bot.Attach(new BotCommandSource(), config);
            yield return new WaitForSeconds(0.3f);

            // Cocodrilo gigante: es Fauna, pero agarrarse lo dejaría quieto; el bot sigue sentado y queda "perseguido".
            var croc = Make<GiantCrocChaser>("H23_GiantCroc", new Vector3(0f, 0f, 3f));
            var crocBody = croc.transform.Find("Cocodrilo");
            var path = new List<Vector3>();
            for (int i = 0; i < 20; i++)
                path.Add(rig.Raft.transform.position + new Vector3(0f, 0f, -60f + i * 10f));
            crocBody.position = path[0];
            croc.Configure(crocBody, path.ToArray(), 0.9f, 5f);
            yield return new WaitForSeconds(0.4f);
            Assert.Contains(croc, warned);
            Assert.AreEqual(AvatarState.Seated, go.GetComponent<AvatarStateMachine>().State, "no se agarra ante el cocodrilo gigante");
            Assert.IsTrue(bot.IsChased, "se apura mientras lo persigue");
            Object.Destroy(croc.gameObject);

            // Remolino avisado delante: lo recuerda para rodearlo.
            var pool = Make<Whirlpool>("H09_Whirlpool", new Vector3(0f, 0f, 14f));
            float end = Time.time + 2f;
            while (!warned.Contains(pool) && Time.time < end)
            {
                var rb = rig.Get<Rigidbody>();
                rb.velocity = new Vector3(0f, rb.velocity.y, 5f);
                yield return new WaitForFixedUpdate();
            }
            Assert.Contains(pool, warned);
            Assert.AreEqual(pool, bot.WarnedHazard, "el bot toma el remolino avisado como peligro a rodear");
        }
    }
}
