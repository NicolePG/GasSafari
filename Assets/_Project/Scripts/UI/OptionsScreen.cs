using System;
using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Progression;
using GasSafari.Save;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Mantiene a la vista el elemento enfocado de una lista con desplazamiento (mando y teclado).
    public class ScrollFollow : MonoBehaviour, ISelectHandler
    {
        public ScrollRect Scroll;

        public void OnSelect(BaseEventData eventData)
        {
            if (Scroll == null || Scroll.content == null)
                return;
            var content = Scroll.content;
            var viewport = Scroll.viewport;
            var item = (RectTransform)transform;
            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4];
            item.GetWorldCorners(corners);
            var local0 = viewport.InverseTransformPoint(corners[0]);
            var local1 = viewport.InverseTransformPoint(corners[1]);
            float margin = 10f;
            float shift = 0f;
            if (local1.y > viewport.rect.yMax - margin)
                shift = viewport.rect.yMax - margin - local1.y;
            else if (local0.y < viewport.rect.yMin + margin)
                shift = viewport.rect.yMin + margin - local0.y;
            if (Mathf.Abs(shift) > 0.01f)
            {
                var position = content.anchoredPosition;
                position.y -= shift;
                float maxY = Mathf.Max(0f, content.rect.height - viewport.rect.height);
                position.y = Mathf.Clamp(position.y, 0f, maxY);
                content.anchoredPosition = position;
            }
        }
    }

    // Opciones completas (docs/07 §5): Audio, Video, Controles (remapeo), Accesibilidad y Juego. Todo se aplica y se guarda al
    // cambiarlo. Se maneja con mando, teclado o ratón: izquierda/derecha cambian el valor de la fila enfocada.
    public class OptionsScreen : MenuScreen
    {
        public const int TAB_AUDIO = 0, TAB_VIDEO = 1, TAB_CONTROLS = 2, TAB_ACCESS = 3, TAB_GAME = 4;

        private static readonly string[] TabKeys =
            { "options.tab.audio", "options.tab.video", "options.tab.controls", "options.tab.access", "options.tab.game" };

        private class Rebind
        {
            public string Action;
            public string LabelKey;
            public OptionRow Row;
        }

        private readonly Button[] tabs = new Button[5];
        private readonly List<OptionRow>[] pages = new List<OptionRow>[5];
        private readonly GameObject[] pageRoots = new GameObject[5];
        private readonly ScrollRect[] scrolls = new ScrollRect[5];
        private readonly List<Rebind> rebinds = new List<Rebind>();
        private Button back;
        private int tab;
        private bool building;
        private LocalizedText rebindPrompt;
        private GameObject rebindOverlay;
        private InputActionRebindingExtensions.RebindingOperation operation;
        private InputActionAsset controls;
        private OptionRow deviceRow;
        private OptionRow resetRow;
        private int deviceIndex;
        private Resolution[] resolutions = new Resolution[0];

        // Los controles de cada fila para que las pruebas los lean y muevan.
        public OptionRow Master, Music, Sfx, VoicesRow, UiVolume, ResolutionRow, FullscreenRow, VsyncRow, QualityRow, FpsRow;
        public OptionRow VibrationRow, AimSensRow, AimInvertRow, ShakeRow, SlowmoRow, ColorblindRow, TextSizeRow, ContrastRow, HoldModeRow;
        public OptionRow DifficultyRow, TipsRow, LanguageRow;

        public int CurrentTab => tab;
        public Button BackButton => back;
        public Button[] Tabs => tabs;
        public IReadOnlyList<OptionRow> Page(int index) => pages[index];
        public bool IsRebinding => operation != null;
        public event Action Closed;

        private static SaveData.SettingsRecord Settings => Services.IsReady && Services.Progression != null ? Services.Progression.Data.settings : fallback;
        private static readonly SaveData.SettingsRecord fallback = new SaveData.SettingsRecord();

        public override Selectable FirstSelected => tabs[tab];

        // ---------------------------------------------------------------- construcción
        protected override void Build()
        {
            building = true;
            var title = UiFactory.Label(Root, "Title", "options.title", 74f, TextAlignmentOptions.Center, UiFactory.Cream, FontStyles.Bold).Outlined(0.3f);
            UiFactory.Place(title.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(1200f, 100f), new Vector2(0f, -20f));

            var tabRow = UiFactory.HorizontalList(Root, "Tabs", 14f);
            UiFactory.Place((RectTransform)tabRow.transform, new Vector2(0.5f, 1f), new Vector2(1760f, 90f), new Vector2(0f, -124f));
            for (int i = 0; i < tabs.Length; i++)
            {
                int captured = i;
                tabs[i] = UiFactory.MakeButton(tabRow.transform, "Tab" + i, TabKeys[i], ButtonStyle.Blue, new Vector2(340f, 86f), () => SetTab(captured), 34f);
            }
            UiFactory.LinkHorizontal(tabs);

            var panel = UiFactory.Panel(Root, "Panel", "Panel_Dark");
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1500f, 700f), new Vector2(0f, -66f));
            panel.raycastTarget = false;

            BuildAudio(panel.transform);
            BuildVideo(panel.transform);
            BuildControls(panel.transform);
            BuildAccess(panel.transform);
            BuildGame(panel.transform);

            back = UiFactory.MakeButton(Root, "Back", "ui.back", ButtonStyle.Red, new Vector2(300f, 92f), CloseScreen, 40f);
            UiFactory.Place((RectTransform)back.transform, new Vector2(0f, 0f), new Vector2(300f, 92f), new Vector2(40f, 24f));

            BuildRebindOverlay();
            building = false;
            SetTab(TAB_AUDIO, false);
        }

        private OptionRow Page(Transform parent, int index, out RectTransform content)
        {
            var scroll = UiFactory.Scroll(parent, "Page" + index, out content);
            UiFactory.Stretch((RectTransform)scroll.transform, 16f);
            pageRoots[index] = scroll.gameObject;
            scrolls[index] = scroll;
            pages[index] = new List<OptionRow>();
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(0, 0, 8, 8);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return null;
        }

        private OptionRow AddRow(int page, RectTransform content, string labelKey, OptionRow.Kind kind)
        {
            var row = OptionRow.Create(content, labelKey, labelKey, kind, new Vector2(1380f, 84f));
            row.gameObject.AddComponent<ScrollFollow>().Scroll = scrolls[page];
            pages[page].Add(row);
            return row;
        }

        private void BuildAudio(Transform parent)
        {
            Page(parent, TAB_AUDIO, out var content);
            var s = Settings;
            Master = Slider(TAB_AUDIO, content, "options.audio.master", s.volMaster, v => { Settings.volMaster = v; GameVolumes.Master = v; });
            Music = Slider(TAB_AUDIO, content, "options.audio.music", s.volMusic, v => { Settings.volMusic = v; GameVolumes.Music = v; });
            Sfx = Slider(TAB_AUDIO, content, "options.audio.sfx", s.volSfx, v => { Settings.volSfx = v; GameVolumes.Sfx = v; });
            VoicesRow = Slider(TAB_AUDIO, content, "options.audio.voices", s.volVoices, v => { Settings.volVoices = v; GameVolumes.Voices = v; });
            UiVolume = Slider(TAB_AUDIO, content, "options.audio.ui", s.volUi, v => { Settings.volUi = v; GameVolumes.Ui = v; });
        }

        private OptionRow Slider(int page, RectTransform content, string key, float value, Action<float> set)
        {
            var row = AddRow(page, content, key, OptionRow.Kind.Slider);
            row.SetSlider(value);
            row.Changed += r => { set(r.Slider01); Persist(); };
            return row;
        }

        private OptionRow Toggle(int page, RectTransform content, string key, bool value, Action<bool> set)
        {
            var row = AddRow(page, content, key, OptionRow.Kind.Toggle);
            row.SetToggle(value);
            row.Changed += r => { set(r.Toggle); Persist(); };
            return row;
        }

        private void BuildVideo(Transform parent)
        {
            Page(parent, TAB_VIDEO, out var content);
            var s = Settings;
            resolutions = UniqueResolutions();
            ResolutionRow = AddRow(TAB_VIDEO, content, "options.video.resolution", OptionRow.Kind.Choice);
            var labels = new string[resolutions.Length];
            for (int i = 0; i < labels.Length; i++)
                labels[i] = resolutions[i].width + "x" + resolutions[i].height;
            if (labels.Length == 0)
                labels = new[] { Screen.width + "x" + Screen.height };
            ResolutionRow.SetLiteralChoices(labels, s.resolutionIndex >= 0 ? s.resolutionIndex : labels.Length - 1);
            ResolutionRow.Changed += r => { Settings.resolutionIndex = resolutions.Length > 0 ? r.ChoiceIndex : -1; Persist(); };

            FullscreenRow = AddRow(TAB_VIDEO, content, "options.video.fullscreen", OptionRow.Kind.Choice);
            FullscreenRow.SetChoices(new[] { "options.video.windowed", "options.video.fullscreen" }, s.fullscreen ? 1 : 0);
            FullscreenRow.Changed += r => { Settings.fullscreen = r.ChoiceIndex == 1; Persist(); };

            VsyncRow = Toggle(TAB_VIDEO, content, "options.video.vsync", s.vsync, v => Settings.vsync = v);

            QualityRow = AddRow(TAB_VIDEO, content, "options.video.quality", OptionRow.Kind.Choice);
            QualityRow.SetChoices(new[] { "options.video.quality_low", "options.video.quality_medium", "options.video.quality_high" }, s.quality);
            QualityRow.Changed += r => { Settings.quality = r.ChoiceIndex; Persist(); };

            FpsRow = AddRow(TAB_VIDEO, content, "options.video.fps_limit", OptionRow.Kind.Choice);
            // Los números no están en la tabla (la tabla devuelve la propia clave); "Sin límite" sí.
            FpsRow.SetChoices(new[] { "30", "60", "120", "options.video.fps_unlimited" }, GameSettings.FpsIndex(s.fpsLimit));
            FpsRow.Changed += r => { Settings.fpsLimit = GameSettings.FpsLimits[r.ChoiceIndex]; Persist(); };
        }

        private static Resolution[] UniqueResolutions()
        {
            var seen = new HashSet<string>();
            var list = new List<Resolution>();
            foreach (var resolution in Screen.resolutions)
            {
                if (seen.Add(resolution.width + "x" + resolution.height))
                    list.Add(resolution);
            }
            return list.ToArray();
        }

        private void BuildControls(Transform parent)
        {
            Page(parent, TAB_CONTROLS, out var content);
            var s = Settings;
            VibrationRow = Toggle(TAB_CONTROLS, content, "options.controls.vibration", s.vibration, v => Settings.vibration = v);
            AimSensRow = Slider(TAB_CONTROLS, content, "options.controls.aim_sens", GameSettings.SliderFromSensitivity(s.aimSensitivity),
                v => Settings.aimSensitivity = GameSettings.SensitivityFromSlider(v));
            AimInvertRow = Toggle(TAB_CONTROLS, content, "options.controls.aim_invert", s.invertAim, v => Settings.invertAim = v);

            // Remapeo por dispositivo: primero se elige Teclado o Mando y luego cada acción.
            deviceRow = AddRow(TAB_CONTROLS, content, "options.controls.rebind", OptionRow.Kind.Choice);
            deviceRow.SetChoices(new[] { "options.controls.keyboard", "options.controls.gamepad" }, 0);
            deviceRow.Changed += r => { deviceIndex = r.ChoiceIndex; RefreshBindings(); };

            AddRebind(content, "Fart", "ctrl.fart");
            AddRebind(content, "Hold", "ctrl.charge");
            AddRebind(content, "Ability", "ctrl.ability");
            AddRebind(content, "Grab", "ctrl.grab");
            AddRebind(content, "SuperLeft", "ctrl.superfart");
            AddRebind(content, "Jump", "ctrl.swim");
            AddRebind(content, "Pause", "ctrl.pause");
            AddRebind(content, "Move", "ctrl.move");
            AddRebind(content, "Aim", "ctrl.aim");
            AddRebind(content, "Ping", "ctrl.pings");

            resetRow = AddRow(TAB_CONTROLS, content, "options.controls.reset", OptionRow.Kind.Action);
            resetRow.SetActionText(">");
            resetRow.Activated += r => ResetBindings();
        }

        private void AddRebind(RectTransform content, string action, string labelKey)
        {
            var row = AddRow(TAB_CONTROLS, content, labelKey, OptionRow.Kind.Action);
            var entry = new Rebind { Action = action, LabelKey = labelKey, Row = row };
            rebinds.Add(entry);
            row.Activated += r => StartRebind(entry);
        }

        private void BuildAccess(Transform parent)
        {
            Page(parent, TAB_ACCESS, out var content);
            var s = Settings;
            ShakeRow = Toggle(TAB_ACCESS, content, "options.access.shake", s.shake, v => Settings.shake = v);
            SlowmoRow = Toggle(TAB_ACCESS, content, "options.access.slowmo", s.slowmo, v => Settings.slowmo = v);
            ColorblindRow = Toggle(TAB_ACCESS, content, "options.access.colorblind", s.colorblind, v => Settings.colorblind = v);
            TextSizeRow = AddRow(TAB_ACCESS, content, "options.access.text_size", OptionRow.Kind.Choice);
            TextSizeRow.SetLiteralChoices(new[] { "100%", "125%", "150%" }, GameSettings.TextScaleIndex(s.textScale));
            TextSizeRow.Changed += r => { Settings.textScale = GameSettings.TextScales[r.ChoiceIndex]; Persist(); };
            ContrastRow = Toggle(TAB_ACCESS, content, "options.access.contrast", s.highContrastPings, v => Settings.highContrastPings = v);
            HoldModeRow = AddRow(TAB_ACCESS, content, "options.access.hold_mode", OptionRow.Kind.Choice);
            HoldModeRow.SetChoices(new[] { "options.access.hold", "options.access.toggle" }, s.toggleHold ? 1 : 0);
            HoldModeRow.Changed += r => { Settings.toggleHold = r.ChoiceIndex == 1; Persist(); };
        }

        private void BuildGame(Transform parent)
        {
            Page(parent, TAB_GAME, out var content);
            var s = Settings;
            DifficultyRow = AddRow(TAB_GAME, content, "options.game.default_difficulty", OptionRow.Kind.Choice);
            var catalog = GameCatalog.Instance;
            var keys = new List<string>();
            int selected = 0;
            if (catalog != null)
            {
                for (int i = 0; i < catalog.difficulties.Length; i++)
                {
                    keys.Add(catalog.difficulties[i].NameKey);
                    if (catalog.difficulties[i].id == s.difficulty)
                        selected = i;
                }
            }
            DifficultyRow.SetChoices(keys.ToArray(), selected);
            DifficultyRow.Changed += r =>
            {
                if (catalog != null)
                    Settings.difficulty = catalog.difficulties[r.ChoiceIndex].id;
                Persist();
            };
            TipsRow = Toggle(TAB_GAME, content, "options.game.tips", s.tips, v => Settings.tips = v);
            LanguageRow = AddRow(TAB_GAME, content, "options.game.language", OptionRow.Kind.Choice);
            LanguageRow.SetChoices(new[] { "lang.es", "lang.en" }, Array.IndexOf(Localization.Languages, Localization.Normalize(s.language)));
            LanguageRow.Changed += r => { Settings.language = Localization.Languages[r.ChoiceIndex]; Persist(); };
        }

        // ---------------------------------------------------------------- pestañas y navegación
        public void SetTab(int index, bool focus = true)
        {
            tab = Mathf.Clamp(index, 0, tabs.Length - 1);
            for (int i = 0; i < tabs.Length; i++)
            {
                pageRoots[i].SetActive(i == tab);
                SetTabStyle(tabs[i], i == tab ? "Green" : "Blue");
            }
            LinkPage();
            if (tab == TAB_CONTROLS)
                RefreshBindings();
            if (scrolls[tab] != null)
                scrolls[tab].verticalNormalizedPosition = 1f;
        }

        private static void SetTabStyle(Button button, string style)
        {
            button.GetComponent<Image>().sprite = UiFactory.Sprite("Button_" + style + "_Normal");
            button.spriteState = new SpriteState
            {
                highlightedSprite = UiFactory.Sprite("Button_" + style + "_Highlighted"),
                selectedSprite = UiFactory.Sprite("Button_" + style + "_Highlighted"),
                pressedSprite = UiFactory.Sprite("Button_" + style + "_Pressed"),
                disabledSprite = UiFactory.Sprite("Button_" + style + "_Disabled")
            };
        }

        // Pestañas ← →, filas ↑ ↓, la primera fila sube a las pestañas y la última baja al botón Atrás.
        private void LinkPage()
        {
            var rows = pages[tab];
            var firstRow = rows.Count > 0 ? rows[0] : null;
            var lastRow = rows.Count > 0 ? rows[rows.Count - 1] : null;
            for (int i = 0; i < tabs.Length; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnLeft = i > 0 ? tabs[i - 1] : null;
                nav.selectOnRight = i < tabs.Length - 1 ? tabs[i + 1] : null;
                nav.selectOnDown = firstRow != null ? (Selectable)firstRow : back;
                tabs[i].navigation = nav;
            }
            for (int i = 0; i < rows.Count; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnUp = i > 0 ? (Selectable)rows[i - 1] : tabs[tab];
                nav.selectOnDown = i < rows.Count - 1 ? (Selectable)rows[i + 1] : back;
                rows[i].navigation = nav;
            }
            back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = lastRow != null ? (Selectable)lastRow : tabs[tab] };
        }

        public override void OnShow()
        {
            building = false;
            Localization.Changed -= OnLanguageChanged;
            Localization.Changed += OnLanguageChanged;
            SetTab(tab, false);
        }

        public override void OnHide()
        {
            Localization.Changed -= OnLanguageChanged;
            CancelRebind();
            if (Services.IsReady && Services.Progression != null)
                Services.Progression.Save();
        }

        protected override void Teardown()
        {
            Localization.Changed -= OnLanguageChanged;
            CancelRebind();
        }

        private void OnLanguageChanged()
        {
            RefreshBindings();
        }

        public override bool HandleCancel()
        {
            if (IsRebinding)
            {
                CancelRebind();
                return true;
            }
            CloseScreen();
            return true;
        }

        private void CloseScreen()
        {
            App.Back();
            Closed?.Invoke();
        }

        // Aplica y guarda todo lo que cambió.
        private void Persist()
        {
            if (building)
                return;
            GameSettings.Apply(Settings);
            if (Services.IsReady && Services.Progression != null)
                Services.Progression.Save();
        }

        // ---------------------------------------------------------------- remapeo
        private string DeviceGroup => deviceIndex == 0 ? "Keyboard" : "Gamepad";

        private InputActionAsset Controls
        {
            get
            {
                if (controls == null)
                {
                    var catalog = GameCatalog.Instance;
                    if (catalog != null && catalog.controls != null)
                    {
                        controls = UnityEngine.Object.Instantiate(catalog.controls);
                        GameSettings.ApplyBindings(controls);
                    }
                }
                return controls;
            }
        }

        public void RefreshBindings()
        {
            var asset = Controls;
            foreach (var entry in rebinds)
            {
                string text = "-";
                bool available = false;
                if (asset != null)
                {
                    var action = asset.FindAction("Player/" + entry.Action);
                    if (action != null)
                    {
                        var indices = RebindableIndices(action, DeviceGroup);
                        available = indices.Count > 0;
                        var parts = new List<string>();
                        foreach (int index in indices)
                            parts.Add(action.GetBindingDisplayString(index));
                        if (available)
                            text = string.Join(" · ", parts);
                    }
                }
                entry.Row.SetActionText(text);
                entry.Row.interactable = available;
            }
            LinkPage();
        }

        // Los bindings del dispositivo que se pueden cambiar: botones y las teclas de un compuesto (no las palancas).
        public static List<int> RebindableIndices(InputAction action, string group)
        {
            var result = new List<int>();
            var bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                if (binding.isComposite || binding.groups == null || !binding.groups.Contains(group))
                    continue;
                string path = binding.effectivePath;
                // Palancas y cruceta quedan fijas: son ejes, no botones.
                if (path.Contains("Stick") || path.EndsWith("/dpad"))
                    continue;
                result.Add(i);
            }
            return result;
        }

        private void BuildRebindOverlay()
        {
            rebindOverlay = UiFactory.Backdrop(Root, "RebindOverlay").gameObject;
            rebindPrompt = UiFactory.Label(rebindOverlay.transform, "Prompt", "options.controls.rebind_prompt", 72f, TextAlignmentOptions.Center, UiFactory.Gold, FontStyles.Bold).Outlined(0.3f);
            UiFactory.Place(rebindPrompt.Label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1500f, 160f), Vector2.zero);
            rebindOverlay.SetActive(false);
        }

        private void StartRebind(Rebind entry)
        {
            var asset = Controls;
            if (asset == null || IsRebinding)
                return;
            var action = asset.FindAction("Player/" + entry.Action);
            var indices = RebindableIndices(action, DeviceGroup);
            if (indices.Count == 0)
                return;
            rebindOverlay.SetActive(true);
            action.Disable();
            RebindNext(action, indices, 0, entry);
        }

        // Cada tecla del compuesto se pide por turno ("Pulsa un botón..."); Esc o el botón Atrás del mando cancelan.
        private void RebindNext(InputAction action, List<int> indices, int position, Rebind entry)
        {
            if (position >= indices.Count)
            {
                FinishRebind(action);
                return;
            }
            var group = DeviceGroup;
            operation = action.PerformInteractiveRebinding(indices[position])
                .WithCancelingThrough(deviceIndex == 0 ? "<Keyboard>/escape" : "<Gamepad>/start")
                .WithControlsHavingToMatchPath(deviceIndex == 0 ? "<Keyboard>" : "<Gamepad>")
                .WithControlsExcluding("<Mouse>")
                .OnComplete(op =>
                {
                    op.Dispose();
                    operation = null;
                    RebindNext(action, indices, position + 1, entry);
                })
                .OnCancel(op =>
                {
                    op.Dispose();
                    operation = null;
                    FinishRebind(action);
                })
                .Start();
        }

        private void FinishRebind(InputAction action)
        {
            action.Enable();
            rebindOverlay.SetActive(false);
            Settings.bindingOverrides = Controls.SaveBindingOverridesAsJson();
            Persist();
            RefreshBindings();
        }

        private void CancelRebind()
        {
            if (operation != null)
            {
                operation.Cancel();
                operation = null;
            }
            if (rebindOverlay != null)
                rebindOverlay.SetActive(false);
        }

        public void ResetBindings()
        {
            Settings.bindingOverrides = string.Empty;
            if (Controls != null)
                Controls.RemoveAllBindingOverrides();
            Persist();
            RefreshBindings();
        }
    }
}
