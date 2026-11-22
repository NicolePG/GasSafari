using System.Collections;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Modes;
using GasSafari.Progression;
using GasSafari.Save;
using GasSafari.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M7-01 Contrarreloj en TEST_LevelTemplate con 2 jugadores: la primera carrera guarda el fantasma; con fantasma se ve
    // semitransparente, da la diferencia en cada checkpoint y en la meta, y un tiempo mejor lo reemplaza.
    public class TimeTrialModeTests
    {
        private const string TEMPLATE = "Assets/_Project/Scenes/Pruebas/TEST_LevelTemplate.unity";
        private const string LEVEL_ID = "TT-TEST";

        private GameObject sources;
        private string savePath;
        private LevelData data;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameEvents.ClearAll();
            Services.Session.ClearPlayers();
            savePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gasSafariTrial_" + System.Guid.NewGuid().ToString("N"), "save.json");
            Services.Progression.Initialize(new SaveSystem(savePath));
            data = ScriptableObject.CreateInstance<LevelData>();
            data.id = LEVEL_ID;
            data.goldTime = 100f;
            data.silverTime = 140f;
            sources = new GameObject("Sources");
#if UNITY_EDITOR
            for (int i = 0; i < 2; i++)
            {
                var slot = new PlayerSlot(i, i == 0 ? Color.red : Color.blue, null, sources.AddComponent<ScriptedCommandSource>())
                {
                    Character = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_Project/Data/Characters/" + (i == 0 ? "Alex" : "Gloria") + ".asset")
                };
                Services.Session.AddPlayer(slot);
            }
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                TEMPLATE, new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield break;
#endif
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Services.Session.ClearPlayers();
            Object.Destroy(sources);
            var replay = Object.FindFirstObjectByType<ReplayPlayer>();
            if (replay != null)
                replay.Stop();
            foreach (var avatar in Object.FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
                Object.Destroy(avatar.gameObject);
            if (LevelManager.Current != null)
                Object.Destroy(LevelManager.Current.Raft.gameObject);
            yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName("TEST_LevelTemplate"));
            string dir = System.IO.Path.GetDirectoryName(savePath);
            if (System.IO.Directory.Exists(dir))
                System.IO.Directory.Delete(dir, true);
            Object.Destroy(data);
        }

        private GameModeBase Attach(string mode)
        {
            var level = LevelManager.Current;
            typeof(LevelManager).GetField("data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(level, data);
            return ModeDirector.Attach(level, mode);
        }

        private static IEnumerator Finish()
        {
            var level = LevelManager.Current;
            level.Raft.Body.position = Object.FindFirstObjectByType<FinishGate>().transform.position;
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(LevelState.Finished, level.State);
            yield return new WaitForSeconds(2.2f);
            Assert.AreEqual(LevelState.Results, level.State);
        }

        [UnityTest]
        public IEnumerator FirstRun_WithoutGhost_SavesItAndTheRecord()
        {
            var mode = (TimeTrialMode)Attach(ProgressionRules.MODE_TIME_TRIAL);
            Assert.IsFalse(mode.AllowsRoyalAssist, "Contrarreloj sin Ayuda Real");
            Assert.IsNull(mode.Ghost, "sin fantasma guardado no hay nada que reproducir");
            Assert.IsNotNull(mode.Hud);
            Assert.AreEqual(LocalizedText.Table.Get("trial.no_ghost"), mode.Hud.GhostText);

            yield return new WaitForSeconds(3.2f);
            Assert.AreEqual(LevelState.Running, LevelManager.Current.State);
            var start = LevelManager.Current.Raft.Body.position;
            for (int i = 0; i < 15; i++)
            {
                LevelManager.Current.Raft.Body.position = start + Vector3.forward * (i * 0.3f);
                yield return new WaitForSeconds(0.1f);
            }
            yield return Finish();

            var results = Object.FindFirstObjectByType<ResultsController>().Results;
            Assert.AreEqual(ProgressionRules.MODE_TIME_TRIAL, results.RunMode);
            Assert.IsTrue(results.NewRecord);
            Assert.IsTrue(float.IsNaN(results.GhostDelta));
            Assert.AreEqual(LocalizedText.Table.Get("trial.first_record"), Object.FindFirstObjectByType<ResultsScreen>().ModeNoteText);

            var ghost = GhostStore.Load(LEVEL_ID);
            Assert.IsNotNull(ghost, "la mejor carrera queda grabada");
            Assert.AreEqual(results.Time, ghost.Time, 1e-3f);
            Assert.GreaterOrEqual(ghost.Count, (int)(LevelManager.Current.Timer.RunTime * 10f), "10 muestras por segundo de carrera");
            CollectionAssert.AreEqual(new[] { "Alex", "Gloria" }, ghost.Characters);
            var table = Services.Progression.Data.FindTrial(LEVEL_ID);
            Assert.AreEqual(1, table.entries.Count);
            Assert.AreEqual(results.Time, table.entries[0].time, 1e-3f);
            Assert.AreEqual(results.Time, Services.Progression.Data.FindLevel(LEVEL_ID).best, 1e-3f, "también guarda el mejor tiempo del nivel");
        }

        [UnityTest]
        public IEnumerator WithGhost_ShowsItAndTheDifference_AndAFasterRunReplacesIt()
        {
            var old = new GhostRecorder(LEVEL_ID, 10f, 60f);
            var origin = LevelManager.Current.Raft.Body.position;
            for (int i = 0; i <= 500; i++)
                old.Tick(i * 0.1f, origin + new Vector3(0f, 0f, i * 0.1f), Quaternion.identity);
            old.Split(1, 0.5f);
            GhostStore.Save(old.Finish(50f));

            var mode = (TimeTrialMode)Attach(ProgressionRules.MODE_TIME_TRIAL);
            Assert.IsNotNull(mode.Ghost);
            Assert.Greater(mode.Ghost.MeshCount, 0, "copia las mallas de la balsa");
            Assert.AreEqual(GameLayers.Ghost, mode.Ghost.gameObject.layer);
            Assert.AreEqual(0, mode.Ghost.GetComponentsInChildren<Collider>().Length, "el fantasma no choca");
            Assert.AreEqual(0, mode.Ghost.GetComponentsInChildren<PlayerAvatar>().Length, "solo la balsa, sin animales");
            Assert.AreEqual(LocalizedText.Table.Get("trial.ghost_time").Replace("{0}", "0:50"), mode.Hud.GhostText);

            yield return new WaitForSeconds(4.2f);
            Assert.IsTrue(mode.Ghost.IsVisible);
            float t = LevelManager.Current.Timer.RunTime;
            Assert.AreEqual(origin.z + t, mode.Ghost.transform.position.z, 0.15f, "sigue la pose grabada según el cronómetro");

            GameEvents.RaiseCheckpoint(1);
            StringAssert.StartsWith("+", mode.Hud.SplitText, "van más lentos que el fantasma en ese checkpoint");
            Assert.AreEqual(TimeTrialHud.Behind, mode.Hud.SplitColor);

            yield return Finish();
            var results = Object.FindFirstObjectByType<ResultsController>().Results;
            Assert.Less(results.GhostDelta, 0f);
            Assert.IsTrue(results.NewRecord);
            Assert.AreEqual(LocalizedText.Table.Get("trial.result_record").Replace("{0}", TimeTrialHud.FormatDelta(results.GhostDelta)),
                Object.FindFirstObjectByType<ResultsScreen>().ModeNoteText);
            Assert.AreEqual(results.Time, GhostStore.Load(LEVEL_ID).Time, 1e-3f, "el fantasma nuevo reemplaza al anterior");
        }

        [UnityTest]
        public IEnumerator Story_RecordsTheGhostWithoutShowingIt()
        {
            var mode = Attach(ProgressionRules.MODE_STORY);
            Assert.IsInstanceOf<StoryMode>(mode);
            Assert.IsTrue(mode.AllowsRoyalAssist);
            Assert.IsNull(Object.FindFirstObjectByType<TimeTrialHud>());
            yield return new WaitForSeconds(3.5f);
            yield return Finish();
            Assert.IsNotNull(GhostStore.Load(LEVEL_ID), "el primer Contrarreloj ya tiene contra quién correr");
            Assert.AreEqual(ProgressionRules.MODE_STORY, Object.FindFirstObjectByType<ResultsController>().Results.RunMode);
        }
    }
}
