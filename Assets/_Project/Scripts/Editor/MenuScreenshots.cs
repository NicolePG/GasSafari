using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using GasSafari.Core;
using GasSafari.Progression;
using GasSafari.Save;
using GasSafari.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GasSafari.Editor
{
    // QA de M6: recorre todas las pantallas de los menús en español e inglés, a tamaño de texto 100 % y 150 %, guarda una captura
    // de cada una en Screenshots/M6/<idioma>[_150]/ (no se suben a Git) y revisa que ningún texto se salga de su caja.
    // Corre en Play Mode con un guardado temporal: el guardado real no se toca.
    [InitializeOnLoad]
    public static class MenuScreenshots
    {
        private const string FLAG = "GasSafari.MenuScreenshots";
        private const string OUTPUT = "Screenshots/M6";
        private static IEnumerator routine;
        private static readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
        private static readonly StringBuilder Report = new StringBuilder();
        private static int shots;
        private static int overflows;

        static MenuScreenshots()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
            if (SessionState.GetBool(FLAG, false) && EditorApplication.isPlaying)
                Begin();
        }

        [MenuItem("Gas Safari/QA/Capturar pantallas de menú en es y en (M6)")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                SessionState.SetBool(FLAG, true);
                Begin();
                return;
            }
            EditorSceneManager.OpenScene(MenuSceneBuilder.SCENE_PATH);
            SetGameViewSize(1920, 1080);
            SessionState.SetBool(FLAG, true);
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayMode(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(FLAG, false))
                Begin();
        }

        private static void Begin()
        {
            if (routine != null)
                return;
            Report.Clear();
            shots = 0;
            overflows = 0;
            routine = Capture();
            stack.Clear();
            stack.Push(routine);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (routine == null || !EditorApplication.isPlaying)
            {
                EditorApplication.update -= Tick;
                return;
            }
            try
            {
                // Pila de iteradores: cada "yield return" de otro iterador lo ejecuta por dentro; null cede un cuadro.
                while (stack.Count > 0)
                {
                    var top = stack.Peek();
                    if (!top.MoveNext())
                    {
                        stack.Pop();
                        continue;
                    }
                    if (top.Current is IEnumerator inner)
                    {
                        stack.Push(inner);
                        continue;
                    }
                    return;
                }
                Finish();
            }
            catch (Exception exception)
            {
                Debug.LogError("MenuScreenshots: " + exception);
                Finish();
            }
        }

        private static void Finish()
        {
            routine = null;
            EditorApplication.update -= Tick;
            SessionState.SetBool(FLAG, false);
            Directory.CreateDirectory(OUTPUT);
            File.WriteAllText(OUTPUT + "/report.txt", Report.ToString(), Encoding.UTF8);
            Debug.Log($"M6-SHOTS {shots} capturas, {overflows} textos fuera de su caja (ver {OUTPUT}/report.txt)");
            EditorApplication.isPlaying = false;
        }

        // Espera cuadros de juego (Time.frameCount), no vueltas del editor: las capturas y las animaciones van por cuadro.
        private static IEnumerator Frames(int count)
        {
            int target = Time.frameCount + count;
            while (Time.frameCount < target)
                yield return null;
        }

        // ---------------------------------------------------------------- recorrido
        private static IEnumerator Capture()
        {
            // Esperar a los servicios y a los menús.
            for (int i = 0; i < 600 && (MenuApp.Current == null || !Services.IsReady || Services.Progression == null); i++)
                yield return null;
            var app = MenuApp.Current;
            if (app == null)
            {
                Debug.LogError("MenuScreenshots: no hay MenuApp (¿está abierta la escena 01_Title?)");
                yield break;
            }
            yield return Frames(30);

            PrepareProfile();
            foreach (var language in new[] { Localization.SPANISH, Localization.ENGLISH })
            {
                foreach (float scale in new[] { 1f, 1.5f })
                {
                    Localization.Set(language);
                    LocalizedText.SetTextScale(scale);
                    string folder = OUTPUT + "/" + language + (scale > 1f ? "_150" : string.Empty);
                    if (Directory.Exists(folder))
                    {
                        foreach (var old in Directory.GetFiles(folder, "*.png"))
                            File.Delete(old);
                    }
                    Directory.CreateDirectory(folder);
                    Report.AppendLine($"== {language} {scale * 100:0}% ==");
                    int index = 0;
                    var steps = Steps(app);
                    while (steps.MoveNext())
                    {
                        if (steps.Current is string name)
                        {
                            yield return Frames(name.StartsWith("shop") || name.StartsWith("06") ? 12 : 4);
                            yield return Shoot(app, folder, ++index, name);
                        }
                        else if (steps.Current is IEnumerator inner)
                        {
                            yield return inner;
                        }
                    }
                }
            }
            LocalizedText.SetTextScale(1f);
            Localization.Set(Localization.SPANISH);
            SceneLoader.HidePreview();
            RestoreProfile();
        }

        // Cada paso prepara una pantalla y devuelve su nombre para la captura.
        private static IEnumerator Steps(MenuApp app)
        {
            var catalog = GameCatalog.Instance;
            app.GoTo<TitleScreen>();
            yield return Frames(3);
            yield return "01_splash";
            app.Get<TitleScreen>().ShowTitle();
            yield return Frames(2);
            yield return "02_title";
            app.GoTo<MainMenuScreen>();
            yield return "03_main_menu";
            app.Push<ModeSelectScreen>();
            yield return "04_modes";
            app.Push<PlayTypeScreen>();
            yield return "05_play_type";

            app.Push<LobbyScreen>();
            yield return Frames(2);
            var lobby = app.Get<LobbyScreen>();
            lobby.AddBot();
            lobby.AddBot();
            lobby.CycleDifficulty();
            yield return "06_lobby";
            app.Back();
            app.Back();
            app.Back();

            app.Push<WorldMapScreen>();
            yield return "07_world_map";
            var sheet = app.Get<LevelSheetScreen>();
            sheet.Level = catalog.FindLevel("W1-3");
            app.Push<LevelSheetScreen>();
            yield return "08_level_sheet";
            app.Back();
            app.Back();

            app.Push<OptionsScreen>();
            var options = app.Get<OptionsScreen>();
            string[] optionTabs = { "audio", "video", "controls", "access", "game" };
            for (int i = 0; i < optionTabs.Length; i++)
            {
                options.SetTab(i, false);
                yield return "09_options_" + optionTabs[i];
            }
            app.Back();

            app.Push<ShopScreen>();
            var shop = app.Get<ShopScreen>();
            string[] shopTabs = { "hats", "gas", "rafts", "celebrations" };
            for (int i = 0; i < shopTabs.Length; i++)
            {
                shop.SetTab(i);
                if (shop.Cards.Count > 0)
                {
                    foreach (var card in shop.Cards)
                    {
                        if (card.Button.gameObject.activeSelf && (i == 0 ? card.Item.id == "hat_pirate_hat" : true))
                        {
                            shop.Show(card);
                            break;
                        }
                    }
                }
                yield return Frames(20);
                yield return "shop_" + shopTabs[i];
            }
            app.Back();

            app.Push<AchievementsScreen>();
            var achievements = app.Get<AchievementsScreen>();
            achievements.SetTab(false);
            yield return "11_achievements";
            achievements.SetTab(true);
            yield return "12_stats";
            app.Back();

            app.Push<CreditsScreen>();
            yield return Frames(2);
            yield return "13_credits";
            app.Back();

            SceneLoader.ShowPreview("Loading_W3");
            yield return Frames(2);
            yield return "14_loading";
            SceneLoader.HidePreview();
            app.GoTo<MainMenuScreen>();
        }

        private static IEnumerator Shoot(MenuApp app, string folder, int index, string name)
        {
            // Los avisos y la pantalla de carga se miden aparte: se mira toda la jerarquía del canvas.
            var issues = UiAudit.Overflows(app.Canvas.transform);
            var loader = GameObject.Find("SceneLoader");
            if (loader != null && loader.transform.childCount > 0 && name.Contains("loading"))
                issues.AddRange(UiAudit.Overflows(loader.transform));
            foreach (string issue in issues)
            {
                overflows++;
                Report.AppendLine($"  [{name}] {issue}");
            }

            string file = $"{folder}/{name}.png";
            if (File.Exists(file))
                File.Delete(file);
            ScreenCapture.CaptureScreenshot(file);
            shots++;
            // La captura se escribe al final del cuadro: esperar a que el archivo exista.
            int limit = Time.frameCount + 30;
            while (!File.Exists(file) && Time.frameCount < limit)
                yield return null;
            yield return Frames(1);
        }

        // ---------------------------------------------------------------- perfil de prueba
        private static string realSavePath;

        private static void PrepareProfile()
        {
            var progression = Services.Progression;
            realSavePath = SaveSystem.DefaultPath;
            string temp = Path.Combine(Application.temporaryCachePath, "m6shots_save.json");
            if (File.Exists(temp))
                File.Delete(temp);
            progression.Initialize(new SaveSystem(temp));
            var data = progression.Data;
            var catalog = GameCatalog.Instance;

            int[] stars = { 3, 3, 2, 3, 3, 3, 1, 2 };
            int world = 0;
            foreach (var level in catalog.AllLevels())
            {
                if (level.world == null)
                    continue;
                if (world >= stars.Length)
                    break;
                var record = data.GetOrAddLevel(level.id);
                record.stars = stars[world];
                record.best = level.goldTime + world * 3f;
                record.mangos = world % 3 == 0 ? 7 : 3;
                world++;
            }
            data.frijolitos = 340;
            data.cosmetics.owned.AddRange(new[] { "hat_pirate_hat", "gas_neon_pink", "raft_lily_pad", "celebrate_applause" });
            data.cosmetics.equipped.Add(new SaveData.EquippedRecord { player = "p1", hat = "hat_pirate_hat", gas = "gas_neon_pink", celebration = "celebrate_applause" });
            data.achievements.AddRange(new[] { "first_fart", "duet", "orchestra", "super_fart", "jungle_slingshot", "world1" });
            data.stats.farts = 1520; data.stats.sync2 = 80; data.stats.sync3 = 22; data.stats.sync4 = 9; data.stats.superFarts = 6;
            data.stats.falls = 41; data.stats.rescues = 12; data.stats.sinks = 7; data.stats.meters = 18234; data.stats.playSeconds = 9021;
            data.stats.characters.Add(new SaveData.CharacterUse { id = "Gloria", count = 9 });

            // Dos jugadores en la sala, sin dispositivo (como en las pruebas).
            var session = Services.Session;
            session.ClearPlayers();
            var palette = catalog.playerPalette;
            session.AddPlayer(new PlayerSlot(0, palette.Get(0), null, null) { Character = catalog.FindCharacter("Alex") });
            session.AddPlayer(new PlayerSlot(1, palette.Get(1), null, null) { Character = catalog.FindCharacter("Marty") });
            progression.Save();
        }

        private static void RestoreProfile()
        {
            if (Services.Progression != null && realSavePath != null)
                Services.Progression.Initialize(new SaveSystem(realSavePath));
            if (Services.Session != null)
                Services.Session.ClearPlayers();
        }

        // ---------------------------------------------------------------- tamaño de la vista Game
        private static void SetGameViewSize(int width, int height)
        {
            try
            {
                var editorAssembly = typeof(UnityEditor.Editor).Assembly;
                var sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
                var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var instance = singleton.GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                var group = sizesType.GetMethod("GetGroup").Invoke(instance, new object[] { 0 });
                var groupType = group.GetType();
                int total = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
                int found = -1;
                for (int i = 0; i < total; i++)
                {
                    var size = groupType.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                    int w = (int)size.GetType().GetProperty("width").GetValue(size);
                    int h = (int)size.GetType().GetProperty("height").GetValue(size);
                    string label = (string)size.GetType().GetProperty("baseText").GetValue(size);
                    if (w == width && h == height && label == "GasSafari 1080p")
                    {
                        found = i;
                        break;
                    }
                }
                if (found < 0)
                {
                    var sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
                    var typeEnum = editorAssembly.GetType("UnityEditor.GameViewSizeType");
                    var ctor = sizeType.GetConstructor(new[] { typeEnum, typeof(int), typeof(int), typeof(string) });
                    var newSize = ctor.Invoke(new object[] { Enum.ToObject(typeEnum, 1), width, height, "GasSafari 1080p" });
                    groupType.GetMethod("AddCustomSize").Invoke(group, new[] { newSize });
                    found = total;
                }
                var gameViewType = editorAssembly.GetType("UnityEditor.GameView");
                var window = EditorWindow.GetWindow(gameViewType);
                gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(window, found);
                window.Repaint();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("MenuScreenshots: no pude fijar la vista Game a 1920x1080 (" + exception.Message + ")");
            }
        }
    }
}
