using UnityEngine;

namespace GasSafari.Core
{
    // Capas físicas de docs/10 §11, resueltas una sola vez.
    public static class GameLayers
    {
        public static readonly int Raft = LayerMask.NameToLayer("Raft");
        public static readonly int Character = LayerMask.NameToLayer("Character");
        public static readonly int CharacterInWater = LayerMask.NameToLayer("CharacterInWater");
        public static readonly int Environment = LayerMask.NameToLayer("Environment");
        public static readonly int Hazard = LayerMask.NameToLayer("Hazard");
        public static readonly int Trigger = LayerMask.NameToLayer("Trigger");
        public static readonly int PowerUp = LayerMask.NameToLayer("PowerUp");
        public static readonly int Water = LayerMask.NameToLayer("Water");
        public static readonly int Ghost = LayerMask.NameToLayer("Ghost");

        public static readonly int WaterMask = 1 << Water;
    }
}
