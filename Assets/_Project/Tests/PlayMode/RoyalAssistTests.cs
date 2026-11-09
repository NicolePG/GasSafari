using System.Collections;
using System.Linq;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Progression;
using GasSafari.Team;
using GasSafari.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // Ayuda Real (docs/06 §3): oferta tras 3 reapariciones en el mismo checkpoint, con tres ayudas.
    public class RoyalAssistTests
    {
        private const string TEMPLATE = "Assets/_Project/Scenes/Levels/L_W1_1_FuentesZoo.unity";

        private GameObject sources;
        private CheckpointSystem checkpoints;
        private RoyalAssist assist;
        private LevelManager level;

        private IEnumerator Load(bool humans)
        {
            GameEvents.ClearAll();
            Services.Session.ClearPlayers();
            Services.Session.IsOnline = false;
            Services.Session.Difficulty = GameCatalog.Instance.FindDifficulty("Normal");
            sources = new GameObject("Sources");
#if UNITY_EDITOR
            var alex = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_Project/Data/Characters/Alex.asset");
            for (int i = 0; i < 2; i++)
            {
                var slot = new PlayerSlot(i, Color.white, null, humans ? (Input.IPlayerCommandSource)sources.AddComponent<ScriptedCommandSource>() : null)
                {
                    Character = alex,
                    IsBot = !humans
                };
                Services.Session.AddPlayer(slot);
            }
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                TEMPLATE, new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield break;
#endif
            level = LevelManager.Current;
            checkpoints = Object.FindFirstObjectByType<CheckpointSystem>();
            assist = checkpoints.gameObject.AddComponent<RoyalAssist>();
            assist.Bind(level, checkpoints);
            yield return new WaitForSeconds(2.2f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            Services.TimeController.SetPaused(false);
            Services.Session.ClearPlayers();
            Object.Destroy(sources);
            foreach (var avatar in Object.FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
                Object.Destroy(avatar.gameObject);
            if (LevelManager.Current != null && LevelManager.Current.Raft != null)
                Object.Destroy(LevelManager.Current.Raft.gameObject);
            var scene = SceneManager.GetSceneByName("L_W1_1_FuentesZoo");
            if (scene.IsValid())
                yield return SceneManager.UnloadSceneAsync(scene);
        }

        private IEnumerator Respawn(int times)
        {
            for (int i = 0; i < times; i++)
            {
                checkpoints.RequestRespawn();
                while (checkpoints.IsRespawning)
                    yield return null;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ThreeRespawnsAtTheSameCheckpoint_OfferHelp_AndFreezeTheGame()
        {
            yield return Load(true);
            yield return Respawn(2);
            Assert.IsFalse(assist.IsOffering, "con 2 reapariciones todavía no");
            yield return Respawn(1);
            Assert.IsTrue(assist.IsOffering);
            Assert.IsTrue(assist.Panel.IsVisible);
            Assert.AreEqual(0f, Time.timeScale, "la partida espera la elección");
            Assert.AreEqual(3, assist.Panel.OptionButtons.Length);
            Assert.IsTrue(assist.Panel.OptionButtons.All(b => b.interactable));

            assist.Decline();
            Assert.IsFalse(assist.IsOffering);
            Assert.AreEqual(1f, Time.timeScale);
            yield return Respawn(1);
            Assert.IsFalse(assist.IsOffering, "una sola oferta por checkpoint");
            Assert.AreEqual(1, assist.OffersMade);
        }

        [UnityTest]
        public IEnumerator Option1_AddsTenthOfASecond_UntilTheNextCheckpoint()
        {
            yield return Load(true);
            var sync = level.Raft.GetComponent<SyncDetector>();
            float before = sync.Window;
            yield return Respawn(3);
            Assert.IsTrue(assist.Choose(RoyalAssist.Option.MoreSyncTime));
            Assert.AreEqual(before + 0.1f, sync.Window, 1e-4f);
            Assert.AreEqual(1f, Time.timeScale);
            yield return Respawn(1);
            Assert.AreEqual(before + 0.1f, sync.Window, 1e-4f, "sigue tras reaparecer");
            GameEvents.RaiseCheckpoint(checkpoints.CurrentIndex + 1);
            Assert.AreEqual(before, sync.Window, 1e-4f, "al llegar al siguiente checkpoint se acaba");
        }

        [UnityTest]
        public IEnumerator Option2_PutsAnExtraCheckpointHalfwayToTheNextOne()
        {
            yield return Load(true);
            Assume.That(checkpoints.Active.Count, Is.GreaterThan(1));
            var from = checkpoints.Current.transform.position;
            var to = checkpoints.Active[checkpoints.CurrentIndex + 1].transform.position;
            int count = checkpoints.Active.Count;
            yield return Respawn(3);
            Assert.IsTrue(assist.Choose(RoyalAssist.Option.ExtraCheckpoint));
            Assert.AreEqual(count + 1, checkpoints.Active.Count);
            Assert.AreEqual(1, checkpoints.CurrentIndex);
            Assert.AreEqual(Vector3.Lerp(from, to, 0.5f).x, checkpoints.Current.transform.position.x, 1e-3f);
            Assert.AreEqual(0, checkpoints.RespawnsAtCurrent);
            Assert.IsFalse(assist.SectionSkipped, "el checkpoint extra no limita las estrellas");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Option3_SkipsTheSection_AndCapsTheLevelAtOneStar()
        {
            yield return Load(true);
            Assume.That(checkpoints.Active.Count, Is.GreaterThan(1));
            var next = checkpoints.Active[1];
            yield return Respawn(3);
            Assert.IsTrue(assist.Choose(RoyalAssist.Option.SkipSection));
            Assert.AreSame(next, checkpoints.Current);
            Assert.IsTrue(assist.SectionSkipped);
            var stats = Object.FindFirstObjectByType<LevelStats>();
            Assert.IsTrue(stats.Data.SectionSkipped);

            var data = level.Data;
            var results = ResultsBuilder.Build(data, data.goldTime - 1f, stats.Data, -1, null, new System.Random(1));
            Assert.AreEqual(1, results.Stars, "con salto, máximo 1 estrella aunque el tiempo sea de oro");
            Assert.IsTrue(results.Skipped);
        }

        [UnityTest]
        public IEnumerator BotsOnlyRuns_NeverGetTheOffer()
        {
            yield return Load(false);
            yield return Respawn(4);
            Assert.IsFalse(assist.IsOffering);
            Assert.AreEqual(0, assist.OffersMade);
        }

        [Test]
        public void Results_StarsAreCappedAtOneOnlyWhenASectionWasSkipped()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.goldTime = 100f;
            level.silverTime = 140f;
            var stats = new StatsData();
            Assert.AreEqual(3, ResultsBuilder.Build(level, 90f, stats, -1, null, new System.Random(1)).Stars);
            stats.SectionSkipped = true;
            var results = ResultsBuilder.Build(level, 90f, stats, -1, null, new System.Random(1));
            Assert.AreEqual(1, results.Stars);
            Assert.AreEqual(20 + 30 + 10, results.Beans, "20 por terminar, +30 primera vez, +10 por la estrella");
            Object.DestroyImmediate(level);
        }
    }
}
