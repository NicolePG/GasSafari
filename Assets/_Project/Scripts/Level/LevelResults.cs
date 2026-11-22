using System.Collections.Generic;

namespace GasSafari.Level
{
    public struct AwardResult
    {
        public string Id;
        public int Player;
        public bool Positive;
        public int Value;
    }

    // Lo que muestra la pantalla de resultados (docs/07 §2, docs/06 §4).
    public sealed class LevelResults
    {
        public float Time;
        public int Stars;
        public int Mangos;
        public int MangoTotal;
        public int MangoMask;
        // Frijolitos ganados (docs/06 §2).
        public int Beans;
        public bool FirstTime;
        public bool RaftSunk;
        public bool Skipped;
        public readonly List<AwardResult> Awards = new List<AwardResult>();
        public ReplayClip Replay;
        public int FullSyncGroups;
        // Modo del nivel (ProgressionRules.MODE_*): lo llenan los modos de M7.
        public string RunMode = "story";
        public bool ModifiersActive;
        // Contrarreloj: tiempo menos el del fantasma anterior (negativo = más rápido); NaN si no había fantasma.
        public float GhostDelta = float.NaN;
        public bool NewRecord;
    }
}
