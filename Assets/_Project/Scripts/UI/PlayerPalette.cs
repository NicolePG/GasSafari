using UnityEngine;

namespace GasSafari.UI
{
    // Colores de jugador (aro, HUD, ping), independientes del personaje (docs/03 Reglas de selección).
    [CreateAssetMenu(menuName = "Gas Safari/Player Palette", fileName = "PlayerPalette")]
    public class PlayerPalette : ScriptableObject
    {
        [Tooltip("Índice = número de jugador (J1 = 0)")]
        public Color[] colors;

        [Tooltip("Paleta para daltonismo (docs/07 §5): J1 #E69F00, J2 #56B4E9, J3 #F0E442, J4 #CC79A7")]
        public Color[] colorblindColors = new Color[0];

        // Opción de Accesibilidad "Paleta para daltonismo".
        public static bool UseColorblind { get; set; }

        public Color Get(int playerIndex)
        {
            var source = UseColorblind && colorblindColors != null && colorblindColors.Length > 0 ? colorblindColors : colors;
            return source[playerIndex % source.Length];
        }
    }
}
