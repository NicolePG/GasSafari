using System.Collections;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GasSafari.UI
{
    // Prueba de humo de la build (build-release, paso 6): con "-gassafari-smoke W1-1" el juego carga ese nivel con 4 bots,
    // espera a que terminen la carrera, escribe "SMOKE ..." en Player.log y se cierra (código 0 = terminó, 1 = no).
    // Sin ese argumento no hace nada.
    public static class BuildSmokeTest
    {
        private const string ARG = "-gassafari-smoke";
        private const float REAL_TIMEOUT = 420f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (Application.isEditor)
                return;
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] != ARG)
                    continue;
                var go = new GameObject("BuildSmokeTest");
                Object.DontDestroyOnLoad(go);
                go.AddComponent<Runner>().StartCoroutine(Run(args[i + 1]));
                return;
            }
        }

        private class Runner : MonoBehaviour { }

        private static IEnumerator Run(string levelId)
        {
            float start = Time.realtimeSinceStartup;
            Debug.Log("SMOKE inicio, nivel " + levelId);
            while (!Services.IsReady || GameCatalog.Instance == null)
                yield return null;
            var level = GameCatalog.Instance.FindLevel(levelId);
            if (level == null)
            {
                Debug.Log("SMOKE ERROR nivel no encontrado " + levelId);
                Application.Quit(1);
                yield break;
            }

            Services.Session.ClearPlayers();
            var characters = GameCatalog.Instance.characters;
            for (int i = 0; i < 4 && i < characters.Length; i++)
                Services.Session.AddPlayer(new PlayerSlot(i, Color.white, null, null) { IsBot = true, Character = characters[i] });
            SceneLoader.LoadLevel(level);
            Debug.Log("SMOKE cargando " + level.sceneName);

            while ((LevelManager.Current == null || SceneManager.GetActiveScene().name != level.sceneName)
                   && Time.realtimeSinceStartup - start < REAL_TIMEOUT)
                yield return null;
            var manager = LevelManager.Current;
            if (manager == null)
            {
                Debug.Log("SMOKE ERROR el nivel no cargó");
                Application.Quit(1);
                yield break;
            }

            float finishedAt = -1f;
            manager.Finished += time => finishedAt = time;
            Debug.Log("SMOKE nivel cargado en " + (Time.realtimeSinceStartup - start).ToString("F1") + " s reales");
            if (Services.TimeController != null)
                Services.TimeController.SetBaseScale(3f);
            float lastLog = Time.realtimeSinceStartup;
            while (finishedAt < 0f && Time.realtimeSinceStartup - start < REAL_TIMEOUT)
            {
                if (Time.realtimeSinceStartup - lastLog > 20f)
                {
                    lastLog = Time.realtimeSinceStartup;
                    var p = manager.Raft.transform.position;
                    Debug.Log("SMOKE en carrera balsa=(" + p.x.ToString("F0") + "," + p.z.ToString("F0") + ") estado=" + manager.State);
                }
                yield return null;
            }
            if (finishedAt >= 0f)
            {
                Debug.Log("SMOKE OK terminó " + levelId + " en " + finishedAt.ToString("F1") + " s de juego, "
                          + (Time.realtimeSinceStartup - start).ToString("F0") + " s reales");
                Application.Quit(0);
            }
            else
            {
                Debug.Log("SMOKE ERROR no terminó en " + REAL_TIMEOUT + " s reales");
                Application.Quit(1);
            }
        }
    }
}
