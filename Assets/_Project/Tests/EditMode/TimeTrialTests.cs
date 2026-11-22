using System.IO;
using GasSafari.Level;
using GasSafari.Modes;
using GasSafari.Progression;
using GasSafari.Save;
using GasSafari.UI;
using NUnit.Framework;
using UnityEngine;

namespace GasSafari.Tests
{
    // M7-01 Contrarreloj (docs/06 §1): grabación a 10 Hz, archivo del fantasma, tabla de los 5 mejores y textos de resultados.
    public class TimeTrialTests
    {
        private string folder;
        private string previousFolder;

        [SetUp]
        public void SetUp()
        {
            previousFolder = GhostStore.Folder;
            folder = Path.Combine(Path.GetTempPath(), "gasSafariGhost_" + System.Guid.NewGuid().ToString("N"));
            GhostStore.Folder = Path.Combine(folder, "ghosts");
        }

        [TearDown]
        public void TearDown()
        {
            GhostStore.Folder = previousFolder;
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        [Test]
        public void Config_FollowsTheDocs()
        {
            var config = ModesConfig.Instance;
            Assert.IsNotNull(config, "Resources/ModesConfig.asset (Gas Safari/Setup/Crear modos de M7)");
            Assert.AreEqual(10f, config.ghostSampleRate, "docs/06: pose de la balsa a 10 Hz");
            Assert.AreEqual(5, config.topTimes, "docs/06: tabla local con los 5 mejores tiempos");
            Assert.IsNotNull(config.ghostMaterial, "material semitransparente del fantasma");
            Assert.Less(config.ghostColor.a, 1f);
            var mode = System.Array.Find(GameCatalog.Instance.modes, m => m.id == ProgressionRules.MODE_TIME_TRIAL);
            Assert.IsTrue(mode.implemented, "la tarjeta de Contrarreloj ya no dice Próximamente");
        }

        [Test]
        public void Recorder_SamplesAtFixedRate_AndFillsSkippedFrames()
        {
            var recorder = new GhostRecorder("W1-1", 10f, 1f);
            recorder.Tick(0f, Vector3.zero, Quaternion.identity);
            Assert.AreEqual(1, recorder.Count);
            recorder.Tick(0.05f, Vector3.one, Quaternion.identity);
            Assert.AreEqual(1, recorder.Count, "a 10 Hz la siguiente muestra es a los 0.1 s");
            recorder.Tick(0.35f, new Vector3(3f, 0f, 0f), Quaternion.identity);
            Assert.AreEqual(4, recorder.Count, "un cuadro largo rellena las muestras 0.1, 0.2 y 0.3");
            // Más de 60 s: el búfer crece.
            recorder.Tick(90f, Vector3.zero, Quaternion.identity);
            Assert.AreEqual(901, recorder.Count);
        }

        [Test]
        public void Ghost_InterpolatesBetweenSamples()
        {
            var recorder = new GhostRecorder("W1-1", 10f, 10f);
            recorder.Tick(0f, Vector3.zero, Quaternion.identity);
            recorder.Tick(0.1f, new Vector3(2f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));
            var ghost = recorder.Finish(0.1f);
            ghost.Sample(0.05f, out var position, out var rotation);
            Assert.AreEqual(1f, position.x, 1e-4f);
            Assert.AreEqual(45f, rotation.eulerAngles.y, 0.1f);
            ghost.Sample(5f, out position, out _);
            Assert.AreEqual(2f, position.x, 1e-4f, "después del final se queda en la última pose");
        }

        [Test]
        public void Splits_KeepTheFirstArrivalOnly()
        {
            var recorder = new GhostRecorder("W1-1", 10f, 10f);
            recorder.Split(2, 30f);
            recorder.Split(2, 45f);
            var ghost = recorder.Finish(60f);
            Assert.AreEqual(3, ghost.Splits.Length);
            Assert.AreEqual(30f, ghost.SplitAt(2));
            Assert.Less(ghost.SplitAt(1), 0f, "checkpoint al que no llegó");
            Assert.Less(ghost.SplitAt(9), 0f);
        }

        [Test]
        public void Store_RoundTripsTheGhost()
        {
            var recorder = new GhostRecorder("W2-3", 10f, 10f);
            for (int i = 0; i <= 30; i++)
                recorder.Tick(i * 0.1f, new Vector3(i, 0.5f, -i), Quaternion.Euler(0f, i, 0f));
            recorder.Split(1, 1.5f);
            var ghost = recorder.Finish(3.2f);
            ghost.Characters.Add("alex");
            ghost.Characters.Add("gloria");
            GhostStore.Save(ghost);

            var loaded = GhostStore.Load("W2-3");
            Assert.IsNotNull(loaded);
            Assert.AreEqual(3.2f, loaded.Time);
            Assert.AreEqual(ghost.Count, loaded.Count);
            Assert.AreEqual(new Vector3(30f, 0.5f, -30f), loaded.Positions[30]);
            Assert.AreEqual(1.5f, loaded.SplitAt(1));
            CollectionAssert.AreEqual(new[] { "alex", "gloria" }, loaded.Characters);
            Assert.IsNull(GhostStore.Load("W5-5"), "otro nivel no tiene fantasma");
        }

        [Test]
        public void Store_DamagedFile_ReturnsNull()
        {
            Directory.CreateDirectory(GhostStore.Folder);
            File.WriteAllBytes(GhostStore.PathFor("W1-2"), new byte[] { 1, 2, 3 });
            Assert.IsNull(GhostStore.Load("W1-2"));
        }

        [Test]
        public void TrialTable_KeepsTheFiveBestInOrder()
        {
            var go = new GameObject("Progression");
            try
            {
                var service = go.AddComponent<ProgressionService>();
                service.Initialize(new SaveSystem(Path.Combine(folder, "save.json")));
                Assert.AreEqual(Path.Combine(folder, "ghosts"), GhostStore.Folder, "los fantasmas van junto al guardado");
                float[] times = { 90f, 70f, 110f, 80f, 100f };
                foreach (float time in times)
                    service.RecordTrial("W1-1", time, "Alex", new[] { "alex" }, 5);
                Assert.AreEqual(-1, service.RecordTrial("W1-1", 120f, "Marty", new[] { "marty" }, 5), "más lento que los 5");
                Assert.AreEqual(0, service.RecordTrial("W1-1", 60f, "Melman", new[] { "melman" }, 5));
                var table = service.Data.FindTrial("W1-1");
                Assert.AreEqual(5, table.entries.Count);
                CollectionAssert.AreEqual(new[] { 60f, 70f, 80f, 90f, 100f }, table.entries.ConvertAll(e => e.time));
                Assert.AreEqual("melman", table.entries[0].characters[0]);

                var reloaded = new SaveSystem(Path.Combine(folder, "save.json")).Load();
                Assert.AreEqual(60f, reloaded.FindTrial("W1-1").entries[0].time, "la tabla queda en el guardado");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ResultsNote_ShowsTheGhostDifference()
        {
            var results = new LevelResults { RunMode = ProgressionRules.MODE_TIME_TRIAL, NewRecord = true };
            Assert.AreEqual("trial.first_record", ResultsScreen.ModeNote(results).key);
            results.GhostDelta = -2.43f;
            var note = ResultsScreen.ModeNote(results);
            Assert.AreEqual("trial.result_record", note.key);
            Assert.AreEqual("-2.4 s", note.args[0]);
            results.NewRecord = false;
            results.GhostDelta = 1.5f;
            note = ResultsScreen.ModeNote(results);
            Assert.AreEqual("trial.result_behind", note.key);
            Assert.AreEqual("+1.5 s", note.args[0]);
            Assert.IsNull(ResultsScreen.ModeNote(new LevelResults()).key, "Historia no tiene línea de modo");
        }

        [Test]
        public void Texts_ExistInBothLanguages()
        {
            var table = LocalizedText.Table;
            foreach (var key in new[] { "trial.ghost_time", "trial.no_ghost", "trial.first_record", "trial.result_record", "trial.result_behind", "tt.top5" })
            {
                foreach (var language in new[] { "es", "en" })
                {
                    string text = table.Get(key, language);
                    Assert.IsFalse(string.IsNullOrEmpty(text) || text == key, key + " en " + language);
                }
            }
        }

        [Test]
        public void SheetPlay_UsesTheChosenCard()
        {
            Assert.AreEqual(ProgressionRules.MODE_TIME_TRIAL, ModeLauncher.RunModeFor(ProgressionRules.MODE_TIME_TRIAL));
            Assert.AreEqual(ProgressionRules.MODE_STORY, ModeLauncher.RunModeFor(ProgressionRules.MODE_STORY));
            var go = new GameObject("Session");
            try
            {
                var session = go.AddComponent<Core.GameSession>();
                ModeLauncher.Prepare(session, ProgressionRules.MODE_TIME_TRIAL);
                Assert.AreEqual(ProgressionRules.MODE_TIME_TRIAL, session.RunMode);
                ModeLauncher.Prepare(session, null);
                Assert.AreEqual(ProgressionRules.MODE_STORY, session.RunMode);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
