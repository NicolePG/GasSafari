using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Splash (logo 2 s, saltable) y pantalla de título "Pulsa cualquier botón" sobre la escena 3D animada (docs/07 §1).
    public class TitleScreen : MenuScreen
    {
        public const float SPLASH_TIME = 2f;

        private GameObject splash;
        private GameObject title;
        private Image splashLogo;
        private Image titleLogo;
        private LocalizedText prompt;
        private float timer;
        private float armDelay;
        private bool onTitle;

        public override bool DimBackground => false;
        public bool IsSplash => !onTitle;
        public bool IsTitle => onTitle;

        protected override void Build()
        {
            var logo = UiFactory.Sprite("Logo_GasSafari");

            splash = UiFactory.Rect("Splash", Root).gameObject;
            UiFactory.Stretch((RectTransform)splash.transform);
            var black = UiFactory.Picture(splash.transform, "Black", (Sprite)null, new Color(0.04f, 0.03f, 0.1f, 1f), false);
            UiFactory.Stretch(black.rectTransform);
            splashLogo = UiFactory.Picture(splash.transform, "Logo", logo, Color.white);
            UiFactory.Place(splashLogo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1280f, 640f), Vector2.zero);

            title = UiFactory.Rect("Title", Root).gameObject;
            UiFactory.Stretch((RectTransform)title.transform);
            titleLogo = UiFactory.Picture(title.transform, "Logo", logo, Color.white);
            UiFactory.Place(titleLogo.rectTransform, new Vector2(0.5f, 1f), new Vector2(900f, 450f), new Vector2(0f, -10f));
            var promptBack = UiFactory.Panel(title.transform, "PromptBack", "Panel_Dark", new Color(1f, 1f, 1f, 0.85f));
            UiFactory.Place(promptBack.rectTransform, new Vector2(0.5f, 0f), new Vector2(1100f, 130f), new Vector2(0f, 70f));
            promptBack.raycastTarget = false;
            prompt = UiFactory.Label(promptBack.transform, "Prompt", "ui.press_any", 56f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold).Outlined(0.3f);
            UiFactory.Stretch(prompt.Label.rectTransform, 20f);
        }

        public override void OnShow()
        {
            onTitle = false;
            timer = 0f;
            armDelay = 0.25f;
            splash.SetActive(true);
            title.SetActive(false);
        }

        public override bool HandleCancel() => false;

        private void Update()
        {
            armDelay -= Time.unscaledDeltaTime;
            timer += Time.unscaledDeltaTime;
            bool pressed = armDelay <= 0f && AnyButtonPressed();

            if (!onTitle)
            {
                // El logo entra con un fundido corto.
                splashLogo.color = new Color(1f, 1f, 1f, Mathf.Clamp01(timer / 0.5f));
                if (timer >= SPLASH_TIME || pressed)
                    ShowTitle();
                return;
            }

            // "Pulsa cualquier botón" late suavemente.
            float pulse = 0.5f + 0.5f * Mathf.Sin(timer * 3.2f);
            prompt.Label.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.55f, 1f, pulse));
            prompt.transform.localScale = Vector3.one * Mathf.Lerp(0.97f, 1.03f, pulse);
            if (pressed)
                App.GoTo<MainMenuScreen>();
        }

        public void ShowTitle()
        {
            onTitle = true;
            timer = 0f;
            armDelay = 0.3f;
            splash.SetActive(false);
            title.SetActive(true);
        }

        // Cualquier botón de teclado, mando o ratón.
        public static bool AnyButtonPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
                return true;
            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
                return true;
            var pads = Gamepad.all;
            for (int i = 0; i < pads.Count; i++)
            {
                var pad = pads[i];
                if (pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame
                    || pad.buttonNorth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame
                    || pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame || pad.dpad.up.wasPressedThisFrame
                    || pad.dpad.down.wasPressedThisFrame)
                    return true;
            }
            return false;
        }
    }
}
