using System;
using System.Collections.Generic;

namespace GasSafari.Level
{
    // Junta tiempo, estrellas, frijolitos, premios y repetición (docs/06 §2 y §4, docs/10 §5).
    public static class ResultsBuilder
    {
        private const int AWARDS_SHOWN = 3;
        private const int BEANS_FINISH = 20;
        private const int BEANS_FIRST_TIME = 30;
        private const int BEANS_PER_NEW_STAR = 10;
        private const int BEANS_FULL_SYNC_MAX = 10;

        private struct AwardDef
        {
            public string Id;
            public bool Positive;
            public bool LowestWins;
            public Func<StatsData, int[]> Counters;
        }

        private static readonly AwardDef[] Defs =
        {
            new AwardDef { Id = "sync_master", Positive = true, Counters = s => s.SyncGroups },
            new AwardDef { Id = "lifeguard", Positive = true, Counters = s => s.Rescues },
            new AwardDef { Id = "show_king", Positive = true, Counters = s => s.Abilities },
            new AwardDef { Id = "glutton", Positive = true, Counters = s => s.PowerUps },
            new AwardDef { Id = "untimely_fart", Positive = false, Counters = s => s.UntimelyFarts },
            new AwardDef { Id = "captain_titanic", Positive = false, Counters = s => s.Falls },
            new AwardDef { Id = "wasted_gas", Positive = false, Counters = s => s.NoGas },
            new AwardDef { Id = "sleeping_beauty", Positive = false, LowestWins = true, Counters = s => s.Farts }
        };

        // previousBestStars: -1 si el nivel no se había terminado nunca.
        public static LevelResults Build(LevelData level, float time, StatsData stats, int previousBestStars,
            ReplayClip worstCrash, Random rng)
        {
            var results = new LevelResults
            {
                Time = time,
                RaftSunk = stats.SunkCount > 0,
                Mangos = stats.GoldenMangos,
                MangoMask = stats.MangoMask,
                MangoTotal = level != null ? level.mangoCount : 3,
                FirstTime = previousBestStars < 0,
                Replay = worstCrash
            };
            results.Skipped = stats.SectionSkipped;
            results.Stars = StarsFor(level, time, results.RaftSunk);
            if (results.Skipped)
                results.Stars = Math.Min(results.Stars, 1);
            results.Beans = BeansFor(results.Stars, previousBestStars, stats.FullSyncGroups);
            PickAwards(stats, rng, results.Awards);
            return results;
        }

        // 1 por terminar; 2 con tiempo igual o menor a Plata; 3 con Oro y sin hundir la balsa (docs/04).
        public static int StarsFor(LevelData level, float time, bool raftSunk)
        {
            if (level == null)
                return 1;
            if (time <= level.goldTime && !raftSunk)
                return 3;
            return time <= level.silverTime ? 2 : 1;
        }

        // Terminar 20 (primera vez +30), 10 por estrella nueva, 1 por grupo de sync x4 (máximo 10).
        public static int BeansFor(int stars, int previousBestStars, int fullSyncGroups)
        {
            int beans = BEANS_FINISH;
            if (previousBestStars < 0)
                beans += BEANS_FIRST_TIME;
            beans += BEANS_PER_NEW_STAR * Math.Max(0, stars - Math.Max(previousBestStars, 0));
            return beans + Math.Min(BEANS_FULL_SYNC_MAX, fullSyncGroups);
        }

        // 3 al azar entre los que tengan ganador, con al menos un positivo y uno de vergüenza.
        // Gana quien tiene el valor más alto (o el más bajo) sin empate; con un solo jugador no hay premios.
        public static void PickAwards(StatsData stats, Random rng, List<AwardResult> output)
        {
            output.Clear();
            if (stats.Players < 2)
                return;

            var positives = new List<AwardResult>();
            var shames = new List<AwardResult>();
            foreach (var def in Defs)
            {
                if (TryWinner(def, stats, out var award))
                    (def.Positive ? positives : shames).Add(award);
            }
            Shuffle(positives, rng);
            Shuffle(shames, rng);

            var rest = new List<AwardResult>();
            TakeFirst(positives, output, rest);
            TakeFirst(shames, output, rest);
            Shuffle(rest, rng);
            for (int i = 0; i < rest.Count && output.Count < AWARDS_SHOWN; i++)
                output.Add(rest[i]);
        }

        private static void TakeFirst(List<AwardResult> source, List<AwardResult> output, List<AwardResult> rest)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (i == 0)
                    output.Add(source[i]);
                else
                    rest.Add(source[i]);
            }
        }

        private static bool TryWinner(AwardDef def, StatsData stats, out AwardResult award)
        {
            award = default;
            var counters = def.Counters(stats);
            int best = -1;
            int bestValue = 0;
            bool tie = false;
            for (int i = 0; i < stats.Players; i++)
            {
                int value = counters[i];
                bool better = best < 0 || (def.LowestWins ? value < bestValue : value > bestValue);
                if (better)
                {
                    best = i;
                    bestValue = value;
                    tie = false;
                }
                else if (value == bestValue)
                {
                    tie = true;
                }
            }
            if (tie || best < 0 || (!def.LowestWins && bestValue <= 0))
                return false;

            award = new AwardResult { Id = def.Id, Player = best, Positive = def.Positive, Value = bestValue };
            return true;
        }

        private static void Shuffle(List<AwardResult> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
