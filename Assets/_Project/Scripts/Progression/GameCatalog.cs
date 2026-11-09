using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Level;
using UnityEngine;

namespace GasSafari.Progression
{
    // Lista de todos los datos que los menús necesitan recorrer: mundos, niveles, personajes, dificultades, tienda, logros y modos.
    // Vive en Resources/GameCatalog.asset y lo llena "Gas Safari/Setup/Crear catálogo, tienda y logros (M6)".
    [CreateAssetMenu(menuName = "Gas Safari/Game Catalog", fileName = "GameCatalog")]
    public class GameCatalog : ScriptableObject
    {
        public const string RESOURCE_PATH = "GameCatalog";

        public WorldData[] worlds = new WorldData[0];
        public LevelData tutorial;
        public CharacterData[] characters = new CharacterData[0];
        public DifficultyData[] difficulties = new DifficultyData[0];
        public CosmeticData[] cosmetics = new CosmeticData[0];
        public AchievementData[] achievements = new AchievementData[0];
        public GameModeData[] modes = new GameModeData[0];
        public UI.PlayerPalette playerPalette;
        public Characters.HatAnchorSet hatAnchors;
        [Tooltip("GasSafariControls: el remapeo de Opciones trabaja sobre una copia")] public UnityEngine.InputSystem.InputActionAsset controls;

        private static GameCatalog instance;

        public static GameCatalog Instance
        {
            get => instance != null ? instance : instance = Resources.Load<GameCatalog>(RESOURCE_PATH);
            set => instance = value;
        }

        public LevelData FindLevel(string id)
        {
            if (tutorial != null && tutorial.id == id)
                return tutorial;
            for (int w = 0; w < worlds.Length; w++)
            {
                var levels = worlds[w].levels;
                for (int i = 0; i < levels.Count; i++)
                {
                    if (levels[i].id == id)
                        return levels[i];
                }
            }
            return null;
        }

        public LevelData FindLevelByScene(string sceneName)
        {
            if (tutorial != null && tutorial.sceneName == sceneName)
                return tutorial;
            for (int w = 0; w < worlds.Length; w++)
            {
                var levels = worlds[w].levels;
                for (int i = 0; i < levels.Count; i++)
                {
                    if (levels[i].sceneName == sceneName)
                        return levels[i];
                }
            }
            return null;
        }

        public WorldData WorldOf(LevelData level)
        {
            if (level == null)
                return null;
            if (level.world != null)
                return level.world;
            for (int w = 0; w < worlds.Length; w++)
            {
                if (worlds[w].levels.Contains(level))
                    return worlds[w];
            }
            return null;
        }

        // El nivel que sigue en la historia: el siguiente del mundo o el primero del siguiente mundo. null al terminar W6-5.
        // El tutorial T-0 lleva a W1-1.
        public LevelData NextLevel(LevelData level)
        {
            if (level == null)
                return null;
            if (level == tutorial)
                return worlds.Length > 0 && worlds[0].levels.Count > 0 ? worlds[0].levels[0] : null;
            var world = WorldOf(level);
            if (world == null)
                return null;
            int index = world.levels.IndexOf(level);
            if (index >= 0 && index + 1 < world.levels.Count)
                return world.levels[index + 1];
            int worldIndex = System.Array.IndexOf(worlds, world);
            if (worldIndex >= 0 && worldIndex + 1 < worlds.Length && worlds[worldIndex + 1].levels.Count > 0)
                return worlds[worldIndex + 1].levels[0];
            return null;
        }

        public IEnumerable<LevelData> AllLevels()
        {
            if (tutorial != null)
                yield return tutorial;
            for (int w = 0; w < worlds.Length; w++)
            {
                for (int i = 0; i < worlds[w].levels.Count; i++)
                    yield return worlds[w].levels[i];
            }
        }

        public CosmeticData FindCosmetic(string id)
        {
            for (int i = 0; i < cosmetics.Length; i++)
            {
                if (cosmetics[i].id == id)
                    return cosmetics[i];
            }
            return null;
        }

        public CharacterData FindCharacter(string id)
        {
            for (int i = 0; i < characters.Length; i++)
            {
                if (string.Equals(characters[i].id, id, System.StringComparison.OrdinalIgnoreCase))
                    return characters[i];
            }
            return null;
        }

        public DifficultyData FindDifficulty(string id)
        {
            for (int i = 0; i < difficulties.Length; i++)
            {
                if (difficulties[i].id == id)
                    return difficulties[i];
            }
            return difficulties.Length > 0 ? difficulties[0] : null;
        }

        public AchievementData FindAchievement(string id)
        {
            for (int i = 0; i < achievements.Length; i++)
            {
                if (achievements[i].id == id)
                    return achievements[i];
            }
            return null;
        }

        public List<CosmeticData> InCategory(CosmeticCategory category)
        {
            var list = new List<CosmeticData>();
            for (int i = 0; i < cosmetics.Length; i++)
            {
                if (cosmetics[i].category == category)
                    list.Add(cosmetics[i]);
            }
            return list;
        }
    }
}
