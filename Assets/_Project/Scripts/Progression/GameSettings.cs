using GasSafari.Core;
using GasSafari.Save;
using GasSafari.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GasSafari.Progression
{
    // Opciones del juego (docs/07 §5) aplicadas al motor. Los valores viven en SaveData.settings; Apply los lee y deja los
    // ajustes estáticos que consultan el audio, la entrada y la interfaz. Se llama al cargar el guardado y al cambiar una opción.
    public static class GameSettings
    {
        // Pasos de la opción "Tamaño de textos" (docs/07 §5).
        public static readonly float[] TextScales = { 1f, 1.25f, 1.5f };
        public static readonly int[] FpsLimits = { 30, 60, 120, 0 };
        public const float AIM_MIN = 0.5f;
        public const float AIM_MAX = 2f;

        public static bool Vibration { get; private set; } = true;
        public static float AimSensitivity { get; private set; } = 1f;
        public static bool InvertAim { get; private set; }
        public static bool SlowMotion { get; private set; } = true;
        public static bool HighContrastPings { get; private set; }
        // Aguantar y Agarrarse: verdadero = pulsar una vez para empezar y otra para soltar.
        public static bool ToggleHold { get; private set; }
        public static bool ShowTips { get; private set; } = true;
        public static string BindingOverrides { get; private set; } = string.Empty;

        public static float AimFactor => AimSensitivity * (InvertAim ? -1f : 1f);

        // Del valor 0..1 de la barra a la sensibilidad (0.5x a 2x).
        public static float SensitivityFromSlider(float value01) => Mathf.Lerp(AIM_MIN, AIM_MAX, Mathf.Clamp01(value01));
        public static float SliderFromSensitivity(float sensitivity) => Mathf.InverseLerp(AIM_MIN, AIM_MAX, sensitivity);

        public static int TextScaleIndex(float scale)
        {
            int best = 0;
            for (int i = 1; i < TextScales.Length; i++)
            {
                if (Mathf.Abs(TextScales[i] - scale) < Mathf.Abs(TextScales[best] - scale))
                    best = i;
            }
            return best;
        }

        public static int FpsIndex(int fps)
        {
            for (int i = 0; i < FpsLimits.Length; i++)
            {
                if (FpsLimits[i] == fps)
                    return i;
            }
            return 1;
        }

        public static void Apply(SaveData.SettingsRecord settings)
        {
            Localization.Set(settings.language);
            LocalizedText.SetTextScale(settings.textScale);

            GameVolumes.Master = settings.volMaster;
            GameVolumes.Music = settings.volMusic;
            GameVolumes.Sfx = settings.volSfx;
            GameVolumes.Voices = settings.volVoices;
            GameVolumes.Ui = settings.volUi;

            Vibration = settings.vibration;
            AimSensitivity = Mathf.Clamp(settings.aimSensitivity, AIM_MIN, AIM_MAX);
            InvertAim = settings.invertAim;
            SlowMotion = settings.slowmo;
            HighContrastPings = settings.highContrastPings;
            ToggleHold = settings.toggleHold;
            ShowTips = settings.tips;
            BindingOverrides = settings.bindingOverrides ?? string.Empty;
            PlayerPalette.UseColorblind = settings.colorblind;

            if (Services.IsReady && Services.Session != null)
            {
                Services.Session.ScreenShake = settings.shake;
                var catalog = GameCatalog.Instance;
                if (catalog != null && catalog.playerPalette != null)
                {
                    foreach (var slot in Services.Session.Players)
                        slot.Color = catalog.playerPalette.Get(slot.Index);
                }
            }

            ApplyVideo(settings);
        }

        // Baja / Media / Alta repartidas entre los niveles de calidad del proyecto (primero, mitad, último).
        public static int QualityLevelFor(int choice)
        {
            int count = Mathf.Max(1, QualitySettings.names.Length);
            return Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp(choice, 0, 2) * (count - 1) / 2f + 0.5f), 0, count - 1);
        }

        private static void ApplyVideo(SaveData.SettingsRecord settings)
        {
            Application.targetFrameRate = settings.fpsLimit <= 0 ? -1 : settings.fpsLimit;
#if UNITY_EDITOR
            // En el editor, cambiar la calidad o el VSync se escribe en ProjectSettings: ahí solo se guarda la opción.
            return;
#else
            QualitySettings.vSyncCount = settings.vsync ? 1 : 0;
            QualitySettings.SetQualityLevel(QualityLevelFor(settings.quality), true);
            // SetQualityLevel puede reponer el VSync del nivel de calidad.
            QualitySettings.vSyncCount = settings.vsync ? 1 : 0;
#endif

            var resolutions = Screen.resolutions;
            if (settings.resolutionIndex >= 0 && settings.resolutionIndex < resolutions.Length)
            {
                var resolution = resolutions[settings.resolutionIndex];
                Screen.SetResolution(resolution.width, resolution.height, settings.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            }
            else if (Screen.fullScreen != settings.fullscreen)
            {
                Screen.fullScreenMode = settings.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            }
        }

        // Remapeo guardado: se carga en las acciones de un jugador (PlayerInput) al unirse.
        public static void ApplyBindings(InputActionAsset actions)
        {
            if (actions == null || string.IsNullOrEmpty(BindingOverrides))
                return;
            actions.LoadBindingOverridesFromJson(BindingOverrides);
        }
    }

    // Volúmenes por categoría (docs/07 §5). Sin mezclador todavía (M9-02): el audio los multiplica al reproducir.
    public static class GameVolumes
    {
        private static float master = 0.8f;

        public static float Music { get; set; } = 0.8f;
        public static float Sfx { get; set; } = 0.8f;
        public static float Voices { get; set; } = 0.8f;
        public static float Ui { get; set; } = 0.8f;

        public static float Master
        {
            get => master;
            set
            {
                master = Mathf.Clamp01(value);
                AudioListener.volume = master;
            }
        }

        // El volumen de categoría de un sonido por el prefijo de su nombre (voice_*, ui_*).
        public static float For(string soundName)
        {
            if (string.IsNullOrEmpty(soundName))
                return Sfx;
            if (soundName.StartsWith("voice", System.StringComparison.OrdinalIgnoreCase))
                return Voices;
            if (soundName.StartsWith("ui", System.StringComparison.OrdinalIgnoreCase))
                return Ui;
            return Sfx;
        }
    }
}
