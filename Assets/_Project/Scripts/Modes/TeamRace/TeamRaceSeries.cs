using System;
using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Progression;
using UnityEngine;

namespace GasSafari.Modes
{
    // Serie de Carrera por Equipos local (docs/06 §1): dos equipos, mejor de 3 pistas. Guarda entre escenas el reparto de
    // equipos del lobby, las pistas, quién ganó cada carrera y las victorias. Lo provisional está en docs/06.
    public static class TeamRaceSeries
    {
        public const int TEAMS = TeamRaceSpawn.TEAMS;
        public const int RACES = 3;
        public const int WINS_NEEDED = 2;
        // Local 2 vs 2 (docs/06): cada balsa sale con 2 animales; los bots completan.
        public const int CREW = 2;

        private static readonly int[] teamOf = { 0, 1, 0, 1 };
        private static readonly int[] wins = new int[TEAMS];
        private static readonly List<LevelData> tracks = new List<LevelData>();
        private static readonly List<int> winners = new List<int>();
        private static readonly List<float> times = new List<float>();

        public static int RaceIndex { get; private set; }
        public static IReadOnlyList<LevelData> Tracks => tracks;
        public static IReadOnlyList<int> Winners => winners;
        public static IReadOnlyList<float> Times => times;
        public static LevelData CurrentTrack => RaceIndex < tracks.Count ? tracks[RaceIndex] : null;
        public static int SeriesWinner => wins[0] >= WINS_NEEDED ? 0 : wins[1] >= WINS_NEEDED ? 1 : -1;
        public static bool IsOver => SeriesWinner >= 0 || winners.Count >= RACES;
        // La carrera en curso ya tiene ganador (la escena está mostrando el final).
        public static bool CurrentDecided => winners.Count > RaceIndex;

        public static int Wins(int team) => team >= 0 && team < TEAMS ? wins[team] : 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            teamOf[0] = 0;
            teamOf[1] = 1;
            teamOf[2] = 0;
            teamOf[3] = 1;
            ResetScore();
            tracks.Clear();
        }

        private static void ResetScore()
        {
            Array.Clear(wins, 0, wins.Length);
            winners.Clear();
            times.Clear();
            RaceIndex = 0;
        }

        // ---------------------------------------------------------------- equipos
        // Equipo (0 Azul, 1 Naranja) del jugador con ese número (PlayerSlot.Index 0-3).
        public static int TeamOf(int playerIndex) => playerIndex >= 0 && playerIndex < teamOf.Length ? teamOf[playerIndex] : 0;

        public static int TeamOf(PlayerSlot slot, IReadOnlyList<PlayerSlot> session)
        {
            // El segundo animal del control doble va con su dueño.
            for (int i = 0; i < session.Count; i++)
            {
                if (session[i] != slot && session[i].Partner == slot)
                    return TeamOf(session[i].Index);
            }
            return TeamOf(slot.Index);
        }

        // Reparto inicial al entrar al lobby de equipos: los humanos en orden, uno a cada lado.
        public static void AutoAssign(IReadOnlyList<PlayerSlot> session)
        {
            int next = 0;
            for (int i = 0; i < session.Count; i++)
            {
                var slot = session[i];
                if (slot.IsBot || IsPartner(slot, session) || slot.Index < 0 || slot.Index >= teamOf.Length)
                    continue;
                teamOf[slot.Index] = next % TEAMS;
                next++;
            }
        }

        // Cambia de equipo a ese jugador si en el otro cabe (2 animales por balsa, contando su segundo animal).
        public static bool SetTeam(PlayerSlot slot, int team, IReadOnlyList<PlayerSlot> session)
        {
            if (slot == null || slot.Index < 0 || slot.Index >= teamOf.Length || team < 0 || team >= TEAMS)
                return false;
            if (TeamOf(slot.Index) == team)
                return true;
            int size = slot.Partner != null ? 2 : 1;
            if (HumansIn(team, session) + size > CREW)
                return false;
            teamOf[slot.Index] = team;
            return true;
        }

