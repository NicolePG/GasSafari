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
    // Pausa de docs/07 §4 dentro de _LevelTemplate.
    public class PauseTests
    {
        private const string TEMPLATE = "Assets/_Project/Scenes/Levels/_LevelTemplate.unity";

        private GameObject sources;
        private PauseController pause;

        private IEnumerator Load(int players)
        {
            GameEvents.ClearAll();
            Services.Session.ClearPlayers();
            Services.Session.IsOnline = false;
            sources = new GameObject("Sources");
#if UNITY_EDITOR
            for (int i = 0; i < players; i++)
            {
                var slot = new PlayerSlot(i, Color.white, null, sources.AddComponent<ScriptedCommandSource>())
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
            pause = Object.FindFirstObjectByType<PauseController>();
            yield return new WaitForSeconds(2.2f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (pause != null && pause.IsOpen)
                pause.Close();
            Time.timeScale = 1f;
            Services.Session.IsOnline = false;
            Services.Session.ClearPlayers();
            Object.Destroy(sources);
            foreach (var avatar in Object.FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
                Object.Destroy(avatar.gameObject);
            if (LevelManager.Current != null)
                Object.Destroy(LevelManager.Current.Raft.gameObject);
            yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName("_LevelTemplate"));
        }

        [UnityTest]
        public IEnumerator OpenAndClose_FreezesTimeAndInput_AndShowsMenu()
        {
            yield return Load(2);
            var level = LevelManager.Current;
            var menu = Object.FindFirstObjectByType<PauseMenu>();
            Assert.AreEqual(LevelState.Running, level.State);

            pause.Open(Services.Session.Players[0]);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(level.IsPaused);
            Assert.IsTrue(menu.IsVisible);
            Assert.IsTrue(level.Avatars[0].InputLocked);

            float before = level.Timer.Total;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(before, level.Timer.Total, 1e-4f, "el cronómetro no corre en pausa");

            pause.Close();
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(menu.IsVisible);
            Assert.IsFalse(level.Avatars[0].InputLocked);
        }

        [UnityTest]
        public IEnumerator Online_PauseDoesNotStopTime()
        {
            yield return Load(2);
            Services.Session.IsOnline = true;
            pause.Open(Services.Session.Players[1]);
            Assert.IsTrue(pause.IsOpen);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(LevelManager.Current.IsPaused);
            Assert.AreSame(Services.Session.Players[1], pause.Owner);
        }

        [UnityTest]
        public IEnumerator CheckpointRestart_WithTwoPlayers_NeedsVote()
        {
            yield return Load(2);
            var checkpoints = Object.FindFirstObjectByType<CheckpointSystem>();
            var players = Services.Session.Players;
            pause.Open(players[0]);

            pause.RequestCheckpointRestart(players[0]);
            Assert.AreEqual(VoteResult.Pending, pause.Vote.Result);
            Assert.IsFalse(checkpoints.IsRespawning);
            Assert.AreSame(players[1], pause.NextVoter());

            pause.CastVote(players[1], true);
            Assert.IsFalse(pause.IsOpen);
            Assert.IsTrue(checkpoints.IsRespawning);
        }

        [UnityTest]
        public IEnumerator CheckpointRestart_Solo_IsImmediate()
        {
            yield return Load(1);
            var checkpoints = Object.FindFirstObjectByType<CheckpointSystem>();
            pause.Open(Services.Session.Players[0]);
            pause.RequestCheckpointRestart(Services.Session.Players[0]);
            Assert.IsTrue(checkpoints.IsRespawning);
            Assert.IsFalse(pause.IsOpen);
        }

        [UnityTest]
        public IEnumerator MenuActions_RaiseTheirEvents()
        {
            yield return Load(1);
            bool restart = false, quit = false, options = false;
            pause.RestartLevelRequested += () => restart = true;
            pause.QuitToMapRequested += () => quit = true;
            pause.OptionsRequested += () => options = true;
            pause.Open(Services.Session.Players[0]);

            pause.OpenOptions();
            Assert.IsTrue(options);
            pause.RestartLevel();
            Assert.IsTrue(restart);
            Assert.IsFalse(pause.IsOpen);
            pause.Open(Services.Session.Players[0]);
            pause.QuitToMap();
            Assert.IsTrue(quit);
        }
    }
}
