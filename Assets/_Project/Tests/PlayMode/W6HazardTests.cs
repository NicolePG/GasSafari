using System.Collections;
using System.Collections.Generic;
using GasSafari.Bots;
using GasSafari.Core;
using GasSafari.Hazards;
using GasSafari.Raft;
using GasSafari.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M5-32 y decisiones del 2026-11-08: H30 Coco que cae (docs/05), el aviso de 30 m antes de cada abismo H22 y las rutas
    // alternativas de los bots (BotHint Route).
    public class W6HazardTests
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
            BotHint.ForcedRoute = null;
            ScreenEdgeWarning.Hide();
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
        public IEnumerator H30_Coconut_ShadowWarns08s_ThenPushesTheRaftWithoutDamage()
        {
            var coconut = Make<CoconutFall>("H30_Coconut", Vector3.zero);
            Assert.AreEqual(0.8f, coconut.Config.warningTime, 0.001f, "docs/05 H30: sombra de 0.8 s");
            Assert.AreEqual(20f, coconut.Push, 0.001f, "docs/05 H30: empuje 20");
            var target = new GameObject("Objetivo").transform;
            created.Add(target.gameObject);
            target.position = rig.Raft.transform.position + new Vector3(1.2f, 0f, 0f);
            var shadow = coconut.transform.Find("Sombra");
            coconut.Configure(new[] { target }, 1f, shadow);
            var chambers = rig.Get<RaftChambers>();
            int intact = chambers.IntactCount;
            var body = rig.Get<Rigidbody>();

            float waited = 0f;
            while (!coconut.IsWarning && waited < 3f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(coconut.IsWarning, "avisa");
            Assert.IsTrue(shadow.gameObject.activeSelf, "la sombra se ve durante el aviso");
            float warnedAt = Time.time;
            while (coconut.Strikes == 0 && Time.time - warnedAt < 2f)
                yield return null;
            Assert.AreEqual(1, coconut.Strikes);
            Assert.AreEqual(0.8f, Time.time - warnedAt, 0.12f, "el coco golpea 0.8 s después de aparecer la sombra");
            Assert.AreEqual(1, coconut.Hits, "la balsa está a menos de 2.5 m del punto");
            Assert.IsFalse(shadow.gameObject.activeSelf, "la sombra desaparece al caer");
            yield return new WaitForSeconds(0.15f); // el impulso se aplica en el siguiente paso de física
            Assert.Less(body.velocity.x, -0.01f, "la empuja alejándose del punto de impacto");
            Assert.AreEqual(intact, chambers.IntactCount, "sin daño");
        }

        [UnityTest]
        public IEnumerator H30_Coconut_MissesARaftOutsideTheRadius()
        {
            var coconut = Make<CoconutFall>("H30_Coconut", Vector3.zero);
            var target = new GameObject("Objetivo").transform;
            created.Add(target.gameObject);
            target.position = rig.Raft.transform.position + new Vector3(6f, 0f, 0f);
            coconut.Configure(new[] { target }, 0.5f, coconut.transform.Find("Sombra"));
            yield return new WaitForSeconds(1.6f);
            Assert.GreaterOrEqual(coconut.Strikes, 1);
            Assert.AreEqual(0, coconut.Hits, "a más de 2.5 m no la toca");
        }

        [UnityTest]
        public IEnumerator H30_Coconut_RainFallsInFrontOfTheRaft()
        {
            var coconut = Make<CoconutFall>("H30_Coconut", Vector3.zero);
            coconut.ConfigureRain(0.6f, coconut.transform.Find("Sombra"), 0.8f, 0.6f);
            var body = rig.Get<Rigidbody>();
            body.velocity = new Vector3(0f, 0f, 6f);
            yield return new WaitForSeconds(0.9f);
            Assert.IsTrue(coconut.AimsAtRaft);
            Assert.Greater(coconut.StrikePoint.z, rig.Raft.transform.position.z - 3f, "el punto está por delante de la balsa que avanza");
        }

        [UnityTest]
        public IEnumerator H22_AbyssWarning_RaisesTheHazardWarningAndTheRedScreenEdge()
        {
            HazardBase warned = null;
            System.Action<HazardBase> onWarning = h => warned = h;
            GameEvents.OnHazardWarning += onWarning;
            var warning = Make<AbyssWarning>("H22_AbyssWarning", Vector3.zero);
            yield return new WaitForSeconds(0.4f);
            GameEvents.OnHazardWarning -= onWarning;
            Assert.AreSame(warning, warned, "emite el aviso de peligro del HUD");
            Assert.AreEqual(1, warning.Warnings);
            Assert.IsTrue(ScreenEdgeWarning.IsShowing, "borde rojo de pantalla");
            Assert.AreEqual(30f, AbyssWarning.DISTANCE_BEFORE, "30 m antes del abismo");
        }

        [UnityTest]
        public IEnumerator BotHint_Route_IsIgnoredUnlessForced_ThenLeadsTheBotAlongItsPoints()
        {
            var go = new GameObject("Ruta");
            created.Add(go);
            var hint = go.AddComponent<BotHint>();
            hint.Configure(BotHintType.Route, 6f);
            hint.ConfigureRoute("rapida", new[] { new Vector3(0f, 0f, 10f), new Vector3(10f, 0f, 20f), new Vector3(10f, 0f, 40f) });
            yield return null;

            BotHint current = null;
            int index = 0;
            Assert.IsFalse(BotHint.FollowRoute(Vector3.zero, 12f, ref current, ref index, out _), "sin ruta forzada no hace nada");

            BotHint.ForcedRoute = "mangos";
            Assert.IsFalse(BotHint.FollowRoute(Vector3.zero, 12f, ref current, ref index, out _), "otra ruta forzada: no es esta");

            BotHint.ForcedRoute = "rapida";
            Assert.IsTrue(BotHint.FollowRoute(Vector3.zero, 12f, ref current, ref index, out var target), "entra a 10 m de su primer punto");
            Assert.AreEqual(new Vector3(0f, 0f, 10f), target);
            Assert.IsTrue(BotHint.FollowRoute(new Vector3(0f, 0f, 9f), 12f, ref current, ref index, out target), "al llegar al primer punto pasa al segundo");
            Assert.AreEqual(new Vector3(10f, 0f, 20f), target);
            Assert.IsTrue(BotHint.FollowRoute(new Vector3(10f, 0f, 19f), 12f, ref current, ref index, out target));
            Assert.AreEqual(new Vector3(10f, 0f, 40f), target);
            Assert.IsFalse(BotHint.FollowRoute(new Vector3(10f, 0f, 39f), 12f, ref current, ref index, out _), "pasado el último punto suelta la ruta");
            Assert.IsNull(current);
        }
    }
}
