using System.Collections;
using System.IO;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Modes;
using GasSafari.Raft;
using GasSafari.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GasSafari.Tests
{
    // Arranca un minijuego de Fiesta con bots (docs/06 §1) a x3 en su escena de Scenes/Juego/Fiesta, con datos clonados
    // (tiempos cortos), guardado temporal y una captura a mitad de partida en Screenshots/M7/<escena>.png.
    internal sealed class PartyTestRig
    {
        private static readonly string[] Characters = { "Alex", "Marty", "Melman", "Gloria" };

        private string savePath;
        private string sceneName;
        private PartyGameData clone;

        public PartyGameBase Game { get; private set; }
        public PartyGameData Data => clone;
        public string ShotPath { get; private set; }

        public IEnumerator SetUp(string id, int sessionBots, System.Action<PartyGameData> tweak)
        {
            Services.Achievements.enabled = false;
            Services.Achievements.enabled = true;
            Services.Session.ClearPlayers();
            savePath = Path.Combine(Path.GetTempPath(), "gasSafariParty_" + System.Guid.NewGuid().ToString("N"), "save.json");
            Services.Progression.Initialize(new SaveSystem(savePath));
            PartyCup.Reset();
            var source = ModesConfig.Instance.FindPartyGame(id);
            Assert.IsNotNull(source, "ModesConfig tiene el minijuego " + id);
            Assert.IsTrue(source.implemented, id + " está activo");
            clone = Object.Instantiate(source);
            tweak?.Invoke(clone);
            sceneName = source.sceneName;
#if UNITY_EDITOR
            for (int i = 0; i < sessionBots; i++)
            {
                var data = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_Project/Data/Characters/" + Characters[i] + ".asset");
                var color = Progression.GameCatalog.Instance.playerPalette.Get(i);
                Services.Session.AddPlayer(new PlayerSlot(i, color, null, null) { IsBot = true, Character = data });
            }
            PartyGameBase.AutoBegin = false;
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Project/Scenes/Juego/Fiesta/" + sceneName + ".unity", new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield break;
#endif
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
            Game = Object.FindFirstObjectByType<PartyGameBase>();
            Assert.IsNotNull(Game, "la escena tiene su controlador de minijuego");
            Game.UseData(clone);
            Game.Begin(PartyCup.BuildRoster(Services.Session.Players, clone));
            Services.TimeController.SetBaseScale(3f);
        }

        // Juega hasta los resultados; saca la captura cuando el minijuego pasó 'shotAt' segundos de juego.
        public IEnumerator PlayToResults(float shotAt, float realLimit)
        {
            float start = Time.realtimeSinceStartup;
            bool shot = false;
            while (Game.State != PartyState.Results)
            {
                Assert.Less(Time.realtimeSinceStartup - start, realLimit, "el minijuego termina solo con bots");
                if (!shot && Game.State == PartyState.Playing && Game.Elapsed >= shotAt)
                {
                    shot = true;
                    yield return new WaitForEndOfFrame();
                    var texture = ScreenCapture.CaptureScreenshotAsTexture();
                    string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Screenshots", "M7"));
                    Directory.CreateDirectory(folder);
                    ShotPath = Path.Combine(folder, sceneName + ".png");
                    File.WriteAllBytes(ShotPath, texture.EncodeToPNG());
                    Object.Destroy(texture);
                }
                yield return null;
            }
            Debug.Log("PARTYTEST " + sceneName + " juego=" + Game.Elapsed.ToString("0.0") + "s real=" + (Time.realtimeSinceStartup - start).ToString("0.0") + "s puntajes=" + string.Join(",", Game.Scores) + " puestos=" + string.Join(",", Game.Ranks));
        }

        // Puestos válidos, puntos de la Fiesta repartidos y el minijuego anotado para el logro "Animal de Fiesta".
        public void AssertScored()
        {
            Assert.IsNotNull(Game.Ranks, "hay puestos al terminar");
            Assert.AreEqual(Game.Players.Count, Game.Ranks.Length);
            Assert.Contains(1, Game.Ranks, "alguien queda primero");
            Assert.AreEqual(1, PartyCup.GamesPlayed);
            int expected = 0;
            foreach (int rank in Game.Ranks)
                expected += PartyCup.PointsForRank(rank);
            int total = 0;
            foreach (var entry in PartyCup.Standings())
                total += entry.points;
            Assert.AreEqual(expected, total, "los puntos de la Fiesta salen de los puestos");
            Assert.Contains(clone.id, Services.Progression.Data.partyPlayed, "cuenta para Animal de Fiesta");
            Assert.IsTrue(Game.Hud.ResultsPanel.activeSelf, "se ve la tabla de resultados");
        }

        public IEnumerator TearDown()
        {
            Services.TimeController.SetBaseScale(1f);
            PartyGameBase.AutoBegin = true;
            var rafts = Object.FindObjectsByType<RaftController>(FindObjectsSortMode.None);
            var avatars = Object.FindObjectsByType<PlayerAvatar>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var scene = SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid() && scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(scene);
            foreach (var avatar in avatars)
            {
                if (avatar != null)
                    Object.Destroy(avatar.gameObject);
            }
            foreach (var raft in rafts)
            {
                if (raft != null)
                    Object.Destroy(raft.gameObject);
            }
            Services.Session.ClearPlayers();
            PartyCup.Reset();
            if (clone != null)
                Object.Destroy(clone);
            string dir = Path.GetDirectoryName(savePath);
            if (dir != null && Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
    }
}
