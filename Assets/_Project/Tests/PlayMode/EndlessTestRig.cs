using System.Collections;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Modes;
using GasSafari.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GasSafari.Tests
{
    // Carga la escena Infinito con 4 bots en Infinito o en el Desafío Diario (M7-09/10), con guardado temporal y tiempo x3.
    public sealed class EndlessTestRig
    {
        public const string SCENE = "Assets/_Project/Scenes/Juego/Infinito/Infinito.unity";
        private static readonly string[] Crew = { "Alex", "Marty", "Melman", "Gloria" };

        private string savePath;

        public LevelManager Level { get; private set; }
        public GameModeBase Mode { get; private set; }
        public EndlessTrack Track { get; private set; }
        public CheckpointSystem Checkpoints { get; private set; }
        public SaveData Save => Services.Progression.Data;
        public float FinishedAt { get; private set; } = -1f;

        public void Prepare(string runMode)
        {
            GameEvents.ClearAll();
            savePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gasSafariEndless_" + System.Guid.NewGuid().ToString("N"), "save.json");
            Services.Progression.Initialize(new SaveSystem(savePath));
            Services.Session.ClearPlayers();
            Services.Session.RunMode = runMode;
#if UNITY_EDITOR
            for (int i = 0; i < Crew.Length; i++)
            {
                var data = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_Project/Data/Characters/" + Crew[i] + ".asset");
                Services.Session.AddPlayer(new PlayerSlot(i, Color.white, null, null) { IsBot = true, Character = data });
            }
#endif
        }

        public IEnumerator Load()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(SCENE, new LoadSceneParameters(LoadSceneMode.Additive));
#endif
            yield return null;
            Level = LevelManager.Current;
            Track = EndlessTrack.Current;
            Checkpoints = Object.FindFirstObjectByType<CheckpointSystem>();
            Assert.IsNotNull(Level, "LevelManager en la escena Infinito");
            Assert.IsNotNull(Track, "EndlessTrack en la escena Infinito");
            Mode = ModeDirector.Attach(Level, Services.Session.RunMode);
            Level.Finished += t => FinishedAt = t;
            SetSpeed(3f);
        }

        public IEnumerator WaitForRunning(float limit = 20f)
        {
            float t = 0f;
            while (Level.State != LevelState.Running && t < limit)
            {
                t += Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual(LevelState.Running, Level.State, "la carrera empieza");
        }

        public IEnumerator WaitForState(LevelState state, float limit)
        {
            float t = 0f;
            while (Level.State != state && t < limit)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        public void Log(string id, float elapsed)
        {
            var p = Level.Raft.transform.position;
            Debug.Log($"ENDLESS {id} t={elapsed:F0}s dist={Track.Distance:F0} max={Track.MaxDistance:F0} nivel={Track.Tier} " +
                      $"checkpoint={Checkpoints.CurrentIndex}/{Checkpoints.Active.Count - 1} tramos={Track.ChunksPlaced} vivos={Track.LiveChunks.Count} " +
                      $"raft=({p.x:F0},{p.z:F0}) v={Level.Raft.Body.velocity.magnitude:F1}");
        }

        public IEnumerator Unload()
        {
            SetSpeed(1f);
            foreach (var avatar in Object.FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
                Object.Destroy(avatar.gameObject);
            if (Level != null && Level.Raft != null)
                Object.Destroy(Level.Raft.gameObject);
            var scene = SceneManager.GetSceneByName("Infinito");
            if (scene.IsValid())
                yield return SceneManager.UnloadSceneAsync(scene);
            Services.Session.ClearPlayers();
            Services.Session.RunMode = GasSafari.Progression.ProgressionRules.MODE_STORY;
            EndlessTrack.SeedOverride = null;
            EndlessTrack.DateOverride = null;
            string dir = System.IO.Path.GetDirectoryName(savePath);
            if (System.IO.Directory.Exists(dir))
                System.IO.Directory.Delete(dir, true);
        }

        public static void SetSpeed(float scale)
        {
            if (Services.TimeController != null)
                Services.TimeController.SetBaseScale(scale);
            else
                Time.timeScale = scale;
        }
    }
}
