using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Modes;
using GasSafari.Progression;

namespace GasSafari.UI
{
    // Carga las pistas de una serie de Carrera por Equipos (docs/06 §1) y vuelve al menú al terminarla.
    public static class TeamRaceLauncher
    {
        // Las pruebas lo cambian para no cargar escenas.
        public static System.Action<LevelData> Loader = SceneLoader.LoadLevel;
        public static System.Action MenuLoader = LoadMenu;

        public static bool Begin(IReadOnlyList<LevelData> tracks)
        {
            if (tracks == null || tracks.Count == 0)
                return false;
            TeamRaceSeries.Begin(tracks);
            return Play(TeamRaceSeries.CurrentTrack);
        }

        private static bool Play(LevelData track)
        {
            if (track == null)
                return false;
            if (Services.IsReady)
                Services.Session.RunMode = ProgressionRules.MODE_TEAM_RACE;
            Loader(track);
            return true;
        }

        // La pista siguiente de la serie o, si ya terminó, el menú.
        public static void Next()
        {
            var next = TeamRaceSeries.Advance();
            if (next != null)
                Play(next);
            else
                Exit();
        }

        public static void Exit() => MenuLoader();

        private static void LoadMenu() => SceneLoader.LoadMenu(MenuRequest.Start.MainMenu);

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Loader = SceneLoader.LoadLevel;
            MenuLoader = LoadMenu;
        }
    }
}
