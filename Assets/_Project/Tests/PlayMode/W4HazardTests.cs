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
    // M5-20: obstáculos de W4 (docs/05) sobre la balsa del prefab en agua quieta.
    public class W4HazardTests
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
        public IEnumerator H17_WaterfallEdge_WarnsBeforeTheDrop()
        {
            var edge = Make<WaterfallEdge>("H17_Waterfall", Vector3.zero);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(edge.IsWarning || edge.WarningProgress >= 0f);
            Assert.AreEqual(3f, edge.Drop);
        }

        [UnityTest]
        public IEnumerator H36_GoldGasGeyser_FillsTheTeamMeter()
        {
            var meter = rig.Get<GasSafari.Team.TeamMeter>();
            Assert.Less(meter.Value, meter.Max);
            var geyser = Make<GoldGasGeyser>("H36_GoldGasGeyser", Vector3.zero);
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(meter.IsFull, "docs/05 H36: al pasar por encima el medidor queda al 100 %");
            Assert.AreEqual(1, geyser.Fills);
        }

        [UnityTest]
        public IEnumerator H22_Abyss_ShowsTheBuoyBridgeAfterTwoFalls()
        {
            var go = new GameObject("Abismo", typeof(BoxCollider));
            created.Add(go);
            var bridge = new GameObject("PuenteBoyas");
            created.Add(bridge);
            bridge.SetActive(false);
            var abyss = go.AddComponent<Abyss>();
            abyss.Configure(bridge);

            Assert.IsTrue(abyss.RegisterFall());
            Assert.IsFalse(abyss.RegisterFall(), "los varios colliders de la balsa cuentan una sola caída");
            Assert.IsFalse(bridge.activeSelf, "con 1 caída no hay ayuda");
            yield return new WaitForSecondsRealtime(2.1f);
            Assert.IsTrue(abyss.RegisterFall());
            Assert.AreEqual(2, abyss.Falls);
            Assert.IsTrue(bridge.activeSelf, "docs/05 H22: a la 2.ª caída aparece el puente de boyas");
            Assert.IsTrue(abyss.HelpActive);
        }

        [UnityTest]
        public IEnumerator H19_Geyser_LaunchesTheRaftEveryThreeSeconds()
        {
            var geyser = Make<Geyser>("H19_Geyser", Vector3.zero);
            var body = rig.Get<Rigidbody>();
            float maxUp = 0f;
            float end = Time.time + 4.5f;
            while (Time.time < end)
            {
                maxUp = Mathf.Max(maxUp, body.velocity.y);
                yield return null;
            }
            Assert.GreaterOrEqual(geyser.Launches, 1, "lanza una vez por ciclo");
            Assert.Greater(maxUp, 8f, "sube a unos 12 m/s");
        }

        [UnityTest]
        public IEnumerator H20_RockFall_ShadowWarnsThenPopsOneChamberIfTheRaftStays()
        {
            var rock = Make<RockFall>("H20_RockFall", Vector3.zero);
            var target = new GameObject("Objetivo").transform;
            created.Add(target.gameObject);
            target.position = rig.Raft.transform.position;
            rock.Configure(new[] { target }, 1.5f, rock.transform.Find("Sombra"));
            yield return new WaitForSeconds(1.9f);
            Assert.IsTrue(rock.IsWarning || rock.Strikes > 0, "avisa con la sombra");
            yield return new WaitForSeconds(1.5f);
            Assert.GreaterOrEqual(rock.Strikes, 1);
            Assert.GreaterOrEqual(popped, 1, "revienta una cámara");
        }

        [UnityTest]
        public IEnumerator H31_Current_PushesSideways()
        {
            Make<CurrentZone>("H31_Current", Vector3.zero);
            var body = rig.Get<Rigidbody>();
            yield return new WaitForSeconds(1.2f);
            Assert.Greater(body.velocity.x, 1.5f, "4 m/s² hacia transform.right");
        }

        [UnityTest]
        public IEnumerator H18_DarkZone_LimitsVisibilityInsideAndRestoresOutside()
        {
            float before = RenderSettings.fogEndDistance;
            var dark = Make<DarkZone>("H18_Dark", Vector3.zero);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(dark.Inside);
            Assert.LessOrEqual(RenderSettings.fogEndDistance, 6.1f, "visibilidad de 6 m");
            rig.Raft.transform.position = new Vector3(200f, 0.2f, 200f);
            rig.Get<Rigidbody>().position = rig.Raft.transform.position;
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(dark.Inside);
            Assert.AreEqual(before, RenderSettings.fogEndDistance, 0.01f);
        }

        [UnityTest]
        public IEnumerator H18_Blackout_FadesTheFirefliesAndDarkensMoreThenTheyReturn()
        {
            var lights = new Light[3];
            for (int i = 0; i < lights.Length; i++)
            {
                var go = new GameObject("Luciernaga_" + i);
                created.Add(go);
                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.intensity = 2f;
                lights[i] = light;
            }
            var zoneGo = new GameObject("ZonaApagon", typeof(BoxCollider));
            created.Add(zoneGo);
            zoneGo.transform.position = rig.Raft.transform.position;
            zoneGo.GetComponent<BoxCollider>().isTrigger = true;
            zoneGo.GetComponent<BoxCollider>().size = new Vector3(20f, 10f, 20f);
            zoneGo.SetActive(false);
            var dark = zoneGo.AddComponent<DarkZone>();
            dark.Configure(6f, lights, 4f);
            bool started = false, ended = false;
            System.Action onStart = () => started = true;
            System.Action onEnd = () => ended = true;
            DarkZone.BlackoutStarted += onStart;
            DarkZone.BlackoutEnded += onEnd;
            zoneGo.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            try
            {
                Assert.IsTrue(started, "el apagón se anuncia");
                Assert.IsTrue(dark.LightsOut);
                Assert.Greater(dark.Darkness, 0f, "el apagón es gradual, no un corte");
                Assert.Less(dark.Darkness, 1f);
                Assert.Less(lights[0].intensity, 2f, "las luciérnagas ya se están apagando");

                yield return new WaitForSeconds(1.6f);
                Assert.AreEqual(1f, dark.Darkness, 0.01f);
                Assert.IsFalse(lights[0].enabled && lights[2].enabled, "al final del fundido están apagadas");
                Assert.AreEqual(dark.BlackoutVisibility, RenderSettings.fogEndDistance, 0.1f, "más oscuro que los 6 m normales");

                yield return new WaitForSeconds(3.6f);
                Assert.IsTrue(ended, "pasado el apagón vuelven");
                Assert.IsFalse(dark.LightsOut);
                Assert.AreEqual(2f, lights[0].intensity, 0.05f);
                Assert.IsTrue(lights[0].enabled);
            }
            finally
            {
                DarkZone.BlackoutStarted -= onStart;
                DarkZone.BlackoutEnded -= onEnd;
            }
        }

        [UnityTest]
        public IEnumerator W4_5_CollapseWarnsBeforeItAdvances_RocksFallBehindAndTheScreenEdgeTurnsRed()
        {
            var rubble = new GameObject("Derrumbe").transform;
            rubble.position = rig.Raft.transform.position - Vector3.forward * 20f;
            created.Add(rubble.gameObject);
            var rocks = new Transform[4];
            for (int i = 0; i < rocks.Length; i++)
            {
                rocks[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
                Object.Destroy(rocks[i].GetComponent<Collider>());
                rocks[i].SetParent(rubble, true);
                created.Add(rocks[i].gameObject);
            }
            var zoneGo = new GameObject("ZonaDerrumbe", typeof(BoxCollider));
            created.Add(zoneGo);
            zoneGo.transform.position = rig.Raft.transform.position;
            zoneGo.GetComponent<BoxCollider>().isTrigger = true;
            zoneGo.GetComponent<BoxCollider>().size = new Vector3(20f, 10f, 20f);
            zoneGo.SetActive(false);
            var chaser = zoneGo.AddComponent<CollapseChaser>();
            chaser.Configure(rubble, new[] { rubble.position, rubble.position + Vector3.forward * 40f }, 4.5f);
            chaser.ConfigureWarning(rocks, 1.5f);
            zoneGo.SetActive(true);
            yield return new WaitForSeconds(0.6f);

            Assert.IsTrue(chaser.Warning, "primero avisa");
            Assert.IsFalse(chaser.Running, "el techo aún no avanza durante el aviso");
            Assert.IsTrue(GasSafari.UI.ScreenEdgeWarning.IsShowing, "borde rojo en la pantalla");
            Assert.Greater(GasSafari.UI.ScreenEdgeWarning.Current.Alpha, 0f);
            Assert.AreEqual(rig.Raft.transform.position.z - 20f, rubble.position.z, 0.1f, "no avanza durante el aviso");

            yield return new WaitForSeconds(1.4f);
            Assert.IsTrue(chaser.Running, "tras el aviso el derrumbe avanza");
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(rocks.Length, chaser.DebrisLanded, "las rocas cayeron detrás de la balsa");
        }

        [UnityTest]
        public IEnumerator H26_Bats_ThrowALooseAnimalAfterTheScreech()
        {
            var loose = rig.AddAvatar(90f);
            yield return new WaitForSeconds(0.3f);
            var bats = Make<BatSwarm>("H26_Bats", new Vector3(2.5f, 0f, 0f));
            yield return new WaitForSeconds(0.25f);
            Assert.IsTrue(bats.IsWarning, "chillido de aviso");
            yield return new WaitForSeconds(1.2f);
            Assert.IsFalse(loose.GetComponent<GasSafari.Raft.PlayerSeat>().IsSeated);
        }

        [UnityTest]
        public IEnumerator H10_CrystalSpikes_PopTheTouchingChamber()
        {
            Make<SpikeHazard>("H10_CrystalSpikes", new Vector3(0f, 0f, 3f));
            rig.Get<Rigidbody>().velocity = Vector3.forward * 3f;
            yield return new WaitForSeconds(1f);
            Assert.GreaterOrEqual(popped, 1);
        }
    }
}
