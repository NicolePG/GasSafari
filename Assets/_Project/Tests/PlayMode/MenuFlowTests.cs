using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Progression;
using GasSafari.Save;
using GasSafari.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GasSafari.Tests
{
    // Base de las pruebas de menús: servicios reales, un guardado temporal y un MenuApp nuevo en cada prueba.
    public abstract class MenuTestBase
    {
        protected MenuApp App;
        protected GameCatalog Catalog;
        private string savePath;
        private DifficultyData originalDifficulty;
        private int originalFrameRate, originalVSync, originalQuality;
        private FullScreenMode originalFullScreen;

        protected SaveData Data => Services.Progression.Data;

        [UnitySetUp]
        public IEnumerator SetUpMenu()
        {
            Time.timeScale = 1f;
            Catalog = GameCatalog.Instance;
            Assert.IsNotNull(Catalog, "falta Resources/GameCatalog.asset");
            savePath = Path.Combine(Application.temporaryCachePath, "menu_test_" + Guid.NewGuid().ToString("N") + ".json");
            Services.Progression.Initialize(new SaveSystem(savePath));
            Services.Session.ClearPlayers();
            originalDifficulty = Services.Session.Difficulty;
            originalFrameRate = Application.targetFrameRate;
            originalVSync = QualitySettings.vSyncCount;
            originalQuality = QualitySettings.GetQualityLevel();
            originalFullScreen = Screen.fullScreenMode;
            Services.Session.Difficulty = Catalog.FindDifficulty("Normal");
            Localization.Set(Localization.SPANISH);
            LocalizedText.SetTextScale(1f);
            MenuRequest.StartAt = MenuRequest.Start.MainMenu;
            MenuRequest.FocusLevel = null;
            App = new GameObject("MenuApp").AddComponent<MenuApp>();
            yield return null;
            yield return null;
            App.GoTo<MainMenuScreen>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDownMenu()
        {
            if (App != null)
                Object.Destroy(App.gameObject);
            Services.Session.ClearPlayers();
            Services.Session.Difficulty = originalDifficulty;
            RestoreVideo();
            // Vuelve al guardado real (solo lo lee) y a sus opciones.
            Services.Progression.Initialize(new SaveSystem(SaveSystem.DefaultPath));
            LocalizedText.SetTextScale(1f);
            Localization.Set(Localization.SPANISH);
            if (File.Exists(savePath))
                File.Delete(savePath);
            yield return null;
        }

        // GameSettings.Apply toca el video (límite de FPS, VSync, calidad): las demás pruebas esperan lo de antes.
        private void RestoreVideo()
        {
            Application.targetFrameRate = originalFrameRate;
            QualitySettings.vSyncCount = originalVSync;
            if (QualitySettings.GetQualityLevel() != originalQuality)
                QualitySettings.SetQualityLevel(originalQuality, true);
            QualitySettings.vSyncCount = originalVSync;
            Screen.fullScreenMode = originalFullScreen;
        }

        protected void Give(string levelId, int stars, int mangoMask = 0, float best = 0f)
        {
            var record = Data.GetOrAddLevel(levelId);
            record.stars = stars;
            record.mangos = mangoMask;
            record.best = best;
        }

        // Estrellas de sobra para abrir el mundo 2 (8) y el 3 (20).
        protected void GiveStars(int total)
        {
            int left = total;
            foreach (var level in Catalog.AllLevels())
            {
                if (level.world == null || left <= 0)
                    continue;
                int stars = Mathf.Min(3, left);
                Give(level.id, stars);
                left -= stars;
            }
        }

        protected static string LabelOf(Selectable selectable)
        {
            var text = selectable.GetComponentInChildren<TMP_Text>(true);
            return text != null ? text.text : string.Empty;
        }

        protected static void Move(Selectable selectable, MoveDirection direction)
        {
            var vector = direction == MoveDirection.Left ? Vector2.left : direction == MoveDirection.Right ? Vector2.right
                : direction == MoveDirection.Up ? Vector2.up : Vector2.down;
            var data = new AxisEventData(EventSystem.current) { moveDir = direction, moveVector = vector };
            ExecuteEvents.Execute(selectable.gameObject, data, ExecuteEvents.moveHandler);
        }

        protected static void Submit(Selectable selectable) =>
            ExecuteEvents.Execute(selectable.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);

        protected static void Cancel(Selectable selectable) =>
            ExecuteEvents.ExecuteHierarchy(selectable.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.cancelHandler);

        // Todo lo que se puede enfocar en la pantalla, y lo que se alcanza con flechas/cruceta desde el primer elemento.
        protected static HashSet<Selectable> Reachable(Selectable start)
        {
            var seen = new HashSet<Selectable>();
            var queue = new Queue<Selectable>();
            queue.Enqueue(start);
            seen.Add(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var next in new[] { current.FindSelectableOnUp(), current.FindSelectableOnDown(), current.FindSelectableOnLeft(), current.FindSelectableOnRight() })
                {
                    if (next != null && seen.Add(next))
                        queue.Enqueue(next);
                }
            }
            return seen;
        }

        protected static List<Selectable> Focusable(Transform root) =>
            root.GetComponentsInChildren<Selectable>(false).Where(s => s.gameObject.activeInHierarchy && s.IsInteractable()
                && s.navigation.mode != Navigation.Mode.None).ToList();

        protected void AssertAllReachable(MenuScreen screen, string label)
        {
            var first = screen.FirstSelected;
            Assert.IsNotNull(first, label + ": sin primer elemento enfocable");
            var reachable = Reachable(first);
            foreach (var selectable in Focusable(screen.transform))
                Assert.IsTrue(reachable.Contains(selectable), label + ": no se llega con mando o teclado a " + selectable.name);
        }
    }

    public class MenuFlowTests : MenuTestBase
    {
        // ---------------------------------------------------------------- título y menú principal
        [UnityTest]
        public IEnumerator Title_SplashThenTitle_ThenMainMenuOnAnyButton()
        {
            App.GoTo<TitleScreen>();
            var title = App.Get<TitleScreen>();
            yield return null;
            Assert.IsTrue(title.IsSplash);
            yield return new WaitForSecondsRealtime(TitleScreen.SPLASH_TIME + 0.4f);
            Assert.IsTrue(title.IsTitle, "tras el splash se ve la pantalla de título");
            Assert.AreEqual("Pulsa cualquier botón", title.GetComponentsInChildren<TMP_Text>().First(t => t.text.StartsWith("Pulsa")).text);
            Localization.Set(Localization.ENGLISH);
            Assert.IsTrue(title.GetComponentsInChildren<TMP_Text>().Any(t => t.text == "Press any button"));
        }

        [UnityTest]
        public IEnumerator MainMenu_HasTheSixButtons_AndEachOpensItsScreen()
        {
            var menu = App.Get<MainMenuScreen>();
            Assert.AreEqual(new[] { "Jugar", "Tienda", "Logros", "Opciones", "Créditos", "Salir" },
                new[] { menu.PlayButton, menu.ShopButton, menu.AchievementsButton, menu.OptionsButton, menu.CreditsButton, menu.QuitButton }.Select(LabelOf).ToArray());
            var expected = new (Button button, Type screen)[]
            {
                (menu.PlayButton, typeof(ModeSelectScreen)), (menu.ShopButton, typeof(ShopScreen)),
                (menu.AchievementsButton, typeof(AchievementsScreen)), (menu.OptionsButton, typeof(OptionsScreen)), (menu.CreditsButton, typeof(CreditsScreen))
            };
            foreach (var (button, screen) in expected)
            {
                button.onClick.Invoke();
                Assert.AreEqual(screen, App.Top.GetType());
                App.Top.HandleCancel();
                yield return null;
                Assert.AreSame(menu, App.Top, "Cancelar vuelve al menú principal desde " + screen.Name);
            }
            menu.QuitButton.onClick.Invoke();
            Assert.IsTrue(App.IsConfirmOpen, "Salir pide confirmación");
            App.CloseConfirm();
            Assert.IsFalse(App.IsConfirmOpen);
            Assert.IsNotNull(menu.FirstSelected);
            Assert.AreSame(menu.PlayButton, menu.FirstSelected);
        }

        [UnityTest]
        public IEnumerator MainMenu_Cancel_AsksBeforeQuitting_AndTheFocusStartsOnNo()
        {
            var menu = App.Get<MainMenuScreen>();
            Assert.IsTrue(menu.HandleCancel());
            yield return null;
            yield return null;
            Assert.IsTrue(App.IsConfirmOpen);
            Assert.AreEqual("No", LabelOf(EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>()), "el foco empieza en No");
            Cancel(EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>());
            Assert.IsFalse(App.IsConfirmOpen, "Cancelar cierra el diálogo");
        }

        // ---------------------------------------------------------------- elegir modo
        [UnityTest]
        public IEnumerator ModeSelect_ShowsEightCards_AndLocksThemByProgress()
        {
            App.Push<ModeSelectScreen>();
            var modes = App.Get<ModeSelectScreen>();
            yield return null;
            Assert.AreEqual(8, modes.CardButtons.Count);

            // Solo Historia está abierto al empezar.
            var buttons = modes.CardButtons;
            string[] names = buttons.Select(b => b.GetComponentsInChildren<TMP_Text>(true).First().text).ToArray();
            CollectionAssert.Contains(names.Select(n => n.Trim()), "Historia");

            // Un modo bloqueado dice qué falta.
            buttons[1].onClick.Invoke();
            Assert.IsTrue(App.IsToastVisible);
            Assert.AreEqual("Termina un nivel", App.ToastText);
            buttons[2].onClick.Invoke();
            Assert.AreEqual("Termina W5-5", App.ToastText);
            buttons[3].onClick.Invoke();
            Assert.AreEqual("30 estrellas", App.ToastText);
            buttons[5].onClick.Invoke();
            Assert.AreEqual("Termina W2-5", App.ToastText);

            // Con W1-5 terminado, Fiesta se abre, pero su escena llega con M7.
            Give("W1-5", 1);
            App.Back();
            App.Push<ModeSelectScreen>();
            buttons[4].onClick.Invoke();
            Assert.AreEqual("Próximamente", App.ToastText);
            Assert.AreSame(modes, App.Top);

            // Historia sigue a "Local / online".
            buttons[0].onClick.Invoke();
            Assert.IsInstanceOf<PlayTypeScreen>(App.Top);
            Assert.AreEqual(ProgressionRules.MODE_STORY, Services.Session.ModeId);
        }

        [UnityTest]
        public IEnumerator ModeRequirements_FollowTheDocs_InBothLanguages()
        {
            foreach (var language in new[] { Localization.SPANISH, Localization.ENGLISH })
            {
                Localization.Set(language);
                var table = LocalizedText.Table;
                string Text((string key, object[] args) r) => string.Format(table.Get(r.key), r.args);
                Assert.AreEqual(language == "es" ? "Termina W5-5" : "Finish W5-5", Text(ModeSelectScreen.ModeRequirement(ProgressionRules.MODE_MIRROR)));
                Assert.AreEqual(language == "es" ? "30 estrellas" : "30 stars", Text(ModeSelectScreen.ModeRequirement(ProgressionRules.MODE_MODIFIERS)));
                Assert.AreEqual(language == "es" ? "Termina W1-5" : "Finish W1-5", Text(ModeSelectScreen.ModeRequirement(ProgressionRules.MODE_PARTY)));
                Assert.AreEqual(language == "es" ? "Termina W2-5" : "Finish W2-5", Text(ModeSelectScreen.ModeRequirement(ProgressionRules.MODE_DAILY)));
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayType_LocalOpensTheLobby_OnlineIsComingSoon()
        {
            App.Push<PlayTypeScreen>();
            var type = App.Get<PlayTypeScreen>();
            yield return null;
            type.CreateButton.onClick.Invoke();
            Assert.AreEqual("Próximamente", App.ToastText);
            type.JoinButton.onClick.Invoke();
            Assert.IsInstanceOf<PlayTypeScreen>(App.Top);
            type.LocalButton.onClick.Invoke();
            Assert.IsInstanceOf<LobbyScreen>(App.Top);
        }

        // ---------------------------------------------------------------- selección de personajes
        private static PlayerSlot Human(int index, string character)
        {
            var catalog = GameCatalog.Instance;
            return new PlayerSlot(index, catalog.playerPalette.Get(index), null, null) { Character = character != null ? catalog.FindCharacter(character) : null };
        }

        [UnityTest]
        public IEnumerator Lobby_BotsFillFreeColumns_UpToFour_WithDifferentAnimals()
        {
            App.Push<LobbyScreen>();
            var lobby = App.Get<LobbyScreen>();
            yield return null;
            Assert.AreEqual(0, lobby.HumanCount);
            Assert.IsFalse(lobby.RemoveBotButton.interactable);
            for (int i = 0; i < 4; i++)
                Assert.IsTrue(lobby.AddBot());
            Assert.IsFalse(lobby.AddBot(), "no caben más de 4");
            Assert.AreEqual(4, lobby.BotCount);
            Assert.IsFalse(lobby.AddBotButton.interactable);
            Assert.AreEqual(4, lobby.Columns.Select(c => c.Character.id).Distinct().Count(), "cada animal una sola vez");
            Assert.IsTrue(Services.Session.Players.All(p => p.IsBot));
            Assert.IsTrue(lobby.RemoveBot());
            Assert.AreEqual(3, lobby.BotCount);
            Assert.IsTrue(lobby.AddBotButton.interactable);
        }

        [UnityTest]
        public IEnumerator Lobby_HumansGetTheFirstFreeAnimal_AndCyclingSkipsTakenOnes()
        {
            Services.Session.AddPlayer(Human(0, "Alex"));
            Services.Session.AddPlayer(Human(1, null));
            App.Push<LobbyScreen>();
            var lobby = App.Get<LobbyScreen>();
            yield return null;
            Assert.AreEqual("Alex", lobby.Columns[0].Character.id);
            Assert.AreEqual("Marty", lobby.Columns[1].Character.id, "el primero libre");
            lobby.CycleCharacter(lobby.Columns[0], 1);
            Assert.AreEqual("Melman", lobby.Columns[0].Character.id, "Marty ya está elegido");
            lobby.CycleCharacter(lobby.Columns[0], -1);
            Assert.AreEqual("Alex", lobby.Columns[0].Character.id);
            lobby.CycleCharacter(lobby.Columns[1], -1);
            Assert.AreEqual("Julien", lobby.Columns[1].Character.id, "da la vuelta al carrusel");
            Assert.AreEqual(lobby.Columns[1].Character, Services.Session.Players[1].Character, "el lugar de la partida sigue a la columna");
        }

        [UnityTest]
        public IEnumerator Lobby_WhenEveryHumanIsReady_GoesToTheWorldMap_WithTheirAnimalsAndCosmetics()
        {
            Services.Session.AddPlayer(Human(0, "Gloria"));
            Services.Session.AddPlayer(Human(1, "Melman"));
            App.Push<LobbyScreen>();
            var lobby = App.Get<LobbyScreen>();
            yield return null;
            lobby.AddBot();
            lobby.ToggleReady(lobby.Columns[0]);
            yield return null;
            Assert.IsFalse(lobby.IsStarting, "falta J2");
            lobby.ToggleReady(lobby.Columns[1]);
            Assert.IsTrue(lobby.IsStarting);
            lobby.ToggleReady(lobby.Columns[1]);
            Assert.IsFalse(lobby.IsStarting, "quitar el Listo cancela el inicio");
            lobby.ToggleReady(lobby.Columns[1]);
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.IsInstanceOf<WorldMapScreen>(App.Top);
            Assert.AreEqual(3, Services.Session.Players.Count, "los bots se quedan para el nivel");
            Assert.AreEqual("Gloria", Services.Session.Players[0].Character.id);
            Assert.AreEqual("hat_zoo_cap", Services.Session.Players[0].Hat);
            Assert.AreEqual("gas_classic", Services.Session.Players[0].Gas);
            Assert.IsTrue(Services.Session.Players[2].IsBot);
            Assert.IsNotNull(Services.Session.Players[2].Character);
        }

        [UnityTest]
        public IEnumerator Lobby_Back_DropsTheBots_AndDifficultyCyclesAndPersists()
        {
            App.Push<LobbyScreen>();
            var lobby = App.Get<LobbyScreen>();
            yield return null;
            lobby.AddBot();
            Assert.AreEqual("Normal", Services.Session.Difficulty.id);
            lobby.CycleDifficulty();
            Assert.AreEqual("AmistadesRotas", Services.Session.Difficulty.id);
            Assert.AreEqual("AmistadesRotas", Data.settings.difficulty);
            StringAssert.Contains("Amistades Rotas", LabelOf(lobby.DifficultyButton));
            lobby.CycleDifficulty();
            Assert.AreEqual("Familiar", Services.Session.Difficulty.id);
            Localization.Set(Localization.ENGLISH);
            StringAssert.Contains("Family", lobby.DifficultyButton.GetComponentInChildren<TMP_Text>().text);
            lobby.BackButton.onClick.Invoke();
            yield return null;
            Assert.IsInstanceOf<MainMenuScreen>(App.Top);
            Assert.AreEqual(0, Services.Session.Players.Count(p => p.IsBot), "los bots no sobreviven a salir de la sala");
        }

        [UnityTest]
        public IEnumerator Lobby_HatAndGasRows_CycleThroughWhatThePlayerOwns()
        {
            Data.cosmetics.owned.AddRange(new[] { "hat_pirate_hat", "gas_neon_pink" });
            Services.Session.AddPlayer(Human(0, "Alex"));
            App.Push<LobbyScreen>();
            var lobby = App.Get<LobbyScreen>();
            yield return null;
            var column = lobby.Columns[0];
            Assert.AreEqual("hat_zoo_cap", Services.Cosmetics.Equipped(0, CosmeticCategory.Hat));
            lobby.CycleCosmetic(column, CosmeticCategory.Hat, 1, true);
            Assert.AreEqual("hat_pirate_hat", Services.Cosmetics.Equipped(0, CosmeticCategory.Hat));
            lobby.CycleCosmetic(column, CosmeticCategory.Hat, 1, true);
            Assert.AreEqual(string.Empty, Services.Cosmetics.Equipped(0, CosmeticCategory.Hat), "sin sombrero");
            lobby.CycleCosmetic(column, CosmeticCategory.Gas, 1, false);
            Assert.AreEqual("gas_neon_pink", Services.Cosmetics.Equipped(0, CosmeticCategory.Gas));
            lobby.CycleCosmetic(column, CosmeticCategory.Gas, 1, false);
            Assert.AreEqual("gas_classic", Services.Cosmetics.Equipped(0, CosmeticCategory.Gas), "solo lo que se tiene, con vuelta");
        }

        // ---------------------------------------------------------------- mapa y ficha
        [UnityTest]
        public IEnumerator WorldMap_Shows31Nodes_AndLocksWorldsByStarsAndMangos()
        {
            App.Push<WorldMapScreen>();
            var map = App.Get<WorldMapScreen>();
            yield return null;
            Assert.AreEqual(31, map.Nodes.Count);
            bool Locked(string levelId) => map.Nodes.First(n => n.Level.id == levelId).Lock.gameObject.activeSelf;
            Assert.IsFalse(Locked("W1-1"));
            Assert.IsFalse(Locked("T-0"));
            Assert.IsTrue(Locked("W2-1"), "el mundo 2 pide 8 estrellas");

            GiveStars(8);
            App.Back();
            App.Push<WorldMapScreen>();
            Assert.IsFalse(Locked("W2-1"));
            Assert.IsTrue(Locked("W3-1"), "el mundo 3 pide 20");
            GiveStars(45);
            App.Back();
            App.Push<WorldMapScreen>();
            Assert.IsFalse(Locked("W5-1"));
            Assert.IsTrue(Locked("W6-1"), "el mundo 6 pide 60 mangos dorados, no estrellas");

            int mask = 7;
            foreach (var level in Catalog.AllLevels().Where(l => l.world != null).Take(21))
                Give(level.id, 3, mask);
            Assert.AreEqual(63, ProgressionRules.TotalMangos(Data));
            App.Back();
            App.Push<WorldMapScreen>();
            Assert.IsFalse(Locked("W6-1"));
        }

        [UnityTest]
        public IEnumerator WorldMap_LockedNode_SaysWhatIsMissing_AndUnlockedOpensTheSheet()
        {
            App.Push<WorldMapScreen>();
            var map = App.Get<WorldMapScreen>();
            yield return null;
            map.Nodes.First(n => n.Level.id == "W3-2").Button.onClick.Invoke();
            Assert.AreEqual("Necesitas 20 estrellas", App.ToastText);
            Assert.AreSame(map, App.Top);
            map.Nodes.First(n => n.Level.id == "W6-3").Button.onClick.Invoke();
            Assert.AreEqual("Necesitas 60 mangos dorados", App.ToastText);
            Localization.Set(Localization.ENGLISH);
            map.Nodes.First(n => n.Level.id == "W4-1").Button.onClick.Invoke();
            Assert.AreEqual("Needs 32 stars", App.ToastText);

            map.Nodes.First(n => n.Level.id == "W1-4").Button.onClick.Invoke();
            Assert.IsInstanceOf<LevelSheetScreen>(App.Top);
            Assert.AreEqual("W1-4", ((LevelSheetScreen)App.Top).Level.id);
            App.Top.HandleCancel();
            Assert.AreSame(map, App.Top, "Cancelar en la ficha vuelve al mapa");
        }

        [UnityTest]
        public IEnumerator WorldMap_FocusStartsOnTheNextPendingLevel_OrTheOneYouJustPlayed()
        {
            Give("W1-1", 3);
            Give("W1-2", 2);
            Give("T-0", 1);
            App.Push<WorldMapScreen>();
            var map = App.Get<WorldMapScreen>();
            yield return null;
            Assert.AreSame(map.Nodes.First(n => n.Level.id == "T-0").Button.GetType(), map.FirstSelected.GetType());
            Assert.AreEqual("Level_W1-3", map.FirstSelected.name, "el primer nivel sin terminar de un mundo abierto");
            MenuRequest.FocusLevel = Catalog.FindLevel("W1-1");
            Assert.AreEqual("Level_W1-1", map.FirstSelected.name, "al volver de un nivel el foco queda en él");
            MenuRequest.FocusLevel = null;
            var node = map.Nodes.First(n => n.Level.id == "W1-1");
            Assert.AreEqual("Icon_Star_Full", node.Stars[2].sprite.name);
            node = map.Nodes.First(n => n.Level.id == "W1-2");
            Assert.AreEqual("Icon_Star_Full", node.Stars[1].sprite.name);
            Assert.AreEqual("Icon_Star_Empty", node.Stars[2].sprite.name);
        }

        [UnityTest]
        public IEnumerator LevelSheet_ShowsTimesBestMangosAndStars_AndLocksTheExtraModes()
        {
            Give("W2-1", 2, 5, 123.4f);
            var sheet = App.Get<LevelSheetScreen>();
            var level = Catalog.FindLevel("W2-1");
            sheet.Level = level;
            App.Push<LevelSheetScreen>();
            yield return null;
            Assert.AreEqual("Cajas a la Deriva", sheet.LevelNameText);
            Assert.AreEqual(LevelSheetScreen.FormatTime(level.goldTime), sheet.GoldText);
            Assert.AreEqual(LevelSheetScreen.FormatTime(level.silverTime), sheet.SilverText);
            Assert.AreEqual("2:04", sheet.BestTimeText);
            Assert.AreEqual("Mangos 2/3", sheet.MangosLabelText);
            Assert.IsTrue(sheet.TimesVisible);

            // Contrarreloj se abre al terminar un nivel; Modificadores pide 30 estrellas; ninguno tiene escena todavía.
            sheet.TimeTrialButton.onClick.Invoke();
            Assert.AreEqual("Próximamente", App.ToastText);
            sheet.ModifiersButton.onClick.Invoke();
            Assert.AreEqual("30 estrellas", App.ToastText);

            Localization.Set(Localization.ENGLISH);
            Assert.AreEqual("Best time", sheet.GetComponentsInChildren<TMP_Text>().First(t => t.text == "Best time").text);

            // Sin tiempo guardado.
            App.Back();
            sheet.Level = Catalog.FindLevel("W2-2");
            App.Push<LevelSheetScreen>();
            Assert.AreEqual("--:--", sheet.BestTimeText);
            Assert.AreEqual("Mangos 0/3", sheet.MangosLabelText);
        }

        [UnityTest]
        public IEnumerator LevelSheet_Tutorial_HasNoTimesOrExtraModes()
        {
            var sheet = App.Get<LevelSheetScreen>();
            sheet.Level = Catalog.tutorial;
            App.Push<LevelSheetScreen>();
            yield return null;
            Assert.IsFalse(sheet.TimesVisible);
            Assert.IsFalse(sheet.TimeTrialButton.gameObject.activeSelf);
            Assert.IsFalse(sheet.ModifiersButton.gameObject.activeSelf);
            Assert.AreEqual("Piscina del Zoológico", sheet.LevelNameText);
        }

        // ---------------------------------------------------------------- pantalla de carga
        [UnityTest]
        public IEnumerator Loading_ShowsATipInTheActiveLanguage()
        {
            SceneLoader.ShowPreview("Loading_W2");
            yield return null;
            Assert.AreEqual("Empujar juntos vale más que empujar fuerte.", SceneLoader.TipText);
            Localization.Set(Localization.ENGLISH);
            Assert.AreEqual("Pushing together beats pushing hard.", SceneLoader.TipText);
            SceneLoader.HidePreview();
            SceneLoader.Dispose();
            yield return null;
        }

        // ---------------------------------------------------------------- mando y teclado
        [UnityTest]
        public IEnumerator EveryScreen_CanBeFullyNavigatedWithoutAMouse()
        {
            GiveStars(30);
            var screens = new List<(string name, Action open, Func<MenuScreen> screen)>
            {
                ("menú principal", () => App.GoTo<MainMenuScreen>(), () => App.Get<MainMenuScreen>()),
                ("modos", () => App.Push<ModeSelectScreen>(), () => App.Get<ModeSelectScreen>()),
                ("local/online", () => App.Push<PlayTypeScreen>(), () => App.Get<PlayTypeScreen>()),
                ("mapa", () => App.Push<WorldMapScreen>(), () => App.Get<WorldMapScreen>()),
                ("opciones", () => App.Push<OptionsScreen>(), () => App.Get<OptionsScreen>()),
                ("tienda", () => App.Push<ShopScreen>(), () => App.Get<ShopScreen>()),
                ("logros", () => App.Push<AchievementsScreen>(), () => App.Get<AchievementsScreen>()),
                ("créditos", () => App.Push<CreditsScreen>(), () => App.Get<CreditsScreen>()),
            };
            foreach (var (name, open, screen) in screens)
            {
                App.GoTo<MainMenuScreen>();
                open();
                yield return null;
                yield return null;
                AssertAllReachable(screen(), name);
            }

            // La ficha del nivel.
            App.GoTo<MainMenuScreen>();
            App.Get<LevelSheetScreen>().Level = Catalog.FindLevel("W1-2");
            App.Push<LevelSheetScreen>();
            yield return null;
            AssertAllReachable(App.Top, "ficha del nivel");

            // Cada pestaña de Opciones, la tienda y los logros.
            App.GoTo<MainMenuScreen>();
            var options = App.Push<OptionsScreen>();
            for (int tab = 0; tab < 5; tab++)
            {
                options.SetTab(tab, false);
                yield return null;
                AssertAllReachable(options, "opciones, pestaña " + tab);
            }
            App.GoTo<MainMenuScreen>();
            var shop = App.Push<ShopScreen>();
            for (int tab = 0; tab < 4; tab++)
            {
                shop.SetTab(tab);
                yield return null;
                AssertAllReachable(shop, "tienda, pestaña " + tab);
            }
            App.GoTo<MainMenuScreen>();
            var achievements = App.Push<AchievementsScreen>();
            foreach (bool stats in new[] { false, true })
            {
                achievements.SetTab(stats);
                yield return null;
                AssertAllReachable(achievements, stats ? "estadísticas" : "logros");
            }
        }

        [UnityTest]
        public IEnumerator Keyboard_ArrowsEnterAndEscape_DriveTheMenu_ThroughNavigationEvents()
        {
            // Las mismas llamadas que hace InputSystemUIInputModule con flechas/WASD (mover), Enter/Espacio (aceptar) y Esc (cancelar).
            var menu = App.Get<MainMenuScreen>();
            yield return null;
            yield return null;
            var play = menu.PlayButton;
            EventSystem.current.SetSelectedGameObject(play.gameObject);
            Move(play, MoveDirection.Down);
            Assert.AreSame(menu.ShopButton.gameObject, EventSystem.current.currentSelectedGameObject);
            Move(menu.ShopButton, MoveDirection.Down);
            Move(menu.AchievementsButton, MoveDirection.Down);
            Assert.AreSame(menu.OptionsButton.gameObject, EventSystem.current.currentSelectedGameObject);
            Submit(menu.OptionsButton);
            yield return null;
            yield return null;
            Assert.IsInstanceOf<OptionsScreen>(App.Top);
            var options = (OptionsScreen)App.Top;
            Assert.AreSame(options.Tabs[0].gameObject, EventSystem.current.currentSelectedGameObject, "el foco entra por la primera pestaña");
            Move(options.Tabs[0], MoveDirection.Right);
            Assert.AreSame(options.Tabs[1].gameObject, EventSystem.current.currentSelectedGameObject);
            Submit(options.Tabs[1]);
            Assert.AreEqual(OptionsScreen.TAB_VIDEO, options.CurrentTab);
            Move(options.Tabs[1], MoveDirection.Down);
            Assert.AreSame(options.Page(OptionsScreen.TAB_VIDEO)[0].gameObject, EventSystem.current.currentSelectedGameObject);
            Cancel(options.Page(OptionsScreen.TAB_VIDEO)[0]);
            yield return null;
            Assert.AreSame(menu, App.Top, "Esc vuelve atrás");
        }

        [Test]
        public void TheUiModule_ListensToKeyboardAndGamepad_ForMoveSubmitAndCancel()
        {
            var module = EventSystem.current.GetComponent<InputSystemUIInputModule>();
            Assert.IsNotNull(module, "los menús usan InputSystemUIInputModule");
            Assert.IsNotNull(module.actionsAsset);
            string Paths(InputActionReference reference) =>
                string.Join(" ", reference.action.bindings.Select(b => b.effectivePath + (b.isPartOfComposite ? "(" + b.name + ")" : string.Empty)));
            string move = Paths(module.move);
            StringAssert.Contains("<Gamepad>/leftStick", move);
            StringAssert.Contains("<Gamepad>/dpad", move);
            StringAssert.Contains("<Keyboard>/w", move);
            StringAssert.Contains("<Keyboard>/upArrow", move);
            StringAssert.Contains("<Keyboard>/s", move);
            StringAssert.Contains("<Keyboard>/rightArrow", move);
            string submit = Paths(module.submit);
            Assert.IsTrue(submit.Contains("<Gamepad>/buttonSouth") || submit.Contains("{Submit}"), "Aceptar: botón A / Enter (uso Submit): " + submit);
            string cancel = Paths(module.cancel);
            Assert.IsTrue(cancel.Contains("<Gamepad>/buttonEast") || cancel.Contains("{Cancel}"), "Cancelar: botón B / Esc (uso Cancel): " + cancel);
        }

        [UnityTest]
        public IEnumerator EveryScreen_CancelGoesBack_AndTheFocusIsNeverLost()
        {
            var screens = new Action[]
            {
                () => App.Push<ModeSelectScreen>(), () => App.Push<ShopScreen>(), () => App.Push<AchievementsScreen>(),
                () => App.Push<OptionsScreen>(), () => App.Push<CreditsScreen>(), () => App.Push<WorldMapScreen>()
            };
            var menu = App.Get<MainMenuScreen>();
            foreach (var open in screens)
            {
                open();
                yield return null;
                yield return null;
                var focused = EventSystem.current.currentSelectedGameObject;
                Assert.IsNotNull(focused, App.Top.GetType().Name + " abre con un elemento enfocado");
                Assert.IsTrue(focused.transform.IsChildOf(App.Top.transform), App.Top.GetType().Name + ": el foco está dentro de la pantalla");
                Cancel(focused.GetComponent<Selectable>());
                yield return null;
                Assert.AreSame(menu, App.Top);
            }
        }

        // ---------------------------------------------------------------- idioma y tamaño de texto
        [UnityTest]
        public IEnumerator ChangingTheLanguage_RelabelsOpenScreens_WithoutRebuilding()
        {
            var menu = App.Get<MainMenuScreen>();
            yield return null;
            Assert.AreEqual("Jugar", LabelOf(menu.PlayButton));
            Localization.Set(Localization.ENGLISH);
            Assert.AreEqual("Play", LabelOf(menu.PlayButton));
            Assert.AreEqual("Trophies", LabelOf(menu.AchievementsButton), "Logros → Trophies (cabe en el botón)");
            Assert.AreEqual("Quit", LabelOf(menu.QuitButton));
            Localization.Set(Localization.SPANISH);
            Assert.AreEqual("Logros", LabelOf(menu.AchievementsButton));
        }

        [UnityTest]
        public IEnumerator EveryScreen_AtEveryLanguageAndTextSize_HasNoTextOutsideItsBox()
        {
            Give("W1-1", 3, 7, 100f);
            Give("W1-2", 2, 3, 150f);
            GiveStars(10);
            Data.achievements.AddRange(new[] { "first_fart", "duet" });
            Services.Session.AddPlayer(Human(0, "Alex"));
            Services.Session.AddPlayer(Human(1, "Marty"));
            var problems = new List<string>();
            foreach (var language in new[] { Localization.SPANISH, Localization.ENGLISH })
            {
                foreach (float scale in new[] { 1f, 1.25f, 1.5f })
                {
                    Localization.Set(language);
                    LocalizedText.SetTextScale(scale);
                    foreach (var step in VisitScreens())
                    {
                        step();
                        yield return null;
                        yield return null;
                        foreach (string issue in UiAudit.Overflows(App.Canvas.transform))
                            problems.Add($"[{language} {scale * 100:0}% {App.Top.GetType().Name}] {issue}");
                    }
                }
            }
            Localization.Set(Localization.SPANISH);
            LocalizedText.SetTextScale(1f);
            Assert.IsEmpty(problems, "textos fuera de su caja:\n" + string.Join("\n", problems.Distinct().Take(30)));
        }

        private IEnumerable<Action> VisitScreens()
        {
            yield return () => App.GoTo<MainMenuScreen>();
            yield return () => App.Push<ModeSelectScreen>();
            yield return () => App.Push<PlayTypeScreen>();
            yield return () =>
            {
                App.GoTo<MainMenuScreen>();
                App.Push<LobbyScreen>();
                var lobby = App.Get<LobbyScreen>();
                lobby.AddBot();
                lobby.AddBot();
            };
            yield return () => { App.GoTo<MainMenuScreen>(); App.Push<WorldMapScreen>(); };
            yield return () =>
            {
                App.GoTo<MainMenuScreen>();
                App.Get<LevelSheetScreen>().Level = Catalog.FindLevel("W1-3");
                App.Push<LevelSheetScreen>();
            };
            for (int tab = 0; tab < 5; tab++)
            {
                int captured = tab;
                yield return () => { App.GoTo<MainMenuScreen>(); App.Push<OptionsScreen>().SetTab(captured, false); };
            }
            for (int tab = 0; tab < 4; tab++)
            {
                int captured = tab;
                yield return () => { App.GoTo<MainMenuScreen>(); App.Push<ShopScreen>().SetTab(captured); };
            }
            yield return () => { App.GoTo<MainMenuScreen>(); App.Push<AchievementsScreen>().SetTab(false); };
            yield return () => { App.GoTo<MainMenuScreen>(); App.Push<AchievementsScreen>().SetTab(true); };
            yield return () => { App.GoTo<MainMenuScreen>(); App.Push<CreditsScreen>(); };
            yield return () => { App.GoTo<MainMenuScreen>(); App.Confirm("menu.quit_confirm", () => { }); };
            yield return () => { App.CloseConfirm(); App.GoTo<MainMenuScreen>(); App.Toast("map.locked_mangos", 60); };
        }
    }
}
