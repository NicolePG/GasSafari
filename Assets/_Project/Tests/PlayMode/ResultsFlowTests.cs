using System.Collections;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // Meta → Finished → Results en TEST_LevelTemplate con 2 jugadores.
    public class ResultsFlowTests
    {
        private const string TEMPLATE = "Assets/_Project/Scenes/Pruebas/TEST_LevelTemplate.unity";

        private GameObject sources;
        private string savePath;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameEvents.ClearAll();
            Services.Session.ClearPlayers();
            // Nunca escribir en el guardado real durante las pruebas.
            savePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gasSafariFlow_" + System.Guid.NewGuid().ToString("N"), "save.json");
            Services.Progression.Initialize(new Save.SaveSystem(savePath));
            sources = new GameObject("Sources");
#if UNITY_EDITOR
            for (int i = 0; i < 2; i++)
            {
                var slot = new PlayerSlot(i, i == 0 ? Color.red : Color.blue, null, sources.AddComponent<ScriptedCommandSource>())
                {
                    Character = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_Project/Data/Characters/Alex.asset")
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
            if (System.IO.File.Exists(savePath))
                System.IO.Directory.Delete(System.IO.Path.GetDirectoryName(savePath), true);
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
        }

        [UnityTest]
        public IEnumerator Finish_BuildsResults_WithStatsAwardsAndReplay()
        {
            var level = LevelManager.Current;
            var controller = Object.FindFirstObjectByType<ResultsController>();
            controller.Rng = new System.Random(3);
            var screen = Object.FindFirstObjectByType<ResultsScreen>();
            yield return new WaitForSeconds(3.2f);
            Assert.AreEqual(LevelState.Running, level.State);

            var a = level.Avatars[0];
            var b = level.Avatars[1];
            GameEvents.RaisePlayerFell(b);
            GameEvents.RaisePlayerFell(b);
            GameEvents.RaiseAbilityUsed(a, "roar");
            GameEvents.RaiseFart(a, FartType.Puff, 10f);
            GameEvents.RaiseChamberPopped(0);
            yield return new WaitForSeconds(0.5f);

            level.Raft.Body.position = Object.FindFirstObjectByType<FinishGate>().transform.position;
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(LevelState.Finished, level.State);
            Assert.IsFalse(screen.IsVisible);

            yield return new WaitForSeconds(2.2f);
            Assert.AreEqual(LevelState.Results, level.State);
            var results = controller.Results;
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Stars, "sin LevelData el nivel solo da la estrella por terminar");
            Assert.AreEqual(0, results.Replay.Culprit, "el pedo previo al choque fue del jugador 1");
            var data = Object.FindFirstObjectByType<LevelStats>().Data;
            Assert.AreEqual(2, data.Falls[1]);
            Assert.AreEqual(1, data.UntimelyFarts[0]);
            Assert.AreEqual(3, results.Awards.Count);
            Assert.IsTrue(results.Awards.Exists(x => x.Positive) && results.Awards.Exists(x => !x.Positive));
            Assert.IsTrue(screen.IsVisible);

            var replay = Object.FindFirstObjectByType<ReplayPlayer>();
            Assert.IsTrue(replay.IsPlaying);
            Assert.IsNotNull(replay.Raft);

            yield return new WaitForSecondsRealtime(1.5f);
            Assert.AreEqual(1, screen.StarsRevealed);
        }
    }
}
