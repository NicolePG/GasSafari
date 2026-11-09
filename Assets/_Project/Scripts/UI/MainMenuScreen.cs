using UnityEngine;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Menú principal (docs/07 §1): botones verticales grandes sobre la balsa en la piscina del zoológico.
    public class MainMenuScreen : MenuScreen
    {
        private static readonly Vector2 ButtonSize = new Vector2(620f, 100f);

        private Button play;
        private Button shop;
        private Button achievements;
        private Button options;
        private Button credits;
        private Button quit;

        public override bool DimBackground => false;
        public override Selectable FirstSelected => play;
        public Button PlayButton => play;
        public Button ShopButton => shop;
        public Button AchievementsButton => achievements;
        public Button OptionsButton => options;
        public Button CreditsButton => credits;
        public Button QuitButton => quit;

        protected override void Build()
        {
            var logo = UiFactory.Picture(Root, "Logo", "Logo_GasSafari", Color.white);
            UiFactory.Place(logo.rectTransform, new Vector2(0f, 1f), new Vector2(640f, 320f), new Vector2(170f, -14f));

            var list = UiFactory.VerticalList(Root, "Buttons", 12f, TextAnchor.UpperCenter);
            var rect = (RectTransform)list.transform;
            UiFactory.Place(rect, new Vector2(0f, 0f), new Vector2(ButtonSize.x, 6 * (ButtonSize.y + 12f)), new Vector2(180f, 40f));

            play = Add(list, "Play", "menu.play", ButtonStyle.Green, () => App.Push<ModeSelectScreen>());
            shop = Add(list, "Shop", "menu.shop", ButtonStyle.Blue, () => App.Push<ShopScreen>());
            achievements = Add(list, "Achievements", "menu.achievements", ButtonStyle.Blue, () => App.Push<AchievementsScreen>());
            options = Add(list, "Options", "menu.options", ButtonStyle.Blue, () => App.Push<OptionsScreen>());
            credits = Add(list, "Credits", "menu.credits", ButtonStyle.Blue, () => App.Push<CreditsScreen>());
            quit = Add(list, "Quit", "menu.quit", ButtonStyle.Red, () => App.Confirm("menu.quit_confirm", QuitGame));
            UiFactory.LinkVertical(play, shop, achievements, options, credits, quit);
        }

        private static Button Add(VerticalLayoutGroup list, string name, string key, ButtonStyle style, UnityEngine.Events.UnityAction onClick) =>
            UiFactory.MakeButton(list.transform, name, key, style, ButtonSize, onClick, 48f);

        // En el menú principal Cancelar pregunta si se quiere salir.
        public override bool HandleCancel()
        {
            App.Confirm("menu.quit_confirm", QuitGame);
            return true;
        }

        public static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
