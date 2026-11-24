using System.Collections;
using System.Linq;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Modes;
using GasSafari.Progression;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M7-09 Infinito con 4 bots (docs/06 §1): el río se arma por delante, la dificultad sube cada 200 m, los checkpoints
    // (uno cada 3 tramos) se alcanzan, los tramos de atrás se quitan, la balsa hundida reaparece en el último checkpoint
    // sobre agua y con cauce, y al tercer hundimiento la partida termina con la distancia como puntuación y récord guardado.
    public class EndlessModeTests
    {
        private const float TARGET_METERS = 450f;   // dificultad 3
        private const float LIMIT = 330f;            // s de juego

        private EndlessTestRig rig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new EndlessTestRig();
            rig.Prepare(ProgressionRules.MODE_ENDLESS);
            EndlessTrack.SeedOverride = 4242;
            yield return rig.Load();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return rig.Unload();
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Endless_FourBots_ClimbDifficulty_RespawnAtCheckpoint_EndAfterThreeSinks()
        {
            var mode = rig.Mode as EndlessMode;
            Assert.IsNotNull(mode, "la escena Infinito se juega en modo Infinito");
            Assert.IsFalse(rig.Track.IsDaily);
            Assert.AreEqual(4242, rig.Track.Seed);
            Assert.IsFalse(Object.FindFirstObjectByType<FinishGate>().GetComponent<Collider>().enabled, "Infinito no tiene meta");
            Assert.GreaterOrEqual(rig.Track.ChunksPlaced, 5, "río armado por delante al cargar");
            int tierUps = 0;
            rig.Track.TierChanged += _ => tierUps++;
            int naturalSinks = 0;
            System.Action sunk = () => naturalSinks++;
            GameEvents.OnRaftSunk += sunk;
            GameEvents.OnRaftCapsized += sunk;
            yield return rig.WaitForRunning();

            float elapsed = 0f;
            while (rig.Track.MaxDistance < TARGET_METERS && elapsed < LIMIT && !mode.Ended)
            {
                yield return new WaitForSeconds(1f);
                elapsed += 1f;
                if ((int)elapsed % 20 == 0)
                    rig.Log("bots", elapsed);
                Assert.LessOrEqual(rig.Track.LiveChunks.Count, 16, "los tramos de atrás se quitan");
            }
            GameEvents.OnRaftSunk -= sunk;
            GameEvents.OnRaftCapsized -= sunk;
            rig.Log("bots-fin", elapsed);
            Assert.IsFalse(mode.Ended, $"los bots no pierden las 3 balsas antes de {TARGET_METERS} m (hundimientos: {naturalSinks})");
            Assert.GreaterOrEqual(rig.Track.MaxDistance, TARGET_METERS, $"4 bots recorren {TARGET_METERS} m en menos de {LIMIT} s");
            Assert.GreaterOrEqual(rig.Track.Tier, 3, "dificultad 3 pasados los 400 m");
            Assert.GreaterOrEqual(tierUps, 2, "aviso de dificultad 2 y 3");
            Assert.GreaterOrEqual(rig.Checkpoints.CurrentIndex, 2, "alcanzan los checkpoints de los tramos");
            Assert.Greater(rig.Track.LiveChunks[0].Index, 0, "el tramo de salida ya se quitó");
            StringAssert.Contains("m", mode.Hud.MainText);

            // Hundimiento: reaparece en el último checkpoint, sobre agua y con cauce, y sigue avanzando.
            var checkpoint = rig.Checkpoints.Current.transform;
            int livesBefore = mode.Lives;
            yield return Sink();
            Assert.AreEqual(livesBefore - 1, mode.Lives);
            var raft = rig.Level.Raft.Body.position;
            Assert.Less(Vector3.Distance(raft, checkpoint.position), 2f, "reaparece en el checkpoint");
            Assert.IsTrue(WaterSurface.TryGetHeight(raft, out _), "hay agua donde reaparece");
            Assert.IsTrue(rig.Track.LiveChunks.Any(c => c.gameObject.activeInHierarchy && Vector3.Distance(c.transform.position, raft) < 70f),
                "el tramo del checkpoint sigue en pie");
            // La distancia se recalcula en el Update siguiente a la reaparición.
            yield return null;
            yield return null;
            float distanceAfter = rig.Track.Distance;
            Assert.Less(distanceAfter, rig.Track.MaxDistance, "la distancia vuelve al checkpoint; el récord de la partida no baja");
            yield return new WaitForSeconds(15f);
            Assert.Greater(rig.Track.Distance, distanceAfter + 5f, "los bots siguen desde el checkpoint");

            // Las balsas que quedan: al último hundimiento la partida termina.
            while (!mode.Ended)
                yield return Sink();
            int meters = mode.Meters;
            yield return rig.WaitForState(LevelState.Results, 10f);
            var results = Object.FindFirstObjectByType<ResultsController>().Results;
            Assert.IsNotNull(results, "resultados al perder la última balsa");
            Assert.AreEqual(ProgressionRules.MODE_ENDLESS, results.RunMode);
            Assert.IsFalse(results.ShowStars);
            Assert.AreEqual(0, results.Stars);
            Assert.AreEqual(meters, (int)results.Distance, "la puntuación es la distancia");
            Assert.IsTrue(results.NewRecord);
            Assert.That(results.Beans, Is.InRange(5, 25), "docs/06 §2: 5 a 25 frijolitos");
            Assert.AreEqual(meters, rig.Save.endlessMeters, "récord de distancia en el guardado");
            Assert.AreEqual(results.Beans, rig.Save.frijolitos);
            Assert.IsEmpty(rig.Save.levels.Where(l => l.id == "INF"), "Infinito no guarda un nivel");
        }

        private IEnumerator Sink()
        {
            yield return rig.WaitForState(LevelState.Running, 10f);
            bool respawned = false;
            System.Action done = () => respawned = true;
            GameEvents.OnRespawn += done;
            GameEvents.RaiseRaftSunk();
            float t = 0f;
            while (!respawned && t < 10f && rig.Level.State != LevelState.Finished && rig.Level.State != LevelState.Results)
            {
                t += Time.deltaTime;
                yield return null;
            }
            GameEvents.OnRespawn -= done;
        }
    }
}
