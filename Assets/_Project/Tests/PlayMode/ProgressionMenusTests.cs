using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Input;
using GasSafari.Level;
using GasSafari.Progression;
using GasSafari.Save;
using GasSafari.UI;
using GasSafari.VFX;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // Tienda, logros, estadísticas y opciones guardadas con los servicios reales (M6-06, M6-07, M10-01).
    public class ProgressionMenusTests
    {
        private GameCatalog catalog;
        private string savePath;
        private readonly List<string> unlocked = new List<string>();
        private int originalFrameRate, originalVSync, originalQuality;
        private DifficultyData originalDifficulty;

        private SaveData Data => Services.Progression.Data;

        [SetUp]
        public void SetUp()
        {
            // Otras pruebas llaman a GameEvents.ClearAll(): se vuelven a conectar los servicios que escuchan eventos.
            foreach (var service in new Behaviour[] { Services.Stats, Services.Achievements })
            {
                service.enabled = false;
                service.enabled = true;
            }
            originalFrameRate = Application.targetFrameRate;
            originalVSync = QualitySettings.vSyncCount;
            originalQuality = QualitySettings.GetQualityLevel();
            originalDifficulty = Services.Session.Difficulty;
            catalog = GameCatalog.Instance;
            savePath = Path.Combine(Application.temporaryCachePath, "prog_test_" + System.Guid.NewGuid().ToString("N") + ".json");
            Services.Progression.Initialize(new SaveSystem(savePath));
            Services.Session.ClearPlayers();
            Services.Session.Difficulty = catalog.FindDifficulty("Normal");
            unlocked.Clear();
            GameEvents.OnAchievementUnlocked += OnUnlocked;
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.OnAchievementUnlocked -= OnUnlocked;
            Services.Session.ClearPlayers();
            Services.Progression.Initialize(new SaveSystem(SaveSystem.DefaultPath));
            Localization.Set(Localization.SPANISH);
            LocalizedText.SetTextScale(1f);
            Services.Session.Difficulty = originalDifficulty;
            Application.targetFrameRate = originalFrameRate;
            if (QualitySettings.GetQualityLevel() != originalQuality)
                QualitySettings.SetQualityLevel(originalQuality, true);
            QualitySettings.vSyncCount = originalVSync;
            if (File.Exists(savePath))
                File.Delete(savePath);
        }

        private void OnUnlocked(string id) => unlocked.Add(id);

        private void Finish(string levelId, int stars, int mangos = 0)
        {
            var record = Data.GetOrAddLevel(levelId);
            record.stars = stars;
            record.mangos = mangos;
        }

        // ---------------------------------------------------------------- tienda
        [Test]
        public void Cosmetics_PerPlayerEquipment_AndTeamRaft()
        {
            Data.cosmetics.owned.AddRange(new[] { "hat_pirate_hat", "hat_chef_hat", "raft_lifeboat", "celebrate_bow" });
            var cosmetics = Services.Cosmetics;
            Assert.IsTrue(cosmetics.Equip(0, CosmeticCategory.Hat, catalog.FindCosmetic("hat_pirate_hat")));
            Assert.IsTrue(cosmetics.Equip(1, CosmeticCategory.Hat, catalog.FindCosmetic("hat_chef_hat")));
            Assert.AreEqual("hat_pirate_hat", cosmetics.Equipped(0, CosmeticCategory.Hat));
            Assert.AreEqual("hat_chef_hat", cosmetics.Equipped(1, CosmeticCategory.Hat));
            Assert.AreEqual("hat_zoo_cap", cosmetics.Equipped(2, CosmeticCategory.Hat), "J3 no tocó nada");
            Assert.IsFalse(cosmetics.Equip(0, CosmeticCategory.Hat, catalog.FindCosmetic("hat_cowboy_hat")), "no se equipa lo que no se tiene");
            Assert.IsFalse(cosmetics.Equip(0, CosmeticCategory.Gas, catalog.FindCosmetic("hat_chef_hat")), "categoría equivocada");
            Assert.IsTrue(cosmetics.Equip(0, CosmeticCategory.Raft, catalog.FindCosmetic("raft_lifeboat")));
            Assert.AreEqual("raft_lifeboat", cosmetics.Equipped(3, CosmeticCategory.Raft), "la balsa es de todos");
            Assert.IsTrue(cosmetics.Equip(0, CosmeticCategory.Celebration, catalog.FindCosmetic("celebrate_bow")));
            Assert.IsTrue(cosmetics.Equip(0, CosmeticCategory.Hat, null));
            Assert.AreEqual(string.Empty, cosmetics.Equipped(0, CosmeticCategory.Hat));

            var slot = new PlayerSlot(0, Color.white, null, null);
            cosmetics.Apply(slot);
            Assert.AreEqual(string.Empty, slot.Hat);
            Assert.AreEqual("gas_classic", slot.Gas);
            Assert.AreEqual("celebrate_bow", slot.Celebration);

            // Se guarda y se recupera.
            var reloaded = new SaveSystem(savePath).Load();
            Assert.AreEqual("hat_chef_hat", reloaded.cosmetics.equipped.First(e => e.player == "p2").hat);
            Assert.AreEqual("raft_lifeboat", reloaded.cosmetics.raft);
        }

        [Test]
        public void Cosmetics_Buying_NeverGoesNegative_AndOnlyPricedItemsAreSold()
        {
            var cosmetics = Services.Cosmetics;
            Data.frijolitos = 99;
            var goggles = catalog.FindCosmetic("hat_diving_goggles");
            Assert.IsFalse(cosmetics.CanAfford(goggles));
            Assert.IsFalse(cosmetics.TryBuy(goggles));
            Assert.AreEqual(99, Data.frijolitos);
            Data.frijolitos = 100;
            Assert.IsTrue(cosmetics.TryBuy(goggles));
            Assert.AreEqual(0, Data.frijolitos);
            Assert.IsFalse(cosmetics.TryBuy(goggles), "ya es tuyo");
            Assert.IsFalse(cosmetics.TryBuy(catalog.FindCosmetic("gas_mango")), "los premios no se compran");
            Assert.IsFalse(cosmetics.TryBuy(catalog.FindCosmetic("hat_zoo_cap")), "lo inicial no se compra");
            Assert.IsTrue(cosmetics.IsOwned("hat_zoo_cap"));
            Assert.IsTrue(cosmetics.IsOwned("raft_orange"));
            Assert.IsTrue(cosmetics.IsOwned("gas_classic"));
        }

        [Test]
        public void Cosmetics_MangoMilestones_At10_25_And40_AndTheCrownAtW65()
        {
            var cosmetics = Services.Cosmetics;
            var granted = new List<string>();
            cosmetics.RewardGranted += c => granted.Add(c.id);
            var levels = catalog.AllLevels().Where(l => l.world != null).ToList();
            void SetMangos(int total)
            {
                Data.levels.Clear();
                int left = total;
                foreach (var level in levels)
                {
                    int here = Mathf.Min(3, left);
                    if (here <= 0) break;
                    Finish(level.id, 1, (1 << here) - 1);
                    left -= here;
                }
                cosmetics.GrantRewards();
            }
            SetMangos(9);
            Assert.IsEmpty(granted);
            SetMangos(10);
            CollectionAssert.AreEqual(new[] { "gas_mango" }, granted);
            SetMangos(25);
            CollectionAssert.Contains(granted, "raft_giant_mango");
            Assert.IsFalse(granted.Contains("hat_mango_peel"));
            SetMangos(40);
            CollectionAssert.Contains(granted, "hat_mango_peel");
            Assert.IsFalse(granted.Contains("hat_golden_crown"));
            Finish("W6-5", 1);
            cosmetics.GrantRewards();
            CollectionAssert.Contains(granted, "hat_golden_crown");
            Assert.AreEqual(4, granted.Count, "cada premio se entrega una sola vez");
            cosmetics.GrantRewards();
            Assert.AreEqual(4, granted.Count);
        }

        [Test]
        public void Cosmetics_RainbowGasChangesColorEveryFart_OthersStayFixed()
        {
            var colors = new HashSet<Color>();
            for (int i = 0; i < 6; i++)
                colors.Add(Services.Cosmetics.GasColor("gas_rainbow"));
            Assert.AreEqual(6, colors.Count);
            Assert.AreEqual(Services.Cosmetics.GasColor("gas_neon_pink"), Services.Cosmetics.GasColor("gas_neon_pink"));
            Assert.AreEqual(Color.white, Services.Cosmetics.GasColor("nope"));
        }

        // ---------------------------------------------------------------- cosméticos dentro del juego
        [UnityTest]
        public IEnumerator InGame_HatsGasAndRaftSkinsAreApplied()
        {
            var avatarPrefab = catalog.FindCharacter("Marty").prefab;
            var avatar = Object.Instantiate(avatarPrefab).GetComponent<PlayerAvatar>();
            var slot = new PlayerSlot(0, Color.white, null, null) { Character = catalog.FindCharacter("Marty"), Hat = "hat_pirate_hat", Gas = "gas_neon_pink" };
            CosmeticsApplier.ApplyAvatar(avatar, slot);
            yield return null;
            var hat = avatar.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == CosmeticsApplier.HAT_NAME);
            Assert.IsNotNull(hat, "el sombrero cuelga de la cabeza");
            Assert.AreEqual("HatSocket", hat.parent.name);
            Assert.IsTrue(hat.GetComponentsInChildren<Renderer>().Length > 0);
            Assert.IsTrue(hat.GetComponentsInChildren<Collider>().All(c => !c.enabled));
            var feedback = avatar.GetComponent<FartFeedback>();
            Assert.AreEqual(catalog.FindCosmetic("gas_neon_pink").gasColor, feedback.GasColor);
            Assert.IsNull(feedback.GasColorSource);

            slot.Gas = "gas_rainbow";
            CosmeticsApplier.ApplyAvatar(avatar, slot);
            Assert.IsNotNull(feedback.GasColorSource, "el arcoíris pide un color por pedo");
            Assert.AreNotEqual(feedback.GasColorSource(), feedback.GasColorSource());
            Object.Destroy(avatar.gameObject);

            Data.cosmetics.owned.Add("raft_zoo_crate");
            Services.Cosmetics.Equip(0, CosmeticCategory.Raft, catalog.FindCosmetic("raft_zoo_crate"));
            var raft = Object.Instantiate(AssetPrefab("Assets/_Project/Prefabs/Raft/Raft.prefab"));
            CosmeticsApplier.ApplyRaft(raft);
            Assert.IsFalse(raft.transform.Find("Model").gameObject.activeSelf, "se oculta el modelo naranja");
            Assert.IsTrue(raft.transform.Find(CosmeticsApplier.SKIN_NAME).gameObject.activeSelf);
            Object.Destroy(raft);

            // Una piel que no se tiene no se muestra.
            Data.cosmetics.owned.Clear();
            var plain = Object.Instantiate(AssetPrefab("Assets/_Project/Prefabs/Raft/Raft.prefab"));
            CosmeticsApplier.ApplyRaft(plain);
            Assert.IsTrue(plain.transform.Find("Model").gameObject.activeSelf);
            Assert.IsNull(plain.transform.Find(CosmeticsApplier.SKIN_NAME));
            Object.Destroy(plain);
        }

        private static GameObject AssetPrefab(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
#else
            return null;
#endif
        }

        [UnityTest]
        public IEnumerator InGame_CelebrationPlaysOnTheFinishLine_AndHandsBackTheAnimator()
        {
            var avatar = Object.Instantiate(catalog.FindCharacter("Alex").prefab).GetComponent<PlayerAvatar>();
            var slot = new PlayerSlot(0, Color.white, null, null) { Character = catalog.FindCharacter("Alex"), Celebration = "celebrate_robot_dance" };
            CosmeticsApplier.ApplyAvatar(avatar, slot);
            var runner = avatar.GetComponent<CelebrationRunner>();
            Assert.IsNotNull(runner);
            Assert.IsNotNull(runner.Clip);
            Assert.AreEqual("Celebrate_BaileRobot", runner.Clip.name);
            Assert.IsFalse(runner.IsPlaying);
            GameEvents.RaiseLevelFinished(60f);
            yield return null;
            Assert.IsTrue(runner.IsPlaying);
            runner.Stop();
            Assert.IsFalse(runner.IsPlaying);
            Object.Destroy(avatar.gameObject);
        }

        // ---------------------------------------------------------------- logros
        [UnityTest]
        public IEnumerator Achievements_EventBased_UnlockOnceAndEmit()
        {
            var service = Services.Achievements;
            Assert.AreEqual(0, service.UnlockedCount);
            GameEvents.RaiseFart(null, FartType.Puff, 1f);
            GameEvents.RaiseFart(null, FartType.Puff, 1f);
            GameEvents.RaiseSync(2, 2f);
            GameEvents.RaiseSync(3, 3f);
            GameEvents.RaiseSync(4, 4f);
            GameEvents.RaiseSuperFart(1f);
            foreach (string synergy in new[] { "honda_selva", "terremoto", "fiesta_total", "duo_pesado", "rescate_real" })
                GameEvents.RaiseSynergy(synergy, true);
            GameEvents.RaiseSynergy("honda_selva", false);
            yield return null;
            CollectionAssert.AreEquivalent(new[] { "first_fart", "duet", "orchestra", "super_fart", "jungle_slingshot", "earthquake", "total_party", "heavy_duo", "royal_rescue" }, unlocked);
            Assert.AreEqual(9, service.UnlockedCount, "cada logro una sola vez");
        }

        [UnityTest]
        public IEnumerator Achievements_TenSinks_Unlock_CaptainTitanic()
        {
            for (int i = 0; i < 9; i++)
            {
                GameEvents.RaiseRaftSunk();
                yield return null;
            }
            Assert.IsFalse(Services.Achievements.IsUnlocked("captain_titanic"), "9 hundimientos no bastan");
            GameEvents.RaiseRaftSunk();
            yield return null;
            yield return null;
            Assert.IsTrue(Services.Achievements.IsUnlocked("captain_titanic"));
            Assert.AreEqual(10, Data.stats.sinks);
        }

        [Test]
        public void Achievements_LevelAndProgressBased()
        {
            var service = Services.Achievements;
            var level = catalog.FindLevel("W1-1");
            service.HandleLevelFinished(level, 1, "Normal");
            Assert.IsFalse(service.IsUnlocked("not_a_scratch"), "perdió una cámara");
            service.HandleLevelFinished(catalog.tutorial, 0, "Normal");
            Assert.IsFalse(service.IsUnlocked("not_a_scratch"), "el tutorial no cuenta");
            service.HandleLevelFinished(level, 0, "Normal");
            Assert.IsTrue(service.IsUnlocked("not_a_scratch"));

            // Mundos: terminar los 5 niveles de cada uno.
            for (int w = 0; w < 6; w++)
            {
                foreach (var l in catalog.worlds[w].levels)
                    Finish(l.id, 1, 0);
                service.Evaluate();
                Assert.IsTrue(service.IsUnlocked("world" + (w + 1)), "mundo " + (w + 1));
            }
            Assert.IsFalse(service.IsUnlocked("perfectionist"));
            foreach (var l in catalog.worlds[1].levels)
                Finish(l.id, 3, 7);
            service.Evaluate();
            Assert.IsTrue(service.IsUnlocked("perfectionist"), "todas las estrellas de un mundo");
            Assert.IsFalse(service.IsUnlocked("speedster"));
            foreach (var l in catalog.worlds[2].levels)
                Finish(l.id, 3, 0);
            service.Evaluate();
            Assert.IsTrue(service.IsUnlocked("speedster"), "3 estrellas en 10 niveles");
            Assert.IsFalse(service.IsUnlocked("constellation"));
            Assert.IsTrue(service.IsUnlocked("raft_king"), "W6-5 terminado");
            foreach (var l in catalog.AllLevels())
                Finish(l.id, 3, 7); // los 31 niveles, tutorial incluido: 93 estrellas y 93 mangos
            service.Evaluate();
            Assert.IsTrue(service.IsUnlocked("constellation"), "93 estrellas");
            Assert.IsTrue(service.IsUnlocked("royal_collector"), "93 mangos");
        }

        [Test]
        public void Achievements_ModesAndDifficulty()
        {
            var service = Services.Achievements;
            service.RecordEndlessDistance(999);
            Assert.IsFalse(service.IsUnlocked("kilometer"));
            service.RecordEndlessDistance(1000);
            Assert.IsTrue(service.IsUnlocked("kilometer"));
            foreach (string game in new[] { "goal", "king", "fishing", "bubble", "coconut" })
            {
                Assert.IsFalse(service.IsUnlocked("party_animal"));
                service.RecordPartyGame(game);
            }
            Assert.IsTrue(service.IsUnlocked("party_animal"));
            Data.daily.streak = 6;
            service.Evaluate();
            Assert.IsFalse(service.IsUnlocked("consistent"));
            Data.daily.streak = 7;
            service.Evaluate();
            Assert.IsTrue(service.IsUnlocked("consistent"));

            // Amistades Rotas: la historia principal (mundos 1 a 5) en esa dificultad.
            Assert.IsFalse(service.IsUnlocked("broken_friendships"));
            foreach (var level in catalog.worlds.Take(5).SelectMany(w => w.levels))
            {
                service.HandleLevelFinished(level, 1, level.id == "W5-5" ? "AmistadesRotas" : "AmistadesRotas");
            }
            service.Evaluate();
            Assert.IsTrue(service.IsUnlocked("broken_friendships"));
        }

        [Test]
        public void Achievements_AllTwentySix_CanBeUnlocked_AndEachEmitsTheEvent()
        {
            var service = Services.Achievements;
            foreach (var achievement in catalog.achievements)
                Assert.IsTrue(service.Unlock(achievement.id), achievement.id);
            Assert.AreEqual(26, unlocked.Count);
            Assert.AreEqual(26, unlocked.Distinct().Count());
            Assert.IsFalse(service.Unlock("duet"), "no se vuelve a emitir");
            Assert.AreEqual(26, new SaveSystem(savePath).Load().achievements.Count, "quedan guardados");
        }

        [UnityTest]
        public IEnumerator Achievements_ShowAPopupWithTheMedal()
        {
            Services.Achievements.Unlock("raft_king");
            yield return null;
            var popup = Object.FindFirstObjectByType<AchievementPopup>();
            Assert.IsNotNull(popup);
            Assert.IsTrue(popup.IsShowing);
        }

        // ---------------------------------------------------------------- estadísticas
        [UnityTest]
        public IEnumerator Stats_CountFartsSyncsFallsRescuesAndFavoriteAnimal()
        {
            for (int i = 0; i < 5; i++)
                GameEvents.RaiseFart(null, FartType.Puff, 1f);
            GameEvents.RaiseSync(2, 2f);
            GameEvents.RaiseSync(3, 3f);
            GameEvents.RaiseSync(4, 4f);
            GameEvents.RaiseSync(4, 4f);
            GameEvents.RaiseSuperFart(1f);
            GameEvents.RaisePlayerFell(null);
            GameEvents.RaisePlayerRescued(null, null);
            GameEvents.RaiseRaftSunk();
            yield return null;
            var stats = Data.stats;
            Assert.AreEqual(5, stats.farts);
            Assert.AreEqual(1, stats.sync2);
            Assert.AreEqual(1, stats.sync3);
            Assert.AreEqual(2, stats.sync4);
            Assert.AreEqual(1, stats.superFarts);
            Assert.AreEqual(1, stats.falls);
            Assert.AreEqual(1, stats.rescues);
            Assert.AreEqual(1, stats.sinks);

            StatsService.AddCharacterUse(stats, "Gloria");
            StatsService.AddCharacterUse(stats, "Gloria");
            StatsService.AddCharacterUse(stats, "Alex");
            Assert.AreEqual("Gloria", StatsService.FavoriteCharacter(stats));
            Assert.IsNull(StatsService.FavoriteCharacter(new SaveData.StatsRecord()));
        }

        [UnityTest]
        public IEnumerator Stats_AreSavedWhenALevelEnds()
        {
            Services.Session.AddPlayer(new PlayerSlot(0, Color.white, null, null) { Character = catalog.FindCharacter("Melman") });
            GameEvents.RaiseFart(null, FartType.Blast, 1f);
            GameEvents.RaiseLevelFinished(90f);
            yield return null;
            var saved = new SaveSystem(savePath).Load();
            Assert.AreEqual(1, saved.stats.farts);
            Assert.AreEqual("Melman", StatsService.FavoriteCharacter(saved.stats));
        }

        // ---------------------------------------------------------------- opciones y arranque
        [Test]
        public void Settings_LanguageAndTextSize_AreAppliedWhenTheSaveLoads()
        {
            var system = new SaveSystem(savePath);
            var data = new SaveData();
            data.settings.language = "en";
            data.settings.textScale = 1.5f;
            data.settings.volMusic = 0.25f;
            data.settings.colorblind = true;
            system.Save(data);

            Localization.Set(Localization.SPANISH);
            Services.Progression.Initialize(new SaveSystem(savePath));
            Assert.AreEqual(Localization.ENGLISH, Localization.Language);
            Assert.AreEqual(1.5f, LocalizedText.TextScale);
            Assert.AreEqual(0.25f, GameVolumes.Music);
            Assert.IsTrue(PlayerPalette.UseColorblind);
            PlayerPalette.UseColorblind = false;
            GameVolumes.Music = 0.8f;
        }

        [Test]
        public void Level_Flow_NextLevelAndMapRequests()
        {
            var next = catalog.NextLevel(catalog.FindLevel("W3-5"));
            Assert.AreEqual("W4-1", next.id);
            Services.Session.CurrentLevel = catalog.FindLevel("W2-2");
            Assert.AreEqual("W2-2", LevelFlow.CurrentLevel.id);
            Services.Session.CurrentLevel = null;
        }
    }
}