        public static int HumansIn(int team, IReadOnlyList<PlayerSlot> session)
        {
            int count = 0;
            for (int i = 0; i < session.Count; i++)
            {
                if (!session[i].IsBot && TeamOf(session[i], session) == team)
                    count++;
            }
            return count;
        }

        // Las tripulaciones de las dos balsas: los humanos en su equipo, los bots agregados en la sala donde haya lugar y
        // bots de relleno hasta CREW.
        public static void BuildCrews(IReadOnlyList<PlayerSlot> session, List<PlayerSlot> blue, List<PlayerSlot> orange)
        {
            blue.Clear();
            orange.Clear();
            var taken = new List<PlayerSlot>();
            for (int i = 0; i < session.Count; i++)
            {
                var slot = session[i];
                if (slot.IsBot)
                    continue;
                var list = TeamOf(slot, session) == 0 ? blue : orange;
                if (list.Count >= CREW)
                    continue;
                list.Add(slot);
                taken.Add(slot);
            }
            for (int i = 0; i < session.Count; i++)
            {
                var slot = session[i];
                if (!slot.IsBot)
                    continue;
                var list = blue.Count <= orange.Count ? blue : orange;
                if (list.Count >= CREW)
                    continue;
                list.Add(slot);
                taken.Add(slot);
            }
            Fill(blue, taken);
            Fill(orange, taken);
        }

        private static void Fill(List<PlayerSlot> crew, List<PlayerSlot> taken)
        {
            while (crew.Count < CREW)
            {
                var bot = PartyCup.MakeBot(taken);
                if (bot == null)
                    return;
                crew.Add(bot);
                taken.Add(bot);
            }
        }

        private static bool IsPartner(PlayerSlot slot, IReadOnlyList<PlayerSlot> session)
        {
            for (int i = 0; i < session.Count; i++)
            {
                if (session[i] != slot && session[i].Partner == slot)
                    return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- pistas
        // Pistas compatibles cuyo mundo está abierto (docs/06 §1 y §2). Sin progreso se cuentan todas.
        public static List<LevelData> AvailableTracks(GameCatalog catalog, Save.SaveData data)
        {
            var list = new List<LevelData>();
            if (catalog == null)
                return list;
            foreach (var level in catalog.AllLevels())
            {
                if (level == null || !level.raceCompatible)
                    continue;
                if (data == null || level.world == null || ProgressionRules.IsWorldUnlocked(level.world, data))
                    list.Add(level);
            }
            return list;
        }

        // 3 pistas al azar sin repetir; si hay menos de 3 abiertas, se repiten.
        public static List<LevelData> PickTracks(IReadOnlyList<LevelData> available, System.Random rng)
        {
            var pool = new List<LevelData>(available);
            var result = new List<LevelData>();
            while (result.Count < RACES && available.Count > 0)
            {
                if (pool.Count == 0)
                    pool.AddRange(available);
                int pick = rng.Next(pool.Count);
                result.Add(pool[pick]);
                pool.RemoveAt(pick);
            }
            return result;
        }

        // ---------------------------------------------------------------- serie
        public static void Begin(IReadOnlyList<LevelData> seriesTracks)
        {
            ResetScore();
            tracks.Clear();
            tracks.AddRange(seriesTracks);
        }

        // Fin de la carrera en curso: el primer equipo en la meta y su tiempo.
        public static void RecordRace(int winnerTeam, float time)
        {
            if (CurrentDecided || winnerTeam < 0 || winnerTeam >= TEAMS)
                return;
            winners.Add(winnerTeam);
            times.Add(time);
            wins[winnerTeam]++;
        }

        // Reiniciar desde la pausa: la carrera en curso vuelve a empezar sin ganador.
        public static void ForgetCurrent()
        {
            if (!CurrentDecided)
                return;
            int team = winners[RaceIndex];
            winners.RemoveAt(RaceIndex);
            times.RemoveAt(RaceIndex);
            wins[team]--;
        }

        // La pista siguiente, o null si la serie terminó.
        public static LevelData Advance()
        {
            if (IsOver || !CurrentDecided)
                return null;
            RaceIndex++;
            return CurrentTrack;
        }
    }
}
