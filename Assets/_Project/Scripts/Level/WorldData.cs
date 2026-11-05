using System.Collections.Generic;
using UnityEngine;

namespace GasSafari.Level
{
    public enum WorldUnlockKind
    {
        None,
        Stars,
        GoldenMangoes
    }

    // Un mundo (docs/04 Mundos, docs/10 §9): 5 niveles, el quinto es el jefe.
    [CreateAssetMenu(menuName = "Gas Safari/World", fileName = "World")]
    public class WorldData : ScriptableObject
    {
        [Tooltip("W1..W6")] public string id;
        public string displayName;
        [Tooltip("Colores con hex en docs/04; farolas ámbar y espuma no tienen hex")] public Color[] palette;
        [Tooltip("Los jefes usan musicId + _boss")] public string musicId;
        public WorldUnlockKind unlockKind;
        public int unlockAmount;
        [Tooltip("En orden: el quinto es el jefe")] public List<LevelData> levels = new List<LevelData>();

        public string BossMusicId => musicId + "_boss";
    }
}
