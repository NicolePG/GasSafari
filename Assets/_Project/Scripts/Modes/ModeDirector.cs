using GasSafari.Level;
using GasSafari.Progression;

namespace GasSafari.Modes
{
    // Pone el modo de la partida (GameSession.RunMode) sobre el nivel recién cargado. Lo llama LevelFlow en los niveles que
    // salen de los menús y las pruebas lo llaman a mano.
    public static class ModeDirector
    {
        public static GameModeBase Attach(LevelManager level, string runMode)
        {
            if (level == null)
                return null;
            var existing = level.GetComponent<GameModeBase>();
            if (existing != null)
                return existing;
            // El tutorial no tiene cronómetro: no hay fantasma, espejo ni modificadores.
            bool timed = level.Data != null && level.Data.showTimer;
            GameModeBase mode;
            switch (timed ? runMode : ProgressionRules.MODE_STORY)
            {
                case ProgressionRules.MODE_TIME_TRIAL: mode = level.gameObject.AddComponent<TimeTrialMode>(); break;
                default: mode = level.gameObject.AddComponent<StoryMode>(); break;
            }
            mode.Bind(level);
            return mode;
        }
    }
}
