using System.Collections;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Level;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M5-01: la escena del T-0 tiene las 10 lecciones de docs/04 en orden, 10 checkpoints y el cartel de la 1 al empezar.
    public class T0LevelTests
    {
        private const string SCENE = "L_T0_PiscinaZoo";

        [UnityTest]
        public IEnumerator T0_HasTenLessonsInOrderAndShowsTheFirstCard()
        {
#if UNITY_EDITOR
            Services.Session.ClearPlayers();
            var data = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_Project/Data/Characters/Alex.asset");
            Services.Session.AddPlayer(new PlayerSlot(0, Color.white, null, null) { Character = data });
            var op = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                $"Assets/_Project/Scenes/Levels/{SCENE}.unity", new LoadSceneParameters(LoadSceneMode.Additive));
            yield return op;
            yield return new WaitForSeconds(0.5f);

            var director = Object.FindFirstObjectByType<TutorialDirector>();
            Assert.IsNotNull(director);
            Assert.AreEqual(10, director.Lessons.Count);
            for (int i = 0; i < 10; i++)
                Assert.AreEqual((TutorialCondition)i, director.Lessons[i].Step.condition, $"lección {i + 1}");
            Assert.AreEqual(10, Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(0, director.CurrentIndex);
            Assert.IsTrue(director.Lessons[0].IsActive);
            Assert.IsTrue(director.Card.IsShowing);
            Assert.AreEqual(director.Lessons[0].Step.title, director.Card.Title);
            Assert.IsFalse(director.Lessons[0].Barrier.IsOpen);

            var manager = LevelManager.Current;
            foreach (var avatar in manager.Avatars)
                Object.Destroy(avatar.gameObject);
            Object.Destroy(manager.Raft.gameObject);
            yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName(SCENE));
            Services.Session.ClearPlayers();
#else
            yield break;
#endif
        }

        // Lección 9: pedo cargado de 1 s al pie del tobogán abre la barrera; con ráfaga la balsa sube y H27 la lanza.
        [UnityTest, Timeout(120000)]
        public IEnumerator T0_Lesson9_ChargedFartOpensBarrierAndRaftClimbsTheSlide()
        {
            yield return Load();
            var director = Object.FindFirstObjectByType<TutorialDirector>();
            var input = Control();
            for (int i = 0; i < 8; i++)
                director.Lessons[i].Complete();
            Assert.AreEqual(8, director.CurrentIndex);
            Teleport(173f);
            yield return new WaitForSeconds(0.6f);

            input.Command.HoldHeld = true;
            yield return new WaitForSeconds(1.15f);
            input.Command.FartPresses++;
            yield return null;
            yield return null;
            input.Command.HoldHeld = false;
            Assert.IsTrue(director.Lessons[8].IsComplete, "cargado de 1 s o más en la zona del tobogán");
            Assert.IsTrue(director.Lessons[8].Barrier.IsOpen);

            var telemetry = LevelManager.Current.Raft.gameObject.AddComponent<GasSafari.Raft.RaftTelemetry>();
            // Tras el cargado hay que soltar y volver a pulsar para la ráfaga (FartThruster espera la suelta).
            yield return new WaitForSeconds(0.2f);
            input.Command.FartHeld = true;
            float maxY = 0f;
            float t = 0f;
            var raft = LevelManager.Current.Raft.transform;
            while (t < 8f && raft.position.z < 198f)
            {
                maxY = Mathf.Max(maxY, raft.position.y);
                t += Time.deltaTime;
                yield return null;
            }
            input.Command.FartHeld = false;
            telemetry.WriteCsv(System.IO.Path.Combine(RaftHighSpeedTests.TelemetryFolder, "t0_tobogan.csv"));
            Assert.Greater(raft.position.z, 198f, "pasa el tobogán");
            Assert.Greater(maxY, 2.5f, "subió la rampa y saltó");
        }

        // Lección 10: el medidor se llena al entrar, el Súper Pedo salta el muro y suena la toma de 2 s.
        [UnityTest, Timeout(120000)]
        public IEnumerator T0_Lesson10_SuperFartJumpsTheZooWall()
        {
            yield return Load();
            var director = Object.FindFirstObjectByType<TutorialDirector>();
            var input = Control();
            for (int i = 0; i < 9; i++)
                director.Lessons[i].Complete();
            Teleport(204f);
            yield return new WaitForSeconds(1.4f);
            var raftController = LevelManager.Current.Raft;
            Assert.IsTrue(raftController.GetComponent<GasSafari.Team.TeamMeter>().IsFull, "medidor regalado al 100 %");

            input.Command.SuperHeld = true;
            yield return new WaitForSeconds(0.3f);
            input.Command.SuperHeld = false;
            Assert.IsTrue(director.Lessons[9].IsComplete);
            Assert.AreEqual(UnityEngine.Playables.PlayState.Playing,
                Object.FindFirstObjectByType<UnityEngine.Playables.PlayableDirector>().state, "toma de Timeline");
            yield return new WaitForSeconds(3f);
            Assert.Greater(raftController.transform.position.z, 217f, "al otro lado del muro (z = 214)");
        }

        private static IEnumerator Load()
        {
#if UNITY_EDITOR
            GameEvents.ClearAll();
            Services.Session.ClearPlayers();
            var data = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_Project/Data/Characters/Alex.asset");
            Services.Session.AddPlayer(new PlayerSlot(0, Color.white, null, null) { Character = data });
            var op = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                $"Assets/_Project/Scenes/Levels/{SCENE}.unity", new LoadSceneParameters(LoadSceneMode.Additive));
            yield return op;
            // Cuenta atrás de la salida.
            float t = 0f;
            while (t < 8f && (LevelManager.Current == null || LevelManager.Current.State != LevelState.Running))
            {
                t += Time.deltaTime;
                yield return null;
            }
#else
            yield break;
#endif
        }

        private static ScriptedCommandSource Control()
        {
            var avatar = LevelManager.Current.Avatars[0];
            var source = avatar.gameObject.AddComponent<ScriptedCommandSource>();
            avatar.SetCommandSource(source);
            avatar.GetComponent<GasTank>().SetFraction(1f);
            return source;
        }

        // El cauce del T-0 es recto hacia +Z: la distancia es la coordenada z.
        private static void Teleport(float distance)
        {
            var body = LevelManager.Current.Raft.Body;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = new Vector3(0f, 0.25f, distance);
            body.rotation = Quaternion.identity;
        }

        // Descarga aunque la prueba haya fallado (si no, la escena queda para la siguiente).
        [UnityTearDown]
        public IEnumerator Unload()
        {
            if (!SceneManager.GetSceneByName(SCENE).isLoaded)
                yield break;
            var manager = LevelManager.Current;
            if (manager != null)
            {
                foreach (var avatar in manager.Avatars)
                    Object.Destroy(avatar.gameObject);
                Object.Destroy(manager.Raft.gameObject);
            }
            yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName(SCENE));
            Services.Session.ClearPlayers();
        }
    }
}
