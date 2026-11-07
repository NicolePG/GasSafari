using System.Collections;
using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Hazards;
using GasSafari.Level;
using GasSafari.Raft;
using GasSafari.Team;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M5-01: condiciones de las lecciones del T-0 (docs/04 T-0) y obstáculos H33-H35 (docs/05).
    public class TutorialTests
    {
        private RaftTestRig rig;
        private readonly List<Object> created = new List<Object>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            yield return new WaitForSeconds(0.3f);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created)
                Object.Destroy(o);
            created.Clear();
            rig.Destroy();
        }

        private TutorialLesson MakeLesson(TutorialCondition condition, float amount, out BubbleBarrier barrier, PlayerAvatar[] crew,
            TutorialGate[] gates = null, HoseJet jet = null)
        {
            var step = ScriptableObject.CreateInstance<TutorialStep>();
            step.condition = condition;
            step.amount = amount;
            created.Add(step);

            var barrierGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrierGo.transform.position = new Vector3(0f, 0f, 30f);
            barrier = barrierGo.AddComponent<BubbleBarrier>();
            barrier.Configure(barrierGo.GetComponent<Collider>(), new[] { barrierGo.GetComponent<Renderer>() });
            created.Add(barrierGo);

            var zone = new GameObject("Zona");
            zone.layer = GameLayers.Trigger;
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(20f, 6f, 20f);
            zone.SetActive(false);
            var lesson = zone.AddComponent<TutorialLesson>();
            lesson.Configure(step, box, barrier, gates, jet);
            zone.SetActive(true);
            lesson.Bind(crew, rig.Get<RaftController>());
            lesson.Begin();
            created.Add(zone);
            return lesson;
        }

        [UnityTest]
        public IEnumerator Puff_CompletesLesson1AndOpensBarrier()
        {
            var input = rig.AddAvatar(180f);
            yield return null;
            var lesson = MakeLesson(TutorialCondition.Puff, 0f, out var barrier, new[] { input.GetComponent<PlayerAvatar>() });
            Assert.IsFalse(barrier.IsOpen);
            input.Command.FartPresses++;
            yield return null;
            yield return null;
            Assert.IsTrue(lesson.IsComplete);
            Assert.IsTrue(barrier.IsOpen);
        }

        [UnityTest]
        public IEnumerator MoveSeat90_CompletesLesson2()
        {
            var input = rig.AddAvatar(180f);
            yield return null;
            var lesson = MakeLesson(TutorialCondition.MoveSeat, 90f, out _, new[] { input.GetComponent<PlayerAvatar>() });
            yield return null;
            input.Command.SeatMove = 1f;
            float t = 0f;
            while (!lesson.IsComplete && t < 3f)
            {
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(lesson.IsComplete, "120°/s de asiento: 90° en menos de 1 s");
        }

        [UnityTest]
        public IEnumerator Blast_NeedsOneSecondHeld()
        {
            var input = rig.AddAvatar(180f);
            yield return null;
            var lesson = MakeLesson(TutorialCondition.Blast, 1f, out _, new[] { input.GetComponent<PlayerAvatar>() });
            input.Command.FartHeld = true;
            yield return new WaitForSeconds(0.7f);
            Assert.IsFalse(lesson.IsComplete);
            yield return new WaitForSeconds(0.7f);
            Assert.IsTrue(lesson.IsComplete);
        }

        [UnityTest]
        public IEnumerator RoundBuoy_GatesMustBePassedInOrder()
        {
            var body = rig.Get<Rigidbody>();
            body.isKinematic = true;
            var gates = new TutorialGate[3];
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("Marca" + i);
                go.layer = GameLayers.Trigger;
                go.transform.position = new Vector3(20f + i * 10f, 0f, 0f);
                go.AddComponent<BoxCollider>().isTrigger = true;
                go.GetComponent<BoxCollider>().size = Vector3.one * 3f;
                gates[i] = go.AddComponent<TutorialGate>();
                created.Add(go);
            }
            var lesson = MakeLesson(TutorialCondition.RoundBuoy, 0f, out _, new PlayerAvatar[0], gates);

            // Primero la marca 2: no cuenta.
            yield return MoveRaft(body, gates[1].transform.position);
            yield return MoveRaft(body, gates[0].transform.position);
            yield return MoveRaft(body, gates[2].transform.position);
            Assert.IsFalse(lesson.IsComplete, "falta la 2 después de la 1");
            yield return MoveRaft(body, gates[1].transform.position);
            yield return MoveRaft(body, gates[2].transform.position);
            Assert.IsTrue(lesson.IsComplete);
        }

        private static IEnumerator MoveRaft(Rigidbody body, Vector3 target)
        {
            body.position = new Vector3(target.x, 0.2f, target.z + 10f);
            yield return new WaitForFixedUpdate();
            body.MovePosition(new Vector3(target.x, 0.2f, target.z));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            body.position = new Vector3(target.x, 0.2f, target.z + 10f);
            yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator HoseJet_PushesSidewaysAndKnocksOffLooseAnimalsAfterOneAndAHalfSeconds()
        {
            var loose = rig.AddAvatar(180f);
            var grabber = rig.AddAvatar(0f);
            grabber.Command.GrabHeld = true;
            var jet = MakeHazard<HoseJet>("H34_HoseJet");
            jet.transform.position = rig.Raft.transform.position;
            yield return new WaitForSeconds(1f);
            Assert.Greater(rig.Get<Rigidbody>().velocity.x, 2f, "6 m/s² hacia transform.right");
            Assert.IsTrue(loose.GetComponent<PlayerSeat>().IsSeated);
            yield return new WaitForSeconds(0.8f);
            Assert.IsFalse(loose.GetComponent<PlayerSeat>().IsSeated, "suelto: al agua tras 1.5 s");
            Assert.IsTrue(grabber.GetComponent<PlayerSeat>().IsSeated, "agarrado: sigue");
        }

        [UnityTest]
        public IEnumerator BalloonWall_BreaksWithSyncNearby()
        {
            var wall = MakeHazard<BalloonWall>("H33_BalloonWall");
            wall.transform.position = rig.Raft.transform.position + new Vector3(0f, 0f, 4f);
            yield return null;
            GameEvents.RaiseSync(1, 1f);
            Assert.IsFalse(wall.Broken, "un solo jugador no es sync");
            GameEvents.RaiseSync(2, 1.3f);
            Assert.IsTrue(wall.Broken);
            GameEvents.RaiseRespawn();
            Assert.IsFalse(wall.Broken, "vuelve al reaparecer");
        }

        [UnityTest]
        public IEnumerator BalloonWall_FarSyncDoesNotBreak()
        {
            var wall = MakeHazard<BalloonWall>("H33_BalloonWall");
            wall.transform.position = rig.Raft.transform.position + new Vector3(0f, 0f, 30f);
            yield return null;
            GameEvents.RaiseSync(4, 2f);
            Assert.IsFalse(wall.Broken);
        }

        [UnityTest]
        public IEnumerator SinkingPlatform_SinksOneSecondAfterTouchAndDisappearsInSix()
        {
            var platform = MakeHazard<SinkingPlatform>("H35_SinkingPlatform");
            platform.gameObject.SetActive(false);
            // Bajo la balsa (dinámica): al apoyarse la toca. Dos cuerpos cinemáticos no generan contacto.
            platform.transform.position = rig.Raft.transform.position + new Vector3(0f, -0.55f, 0f);
            platform.gameObject.SetActive(true);
            float y0 = platform.transform.position.y;
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(y0, platform.transform.position.y, 0.01f, "1 s de aviso sin moverse");
            yield return new WaitForSeconds(3.5f);
            Assert.Less(platform.transform.position.y, y0 - 0.3f, "hundiéndose");
            Assert.IsFalse(platform.IsGone);
            yield return new WaitForSeconds(3.2f);
            Assert.IsTrue(platform.IsGone, "bajo el agua a los 7 s");
            GameEvents.RaiseRespawn();
            Assert.IsFalse(platform.IsGone);
            Assert.AreEqual(y0, platform.transform.position.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator SuperFartLesson_FillsTeamMeterInOneSecondOnEnter()
        {
            var input = rig.AddAvatar(180f);
            var step = ScriptableObject.CreateInstance<TutorialStep>();
            step.condition = TutorialCondition.SuperFart;
            step.fillTeamMeter = true;
            created.Add(step);
            var zone = new GameObject("Zona10");
            zone.SetActive(false);
            zone.layer = GameLayers.Trigger;
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = Vector3.one * 20f;
            zone.transform.position = rig.Raft.transform.position + Vector3.forward * 40f;
            var lesson = zone.AddComponent<TutorialLesson>();
            lesson.Configure(step, box, null, null, null);
            var director = zone.AddComponent<TutorialDirector>();
            director.Configure(new[] { lesson }, null, null, null);
            zone.SetActive(true);
            created.Add(zone);
            director.Bind(rig.Get<RaftController>(), new[] { input.GetComponent<PlayerAvatar>() });

            var meter = rig.Get<TeamMeter>();
            Assert.AreEqual(0f, meter.Value, 0.01f);
            zone.transform.position = rig.Raft.transform.position;
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(meter.Value, 10f);
            Assert.Less(meter.Value, meter.Max);
            yield return new WaitForSeconds(0.8f);
            Assert.IsTrue(meter.IsFull);
        }

        private T MakeHazard<T>(string prefab) where T : Component
        {
            GameObject go = null;
#if UNITY_EDITOR
            go = Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Hazards/" + prefab + ".prefab"));
#endif
            created.Add(go);
            return go.GetComponent<T>();
        }
    }
}
