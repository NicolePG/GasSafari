using GasSafari.Core;
using GasSafari.Modes;
using GasSafari.Progression;

namespace GasSafari.UI
{
    // Entra en un minijuego de Fiesta y vuelve a la pantalla de minijuegos (docs/06 §1).
    public static class PartyLauncher
    {
        // Las pruebas de menús lo cambian para no cargar escenas.
        public static System.Action<string> Loader = LoadScene;
        public static System.Action MenuLoader = LoadMenu;

        public static bool Play(PartyGameData game)
        {
            if (game == null || !game.implemented || string.IsNullOrEmpty(game.sceneName))
                return false;
            PartyCup.Current = game;
            if (Services.IsReady)
                Services.Session.RunMode = ProgressionRules.MODE_PARTY;
            Loader(game.sceneName);
            return true;
        }

        public static void BackToParty() => MenuLoader();

        private static void LoadScene(string scene) => SceneLoader.LoadScene(scene, "Loading_W1");

        private static void LoadMenu() => SceneLoader.LoadMenu(MenuRequest.Start.Party);

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Loader = LoadScene;
            MenuLoader = LoadMenu;
        }
    }
}
