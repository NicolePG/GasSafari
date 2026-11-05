using GasSafari.Level;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // Crea o actualiza los 31 LevelData y los 6 WorldData de docs/04. Se puede ejecutar varias veces.
    public static class LevelDataBuilder
    {
        private const string LEVELS_FOLDER = "Assets/_Project/Data/Levels";
        private const string WORLDS_FOLDER = "Assets/_Project/Data/Worlds";
        private const string TUTORIAL_ID = "T-0";
        private const string TUTORIAL_MUSIC = "mus_tutorial";

        private struct WorldRow
        {
            public string Id, Name, Music;
            public WorldUnlockKind Unlock;
            public int Amount;
            public string[] Palette;
        }

        // Colores con hex en docs/04 Mundos.
        private static readonly WorldRow[] Worlds =
        {
            new WorldRow { Id = "W1", Name = "Fuga del Zoológico", Music = "mus_w1", Palette = new[] { "#1B2A4A", "#FF4FA3", "#3AE1FF" } },
            new WorldRow { Id = "W2", Name = "Mar Abierto", Music = "mus_w2", Unlock = WorldUnlockKind.Stars, Amount = 8, Palette = new[] { "#2EC4B6", "#5C6B73" } },
            new WorldRow { Id = "W3", Name = "Selva de Madagascar", Music = "mus_w3", Unlock = WorldUnlockKind.Stars, Amount = 20, Palette = new[] { "#2D6A4F", "#F77F00" } },
            new WorldRow { Id = "W4", Name = "Cuevas y Volcán", Music = "mus_w4", Unlock = WorldUnlockKind.Stars, Amount = 32, Palette = new[] { "#3C1642", "#4CC9F0", "#D00000" } },
            new WorldRow { Id = "W5", Name = "El Gran Regreso", Music = "mus_w5", Unlock = WorldUnlockKind.Stars, Amount = 45, Palette = new[] { "#FFB703", "#9C3D1C", "#FB8500" } },
            new WorldRow { Id = "W6", Name = "Reino de Julien", Music = "mus_w6", Unlock = WorldUnlockKind.GoldenMangoes, Amount = 60, Palette = new[] { "#F72585", "#B5E48C" } },
        };

        // docs/06 Carrera por Equipos.
        private static readonly string[] RaceTracks = { "W1-3", "W2-2", "W3-2", "W4-1", "W5-1", "W5-5" };

        [MenuItem("Gas Safari/Setup/Crear LevelData y WorldData")]
        public static void Build()
        {
            EnsureFolder(LEVELS_FOLDER);
            EnsureFolder(WORLDS_FOLDER);

            var worlds = new WorldData[Worlds.Length];
            for (int i = 0; i < Worlds.Length; i++)
                worlds[i] = BuildWorld(Worlds[i]);

            foreach (var row in LevelSeedTable.Rows)
                BuildLevel(row, worlds);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static WorldData BuildWorld(WorldRow row)
        {
            var world = LoadOrCreate<WorldData>(WORLDS_FOLDER + "/" + row.Id + ".asset");
            world.id = row.Id;
            world.displayName = row.Name;
            world.musicId = row.Music;
            world.unlockKind = row.Unlock;
            world.unlockAmount = row.Amount;
            world.palette = new Color[row.Palette.Length];
            for (int i = 0; i < row.Palette.Length; i++)
                ColorUtility.TryParseHtmlString(row.Palette[i], out world.palette[i]);
            world.levels.Clear();
            EditorUtility.SetDirty(world);
            return world;
        }

        private static void BuildLevel(LevelSeedTable.Row row, WorldData[] worlds)
        {
            var level = LoadOrCreate<LevelData>(LEVELS_FOLDER + "/" + row.Id + ".asset");
            bool tutorial = row.Id == TUTORIAL_ID;
            WorldData world = null;
            int order = 0;
            if (!tutorial)
            {
                world = worlds[row.Id[1] - '1'];
                order = row.Id[3] - '0';
            }

            level.id = row.Id;
            level.sceneName = row.Scene;
            level.world = world;
            level.orderInWorld = order;
            level.goldTime = row.Gold;
            level.silverTime = row.Silver;
            level.mangoCount = 3;
            level.checkpointCount = row.Checkpoints;
            level.isBoss = row.Boss;
            level.showTimer = !tutorial;
            level.raceCompatible = System.Array.IndexOf(RaceTracks, row.Id) >= 0;
            level.musicId = tutorial ? TUTORIAL_MUSIC : world.musicId + (row.Boss ? "_boss" : string.Empty);
            level.mechanicText = row.Mechanic;
            EditorUtility.SetDirty(level);
            if (world != null)
                world.levels.Add(level);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'), System.IO.Path.GetFileName(path));
        }
    }
}
