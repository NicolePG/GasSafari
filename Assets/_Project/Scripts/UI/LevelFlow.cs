using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GasSafari.UI
{
    // Opciones encima de un nivel en pausa (docs/07 §4): se abren como una instancia superpuesta de los menús.
    public static class OptionsOverlay
    {
        public static bool IsOpen { get; private set; }

        public static void Open(int tab = OptionsScreen.TAB_AUDIO)
        {
            if (IsOpen)
                return;
            var app = MenuApp.CreateOverlay();
            IsOpen = true;
            app.OverlayClosed += () => IsOpen = false;
            var options = app.Push<OptionsScreen>();
            options.SetTab(tab);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => IsOpen = false;
    }

    // Conecta los botones de un nivel con la carga de escenas: Siguiente / Repetir / Mapa de los resultados y Reiniciar nivel /
    // Opciones / Controles / Salir al mapa de la pausa. Se engancha sola cada vez que carga una escena de nivel.
    public static class LevelFlow
    {
        private static readonly HashSet<int> hooked = new HashSet<int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            hooked.Clear();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Hook();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Hook();

        // Lo arma SceneLoader al cargar un nivel desde los menús: ahí sí hay Ayuda Real (docs/06 §3).
        // Las pruebas y los niveles abiertos a mano en el editor no la tienen salvo que se agregue RoyalAssist.
        public static bool AssistArmed { get; set; }

        // Verdadero mientras se juega un nivel cargado desde los menús (SceneLoader.LoadLevel). Los botones de resultados y
        // pausa solo cambian de escena en ese flujo; un nivel abierto a mano o por una prueba no sale a ninguna parte.
        public static bool InGameFlow { get; set; }

        public static void Hook()
        {
            if (!InGameFlow)
                return;
            foreach (var results in Object.FindObjectsByType<ResultsScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!hooked.Add(results.GetInstanceID()))
                    continue;
                results.NextRequested += Next;
                results.RetryRequested += Retry;
                results.MapRequested += ToMap;
            }
            if (AssistArmed)
            {
                var checkpoints = Object.FindFirstObjectByType<CheckpointSystem>();
                if (checkpoints != null && checkpoints.GetComponent<RoyalAssist>() == null && LevelManager.Current != null)
                {
                    AssistArmed = false;
                    checkpoints.gameObject.AddComponent<RoyalAssist>().Bind(LevelManager.Current, checkpoints);
                }
            }
            foreach (var pause in Object.FindObjectsByType<PauseController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!hooked.Add(pause.GetInstanceID()))
                    continue;
                pause.RestartLevelRequested += Retry;
                pause.QuitToMapRequested += ToMap;
                pause.OptionsRequested += () => OptionsOverlay.Open(OptionsScreen.TAB_AUDIO);
                pause.ControlsRequested += () => OptionsOverlay.Open(OptionsScreen.TAB_CONTROLS);
            }
        }

        public static LevelData CurrentLevel
        {
            get
            {
                if (LevelManager.Current != null && LevelManager.Current.Data != null)
                    return LevelManager.Current.Data;
                return Services.IsReady ? Services.Session.CurrentLevel : null;
            }
        }

        public static void Retry()
        {
            var level = CurrentLevel;
            if (level != null)
                SceneLoader.LoadLevel(level);
        }

        public static void Next()
        {
            var level = CurrentLevel;
            var catalog = GameCatalog.Instance;
            var next = catalog != null ? catalog.NextLevel(level) : null;
            if (next != null)
                SceneLoader.LoadLevel(next);
            else
                ToMap();
        }

        public static void ToMap()
        {
            SceneLoader.LoadMenu(MenuRequest.Start.WorldMap, CurrentLevel);
        }
    }
}
