using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Modes;
using GasSafari.Progression;

namespace GasSafari.UI
{
    // Entra en Infinito o en el Desafío Diario desde la sala (docs/06 §1): los dos cargan la escena Infinito; el modo de la
    // partida decide qué río se arma.
    public static class EndlessLauncher
    {
        // Las pruebas de menús lo cambian para no cargar escenas.
        public static System.Action<LevelData> Loader = SceneLoader.LoadLevel;

        public static bool IsEndlessMode(string modeId) => modeId == ProgressionRules.MODE_ENDLESS || modeId == ProgressionRules.MODE_DAILY;

        public static bool IsEndlessLevel(LevelData level)
        {
            var config = EndlessConfig.Instance;
            return level != null && config != null && config.level == level;
        }

        public static bool Play(string runMode)
        {
            var config = EndlessConfig.Instance;
            if (!IsEndlessMode(runMode) || config == null || config.level == null)
                return false;
            if (Services.IsReady)
                Services.Session.RunMode = runMode;
            Loader(config.level);
            return true;
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Loader = SceneLoader.LoadLevel;
    }
}
