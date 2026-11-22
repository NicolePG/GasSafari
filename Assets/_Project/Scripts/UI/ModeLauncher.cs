using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Progression;

namespace GasSafari.UI
{
    // Empieza un nivel en un modo (docs/06 §1): guarda el modo de la partida en la sesión y carga la escena. Repetir y
    // Siguiente vuelven a cargar con el mismo modo.
    public static class ModeLauncher
    {
        // Las pruebas de menús lo cambian para no cargar escenas.
        public static System.Action<LevelData> Loader = SceneLoader.LoadLevel;

        public static void Launch(LevelData level, string runMode)
        {
            if (level == null)
                return;
            if (Services.IsReady)
                Prepare(Services.Session, runMode);
            Loader(level);
        }

        // Lo que cambia en la sesión al elegir el modo (sin cargar nada: lo usan las pruebas).
        public static void Prepare(GameSession session, string runMode)
        {
            session.RunMode = string.IsNullOrEmpty(runMode) ? ProgressionRules.MODE_STORY : runMode;
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Loader = SceneLoader.LoadLevel;

        // Modo con el que "Jugar" de la ficha empieza el nivel, según la tarjeta elegida en "Elegir modo".
        public static string RunModeFor(string cardMode)
        {
            switch (cardMode)
            {
                case ProgressionRules.MODE_TIME_TRIAL:
                    return cardMode;
                default:
                    return ProgressionRules.MODE_STORY;
            }
        }
    }
}
