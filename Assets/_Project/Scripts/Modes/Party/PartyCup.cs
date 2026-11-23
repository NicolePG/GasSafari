using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Progression;
using UnityEngine;

namespace GasSafari.Modes
{
    // Una Fiesta (docs/06 §1): los puntos que suma cada jugador de minijuego en minijuego y la lista de participantes con
    // los bots que completan. Vive mientras dura la sesión; el lobby la reinicia al entrar en Fiesta.
    public static class PartyCup
    {
        private static readonly int[] DefaultPoints = { 3, 2, 1, 0 };
        private static readonly int[] points = new int[GameSession.MAX_PLAYERS];
        // Quién jugó en cada lugar (incluidos los bots de relleno), para el ranking de la Fiesta.
        private static readonly PlayerSlot[] seen = new PlayerSlot[GameSession.MAX_PLAYERS];

        private static readonly int[] DefaultBeans = { 25, 15, 10, 5 };

        public static int GamesPlayed { get; private set; }
        // Minijuego que se va a jugar o se está jugando (lo fija la pantalla de minijuegos).
        public static PartyGameData Current { get; set; }
        // Fiesta completa (provisional): los 5 minijuegos en orden, uno detrás de otro, y al final el ranking.
        public static bool Tour { get; private set; }
        // Índice en Order del minijuego que se está jugando en la Fiesta completa.
        public static int TourIndex { get; private set; }
        // Al volver al menú tras el último minijuego de la Fiesta completa, la pantalla de minijuegos abre el ranking final.
        public static bool ShowFinalOnReturn { get; set; }
        // Frijolitos ya entregados en esta Fiesta (el ranking final se puede abrir más de una vez).
        public static int BeansAwarded { get; private set; } = -1;

        public static void Reset()
        {
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = 0;
                seen[i] = null;
            }
            GamesPlayed = 0;
            Tour = false;
            TourIndex = 0;
            ShowFinalOnReturn = false;
            BeansAwarded = -1;
        }

        // Los minijuegos de ModesConfig que ya tienen escena, por su orden (docs/06 §1).
        public static List<PartyGameData> Order()
        {
            var list = new List<PartyGameData>();
            if (ModesConfig.Instance == null)
                return list;
            foreach (var game in ModesConfig.Instance.partyGames)
            {
                if (game != null && game.implemented)
                    list.Add(game);
            }
            list.Sort((a, b) => a.order.CompareTo(b.order));
            return list;
        }

        // Empieza una Fiesta completa con los puntos en cero; devuelve el primer minijuego.
        public static PartyGameData StartTour()
        {
            Reset();
            var order = Order();
            if (order.Count == 0)
                return null;
            Tour = true;
            TourIndex = 0;
            return order[0];
        }

        public static int TourLength => Tour ? Order().Count : 0;

        // El minijuego que sigue en la Fiesta completa, o null si el que se jugó era el último.
        public static PartyGameData PeekNext()
        {
            if (!Tour)
                return null;
            var order = Order();
            return TourIndex + 1 < order.Count ? order[TourIndex + 1] : null;
        }

        public static PartyGameData AdvanceTour()
        {
            var next = PeekNext();
            if (next != null)
                TourIndex++;
            return next;
        }

        // Provisional (docs/06 §2: Fiesta da 5 a 25 frijolitos según resultado): por el mejor puesto de un humano en el
        // ranking final (1.º 25, 2.º 15, 3.º 10, 4.º 5), proporcional a los minijuegos jugados de 5 y nunca menos de 5.
        public static int BeansFor(int bestHumanRank, int gamesPlayed)
        {
            if (gamesPlayed <= 0)
                return 0;
            int full = bestHumanRank >= 1 && bestHumanRank <= DefaultBeans.Length ? DefaultBeans[bestHumanRank - 1] : DefaultBeans[DefaultBeans.Length - 1];
            float share = Mathf.Min(1f, gamesPlayed / (float)AchievementService.PARTY_GAMES);
            return Mathf.Max(5, Mathf.RoundToInt(full * share));
        }

        // Entrega los frijolitos de la Fiesta una sola vez y devuelve cuántos fueron. Sin humanos (pruebas), cuenta el mejor.
        public static int AwardBeans()
        {
            if (BeansAwarded >= 0)
                return BeansAwarded;
            var standings = Standings();
            int best = int.MaxValue, bestAny = int.MaxValue;
            foreach (var entry in standings)
            {
                bestAny = Mathf.Min(bestAny, entry.rank);
                if (!entry.slot.IsBot)
                    best = Mathf.Min(best, entry.rank);
            }
            if (best == int.MaxValue)
                best = bestAny;
            BeansAwarded = BeansFor(best, GamesPlayed);
            if (BeansAwarded > 0 && Services.IsReady && Services.Progression != null)
            {
                Services.Progression.AddBeans(BeansAwarded);
                Services.Progression.Save();
            }
            return BeansAwarded;
        }

