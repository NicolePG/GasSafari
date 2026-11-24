using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Modes;
using GasSafari.Progression;
using GasSafari.Raft;
using GasSafari.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M7-11 + M7-12: Carrera por Equipos local con 4 bots (2 por balsa) en pistas de 3 mundos. Las dos balsas salen de sus
    // TeamRaceSpawn, cada mitad de la pantalla sigue a la suya, la primera en la meta gana (sin contar como nivel terminado)
    // y el panel de la serie ofrece la pista siguiente. Tiempo de juego x3; límite Plata x1.3 de cada pista.
    public class TeamRaceBotTests
    {
        private static readonly string[] Crew = { "Alex", "Marty", "Melman", "Gloria" };

        private string savePath;
        private string sceneName;
        private TeamRaceMode mode;
        private int levelFinishedEvents;
        private int globalRespawns;

        [UnityTest, Timeout(600000)]
        public IEnumerator W1_3_TwoBotRafts_RaceToTheFinish()
        {
            yield return Race("N03_W1-3_RioCiudad", "W1-3", 360f * 1.3f);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator W2_2_TwoBotRafts_RaceToTheFinish()
        {
            yield return Race("N07_W2-2_Tormenta", "W2-2", 295f * 1.3f);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator W4_1_TwoBotRafts_RaceToTheFinish()
        {
            yield return Race("N16_W4-1_CascadasEscalonadas", "W4-1", 240f * 1.3f);
        }

        // Una balsa que se hunde reaparece sola en su salida; la otra sigue corriendo y el nivel no entra en reaparición.
        [UnityTest, Timeout(180000)]
        public IEnumerator SinkingOneRaft_RespawnsOnlyThatRaft_AtItsLane()
        {
            yield return Load("N03_W1-3_RioCiudad", "W1-3");
            var level = LevelManager.Current;
            yield return WaitForRunning(level);
            yield return new WaitForSeconds(4f);
            var blue = mode.Rafts[TeamRaceMode.BLUE];
            var orange = mode.Rafts[TeamRaceMode.ORANGE];
            var blueBefore = blue.transform.position;
            var orangeSpawn = TeamRaceSpawn.FindAll()[TeamRaceMode.ORANGE];
            var problems = new List<string>();
            if (Vector3.Distance(orange.transform.position, orangeSpawn.Position) < 3f)
                problems.Add("la balsa Naranja no avanzó en 4 s");

            Vector3? respawnedAt = null;
            System.Action<RaftController> onRespawned = r => { if (r == orange) respawnedAt = r.transform.position; };
            mode.Checkpoints.RaftRespawned += onRespawned;
            GameEvents.RaiseRaftSunk(orange);
            yield return null;
            if (!mode.Checkpoints.IsRaftRespawning(orange))
                problems.Add("la Naranja no empezó a reaparecer");
            if (mode.Checkpoints.IsRespawning)
                problems.Add("la balsa Azul también reaparece");
            yield return new WaitForSeconds(2.5f);
            mode.Checkpoints.RaftRespawned -= onRespawned;
            if (respawnedAt == null)
                problems.Add("la Naranja no reapareció");
            else if (Vector3.Distance(respawnedAt.Value, orangeSpawn.Position) > 0.5f)
                problems.Add($"la Naranja reaparece lejos de su salida ({Vector3.Distance(respawnedAt.Value, orangeSpawn.Position):F1} m)");
            if (Vector3.Distance(blue.transform.position, blueBefore) < 1f)
                problems.Add("la Azul se quedó quieta");
            if ((blue.transform.position - orangeSpawn.Position).sqrMagnitude < 1f)
                problems.Add("la Azul fue llevada a la salida Naranja");
            if (level.State != LevelState.Running)
                problems.Add("el nivel salió de Running: " + level.State);
            if (globalRespawns != 0)
                problems.Add("la reaparición de la Naranja reinició los peligros del nivel (OnRespawn global)");
            if (mode.Checkpoints.IndexOf(orange) != 0)
                problems.Add("la Naranja tiene checkpoint " + mode.Checkpoints.IndexOf(orange));
            yield return Unload();
            Assert.IsEmpty(problems, string.Join("; ", problems));
        }

        // ---------------------------------------------------------------- carrera completa
        private IEnumerator Race(string scene, string id, float limit)
        {
            yield return Load(scene, id);
            var level = LevelManager.Current;
            var problems = new List<string>();
            CheckStart(id, problems);
            yield return WaitForRunning(level);
            yield return new WaitForSeconds(0.5f);
            CheckCameras(id, problems);

            float elapsed = 0f;
            float realStart = Time.realtimeSinceStartup;
            int respawns = 0;
            System.Action<RaftController> onRespawn = r => respawns++;
            mode.Checkpoints.RaftRespawned += onRespawn;
            while (mode.Winner < 0 && elapsed < limit)
            {
                yield return new WaitForSeconds(1f);
                elapsed += 1f;
                if (((int)elapsed) % 20 == 0)
                    Debug.Log($"TEAMRACE {id} t={elapsed:F0}s real={Time.realtimeSinceStartup - realStart:F0}s azul={mode.Distance(0):F0}m naranja={mode.Distance(1):F0}m de {mode.CourseLength:F0} reapariciones={respawns}");
            }
            mode.Checkpoints.RaftRespawned -= onRespawn;
            int loser = 1 - mode.Winner;
            Debug.Log($"TEAMRACE {id} ganador={mode.Winner} tiempo={mode.WinnerTime:F1}s azul={mode.Distance(0):F0}m naranja={mode.Distance(1):F0}m de {mode.CourseLength:F0} reapariciones={respawns} real={Time.realtimeSinceStartup - realStart:F0}s");

            if (mode.Winner < 0)
                problems.Add($"{id}: ninguna balsa llegó a la meta en {limit:F0} s");
            else
            {
                if (TeamRaceSeries.Wins(mode.Winner) != 1 || TeamRaceSeries.Wins(loser) != 0)
                    problems.Add($"{id}: la serie no anotó la victoria ({TeamRaceSeries.Wins(0)}-{TeamRaceSeries.Wins(1)})");
                if (mode.Distance(loser) < mode.CourseLength * 0.25f)
                    problems.Add($"{id}: la balsa perdedora casi no avanzó ({mode.Distance(loser):F0} m de {mode.CourseLength:F0})");
                float wait = 0f;
                while (level.State != LevelState.Results && wait < 6f)
                {
                    wait += Time.deltaTime;
                    yield return null;
                }
                yield return null;
                if (mode.Hud == null || !mode.Hud.ResultsPanel.activeSelf)
                    problems.Add($"{id}: no aparece el panel de la serie");
                else if (!mode.Hud.NextButton.gameObject.activeSelf)
                    problems.Add($"{id}: con 1-0 falta \"Siguiente pista\"");
                var levelResults = Object.FindFirstObjectByType<ResultsController>();
                if (levelResults != null && levelResults.Results != null)
                    problems.Add($"{id}: se armaron los resultados del nivel (estrellas)");
            }
            if (levelFinishedEvents != 0)
                problems.Add($"{id}: la carrera contó como nivel terminado");
            EndlessTestRig.SetSpeed(1f);
            yield return Unload();
            Assert.IsEmpty(problems, string.Join("; ", problems));
        }

        private void CheckStart(string id, List<string> problems)
        {
            var spawns = TeamRaceSpawn.FindAll();
            if (spawns[0] == null || spawns[1] == null)
            {
                problems.Add($"{id}: faltan las TeamRaceSpawn");
                return;
            }
            for (int team = 0; team < 2; team++)
            {
                var raft = mode.Rafts[team];
                if (raft == null)
                {
                    problems.Add($"{id}: falta la balsa {team}");
                    continue;
                }
                if (Vector3.Distance(raft.transform.position, spawns[team].Position) > 1f)
                    problems.Add($"{id}: la balsa {team} no sale de su TeamRaceSpawn");
                int crew = raft.GetComponent<RaftSeats>().Crew.Count;
                if (crew != 2)
                    problems.Add($"{id}: la balsa {team} lleva {crew} animales");
            }
            if (mode.Rafts[0] != LevelManager.Current.Raft)
                problems.Add($"{id}: la Azul no es la balsa del nivel");
        }

        // Cada mitad de la pantalla: su cámara, su cámara virtual activa y su balsa en el encuadre.
        private void CheckCameras(string id, List<string> problems)
        {
            var split = mode.Split;
            if (split == null || !split.IsSplit)
            {
                problems.Add($"{id}: no hay pantalla dividida");
                return;
            }
            if (!Mathf.Approximately(split.LeftCamera.rect.width, 0.5f) || !Mathf.Approximately(split.RightCamera.rect.x, 0.5f))
                problems.Add($"{id}: las cámaras no dividen la pantalla en dos mitades");
            var leftBrain = split.LeftCamera.GetComponent<CinemachineBrain>();
            var rightBrain = split.RightCamera.GetComponent<CinemachineBrain>();
            if (leftBrain.ActiveVirtualCamera?.VirtualCameraGameObject != split.LeftRig.Vcam.gameObject)
                problems.Add($"{id}: la mitad izquierda no usa la cámara de la Azul");
            if (rightBrain.ActiveVirtualCamera?.VirtualCameraGameObject != split.RightRig.Vcam.gameObject)
                problems.Add($"{id}: la mitad derecha no usa la cámara de la Naranja");
            for (int team = 0; team < 2; team++)
            {
                var cam = split.CameraFor(team);
                var view = cam.WorldToViewportPoint(mode.Rafts[team].transform.position);
                if (view.z <= 0f || view.x < 0.05f || view.x > 0.95f || view.y < 0.05f || view.y > 0.95f)
                    problems.Add($"{id}: la balsa {team} no está en su mitad ({view})");
            }
        }

        // ---------------------------------------------------------------- escena
        private IEnumerator Load(string scene, string id)
        {
#if UNITY_EDITOR
            sceneName = scene;
            levelFinishedEvents = 0;
            globalRespawns = 0;
            savePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gasSafariTeamRace_" + System.Guid.NewGuid().ToString("N"), "save.json");
            Services.Progression.Initialize(new SaveSystem(savePath));
            Services.Session.ClearPlayers();
            Services.Session.RunMode = ProgressionRules.MODE_TEAM_RACE;
            for (int i = 0; i < Crew.Length; i++)
            {
                var data = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_Project/Data/Characters/" + Crew[i] + ".asset");
                Services.Session.AddPlayer(new PlayerSlot(i, GameCatalog.Instance.playerPalette.Get(i), null, null) { IsBot = true, Character = data });
            }
            TeamRaceSeries.Reset();
            var track = GameCatalog.Instance.FindLevel(id);
            TeamRaceSeries.Begin(new[] { track, track, track });
            GameEvents.OnLevelFinished += CountFinish;
            GameEvents.OnRespawn += CountRespawn;

            mode = null;
            // Como LevelFlow: el modo se engancha al cargar la escena, antes del Start del LevelManager.
            UnityEngine.Events.UnityAction<Scene, LoadSceneMode> hook = (s, m) =>
            {
                if (s.name == scene && LevelManager.Current != null)
                    mode = ModeDirector.Attach(LevelManager.Current, ProgressionRules.MODE_TEAM_RACE) as TeamRaceMode;
            };
            SceneManager.sceneLoaded += hook;
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                $"Assets/_Project/Scenes/Juego/Niveles/{scene}.unity", new LoadSceneParameters(LoadSceneMode.Additive));
            SceneManager.sceneLoaded -= hook;
            yield return null;
            Assert.IsNotNull(mode, "ModeDirector pone TeamRaceMode en una pista compatible");
            Assert.IsTrue(mode.IsReady, "el modo creó las dos balsas");
            EndlessTestRig.SetSpeed(3f);
#else
            yield break;
#endif
        }

        private void CountFinish(float time) => levelFinishedEvents++;
        private void CountRespawn() => globalRespawns++;

        private static IEnumerator WaitForRunning(LevelManager level)
        {
            float t = 0f;
            while (level.State != LevelState.Running && t < 20f)
            {
                t += Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual(LevelState.Running, level.State, "la carrera empieza");
        }

        private IEnumerator Unload()
        {
            EndlessTestRig.SetSpeed(1f);
            GameEvents.OnLevelFinished -= CountFinish;
            GameEvents.OnRespawn -= CountRespawn;
            var rafts = new List<GameObject>();
            if (mode != null)
            {
                foreach (var raft in mode.Rafts)
                {
                    if (raft != null)
                        rafts.Add(raft.gameObject);
                }
            }
            // Primero la escena (cámaras y HUD), después balsas y animales, que quedaron en la escena de la prueba.
            var scene = SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid())
                yield return SceneManager.UnloadSceneAsync(scene);
            foreach (var avatar in Object.FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
                Object.Destroy(avatar.gameObject);
            foreach (var raft in rafts)
                Object.Destroy(raft);
            yield return null;
            Services.Session.ClearPlayers();
            Services.Session.RunMode = ProgressionRules.MODE_STORY;
            TeamRaceSeries.Reset();
            string dir = System.IO.Path.GetDirectoryName(savePath);
            if (System.IO.Directory.Exists(dir))
                System.IO.Directory.Delete(dir, true);
        }
    }
}
