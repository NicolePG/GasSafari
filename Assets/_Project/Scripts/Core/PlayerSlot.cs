using GasSafari.Characters;
using GasSafari.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GasSafari.Core
{
    // Un lugar en la partida (docs/10 §4): humano local, bot o remoto.
    public class PlayerSlot
    {
        public int Index { get; }
        public Color Color { get; set; }
        // Solo humanos locales.
        public PlayerInput Input { get; }
        public IPlayerCommandSource CommandSource { get; set; }
        public CharacterData Character { get; set; }
        public bool IsBot { get; set; }
        public bool IsRemote { get; set; }
        // Cosméticos equipados (ids del guardado, ver CosmeticsService): sombrero, color de gas y celebración. Vacío = ninguno.
        public string Hat { get; set; }
        public string Gas { get; set; }
        public string Celebration { get; set; }
        public bool IsReady { get; set; }
        // Modo solo con 2 animales: el segundo animal que controla el mismo mando (docs/02 §13).
        public PlayerSlot Partner { get; set; }

        public PlayerSlot(int index, Color color, PlayerInput input, IPlayerCommandSource commandSource)
        {
            Index = index;
            Color = color;
            Input = input;
            CommandSource = commandSource;
        }
    }
}
