using System.Collections;
using System.IO;
using System.Linq;
using GasSafari.Core;
using GasSafari.Progression;
using GasSafari.Save;
using GasSafari.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // Opciones (M6-04), tienda (M6-06) y logros/estadísticas (M6-07) vistos desde los menús.
    public class MenuOptionsShopTests : MenuTestBase
    {
        private OptionsScreen OpenOptions()
        {
            return App.Push<OptionsScreen>();
        }

        private static void Step(OptionRow row, MoveDirection direction) => Move(row, direction);

        // ---------------------------------------------------------------- opciones
        [UnityTest]
        public IEnumerator Options_HaveTheFiveTabsOfTheDocs_WithTheirRows()
        {
            var options = OpenOptions();
            yield return null;
            Assert.AreEqual(new[] { "Audio", "Video", "Controles", "Accesibilidad", "Juego" }, options.Tabs.Select(LabelOf).ToArray());
            Assert.AreEqual(5, options.Page(OptionsScreen.TAB_AUDIO).Count);
            Assert.AreEqual(5, options.Page(OptionsScreen.TAB_VIDEO).Count);
            Assert.AreEqual(15, options.Page(OptionsScreen.TAB_CONTROLS).Count, "3 ajustes + dispositivo + 10 acciones + restablecer");
            Assert.AreEqual(6, options.Page(OptionsScreen.TAB_ACCESS).Count);
            Assert.AreEqual(3, options.Page(OptionsScreen.TAB_GAME).Count);
            options.SetTab(OptionsScreen.TAB_ACCESS, false);
            Assert.IsTrue(options.Page(OptionsScreen.TAB_ACCESS).All(r => r.gameObject.activeInHierarchy));
            Assert.IsFalse(options.Page(OptionsScreen.TAB_AUDIO)[0].gameObject.activeInHierarchy);
        }

        [UnityTest]
        public IEnumerator Options_AudioSliders_ApplyAndSave()
        {
            var options = OpenOptions();
            yield return null;
            Assert.AreEqual(0.8f, options.Master.Slider01, 1e-3f);
            Step(options.Master, MoveDirection.Left);
            Assert.AreEqual(0.7f, options.Master.Slider01, 1e-3f);
            Assert.AreEqual(0.7f, Data.settings.volMaster, 1e-3f);
            Assert.AreEqual(0.7f, AudioListener.volume, 1e-3f);
            Step(options.Music, MoveDirection.Left);
            Step(options.Music, MoveDirection.Left);
            Assert.AreEqual(0.6f, GameVolumes.Music, 1e-3f);
            Step(options.VoicesRow, MoveDirection.Right);
            Assert.AreEqual(0.9f, Data.settings.volVoices, 1e-3f);
            Step(options.UiVolume, MoveDirection.Left);
            Assert.AreEqual(0.7f, GameVolumes.Ui, 1e-3f);
            Step(options.Sfx, MoveDirection.Left);
            Assert.AreEqual(0.7f, GameVolumes.For("water_splash"), 1e-3f);
            for (int i = 0; i < 15; i++)
                Step(options.Master, MoveDirection.Left);
            Assert.AreEqual(0f, options.Master.Slider01, "la barra no pasa de 0");
            AudioListener.volume = 1f;
        }

        [UnityTest]
        public IEnumerator Options_Accessibility_TextSizeToggleColorblindHold()
        {
            var options = OpenOptions();
            options.SetTab(OptionsScreen.TAB_ACCESS, false);
            yield return null;
            Step(options.TextSizeRow, MoveDirection.Right);
            Assert.AreEqual(1.25f, Data.settings.textScale);
            Assert.AreEqual(1.25f, LocalizedText.TextScale);
            Step(options.TextSizeRow, MoveDirection.Right);
            Assert.AreEqual(1.5f, LocalizedText.TextScale);
            Step(options.TextSizeRow, MoveDirection.Right);
            Assert.AreEqual(1.5f, LocalizedText.TextScale, "no pasa del 150 %");
            App.Back();
            yield return null;
            var title = App.Get<MainMenuScreen>().PlayButton.GetComponentInChildren<LocalizedText>(true);
            Assert.AreEqual(48f * 1.5f, title.Label.fontSizeMax, 0.1f, "los textos ya construidos crecen");
            OpenOptions().SetTab(OptionsScreen.TAB_ACCESS, false);
            yield return null;

            Step(options.ShakeRow, MoveDirection.Right);
            Assert.IsFalse(Data.settings.shake);
            Assert.IsFalse(Services.Session.ScreenShake);
            Step(options.SlowmoRow, MoveDirection.Right);
            Assert.IsFalse(GameSettings.SlowMotion);
            Step(options.ColorblindRow, MoveDirection.Right);
            Assert.IsTrue(PlayerPalette.UseColorblind);
            Services.Session.AddPlayer(new PlayerSlot(1, Color.white, null, null));
            GameSettings.Apply(Data.settings);
            Assert.AreEqual("#56B4E9", "#" + ColorUtility.ToHtmlStringRGB(Services.Session.Players[0].Color), "J2 con la paleta de daltonismo");
            Step(options.ContrastRow, MoveDirection.Right);
            Assert.IsTrue(GameSettings.HighContrastPings);
            Step(options.HoldModeRow, MoveDirection.Right);
            Assert.IsTrue(GameSettings.ToggleHold);
            Assert.IsTrue(Data.settings.toggleHold);
            Step(options.HoldModeRow, MoveDirection.Left);
            Assert.IsFalse(GameSettings.ToggleHold);
        }

        [UnityTest]
        public IEnumerator Options_ControlsGameAndVideo_ApplyAndSave()
        {
            var options = OpenOptions();
            yield return null;
            options.SetTab(OptionsScreen.TAB_CONTROLS, false);
            Step(options.VibrationRow, MoveDirection.Right);
            Assert.IsFalse(GameSettings.Vibration);
            Step(options.AimSensRow, MoveDirection.Right);
            Assert.AreEqual(GameSettings.SensitivityFromSlider(0.4f), GameSettings.AimSensitivity, 1e-3f);
            Step(options.AimInvertRow, MoveDirection.Right);
            Assert.Less(GameSettings.AimFactor, 0f);

            options.SetTab(OptionsScreen.TAB_VIDEO, false);
            Step(options.VsyncRow, MoveDirection.Right);
            Assert.IsFalse(Data.settings.vsync);
            Step(options.FpsRow, MoveDirection.Right);
            Assert.AreEqual(120, Data.settings.fpsLimit);
            Step(options.FpsRow, MoveDirection.Right);
            Assert.AreEqual(0, Data.settings.fpsLimit, "Sin límite");
            Step(options.QualityRow, MoveDirection.Left);
            Assert.AreEqual(1, Data.settings.quality);
            Step(options.FullscreenRow, MoveDirection.Left);
            Assert.IsFalse(Data.settings.fullscreen);
            Step(options.FullscreenRow, MoveDirection.Right);
            Assert.IsTrue(Data.settings.fullscreen);

            options.SetTab(OptionsScreen.TAB_GAME, false);
            Step(options.DifficultyRow, MoveDirection.Right);
            Assert.AreEqual("AmistadesRotas", Data.settings.difficulty);
            Step(options.TipsRow, MoveDirection.Right);
            Assert.IsFalse(GameSettings.ShowTips);
        }

        [UnityTest]
        public IEnumerator Options_Language_SwitchesEverythingAndSurvivesARestart()
        {
            var options = OpenOptions();
            options.SetTab(OptionsScreen.TAB_GAME, false);
            yield return null;
            Assert.AreEqual("< Español >", options.LanguageRow.transform.Find("Value").GetComponent<TMP_Text>().text);
            Step(options.LanguageRow, MoveDirection.Right);
            Assert.AreEqual(Localization.ENGLISH, Localization.Language);
            Assert.AreEqual("en", Data.settings.language);
            Assert.AreEqual("Options", options.GetComponentsInChildren<TMP_Text>().First(t => t.text == "Options").text);
            Assert.AreEqual("Game", LabelOf(options.Tabs[OptionsScreen.TAB_GAME]));
            Assert.AreEqual("Language", options.Page(OptionsScreen.TAB_GAME)[2].Label.Label.text);

            // Reiniciar el juego = volver a cargar el mismo guardado: el idioma elegido vuelve solo.
            Services.Progression.Save();
            Localization.Set(Localization.SPANISH);
            Services.Progression.Initialize(new SaveSystem(Application.temporaryCachePath + "/options_language_check.json"));
            Assert.AreEqual(Localization.SPANISH, Localization.Language, "un guardado nuevo arranca en español");
            Data.settings.language = "en";
            Services.Progression.Save();
            Services.Progression.Initialize(new SaveSystem(Application.temporaryCachePath + "/options_language_check.json"));
            Assert.AreEqual(Localization.ENGLISH, Localization.Language);
            File.Delete(Application.temporaryCachePath + "/options_language_check.json");
        }

        [UnityTest]
        public IEnumerator Options_Rebinding_ShowsKeysAndGamepadButtons_AndResets()
        {
            var options = OpenOptions();
            options.SetTab(OptionsScreen.TAB_CONTROLS, false);
            yield return null;
            string ValueOf(int index) => options.Page(OptionsScreen.TAB_CONTROLS)[index].transform.Find("Value").GetComponent<TMP_Text>().text;
            // Filas: 0 vibración, 1 sensibilidad, 2 invertir, 3 dispositivo, 4 Pedo, 5 Aguantar, 6 Habilidad, 7 Agarrarse, 8 Súper Pedo...
            Assert.AreEqual("Space", ValueOf(4));
            Assert.AreEqual("Shift", ValueOf(5));
            Assert.AreEqual("F", ValueOf(6));
            Step(options.Page(OptionsScreen.TAB_CONTROLS)[3], MoveDirection.Right);
            Assert.AreNotEqual("Space", ValueOf(4), "el mando tiene otro botón");
            Assert.AreNotEqual("-", ValueOf(4));
            var move = options.Page(OptionsScreen.TAB_CONTROLS)[11];
            Assert.IsFalse(move.interactable, "la palanca no se remapea");
            Step(options.Page(OptionsScreen.TAB_CONTROLS)[3], MoveDirection.Left);
            Assert.IsTrue(move.interactable, "las teclas de WASD sí");

            Data.settings.bindingOverrides = "{\"bindings\":[]}";
            options.ResetBindings();
            Assert.AreEqual(string.Empty, Data.settings.bindingOverrides);
            Assert.IsFalse(options.IsRebinding);
        }

        [UnityTest]
        public IEnumerator Options_OverlayOnALevel_OpensOnTheRequestedTab_AndClosesWithCancel()
        {
            Assert.IsFalse(OptionsOverlay.IsOpen);
            OptionsOverlay.Open(OptionsScreen.TAB_CONTROLS);
            yield return null;
            yield return null;
            Assert.IsTrue(OptionsOverlay.IsOpen);
            var options = Object.FindObjectsByType<OptionsScreen>(FindObjectsSortMode.None).First(o => o.App.IsOverlay);
            Assert.AreEqual(OptionsScreen.TAB_CONTROLS, options.CurrentTab);
            options.HandleCancel();
            yield return null;
            yield return null;
            Assert.IsFalse(OptionsOverlay.IsOpen);
        }

        // ---------------------------------------------------------------- tienda
        private ShopScreen OpenShop()
        {
            var shop = App.Push<ShopScreen>();
            return shop;
        }

        private ShopScreen.Card CardOf(ShopScreen shop, string id) => shop.Cards.First(c => c.Item.id == id);

        [UnityTest]
        public IEnumerator Shop_FourTabs_ShowTheItemsOfTheDocs()
        {
            var shop = OpenShop();
            yield return null;
            int[] expected = { 9, 7, 8, 6 };
            for (int tab = 0; tab < 4; tab++)
            {
                shop.SetTab(tab);
                Assert.AreEqual(expected[tab], shop.Cards.Count(c => c.Button.gameObject.activeSelf), "pestaña " + tab);
            }
            shop.SetTab(0);
            Assert.AreEqual("Frijolitos: 0", shop.BalanceText);
            Assert.AreEqual("100", CardOf(shop, "hat_pirate_hat").Status.Label.text);
            Assert.AreEqual("Puesto", CardOf(shop, "hat_zoo_cap").Status.Label.text, "la gorra inicial ya viene puesta");
            Assert.AreEqual("Inicial", CardOf(shop, "gas_classic").Status.Label.text.Length > 0 ? "Inicial" : "");
            Assert.AreEqual("Premio de W6-5", CardOf(shop, "hat_golden_crown").Status.Label.text);
            Assert.AreEqual("Hito: 40 mangos dorados", CardOf(shop, "hat_mango_peel").Status.Label.text);
            Localization.Set(Localization.ENGLISH);
            Assert.AreEqual("Beans: 0", shop.BalanceText);
            Assert.AreEqual("Equipped", CardOf(shop, "hat_zoo_cap").Status.Label.text);
        }

        [UnityTest]
        public IEnumerator Shop_Buying_SpendsBeansOwnsAndEquips()
        {
            Data.frijolitos = 250;
            var shop = OpenShop();
            yield return null;
            var pirate = CardOf(shop, "hat_pirate_hat");
            shop.Show(pirate);
            Assert.IsTrue(shop.ActionButton.interactable);
            shop.DoAction();
            Assert.AreEqual(150, Data.frijolitos);
            Assert.IsTrue(Data.cosmetics.owned.Contains("hat_pirate_hat"));
            Assert.AreEqual("hat_pirate_hat", Services.Cosmetics.Equipped(0, CosmeticCategory.Hat));
            Assert.AreEqual("Puesto", pirate.Status.Label.text);
            Assert.AreEqual("Frijolitos: 150", shop.BalanceText);
            shop.DoAction();
            Assert.AreEqual(150, Data.frijolitos, "no se cobra dos veces");

            // Tienes la corona de flores a 120, pero la balsa sale 250: no alcanza.
            shop.SetTab(2);
            var log = CardOf(shop, "raft_watermelon_rind");
            shop.Show(log);
            shop.DoAction();
            Assert.AreEqual(150, Data.frijolitos);
            Assert.IsFalse(Data.cosmetics.owned.Contains("raft_watermelon_rind"));
            Assert.AreEqual("Te faltan frijolitos.", shop.MessageText);

            // La balsa es del equipo: se equipa para todos.
            Data.frijolitos = 400;
            shop.Show(CardOf(shop, "raft_lily_pad"));
            shop.DoAction();
            Assert.AreEqual("raft_lily_pad", Data.cosmetics.raft);
            Assert.AreEqual(250, Data.frijolitos);
            Assert.GreaterOrEqual(Data.frijolitos, 0);
        }

        [UnityTest]
        public IEnumerator Shop_Rewards_CannotBeBought_ButAreGrantedByMangosAndTheCrown()
        {
            Data.frijolitos = 9999;
            var shop = OpenShop();
            yield return null;
            var crown = CardOf(shop, "hat_golden_crown");
            shop.Show(crown);
            Assert.IsFalse(shop.ActionButton.interactable);
            shop.DoAction();
            Assert.IsFalse(Data.cosmetics.owned.Contains("hat_golden_crown"));
            Assert.AreEqual(9999, Data.frijolitos);

            // 10 mangos dorados: color de gas "Mango".
            for (int i = 0; i < 4; i++)
                Give(Catalog.worlds[0].levels[i].id, 1, 7);
            Services.Cosmetics.GrantRewards();
            Assert.IsTrue(Services.Cosmetics.IsOwned("gas_mango"), "12 mangos ≥ 10");
            Assert.IsFalse(Services.Cosmetics.IsOwned("raft_giant_mango"));
            // Terminar W6-5: corona dorada.
            Give("W6-5", 1);
            CosmeticData granted = null;
            Services.Cosmetics.RewardGranted += c => granted = c;
            Services.Cosmetics.GrantRewards();
            Assert.AreEqual("hat_golden_crown", granted.id);
            shop.SetTab(0);
            shop.Show(CardOf(shop, "hat_golden_crown"));
            Assert.AreEqual("Tuyo", CardOf(shop, "hat_golden_crown").Status.Label.text);
            Assert.IsTrue(shop.ActionButton.interactable);
        }

        [UnityTest]
        public IEnumerator Shop_Preview_ShowsEveryItemOnEveryAnimalWithoutErrors()
        {
            var shop = OpenShop();
            yield return null;
            Assert.IsNotNull(shop.Preview.Texture);
            foreach (var character in Catalog.characters)
            {
                Services.Session.ClearPlayers();
                Services.Session.AddPlayer(new PlayerSlot(0, Color.white, null, null) { Character = character });
                for (int tab = 0; tab < 4; tab++)
                {
                    shop.SetTab(tab);
                    foreach (var card in shop.Cards.Where(c => c.Button.gameObject.activeSelf))
                    {
                        shop.Show(card);
                        yield return null;
                        if (card.Item.category == CosmeticCategory.Gas)
                            continue;
                        Assert.IsNotNull(shop.Preview.Current, card.Item.id + " sobre " + character.id);
                        if (card.Item.category == CosmeticCategory.Celebration)
                            Assert.IsTrue(shop.Preview.IsPlayingClip, card.Item.id + " debe bailar");
                        if (card.Item.category == CosmeticCategory.Hat)
                            Assert.Greater(FindDeep(shop.Preview.Current.transform, "HatSocket").childCount, 0,
                                card.Item.id + " sobre " + character.id + ": el sombrero se pone en la cabeza");
                    }
                }
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.StartsWith(name))
                    return child;
            }
            return null;
        }

        // ---------------------------------------------------------------- logros y estadísticas
        [UnityTest]
        public IEnumerator Achievements_Grid_ShowsTwentySix_ProgressAndTheSecretOne()
        {
            Data.achievements.Add("first_fart");
            Data.stats.sinks = 7;
            var screen = App.Push<AchievementsScreen>();
            yield return null;
            Assert.AreEqual(26, screen.CellCount);
            Assert.IsTrue(screen.CellUnlocked(0));
            Assert.IsFalse(screen.CellUnlocked(1));
            Assert.AreEqual("Primer Pedo", screen.CellNameText(0));
            Assert.AreEqual("???", screen.CellNameText(25), "Rey de la Balsa es secreto");
            var king = Catalog.FindAchievement("raft_king");
            screen.ShowDetail(king);
            Assert.AreEqual("???", screen.DetailNameText);

            var captain = Catalog.FindAchievement("captain_titanic");
            Assert.AreEqual((7, 10), Services.Achievements.Progress(captain));
            Data.achievements.Add("raft_king");
            screen.Refresh();
            Assert.AreEqual("Rey de la Balsa", screen.CellNameText(25));
            screen.ShowDetail(king);
            Assert.AreEqual("Rey de la Balsa", screen.DetailNameText);
            Localization.Set(Localization.ENGLISH);
            Assert.AreEqual("First Fart", screen.CellNameText(0));
        }

        [UnityTest]
        public IEnumerator Achievements_StatsTab_ShowsTheSavedNumbers()
        {
            Data.stats.farts = 1520;
            Data.stats.sync4 = 9;
            Data.stats.meters = 18234;
            Data.stats.playSeconds = 9021;
            Data.stats.characters.Add(new SaveData.CharacterUse { id = "Gloria", count = 4 });
            Data.stats.characters.Add(new SaveData.CharacterUse { id = "Alex", count = 2 });
            var screen = App.Push<AchievementsScreen>();
            screen.SetTab(true);
            yield return null;
            Assert.IsTrue(screen.ShowingStats);
            Assert.AreEqual("1520", screen.StatValueText(0));
            Assert.AreEqual("9", screen.StatValueText(3));
            Assert.AreEqual("18234 m", screen.StatValueText(8));
            Assert.AreEqual("2:30:21", screen.StatValueText(9));
            Assert.AreEqual("Gloria", screen.StatValueText(10));
        }
    }
}
