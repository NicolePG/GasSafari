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
            // La escena de Infinito solo se juega en Infinito o en el Desafío Diario (EndlessTrack decide con el mismo modo).
            if (EndlessTrack.Current != null && runMode != ProgressionRules.MODE_DAILY)
                runMode = ProgressionRules.MODE_ENDLESS;
            // El tutorial no tiene cronómetro: no hay fantasma, espejo ni modificadores.
            bool timed = level.Data != null && level.Data.showTimer;
            GameModeBase mode;
            switch (timed ? runMode : ProgressionRules.MODE_STORY)
            {
                case ProgressionRules.MODE_ENDLESS: mode = level.gameObject.AddComponent<EndlessMode>(); break;
                case ProgressionRules.MODE_DAILY: mode = level.gameObject.AddComponent<DailyMode>(); break;
                case ProgressionRules.MODE_TIME_TRIAL: mode = level.gameObject.AddComponent<TimeTrialMode>(); break;
                case ProgressionRules.MODE_MIRROR: mode = level.gameObject.AddComponent<MirrorMode>(); break;
                case ProgressionRules.MODE_MODIFIERS: mode = level.gameObject.AddComponent<ModifiersMode>(); break;
                // Solo en pistas con TeamRaceSpawn (docs/06 §1); en otro nivel se juega como Historia.
                case ProgressionRules.MODE_TEAM_RACE when level.Data.raceCompatible: mode = level.gameObject.AddComponent<TeamRaceMode>(); break;
                default: mode = level.gameObject.AddComponent<StoryMode>(); break;
            }
            mode.Bind(level);
            return mode;
        }
    }
}
