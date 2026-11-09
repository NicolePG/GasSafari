using GasSafari.Core;

namespace GasSafari.Level
{
    // Contadores de una partida de nivel, por jugador (índice = posición del avatar en la balsa). Los llena LevelStats.
    public sealed class StatsData
    {
        public readonly int[] SyncGroups = new int[GameSession.MAX_PLAYERS];
        public readonly int[] Rescues = new int[GameSession.MAX_PLAYERS];
        public readonly int[] Abilities = new int[GameSession.MAX_PLAYERS];
        public readonly int[] PowerUps = new int[GameSession.MAX_PLAYERS];
        public readonly int[] Falls = new int[GameSession.MAX_PLAYERS];
        public readonly int[] NoGas = new int[GameSession.MAX_PLAYERS];
        public readonly int[] Farts = new int[GameSession.MAX_PLAYERS];
        // Pedos en el segundo previo a un choque o hundimiento.
        public readonly int[] UntimelyFarts = new int[GameSession.MAX_PLAYERS];

        public int Players;
        public int FullSyncGroups;
        public int GoldenMangos;
        // Un bit por mango dorado recogido (ranura 0-2).
        public int MangoMask;
        public int SunkCount;
        // Ayuda Real: se saltó una sección (el nivel queda con máximo 1 estrella).
        public bool SectionSkipped;
    }
}
