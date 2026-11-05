using GasSafari.Level;
using GasSafari.Save;

namespace GasSafari.Progression
{
    // Reglas de desbloqueo de docs/06 §1-2 como funciones puras sobre SaveData.
    public static class ProgressionRules
    {
        public const string MODE_STORY = "story";
        public const string MODE_TIME_TRIAL = "time_trial";
        public const string MODE_MIRROR = "mirror";
        public const string MODE_MODIFIERS = "modifiers";
        public const string MODE_PARTY = "party";
        public const string MODE_ENDLESS = "endless";
        public const string MODE_DAILY = "daily";
        public const string MODE_TEAM_RACE = "team_race";

        private const int MODIFIERS_STARS = 30;

        public static int TotalStars(SaveData data)
        {
            int total = 0;
            for (int i = 0; i < data.levels.Count; i++)
                total += data.levels[i].stars;
            return total;
        }

        public static int TotalMangos(SaveData data)
        {
            int total = 0;
            for (int i = 0; i < data.levels.Count; i++)
                total += PopCount(data.levels[i].mangos);
            return total;
        }

        public static int PopCount(int mask)
        {
            int count = 0;
            for (uint bits = (uint)mask; bits != 0; bits &= bits - 1)
                count++;
            return count;
        }

        public static bool IsWorldUnlocked(WorldUnlockKind kind, int amount, SaveData data)
        {
            switch (kind)
            {
                case WorldUnlockKind.Stars: return TotalStars(data) >= amount;
                case WorldUnlockKind.GoldenMangoes: return TotalMangos(data) >= amount;
                default: return true;
            }
        }

        public static bool IsWorldUnlocked(WorldData world, SaveData data)
        {
            return IsWorldUnlocked(world.unlockKind, world.unlockAmount, data);
        }

        public static bool HasFinished(SaveData data, string levelId)
        {
            var record = data.FindLevel(levelId);
            return record != null && record.stars > 0;
        }

        public static bool IsModeUnlocked(string modeId, SaveData data)
        {
            switch (modeId)
            {
                case MODE_STORY: return true;
                case MODE_TIME_TRIAL: return data.levels.Exists(r => r.stars > 0);
                case MODE_MIRROR: return HasFinished(data, "W5-5");
                case MODE_MODIFIERS: return TotalStars(data) >= MODIFIERS_STARS;
                case MODE_PARTY:
                case MODE_TEAM_RACE: return HasFinished(data, "W1-5");
                case MODE_ENDLESS:
                case MODE_DAILY: return HasFinished(data, "W2-5");
                default: return false;
            }
        }

        // Modificadores (docs/06 §1): cada uno pide cierto número de estrellas.
        public static bool IsModifierUnlocked(int starsRequired, SaveData data)
        {
            return TotalStars(data) >= starsRequired;
        }
    }
}
