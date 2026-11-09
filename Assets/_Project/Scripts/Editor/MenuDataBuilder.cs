using System.Collections.Generic;
using System.IO;
using System.Linq;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Progression;
using GasSafari.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GasSafari.Editor
{
    // M6: crea los datos de la tienda (30 artículos), los 26 logros, los 8 modos y el GameCatalog que los reúne con los mundos,
    // personajes y dificultades. También importa las 30 celebraciones (Celebrate_*) de Tools/Export/CharacterAnims.
    public static class MenuDataBuilder
    {
        private const string DATA = "Assets/_Project/Data/";
        public const string CATALOG_PATH = "Assets/_Project/Resources/GameCatalog.asset";
        private const string ANIM_SOURCE = "Tools/Export/CharacterAnims/";
        private const string CHARACTERS = "Assets/_Project/Art/Characters/";

        private static readonly string[] CharacterNames = { "Alex", "Marty", "Melman", "Gloria", "Julien" };

        // id, categoría, fuente, precio, icono, modelo (sombrero o piel), color de gas, extras
        private struct Def
        {
            public string Id;
            public CosmeticCategory Category;
            public CosmeticSource Source;
            public int Price;
            public string Icon;
            public string Prefab;
            public Color Gas;
            public bool Rainbow;
            public string Clip;
            public int Mangos;
            public bool Provisional;
        }

        private static Def Hat(string id, int price, string icon, string prefab, CosmeticSource source = CosmeticSource.Price) =>
            new Def { Id = id, Category = CosmeticCategory.Hat, Source = source, Price = price, Icon = icon, Prefab = prefab };

        private static Def Gas(string id, int price, string icon, Color color, bool rainbow = false, CosmeticSource source = CosmeticSource.Price) =>
            new Def { Id = id, Category = CosmeticCategory.Gas, Source = source, Price = price, Icon = icon, Gas = color, Rainbow = rainbow };

        private static Def Raft(string id, int price, string icon, string prefab, CosmeticSource source = CosmeticSource.Price) =>
            new Def { Id = id, Category = CosmeticCategory.Raft, Source = source, Price = price, Icon = icon, Prefab = prefab };

        private static Def Celebration(string id, int price, string icon, string clip) =>
            new Def { Id = id, Category = CosmeticCategory.Celebration, Source = CosmeticSource.Price, Price = price, Icon = icon, Clip = clip };

        private static List<Def> Definitions()
        {
            const string hats = "Assets/_Project/Prefabs/Cosmetics/Hats/";
            const string rafts = "Assets/_Project/Prefabs/Raft/Skins/RaftSkin_";
            var list = new List<Def>
            {
                // Sombreros (docs/06 §2).
                Hat("hat_zoo_cap", 0, "Icon_Hat_H01_ZooCap", hats + "H01_ZooCap.prefab", CosmeticSource.Starter),
                Hat("hat_diving_goggles", 100, "Icon_Hat_H02_DivingGoggles", hats + "H02_DivingGoggles.prefab"),
                Hat("hat_pirate_hat", 100, "Icon_Hat_H03_PirateHat", hats + "H03_PirateHat.prefab"),
                Hat("hat_explorer_helmet", 120, "Icon_Hat_H04_ExplorerHelmet", hats + "H04_ExplorerHelmet.prefab"),
                Hat("hat_flower_crown", 120, "Icon_Hat_H05_FlowerCrown", hats + "H05_FlowerCrown.prefab"),
                Hat("hat_chef_hat", 150, "Icon_Hat_H06_ChefHat", hats + "H06_ChefHat.prefab"),
                Hat("hat_cowboy_hat", 150, "Icon_Hat_H07_CowboyHat", hats + "H07_CowboyHat.prefab"),
                Hat("hat_golden_crown", 0, "Icon_Hat_H08_GoldenCrown", hats + "H08_GoldenCrown.prefab", CosmeticSource.FinishW65),
                // Premio de 40 mangos: todavía sin modelo propio (usa el gorro de chef) ni icono propio (usa el mango).
                new Def { Id = "hat_mango_peel", Category = CosmeticCategory.Hat, Source = CosmeticSource.MangoMilestone, Mangos = 40,
                          Icon = "Icon_P03_Mango", Prefab = hats + "H06_ChefHat.prefab", Provisional = true },
                // Colores de gas.
                Gas("gas_classic", 0, "Icon_Gas_G01_Classic", new Color(0.55f, 0.9f, 0.3f), false, CosmeticSource.Starter),
                Gas("gas_black_smoke", 100, "Icon_Gas_G02_BlackSmoke", new Color(0.12f, 0.12f, 0.14f)),
                Gas("gas_neon_pink", 120, "Icon_Gas_G03_NeonPink", new Color(1f, 0.25f, 0.8f)),
                Gas("gas_bubbles", 150, "Icon_Gas_G04_Bubbles", new Color(0.7f, 0.95f, 1f)),
                Gas("gas_rainbow", 200, "Icon_Gas_G05_Rainbow", Color.white, true),
                Gas("gas_gold_glitter", 250, "Icon_Gas_G06_GoldGlitter", new Color(1f, 0.82f, 0.2f)),
                new Def { Id = "gas_mango", Category = CosmeticCategory.Gas, Source = CosmeticSource.MangoMilestone, Mangos = 10,
                          Icon = "Icon_P03_Mango", Gas = new Color(1f, 0.62f, 0.12f) },
                // Balsas.
                Raft("raft_orange", 0, "Icon_Raft_R00_Orange", null, CosmeticSource.Starter),
                Raft("raft_lily_pad", 150, "Icon_Raft_R01_LilyPad", rafts + "R01_LilyPad.prefab"),
                Raft("raft_zoo_crate", 150, "Icon_Raft_R02_ZooCrate", rafts + "R02_ZooCrate.prefab"),
                Raft("raft_hollow_log", 180, "Icon_Raft_R03_HollowLog", rafts + "R03_HollowLog.prefab"),
                Raft("raft_coconut_float", 200, "Icon_Raft_R04_CoconutFloat", rafts + "R04_CoconutFloat.prefab"),
                Raft("raft_lifeboat", 200, "Icon_Raft_R05_Lifeboat", rafts + "R05_Lifeboat.prefab"),
                Raft("raft_watermelon_rind", 250, "Icon_Raft_R06_WatermelonRind", rafts + "R06_WatermelonRind.prefab"),
                // Premio de 25 mangos: sin modelo propio todavía (usa el flotador de coco).
                new Def { Id = "raft_giant_mango", Category = CosmeticCategory.Raft, Source = CosmeticSource.MangoMilestone, Mangos = 25,
                          Icon = "Icon_P03_Mango", Prefab = rafts + "R04_CoconutFloat.prefab", Provisional = true },
                // Celebraciones.
                Celebration("celebrate_applause", 50, "Icon_Celebrate_Aplauso", "Celebrate_Aplauso"),
                Celebration("celebrate_robot_dance", 80, "Icon_Celebrate_BaileRobot", "Celebrate_BaileRobot"),
                Celebration("celebrate_bow", 80, "Icon_Celebrate_Reverencia", "Celebrate_Reverencia"),
                Celebration("celebrate_captain_salute", 80, "Icon_Celebrate_SaludoCapitan", "Celebrate_SaludoCapitan"),
                Celebration("celebrate_somersault", 120, "Icon_Celebrate_Voltereta", "Celebrate_Voltereta"),
                Celebration("celebrate_conga_step", 120, "Icon_Celebrate_PasoConga", "Celebrate_PasoConga"),
            };
            return list;
        }

        [MenuItem("Gas Safari/Setup/Crear catálogo, tienda y logros (M6)")]
        public static void BuildAll()
        {
            ImportCelebrations();
            EnsureFolder("Assets/_Project/Data/Cosmetics");
            EnsureFolder("Assets/_Project/Data/Achievements");
            EnsureFolder("Assets/_Project/Data/Modes");

            var skin = UiSkin.Instance != null ? UiSkin.Instance : AssetDatabase.LoadAssetAtPath<UiSkin>(MenuArtImporter.SKIN_PATH);
            var cosmetics = BuildCosmetics(skin);
            var achievements = BuildAchievements(skin);
            var modes = BuildModes(skin);

            var catalog = HudBuilder.LoadOrCreate<GameCatalog>(CATALOG_PATH);
            catalog.worlds = Enumerable.Range(1, 6).Select(i => AssetDatabase.LoadAssetAtPath<WorldData>(DATA + "Worlds/W" + i + ".asset")).ToArray();
            catalog.tutorial = AssetDatabase.LoadAssetAtPath<LevelData>(DATA + "Levels/T-0.asset");
            catalog.characters = CharacterNames.Select(n => AssetDatabase.LoadAssetAtPath<CharacterData>(DATA + "Characters/" + n + ".asset")).ToArray();
            catalog.difficulties = new[] { "Familiar", "Normal", "AmistadesRotas" }
                .Select(n => AssetDatabase.LoadAssetAtPath<DifficultyData>(DATA + "Difficulty/Difficulty_" + n + ".asset")).ToArray();
            catalog.cosmetics = cosmetics;
            catalog.achievements = achievements;
            catalog.modes = modes;
            catalog.playerPalette = AssetDatabase.LoadAssetAtPath<PlayerPalette>(DATA + "UI/PlayerPalette.asset");
            catalog.hatAnchors = AssetDatabase.LoadAssetAtPath<HatAnchorSet>(DATA + "Cosmetics/HatAnchors.asset");
            catalog.controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/GasSafariControls.inputactions");
            EditorUtility.SetDirty(catalog);

            // Paleta de daltonismo (docs/07 §5).
            if (catalog.playerPalette != null)
            {
                catalog.playerPalette.colorblindColors = new[] { Hex("E69F00"), Hex("56B4E9"), Hex("F0E442"), Hex("CC79A7") };
                EditorUtility.SetDirty(catalog.playerPalette);
            }

            // Cada WorldData lleva el nivel en su lista; los niveles ya apuntan a su mundo.
            AssetDatabase.SaveAssets();
            GameCatalog.Instance = catalog;
            Debug.Log($"M6 catálogo: {cosmetics.Length} cosméticos, {achievements.Length} logros, {modes.Length} modos, {catalog.worlds.Length} mundos");
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return color;
        }

        // ---------------------------------------------------------------- celebraciones
        // Copia a Art/Characters/<Nombre>/Anims los 6 FBX Celebrate_* de cada personaje y los importa como los demás clips.
        public static void ImportCelebrations()
        {
            bool copied = false;
            foreach (var name in CharacterNames)
            {
                string folder = CHARACTERS + name + "/Anims/";
                if (!Directory.Exists(folder) || !Directory.Exists(ANIM_SOURCE + name))
                    continue;
                foreach (var file in Directory.GetFiles(ANIM_SOURCE + name, name + "@Celebrate_*.fbx"))
                {
                    string target = folder + Path.GetFileName(file);
                    var from = new FileInfo(file);
                    var to = new FileInfo(target);
                    if (!to.Exists || to.Length != from.Length)
                    {
                        File.Copy(file, target, true);
                        copied = true;
                    }
                }
            }
            if (!copied && AllCelebrationsConfigured())
                return;
            AssetDatabase.Refresh();
            BlenderAssetImporter.ImportCharacterAnims();
            AssetDatabase.SaveAssets();
        }

        private static bool AllCelebrationsConfigured()
        {
            foreach (var name in CharacterNames)
            {
                string path = CHARACTERS + name + "/Anims/" + name + "@Celebrate_Aplauso.fbx";
                if (!File.Exists(path) || FindClip(path, "Celebrate_Aplauso") == null)
                    return false;
            }
            return true;
        }

        private static AnimationClip FindClip(string fbxPath, string clipName)
        {
            return AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>().FirstOrDefault(c => c.name == clipName);
        }

        // ---------------------------------------------------------------- tienda
        private static CosmeticData[] BuildCosmetics(UiSkin skin)
        {
            var result = new List<CosmeticData>();
            foreach (var def in Definitions())
            {
                var item = HudBuilder.LoadOrCreate<CosmeticData>(DATA + "Cosmetics/" + def.Id + ".asset");
                item.id = def.Id;
                item.category = def.Category;
                item.source = def.Source;
                item.price = def.Price;
                item.mangosRequired = def.Mangos;
                // hat_pirate_hat → shop.hat.pirate_hat
                int cut = def.Id.IndexOf('_');
                item.nameKey = "shop." + def.Id.Substring(0, cut) + "." + def.Id.Substring(cut + 1);
                item.icon = skin != null ? skin.Get(def.Icon) : null;
                item.prefab = !string.IsNullOrEmpty(def.Prefab) ? AssetDatabase.LoadAssetAtPath<GameObject>(def.Prefab) : null;
                // La balsa naranja no tiene piel (es el modelo de serie): su vista previa usa ese mismo modelo.
                item.previewPrefab = def.Id == "raft_orange" ? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Props/Raft/Raft.fbx") : null;
                item.gasColor = def.Gas;
                item.rainbow = def.Rainbow;
                item.clipName = def.Clip;
                item.teamItem = def.Category == CosmeticCategory.Raft;
                item.provisional = def.Provisional;
                item.tint = def.Provisional ? new Color(1f, 0.8f, 0.3f) : Color.white;
                item.clips = def.Category == CosmeticCategory.Celebration ? CelebrationClips(def.Clip) : new CelebrationClip[0];
                EditorUtility.SetDirty(item);
                result.Add(item);
            }
            return result.ToArray();
        }

        private static CelebrationClip[] CelebrationClips(string clipName)
        {
            var clips = new List<CelebrationClip>();
            foreach (var name in CharacterNames)
            {
                var clip = FindClip(CHARACTERS + name + "/Anims/" + name + "@" + clipName + ".fbx", clipName);
                if (clip != null)
                    clips.Add(new CelebrationClip { character = name, clip = clip });
                else
                    Debug.LogWarning($"M6 falta el clip {clipName} de {name}");
            }
            return clips.ToArray();
        }

        // ---------------------------------------------------------------- logros
        private struct Ach
        {
            public string Id;
            public AchievementKind Kind;
            public int Value;
            public string Target;
            public bool Secret;
        }

        private static Ach A(string id, AchievementKind kind, int value = 0, string target = null, bool secret = false) =>
            new Ach { Id = id, Kind = kind, Value = value, Target = target, Secret = secret };

        private static readonly Ach[] Achievements =
        {
            A("first_fart", AchievementKind.FirstFart), A("duet", AchievementKind.SyncGroup, 2), A("orchestra", AchievementKind.SyncGroup, 4),
            A("super_fart", AchievementKind.SuperFart),
            A("jungle_slingshot", AchievementKind.Synergy, 0, "honda_selva"), A("earthquake", AchievementKind.Synergy, 0, "terremoto"),
            A("total_party", AchievementKind.Synergy, 0, "fiesta_total"), A("heavy_duo", AchievementKind.Synergy, 0, "duo_pesado"),
            A("royal_rescue", AchievementKind.Synergy, 0, "rescate_real"),
            A("captain_titanic", AchievementKind.Sinks, 10), A("not_a_scratch", AchievementKind.NoChambersLost),
            A("world1", AchievementKind.WorldFinished, 0, "W1"), A("world2", AchievementKind.WorldFinished, 0, "W2"),
            A("world3", AchievementKind.WorldFinished, 0, "W3"), A("world4", AchievementKind.WorldFinished, 0, "W4"),
            A("world5", AchievementKind.WorldFinished, 0, "W5"), A("world6", AchievementKind.WorldFinished, 0, "W6"),
            A("perfectionist", AchievementKind.PerfectWorld), A("constellation", AchievementKind.TotalStars, 93),
            A("royal_collector", AchievementKind.TotalMangos, 93), A("speedster", AchievementKind.ThreeStarLevels, 10),
            A("broken_friendships", AchievementKind.StoryOnHardest), A("party_animal", AchievementKind.PartyPlayed, 5),
            A("kilometer", AchievementKind.EndlessMeters, 1000), A("consistent", AchievementKind.DailyStreak, 7),
            A("raft_king", AchievementKind.FinishLevel, 0, "W6-5", true),
        };

        private static AchievementData[] BuildAchievements(UiSkin skin)
        {
            var result = new List<AchievementData>();
            for (int i = 0; i < Achievements.Length; i++)
            {
                var def = Achievements[i];
                var data = HudBuilder.LoadOrCreate<AchievementData>(DATA + "Achievements/Ach_" + (i + 1).ToString("00") + "_" + def.Id + ".asset");
                data.id = def.Id;
                data.order = i + 1;
                data.kind = def.Kind;
                data.value = def.Value;
                data.target = def.Target;
                data.secret = def.Secret;
                string prefix = "Icon_Ach_" + (i + 1).ToString("00") + "_";
                data.icon = skin != null ? skin.Sprites.FirstOrDefault(s => s != null && s.name.StartsWith(prefix)) : null;
                if (data.icon == null)
                    Debug.LogWarning("M6 logro sin medalla: " + def.Id);
                EditorUtility.SetDirty(data);
                result.Add(data);
            }
            return result.ToArray();
        }

        // ---------------------------------------------------------------- modos
        private static GameModeData[] BuildModes(UiSkin skin)
        {
            var defs = new (string id, string key, string icon, bool implemented, int min, int max)[]
            {
                (ProgressionRules.MODE_STORY, "story", "Icon_Mode_M1_Historia", true, 1, 4),
                (ProgressionRules.MODE_TIME_TRIAL, "timetrial", "Icon_Mode_M2_Contrarreloj", false, 1, 4),
                (ProgressionRules.MODE_MIRROR, "mirror", "Icon_Mode_M3_Espejo", false, 1, 4),
                (ProgressionRules.MODE_MODIFIERS, "modifiers", "Icon_Mode_M4_Caos", false, 1, 4),
                (ProgressionRules.MODE_PARTY, "party", "Icon_Mode_M5_Fiesta", false, 2, 4),
                (ProgressionRules.MODE_ENDLESS, "endless", "Icon_Mode_M6_Infinito", false, 1, 4),
                (ProgressionRules.MODE_DAILY, "daily", "Icon_Mode_M7_DesafioDiario", false, 1, 4),
                (ProgressionRules.MODE_TEAM_RACE, "teamrace", "Icon_Mode_M8_CarreraEquipos", false, 2, 4),
            };
            var result = new List<GameModeData>();
            for (int i = 0; i < defs.Length; i++)
            {
                var data = HudBuilder.LoadOrCreate<GameModeData>(DATA + "Modes/Mode_" + defs[i].id + ".asset");
                data.id = defs[i].id;
                data.order = i + 1;
                data.keyBase = defs[i].key;
                data.icon = skin != null ? skin.Get(defs[i].icon) : null;
                data.implemented = defs[i].implemented;
                data.minPlayers = defs[i].min;
                data.maxPlayers = defs[i].max;
                EditorUtility.SetDirty(data);
                result.Add(data);
            }
            return result.ToArray();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