        public static int Points(int slotIndex) => slotIndex >= 0 && slotIndex < points.Length ? points[slotIndex] : 0;

        // Provisional (docs/06 no lo define): 1.º 3 puntos, 2.º 2, 3.º 1, 4.º 0; los empatados se llevan los del puesto compartido.
        public static int PointsForRank(int rank)
        {
            var table = ModesConfig.Instance != null && ModesConfig.Instance.partyPoints.Length > 0 ? ModesConfig.Instance.partyPoints : DefaultPoints;
            return rank >= 1 && rank <= table.Length ? table[rank - 1] : 0;
        }

        public static void Award(IReadOnlyList<PlayerSlot> players, IReadOnlyList<int> ranks)
        {
            for (int i = 0; i < players.Count && i < ranks.Count; i++)
            {
                int slot = players[i].Index;
                if (slot < 0 || slot >= points.Length)
                    continue;
                points[slot] += PointsForRank(ranks[i]);
                seen[slot] = players[i];
            }
            GamesPlayed++;
        }

        // Ranking de la Fiesta: los lugares que jugaron, de más a menos puntos, con su puesto (empates compartidos).
        public static List<(PlayerSlot slot, int points, int rank)> Standings()
        {
            var list = new List<(PlayerSlot slot, int points, int rank)>();
            var totals = new List<int>();
            for (int i = 0; i < seen.Length; i++)
            {
                if (seen[i] == null)
                    continue;
                list.Add((seen[i], points[i], 0));
                totals.Add(points[i]);
            }
            var ranks = Rank(totals);
            for (int i = 0; i < list.Count; i++)
                list[i] = (list[i].slot, list[i].points, ranks[i]);
            list.Sort((a, b) => a.rank != b.rank ? a.rank.CompareTo(b.rank) : a.slot.Index.CompareTo(b.slot.Index));
            return list;
        }

        // Puesto de cada uno (1 = mejor) con empates compartidos: 10, 7, 7, 2 → 1, 2, 2, 4.
        public static int[] Rank(IReadOnlyList<int> scores)
        {
            var ranks = new int[scores.Count];
            for (int i = 0; i < scores.Count; i++)
            {
                int better = 0;
                for (int j = 0; j < scores.Count; j++)
                    better += scores[j] > scores[i] ? 1 : 0;
                ranks[i] = better + 1;
            }
            return ranks;
        }

        // Los jugadores de la sesión más los bots que hacen falta (docs/06: 2-4, los bots completan; Balsa-Gol 1v1 o 2v2).
        // Los bots de relleno no entran en la sesión: se crean de nuevo en cada minijuego con el mismo lugar libre, así
        // conservan sus puntos en la Fiesta.
        public static List<PlayerSlot> BuildRoster(IReadOnlyList<PlayerSlot> session, PartyGameData game)
        {
            var roster = new List<PlayerSlot>();
            int max = game != null ? Mathf.Clamp(game.maxPlayers, 1, GameSession.MAX_PLAYERS) : GameSession.MAX_PLAYERS;
            int min = game != null ? Mathf.Clamp(game.minPlayers, 1, max) : 2;
            for (int i = 0; i < session.Count && roster.Count < max; i++)
                roster.Add(session[i]);
            int wanted = Mathf.Max(min, roster.Count);
            if (game != null && game.evenPlayers && wanted % 2 == 1)
                wanted = Mathf.Min(wanted + 1, max - max % 2);
            while (roster.Count < wanted)
            {
                var bot = MakeBot(roster);
                if (bot == null)
                    break;
                roster.Add(bot);
            }
            roster.Sort((a, b) => a.Index.CompareTo(b.Index));
            return roster;
        }

        private static PlayerSlot MakeBot(List<PlayerSlot> taken)
        {
            int index = -1;
            for (int i = 0; i < GameSession.MAX_PLAYERS && index < 0; i++)
            {
                bool used = false;
                foreach (var slot in taken)
                    used |= slot.Index == i;
                if (!used)
                    index = i;
            }
            if (index < 0)
                return null;
            var catalog = GameCatalog.Instance;
            var color = catalog != null && catalog.playerPalette != null ? catalog.playerPalette.Get(index) : Color.white;
            return new PlayerSlot(index, color, null, null) { IsBot = true, IsReady = true, Character = FreeCharacter(taken, catalog) };
        }

        // El primer animal del catálogo que nadie usa (siempre el mismo para el mismo grupo).
        private static CharacterData FreeCharacter(List<PlayerSlot> taken, GameCatalog catalog)
        {
            if (catalog == null)
                return null;
            foreach (var character in catalog.characters)
            {
                bool used = false;
                foreach (var slot in taken)
                    used |= slot.Character == character;
                if (!used && character != null)
                    return character;
            }
            return catalog.characters.Length > 0 ? catalog.characters[0] : null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Reset();
            Current = null;
        }
    }
}
