using System.Collections.Generic;
using System.IO;
using System.Linq;
using GasSafari.Core;
using GasSafari.Editor;
using GasSafari.Level;
using GasSafari.Progression;
using GasSafari.Save;
using GasSafari.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GasSafari.Tests
{
    // M6: los datos de los menús (catálogo, tienda, logros, modos), el arte del kit y las opciones guardadas.
    public class MenuDataTests
    {
        private GameCatalog catalog;
        private UiSkin skin;

        [SetUp]
        public void SetUp()
        {
            catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(MenuDataBuilder.CATALOG_PATH);
            skin = AssetDatabase.LoadAssetAtPath<UiSkin>(MenuArtImporter.SKIN_PATH);
            Assert.IsNotNull(catalog, "falta Resources/GameCatalog.asset (menú Gas Safari/Setup/Crear catálogo, tienda y logros (M6))");
            Assert.IsNotNull(skin, "falta Resources/UiSkin.asset (menú Gas Safari/Setup/Importar arte de menús (M6))");
        }

        // ---------------------------------------------------------------- catálogo
        [Test]
        public void Catalog_HasSixWorldsOfFiveLevels_PlusTheTutorial()
        {
            Assert.AreEqual(6, catalog.worlds.Length);
            foreach (var world in catalog.worlds)
                Assert.AreEqual(5, world.levels.Count, world.id);
            Assert.AreEqual("T-0", catalog.tutorial.id);
            Assert.AreEqual(31, catalog.AllLevels().Count());
            Assert.AreEqual(5, catalog.characters.Length);
            Assert.AreEqual(3, catalog.difficulties.Length);
            Assert.AreEqual(new[] { "Familiar", "Normal", "AmistadesRotas" }, catalog.difficulties.Select(d => d.id).ToArray());
        }

        [Test]
        public void NextLevel_WalksTheStory_AndEndsAfterTheCrown()
        {
            var level = catalog.tutorial;
            var visited = new List<string>();
            while (level != null)
            {
                visited.Add(level.id);
                level = catalog.NextLevel(level);
            }
            Assert.AreEqual(31, visited.Count);
            Assert.AreEqual("T-0", visited[0]);
            Assert.AreEqual("W1-1", visited[1]);
            Assert.AreEqual("W2-1", visited[6], "tras W1-5 sigue el mundo 2");
            Assert.AreEqual("W6-5", visited[30]);
            Assert.IsNull(catalog.NextLevel(catalog.FindLevel("W6-5")));
            Assert.AreEqual(catalog.worlds[2], catalog.WorldOf(catalog.FindLevel("W3-4")));
            Assert.AreEqual("L_W1_1_FuentesZoo", catalog.FindLevel("W1-1").sceneName);
            Assert.AreEqual(catalog.FindLevel("W4-2"), catalog.FindLevelByScene(catalog.FindLevel("W4-2").sceneName));
        }

        // ---------------------------------------------------------------- tienda
        [Test]
        public void Shop_HasThirtyItems_WithThePricesOfTheDocs()
        {
            Assert.AreEqual(30, catalog.cosmetics.Length);
            Assert.AreEqual(9, catalog.InCategory(CosmeticCategory.Hat).Count);
            Assert.AreEqual(7, catalog.InCategory(CosmeticCategory.Gas).Count);
            Assert.AreEqual(8, catalog.InCategory(CosmeticCategory.Raft).Count);
            Assert.AreEqual(6, catalog.InCategory(CosmeticCategory.Celebration).Count);
            Assert.AreEqual(30, catalog.cosmetics.Select(c => c.id).Distinct().Count(), "ids repetidos");

            var prices = new Dictionary<string, int>
            {
                { "hat_diving_goggles", 100 }, { "hat_pirate_hat", 100 }, { "hat_explorer_helmet", 120 }, { "hat_flower_crown", 120 },
                { "hat_chef_hat", 150 }, { "hat_cowboy_hat", 150 },
                { "gas_black_smoke", 100 }, { "gas_neon_pink", 120 }, { "gas_bubbles", 150 }, { "gas_rainbow", 200 }, { "gas_gold_glitter", 250 },
                { "raft_lily_pad", 150 }, { "raft_zoo_crate", 150 }, { "raft_hollow_log", 180 }, { "raft_coconut_float", 200 },
                { "raft_lifeboat", 200 }, { "raft_watermelon_rind", 250 },
                { "celebrate_applause", 50 }, { "celebrate_robot_dance", 80 }, { "celebrate_bow", 80 }, { "celebrate_captain_salute", 80 },
                { "celebrate_somersault", 120 }, { "celebrate_conga_step", 120 },
            };
            foreach (var pair in prices)
            {
                var item = catalog.FindCosmetic(pair.Key);
                Assert.IsNotNull(item, pair.Key);
                Assert.AreEqual(CosmeticSource.Price, item.source, pair.Key);
                Assert.AreEqual(pair.Value, item.price, pair.Key);
            }
            foreach (string starter in new[] { "hat_zoo_cap", "gas_classic", "raft_orange" })
                Assert.AreEqual(CosmeticSource.Starter, catalog.FindCosmetic(starter).source, starter);
            Assert.AreEqual(CosmeticSource.FinishW65, catalog.FindCosmetic("hat_golden_crown").source);
            Assert.AreEqual(10, catalog.FindCosmetic("gas_mango").mangosRequired);
            Assert.AreEqual(25, catalog.FindCosmetic("raft_giant_mango").mangosRequired);
            Assert.AreEqual(40, catalog.FindCosmetic("hat_mango_peel").mangosRequired);
        }

        [Test]
        public void ShopItems_HaveIconsAndTheirVisuals()
        {
            foreach (var item in catalog.cosmetics)
            {
                Assert.IsNotNull(item.icon, item.id + " sin icono");
                Assert.IsNotEmpty(item.nameKey, item.id);
                if (item.category == CosmeticCategory.Hat)
                    Assert.IsNotNull(item.prefab, item.id + " sin modelo de sombrero");
                if (item.category == CosmeticCategory.Raft && item.id != "raft_orange")
                    Assert.IsNotNull(item.prefab, item.id + " sin piel de balsa");
                if (item.category == CosmeticCategory.Raft)
                    Assert.IsTrue(item.teamItem, "la balsa es del equipo");
                if (item.category == CosmeticCategory.Celebration)
                {
                    Assert.AreEqual(5, item.clips.Length, item.id + ": un clip por personaje");
                    foreach (var entry in item.clips)
                        Assert.IsNotNull(entry.clip, item.id + " " + entry.character);
                }
            }
            Assert.IsTrue(catalog.FindCosmetic("gas_rainbow").rainbow);
            Assert.AreEqual(5, catalog.characters.Length);
            Assert.IsNotNull(catalog.hatAnchors);
        }

        // ---------------------------------------------------------------- logros
        [Test]
        public void Achievements_AreTwentySix_WithMedals_AndTheSecretOne()
        {
            Assert.AreEqual(26, catalog.achievements.Length);
            Assert.AreEqual(26, catalog.achievements.Select(a => a.id).Distinct().Count());
            Assert.AreEqual(Enumerable.Range(1, 26), catalog.achievements.Select(a => a.order).OrderBy(o => o).ToArray());
            foreach (var achievement in catalog.achievements)
                Assert.IsNotNull(achievement.icon, achievement.id + " sin medalla");
            var king = catalog.FindAchievement("raft_king");
            Assert.IsTrue(king.secret);
            Assert.AreEqual(26, king.order);
            Assert.AreEqual("W6-5", king.target);
            Assert.AreEqual(1, catalog.achievements.Count(a => a.secret), "solo Rey de la Balsa es secreto");
            Assert.AreEqual("Icon_Ach_26_ReyDeLaBalsa", king.icon.name);
        }

        [Test]
        public void Modes_AreEight_AndOnlyStoryIsPlayableForNow()
        {
            Assert.AreEqual(8, catalog.modes.Length);
            Assert.AreEqual(ProgressionRules.MODE_STORY, catalog.modes.OrderBy(m => m.order).First().id);
            foreach (var mode in catalog.modes)
                Assert.IsNotNull(mode.icon, mode.id);
            Assert.AreEqual(new[] { ProgressionRules.MODE_STORY }, catalog.modes.Where(m => m.implemented).Select(m => m.id).ToArray());
        }

        // ---------------------------------------------------------------- arte
        [Test]
        public void Skin_HasTheWholeKitAndTheScreensArt()
        {
            var names = new List<string> { "Panel_Light", "Panel_Dark", "Panel_Inset", "Meter_Back", "Meter_Fill", "Meter_Fill_Ready", "Ring_Gas_Back",
                "Icon_Star_Empty", "Icon_Star_Full", "Icon_GoldenMango_Empty", "Icon_GoldenMango_Full", "Icon_Lock", "Icon_Bean", "Icon_Chamber_Healthy",
                "Logo_GasSafari", "WorldMap", "Frame_J1", "Frame_J4", "Frame_CB_J2", "Badge_J3_Square", "Event_Banner_Sync", "Ping_Now_Exclamation",
                "Icon_Raft_R00_Orange", "Icon_Difficulty_D1_Familiar", "Icon_Difficulty_D3_AmistadesRotas" };
            foreach (string color in new[] { "Green", "Blue", "Red" })
            {
                foreach (string state in new[] { "Normal", "Highlighted", "Pressed", "Disabled" })
                    names.Add("Button_" + color + "_" + state);
            }
            for (int i = 1; i <= 6; i++)
                names.Add("Loading_W" + i);
            foreach (string character in new[] { "Alex", "Marty", "Melman", "Gloria", "Julien" })
            {
                names.Add("Portrait_" + character);
                names.Add("Select_" + character);
            }
            foreach (string name in names)
                Assert.IsTrue(skin.Has(name), "falta el sprite " + name);
            Assert.IsNotNull(skin.Font);
            Assert.IsNotNull(skin.Credits, "CREDITS.md");
        }

        [Test]
        public void KitSprites_HaveTheNineSliceBordersOfTheReadme()
        {
            AssertBorder("Panel_Light", 58, 58, 58, 58);
            AssertBorder("Panel_Dark", 58, 58, 58, 58);
            AssertBorder("Panel_Inset", 50, 50, 50, 50);
            AssertBorder("Button_Green_Normal", 52, 52, 52, 52);
            AssertBorder("Button_Red_Pressed", 52, 52, 52, 52);
            AssertBorder("Meter_Fill", 40, 8, 40, 8);
            AssertBorder("Event_Banner_Info", 72, 40, 72, 40);
        }

        private void AssertBorder(string spriteName, float left, float top, float right, float bottom)
        {
            var border = skin.Get(spriteName).border;
            Assert.AreEqual(new Vector4(left, bottom, right, top), border, spriteName);
        }

        [Test]
        public void PlayerPalette_HasTheColorblindColorsOfTheDocs()
        {
            var palette = catalog.playerPalette;
            Assert.AreEqual(4, palette.colorblindColors.Length);
            string[] hex = { "E69F00", "56B4E9", "F0E442", "CC79A7" };
            for (int i = 0; i < 4; i++)
                Assert.AreEqual("#" + hex[i], "#" + ColorUtility.ToHtmlStringRGB(palette.colorblindColors[i]), "J" + (i + 1));
            PlayerPalette.UseColorblind = true;
            try
            {
                Assert.AreEqual(palette.colorblindColors[2], palette.Get(2));
            }
            finally
            {
                PlayerPalette.UseColorblind = false;
            }
            Assert.AreEqual(palette.colors[2], palette.Get(2));
        }

        // ---------------------------------------------------------------- opciones guardadas
        [Test]
        public void Settings_RoundTripThroughTheSaveFile_AndOldSavesGetDefaults()
        {
            string path = Path.Combine(Path.GetTempPath(), "gs_settings_" + System.Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var system = new SaveSystem(path);
                var data = new SaveData();
                data.settings.language = "en";
                data.settings.textScale = 1.5f;
                data.settings.volMusic = 0.3f;
                data.settings.toggleHold = true;
                data.settings.bindingOverrides = "{\"bindings\":[]}";
                data.settings.fpsLimit = 120;
                system.Save(data);
                var loaded = system.Load();
                Assert.AreEqual("en", loaded.settings.language);
                Assert.AreEqual(1.5f, loaded.settings.textScale);
                Assert.AreEqual(0.3f, loaded.settings.volMusic);
                Assert.IsTrue(loaded.settings.toggleHold);
                Assert.AreEqual(120, loaded.settings.fpsLimit);
                Assert.AreEqual("{\"bindings\":[]}", loaded.settings.bindingOverrides);

                // Un guardado de antes de M6 (sin los campos nuevos) carga con los valores por defecto.
                File.WriteAllText(path, "{\"version\":1,\"frijolitos\":50,\"settings\":{\"volMaster\":0.5,\"shake\":false}}");
                var old = new SaveSystem(path).Load();
                Assert.AreEqual(50, old.frijolitos);
                Assert.AreEqual(0.5f, old.settings.volMaster);
                Assert.IsFalse(old.settings.shake);
                Assert.AreEqual("es", old.settings.language);
                Assert.AreEqual(1f, old.settings.textScale);
                Assert.IsTrue(old.settings.vsync);
                Assert.IsTrue(old.settings.tips);
            }
            finally
            {
                File.Delete(path);
                File.Delete(path + ".bak");
            }
        }

        [Test]
        public void GameSettings_Helpers()
        {
            Assert.AreEqual(0, GameSettings.TextScaleIndex(1f));
            Assert.AreEqual(1, GameSettings.TextScaleIndex(1.25f));
            Assert.AreEqual(2, GameSettings.TextScaleIndex(1.5f));
            Assert.AreEqual(1, GameSettings.FpsIndex(60));
            Assert.AreEqual(3, GameSettings.FpsIndex(0));
            Assert.AreEqual(0.5f, GameSettings.SensitivityFromSlider(0f));
            Assert.AreEqual(2f, GameSettings.SensitivityFromSlider(1f));
            Assert.AreEqual(0.5f, GameSettings.SliderFromSensitivity(1.25f), 1e-4f);
            Assert.AreEqual(GameVolumes.Sfx, GameVolumes.For("farts_puff"));
            GameVolumes.Sfx = 0.5f;
            GameVolumes.Voices = 0.25f;
            GameVolumes.Ui = 0.75f;
            Assert.AreEqual(0.25f, GameVolumes.For("voice_alex_cheer"));
            Assert.AreEqual(0.75f, GameVolumes.For("ui_select"));
            Assert.AreEqual(0.5f, GameVolumes.For("water_splash"));
            GameVolumes.Sfx = GameVolumes.Voices = GameVolumes.Ui = 0.8f;
        }

        [Test]
        public void Rebinding_OnlyOffersButtonsAndKeys_NotSticks()
        {
            var controls = catalog.controls;
            Assert.IsNotNull(controls);
            var fart = controls.FindAction("Player/Fart");
            Assert.AreEqual(1, OptionsScreen.RebindableIndices(fart, "Keyboard").Count);
            Assert.AreEqual(1, OptionsScreen.RebindableIndices(fart, "Gamepad").Count);
            var move = controls.FindAction("Player/Move");
            Assert.AreEqual(4, OptionsScreen.RebindableIndices(move, "Keyboard").Count, "WASD son 4 teclas");
            Assert.AreEqual(0, OptionsScreen.RebindableIndices(move, "Gamepad").Count, "la palanca queda fija");
            var ping = controls.FindAction("Player/Ping");
            Assert.AreEqual(4, OptionsScreen.RebindableIndices(ping, "Keyboard").Count);
            Assert.AreEqual(0, OptionsScreen.RebindableIndices(ping, "Gamepad").Count, "la cruceta queda fija");
        }

        [Test]
        public void Rebinding_OverridesSurviveSaveAndLoad()
        {
            var copy = Object.Instantiate(catalog.controls);
            try
            {
                var fart = copy.FindAction("Player/Fart");
                int index = OptionsScreen.RebindableIndices(fart, "Keyboard")[0];
                fart.ApplyBindingOverride(index, "<Keyboard>/g");
                string json = copy.SaveBindingOverridesAsJson();

                var fresh = Object.Instantiate(catalog.controls);
                fresh.LoadBindingOverridesFromJson(json);
                Assert.AreEqual("<Keyboard>/g", fresh.FindAction("Player/Fart").bindings[index].effectivePath);
                Assert.AreEqual("<Keyboard>/space", catalog.controls.FindAction("Player/Fart").bindings[index].effectivePath, "el asset original no se toca");
                Object.DestroyImmediate(fresh);
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }
        }

        // ---------------------------------------------------------------- créditos
        [Test]
        public void Credits_ComposeToolsAndAssets_FromCreditsMd()
        {
            string markdown = File.ReadAllText("Assets/_Project/CREDITS.md");
            string text = CreditsScreen.Compose(markdown);
            StringAssert.Contains("Modelos 3D", text);
            StringAssert.Contains("Packs externos", text);
            StringAssert.Contains("Kenney", text);
            StringAssert.Contains("CC0", text);
            Assert.Less(text.IndexOf("Modelos 3D"), text.IndexOf("Packs externos"), "herramientas antes que assets");
            Assert.IsFalse(text.Contains("---"), "las tablas se convierten en viñetas");
            Assert.IsFalse(text.Contains("|"));
        }

        [Test]
        public void MenuScenes_AreInBuildSettings_BootFirst()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
            Assert.AreEqual(MenuSceneBuilder.BOOT_PATH, scenes[0]);
            Assert.AreEqual(MenuSceneBuilder.SCENE_PATH, scenes[1]);
            Assert.AreEqual(31 + 2, scenes.Count, "boot + título + 31 niveles");
            Assert.IsTrue(File.Exists(MenuSceneBuilder.SCENE_PATH));
        }
    }
}
